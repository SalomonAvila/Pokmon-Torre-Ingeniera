using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

        /// <summary>
        /// Muestra las opciones y llama a onChosen con el índice elegido.
        /// El objeto empieza desactivado en la escena y solo se activa mientras se elige.
        /// </summary>
        public IEnumerator Choose(string[] options, Action<int> onChosen)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

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
