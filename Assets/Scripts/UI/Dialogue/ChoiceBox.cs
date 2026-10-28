using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Cuadro de opciones estilo Pokémon clásico (por ejemplo SÍ / NO), con el cursor ► a la izquierda.
    /// Se mueve con arriba/abajo y se elige con Z/E/Enter.
    /// </summary>
    public class ChoiceBox : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] TMP_Text[] optionLabels;
        [SerializeField] GameObject[] cursors;

        InputAction submitAction;
        InputAction navigateAction;

        /// <summary>True mientras el jugador está eligiendo una opción (el coroutine Choose está corriendo).</summary>
        public static bool IsChoosing { get; private set; }

        // El proyecto entra a Play sin recargar el dominio: el estático se reinicia a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsChoosing = false;

        /// <summary>
        /// Muestra las opciones y llama a onChosen con el índice elegido.
        /// El objeto empieza desactivado en la escena y solo se activa mientras se elige.
        /// </summary>
        public IEnumerator Choose(string[] options, Action<int> onChosen)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

            // El cuadro se adapta al número de opciones: tres (iniciales) necesita más alto y ancho.
            bool tall = options.Length > 2;
            var box = (RectTransform)transform;
            box.sizeDelta = new Vector2(tall ? 480f : 400f, tall ? 220f : 160f);
            if (TryGetComponent<VerticalLayoutGroup>(out VerticalLayoutGroup layout))
                layout.padding = new RectOffset(layout.padding.left, layout.padding.right,
                    tall ? 24 : 40, tall ? 24 : 40);

            for (int i = 0; i < optionLabels.Length; i++)
            {
                bool used = i < options.Length;
                optionLabels[i].transform.parent.gameObject.SetActive(used);
                if (used)
                    optionLabels[i].text = options[i];
            }

            int index = 0;
            bool axisHeld = false;
            gameObject.SetActive(true);
            IsChoosing = true;
            Highlight(index);
            yield return null;

            while (!submitAction.WasPressedThisFrame())
            {
                float y = navigateAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(y) > 0.5f)
                {
                    if (!axisHeld)
                    {
                        index = (index + (y < 0 ? 1 : -1) + options.Length) % options.Length;
                        Highlight(index);
                    }
                    axisHeld = true;
                }
                else
                {
                    axisHeld = false;
                }
                yield return null;
            }

            gameObject.SetActive(false);
            IsChoosing = false;
            yield return null;
            onChosen(index);
        }

        void Highlight(int index)
        {
            for (int i = 0; i < cursors.Length; i++)
                cursors[i].SetActive(i == index);
        }
    }
}
