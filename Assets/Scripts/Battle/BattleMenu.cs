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
        [SerializeField] GameObject scrollUp;
        [SerializeField] GameObject scrollDown;

        InputAction submitAction;
        InputAction navigateAction;
        InputAction cancelAction;

        /// <summary>
        /// Muestra las opciones y llama a <paramref name="onChosen"/> con el índice elegido.
        /// Devuelve −1 si el jugador cancela (X/Backspace/Esc en el mapa de acciones UI).
        /// <paramref name="detailWidth"/> reserva el ancho de la columna derecha (PP o cantidades).
        /// <paramref name="onHighlight"/> avisa del índice resaltado para mostrar su descripción.
        /// Si hay más opciones que filas, la lista se desplaza con flechas ↑/↓.
        /// </summary>
        public IEnumerator Choose(string[] options, string[] extraDetails, int columns, Action<int> onChosen,
            float detailWidth = 84f, Action<int> onHighlight = null)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);
            cancelAction ??= controls.FindAction("UI/Cancel", true);

            columns = Mathf.Max(1, columns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            FitCells(columns);

            int count = options.Length;
            int visibleRows = rows.Length;
            int firstVisible = 0;
            RefreshRows(options, extraDetails, detailWidth, firstVisible);
            UpdateScrollIndicators(firstVisible, visibleRows, count);

            int index = 0;
            bool held = false;
            SetVisible(true);
            Highlight(index, firstVisible);
            onHighlight?.Invoke(index);
            yield return null; // evita que la pulsación que abrió el menú lo confirme

            int rowCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            while (true)
            {
                if (submitAction.WasPressedThisFrame())
                {
                    SetVisible(false);
                    yield return null;
                    onChosen(index);
                    yield break;
                }

                if (cancelAction.WasPressedThisFrame())
                {
                    SetVisible(false);
                    yield return null;
                    onChosen(-1);
                    yield break;
                }

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

                        int next = Mathf.Clamp(row * columns + col, 0, count - 1);
                        if (next != index)
                        {
                            index = next;
                            if (ScrollTo(index, ref firstVisible, visibleRows))
                                RefreshRows(options, extraDetails, detailWidth, firstVisible);
                            UpdateScrollIndicators(firstVisible, visibleRows, count);
                            Highlight(index, firstVisible);
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

        /// <summary>Rellena las filas visibles desde <paramref name="firstVisible"/>.</summary>
        void RefreshRows(string[] options, string[] extraDetails, float detailWidth, int firstVisible)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int item = firstVisible + i;
                bool used = item < options.Length;
                rows[i].SetActive(used);
                if (!used)
                    continue;

                labels[i].text = options[item] != null ? options[item].ToUpperInvariant() : string.Empty;
                labels[i].overflowMode = TextOverflowModes.Ellipsis;
                details[i].text = extraDetails != null && item < extraDetails.Length ? extraDetails[item] : string.Empty;
                details[i].gameObject.SetActive(!string.IsNullOrEmpty(details[i].text));

                // Ancho fijo para que "x5" o "35/35" no toquen el borde del marco.
                var detailLayout = details[i].GetComponent<LayoutElement>();
                if (detailLayout == null)
                    detailLayout = details[i].gameObject.AddComponent<LayoutElement>();
                detailLayout.preferredWidth = detailWidth;
                detailLayout.flexibleWidth = 0f;
            }
        }

        /// <summary>Desplaza la ventana visible para que <paramref name="index"/> quede dentro.</summary>
        static bool ScrollTo(int index, ref int firstVisible, int visibleRows)
        {
            if (index < firstVisible)
            {
                firstVisible = index;
                return true;
            }

            if (index >= firstVisible + visibleRows)
            {
                firstVisible = index - visibleRows + 1;
                return true;
            }

            return false;
        }

        void UpdateScrollIndicators(int firstVisible, int visibleRows, int count)
        {
            if (scrollUp != null)
                scrollUp.SetActive(firstVisible > 0);
            if (scrollDown != null)
                scrollDown.SetActive(firstVisible + visibleRows < count);
        }

        /// <summary>
        /// Reparte el ancho del panel entre las columnas. El panel cambia de ancho con la pantalla, así
        /// que las celdas no pueden tener un tamaño fijo.
        /// </summary>
        void FitCells(int columns)
        {
            var rect = (RectTransform)transform;
            float width = rect.rect.width;
            if (width <= 1f)
                return; // aún sin layout: se usa el tamaño del builder

            float usable = width - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1);
            grid.cellSize = new Vector2(Mathf.Max(1f, usable / columns), grid.cellSize.y);
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

        void Highlight(int index, int firstVisible)
        {
            int slot = index - firstVisible;
            for (int i = 0; i < cursors.Length; i++)
                cursors[i].SetActive(i == slot);
        }
    }
}