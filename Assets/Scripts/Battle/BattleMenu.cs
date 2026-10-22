using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Menú de combate en rejilla (corre el cursor con las flechas, confirma con Z/E/Enter). Sirve para el
    /// menú principal (Luchar/Mochila/Pokémon/Huir, 2 columnas) y para la lista de movimientos (1 columna).
    /// Cada fila tiene su etiqueta y un texto secundario a la derecha (por ejemplo, los PP del movimiento).
    /// </summary>
    public class BattleMenu : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] CanvasGroup group;
        [SerializeField] GridLayoutGroup grid;
        [SerializeField] GameObject[] rows;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] TMP_Text[] details;
        [SerializeField] GameObject[] cursors;

        InputAction submitAction;
        InputAction navigateAction;

        /// <summary>Muestra las opciones y llama a <paramref name="onChosen"/> con el índice elegido.</summary>
        public IEnumerator Choose(string[] options, string[] extraDetails, int columns, Action<int> onChosen)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

            columns = Mathf.Max(1, columns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            int count = Mathf.Min(options.Length, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < count;
                rows[i].SetActive(used);
                if (!used)
                    continue;

                labels[i].text = options[i];
                details[i].text = extraDetails != null && i < extraDetails.Length ? extraDetails[i] : string.Empty;
                details[i].gameObject.SetActive(!string.IsNullOrEmpty(details[i].text));
            }

            int index = 0;
            bool held = false;
            SetVisible(true);
            Highlight(index);
            yield return null; // evita que la pulsación que abrió el menú lo confirme

            int rowCount = Mathf.CeilToInt(count / (float)columns);
            while (!submitAction.WasPressedThisFrame())
            {
                Vector2 nav = navigateAction.ReadValue<Vector2>();
                if (nav.sqrMagnitude > 0.25f)
                {
                    if (!held)
                    {
                        int row = index / columns;
                        int col = index % columns;
                        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                            col = (col + (nav.x > 0 ? 1 : -1) + columns) % columns;
                        else
                            row = (row + (nav.y > 0 ? -1 : 1) + rowCount) % rowCount;

                        index = Mathf.Clamp(row * columns + col, 0, count - 1);
                        Highlight(index);
                    }
                    held = true;
                }
                else
                {
                    held = false;
                }

                yield return null;
            }

            SetVisible(false);
            yield return null;
            onChosen(index);
        }

        void SetVisible(bool visible)
        {
            // Se oculta con CanvasGroup (no con SetActive) para que desactivar el objeto no corte
            // esta corrutina antes de devolver la opción elegida.
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }

        void Highlight(int index)
        {
            for (int i = 0; i < cursors.Length; i++)
                cursors[i].SetActive(i == index);
        }
    }
}