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
        [SerializeField] InputActionAsset controls;
        [SerializeField] TMP_Text text;
        [SerializeField] RectTransform continueArrow;
        [SerializeField] float charactersPerSecond = 30f;
        [SerializeField, Tooltip("Pausa extra tras . , ! ?")] float punctuationPause = 0.12f;

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
            Clear();
        }

        public void Clear()
        {
            text.text = string.Empty;
            continueArrow.gameObject.SetActive(false);
            IsOpen = false;
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
            text.text = message;
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
