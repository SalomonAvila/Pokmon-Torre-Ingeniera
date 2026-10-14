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
        }

        /// <summary>Escribe el mensaje y espera a que el jugador presione aceptar.</summary>
        public IEnumerator Say(string message)
        {
            yield return Type(message);
            yield return WaitForAdvance();
        }

        /// <summary>Escribe el mensaje sin esperar (por ejemplo, antes de mostrar opciones).</summary>
        public IEnumerator Type(string message)
        {
            continueArrow.gameObject.SetActive(false);
            text.text = message;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;

            yield return null; // evita que la misma pulsación que abrió el cuadro lo salte
            float delay = 1f / charactersPerSecond;
            for (int shown = 1; shown <= total; shown++)
            {
                text.maxVisibleCharacters = shown;
                char current = text.textInfo.characterInfo[shown - 1].character;
                float wait = delay + (".,!?".IndexOf(current) >= 0 ? punctuationPause : 0f);

                for (float t = 0f; t < wait; t += Time.unscaledDeltaTime)
                {
                    if (submitAction.WasPressedThisFrame())
                    {
                        text.maxVisibleCharacters = total;
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
