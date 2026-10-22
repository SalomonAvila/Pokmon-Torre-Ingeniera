using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Cuadro de texto estilo Pokémon clásico: el texto aparece letra por letra y,
    /// al terminar, la flecha ▼ indica que se puede continuar con Z/E/Enter.
    /// Presionar mientras se escribe muestra el texto completo de una vez.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        /// <summary>Mitad de la franja inferior que ocupa el cuadro cuando hay un menú abierto.</summary>
        public const float SplitRatio = 0.55f;

        /// <summary>Margen con el borde de la pantalla (debe coincidir con el builder).</summary>
        const float EdgeMargin = 8f;

        /// <summary>Separación entre los dos marcos cuando comparten la franja inferior.</summary>
        const float SeamGap = 6f;

        /// <summary>Alto de la franja inferior (debe coincidir con el builder).</summary>
        const float PanelHeight = 176f;

        /// <summary>Lado de la franja inferior en el que se dibuja el cuadro.</summary>
        public enum PanelSide
        {
            Full,
            Left,
            Right,
        }

        [SerializeField] InputActionAsset controls;
        [SerializeField] TMP_Text text;
        [SerializeField] RectTransform continueArrow;
        [SerializeField] float charactersPerSecond = 30f;
        [SerializeField, Tooltip("Pausa extra tras . , ! ?")] float punctuationPause = 0.12f;
        [SerializeField, Tooltip("Interlineado extra (%) de la descripción de un objeto o movimiento.")]
        float descriptionLineSpacing = 12f;

        InputAction submitAction;
        Vector2 arrowRestPosition;

        /// <summary>True mientras se está escribiendo o esperando a que el jugador avance (Say/Type/WaitForAdvance).</summary>
        public static bool IsOpen { get; private set; }

        // El proyecto entra a Play sin recargar el dominio: el estático se reinicia a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        void Awake()
        {
            submitAction = controls.FindAction("UI/Submit", true);
            arrowRestPosition = continueArrow.anchoredPosition;
            text.textWrappingMode = TextWrappingModes.Normal; // las frases largas pasan a dos líneas
            text.lineSpacing = 0f;
            Clear();
        }

        public void Clear()
        {
            text.text = string.Empty;
            text.lineSpacing = 0f;
            continueArrow.gameObject.SetActive(false);
            IsOpen = false;
        }

        /// <summary>Texto que hay ahora mismo en el cuadro (para restaurarlo tras una descripción).</summary>
        public string CurrentText => text != null ? text.text : string.Empty;

        /// <summary>
        /// Muestra un texto fijo, sin máquina de escribir ni espera. Se usa para la descripción del
        /// objeto o movimiento resaltado en una lista. Usa el mismo margen interior que los mensajes;
        /// <paramref name="description"/> activa el interlineado holgado de las descripciones.
        /// </summary>
        public void ShowText(string message, bool description = true) =>
            Show(message, description, true);

        /// <summary>
        /// Igual que <see cref="ShowText"/>, pero respeta las etiquetas de texto enriquecido tal cual
        /// (el llamador ya escribió en mayúsculas las partes visibles). Se usa para la etiqueta de tipo
        /// del movimiento, que necesita &lt;mark&gt; y &lt;color&gt;.
        /// </summary>
        public void ShowMarkup(string message, bool description = true) =>
            Show(message, description, false);

        void Show(string message, bool description, bool uppercase)
        {
            IsOpen = false;
            continueArrow.gameObject.SetActive(false);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.lineSpacing = description ? descriptionLineSpacing : 0f;
            text.text = string.IsNullOrEmpty(message)
                ? string.Empty
                : uppercase ? Uppercase(message) : message;
            text.pageToDisplay = 1;
            text.maxVisibleCharacters = 99999;
            text.ForceMeshUpdate();
        }

        /// <summary>
        /// Coloca el cuadro en la franja inferior: ancho completo cuando no hay menú, o solo una de las
        /// dos mitades cuando el menú de combate ocupa la otra. Así el menú no tapa el mensaje.
        /// </summary>
        public void SetPanel(PanelSide side)
        {
            float min = side == PanelSide.Right ? SplitRatio : 0f;
            float max = side == PanelSide.Left ? SplitRatio : 1f;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(min, 0f);
            rect.anchorMax = new Vector2(max, 0f);

            // En la unión se deja una rendija para que no se dupliquen las esquinas del marco.
            float left = min > 0f ? SeamGap : EdgeMargin;
            float right = max < 1f ? SeamGap : EdgeMargin;
            rect.offsetMin = new Vector2(left, EdgeMargin);
            rect.offsetMax = new Vector2(-right, EdgeMargin + PanelHeight);

            if (text != null && !string.IsNullOrEmpty(text.text))
                text.ForceMeshUpdate(); // reajusta el texto al nuevo ancho en el mismo fotograma
        }

        /// <summary>Escribe el mensaje y espera a que el jugador presione aceptar.</summary>
        public IEnumerator Say(string message)
        {
            IsOpen = true;
            yield return Type(message);
            yield return WaitForAdvance();
            IsOpen = false;
        }

        /// <summary>
        /// Escribe el mensaje sin esperar al final (por ejemplo, antes de mostrar opciones).
        /// Si no cabe en el cuadro se divide en páginas, y entre página y página se espera a que el jugador acepte.
        /// </summary>
        public IEnumerator Type(string message)
        {
            IsOpen = true;
            continueArrow.gameObject.SetActive(false);
            text.overflowMode = TextOverflowModes.Page;
            text.lineSpacing = 0f;
            text.text = Uppercase(message);
            text.pageToDisplay = 1;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();

            yield return null; // evita que la misma pulsación que abrió el cuadro lo salte

            TMP_TextInfo info = text.textInfo;
            int pageCount = Mathf.Max(1, info.pageCount);
            for (int page = 0; page < pageCount; page++)
            {
                if (page > 0)
                    yield return WaitForAdvance();

                text.pageToDisplay = page + 1;
                int first = info.pageInfo[page].firstCharacterIndex;
                int last = pageCount == 1 ? info.characterCount - 1 : info.pageInfo[page].lastCharacterIndex;
                yield return TypePage(first, last);
            }
        }

        /// <summary>La interfaz va en mayúsculas; se convierte antes de buscar el glifo en el atlas.</summary>
        static string Uppercase(string message) =>
            string.IsNullOrEmpty(message) ? string.Empty : message.ToUpperInvariant();

        IEnumerator TypePage(int first, int last)
        {
            float delay = 1f / charactersPerSecond;
            for (int index = first; index <= last; index++)
            {
                text.maxVisibleCharacters = index + 1;
                char current = text.textInfo.characterInfo[index].character;
                float wait = delay + (".,!?".IndexOf(current) >= 0 ? punctuationPause : 0f);

                for (float t = 0f; t < wait; t += Time.unscaledDeltaTime)
                {
                    if (submitAction.WasPressedThisFrame())
                    {
                        text.maxVisibleCharacters = last + 1;
                        yield return null;
                        yield break;
                    }
                    yield return null;
                }
            }
        }

        IEnumerator WaitForAdvance()
        {
            continueArrow.gameObject.SetActive(true);
            while (!submitAction.WasPressedThisFrame())
            {
                // La flecha sube y baja un píxel, como en los juegos originales.
                bool down = Mathf.Repeat(Time.unscaledTime, 0.6f) > 0.3f;
                continueArrow.anchoredPosition = arrowRestPosition + (down ? new Vector2(0, -4) : Vector2.zero);
                yield return null;
            }
            continueArrow.gameObject.SetActive(false);
            yield return null;
        }
    }
}
