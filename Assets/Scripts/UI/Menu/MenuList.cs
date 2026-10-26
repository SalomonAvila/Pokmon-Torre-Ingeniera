using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Lista vertical de opciones estilo FireRed con el cursor ► a la izquierda: menú de pausa, submenús
    /// (DATOS / CAMBIAR / SALIR) y la lista de la Mochila. A diferencia de <see cref="ChoiceBox"/>, se puede
    /// cancelar (X/Backspace/Esc), tiene una columna de detalle a la derecha (cantidades) y se desplaza si
    /// hay más opciones que filas. Opcionalmente avisa de ◀ ▶ (cambiar de bolsillo en la Mochila).
    /// </summary>
    public class MenuList : MonoBehaviour
    {
        /// <summary>Resultado de <see cref="Choose"/> cuando el jugador cancela.</summary>
        public const int Cancelled = -1;

        /// <summary>Resultado cuando se pulsa ◀ con <c>horizontal</c> activado.</summary>
        public const int PressedLeft = -2;

        /// <summary>Resultado cuando se pulsa ▶ con <c>horizontal</c> activado.</summary>
        public const int PressedRight = -3;

        [SerializeField] InputActionAsset controls;
        [SerializeField] GameObject[] rows;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] TMP_Text[] details;
        [SerializeField] GameObject[] cursors;
        [SerializeField] GameObject scrollUp;
        [SerializeField] GameObject scrollDown;

        InputAction submitAction;
        InputAction cancelAction;
        InputAction navigateAction;

        /// <summary>Filas visibles a la vez.</summary>
        public int VisibleRows => rows.Length;

        /// <summary>
        /// Muestra las opciones y llama a <paramref name="onChosen"/> con el índice elegido,
        /// <see cref="Cancelled"/>, o <see cref="PressedLeft"/>/<see cref="PressedRight"/> si
        /// <paramref name="horizontal"/> está activado. La lista queda visible al terminar: quien la abre
        /// decide si la cierra (así el submenú sigue en pantalla mientras sale el mensaje siguiente).
        /// </summary>
        public IEnumerator Choose(string[] options, string[] extraDetails, Action<int> onChosen,
            Action<int> onHighlight = null, int start = 0, bool horizontal = false)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            cancelAction ??= controls.FindAction("UI/Cancel", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

            int count = options.Length;
            if (count == 0)
            {
                onChosen(Cancelled);
                yield break;
            }

            int index = Mathf.Clamp(start, 0, count - 1);
            int first = 0;
            ScrollTo(index, ref first);
            Refresh(options, extraDetails, first);
            Highlight(index, first);
            gameObject.SetActive(true);
            onHighlight?.Invoke(index);
            yield return null; // la pulsación que abrió la lista no la confirma

            bool held = true; // si se llegó con una flecha pulsada, hay que soltarla antes de moverse
            while (true)
            {
                if (submitAction.WasPressedThisFrame())
                {
                    yield return null;
                    onChosen(index);
                    yield break;
                }

                if (cancelAction.WasPressedThisFrame())
                {
                    yield return null;
                    onChosen(Cancelled);
                    yield break;
                }

                Vector2 nav = navigateAction.ReadValue<Vector2>();
                if (nav.sqrMagnitude > 0.25f)
                {
                    if (!held)
                    {
                        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                        {
                            if (horizontal)
                            {
                                yield return null;
                                onChosen(nav.x < 0 ? PressedLeft : PressedRight);
                                yield break;
                            }
                        }
                        else
                        {
                            index = (index + (nav.y < 0 ? 1 : -1) + count) % count;
                            if (ScrollTo(index, ref first))
                                Refresh(options, extraDetails, first);
                            Highlight(index, first);
                            onHighlight?.Invoke(index);
                        }
                    }
                    held = true;
                }
                else
                {
                    held = false;
                }

                yield return null;
            }
        }

        public void Hide() => gameObject.SetActive(false);

        void Refresh(string[] options, string[] extraDetails, int first)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int item = first + i;
                bool used = item < options.Length;
                rows[i].SetActive(used);
                if (!used)
                    continue;

                labels[i].text = options[item] ?? string.Empty;
                if (details != null && i < details.Length && details[i] != null)
                {
                    string detail = extraDetails != null && item < extraDetails.Length ? extraDetails[item] : null;
                    details[i].text = detail ?? string.Empty;
                    details[i].gameObject.SetActive(!string.IsNullOrEmpty(detail));
                }
            }

            if (scrollUp != null)
                scrollUp.SetActive(first > 0);
            if (scrollDown != null)
                scrollDown.SetActive(first + rows.Length < options.Length);
        }

        bool ScrollTo(int index, ref int first)
        {
            if (index < first)
            {
                first = index;
                return true;
            }
            if (index >= first + rows.Length)
            {
                first = index - rows.Length + 1;
                return true;
            }
            return false;
        }

        void Highlight(int index, int first)
        {
            for (int i = 0; i < cursors.Length; i++)
                cursors[i].SetActive(i == index - first);
        }
    }
}
