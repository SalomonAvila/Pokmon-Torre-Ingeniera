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
    /// menú principal (Luchar/Mochila/Pokémon/Huir, 2 columnas) y para las listas de movimientos, objetos
    /// y equipo (1 columna). Cada fila tiene tres columnas: nombre, nivel/estado y un texto secundario a
    /// la derecha (PP, cantidad o PS). Las filas no elegibles se ven atenuadas y el cursor las salta; en
    /// la lista del equipo el Pokémon activo se marca y los debilitados se ven en gris con su PS en rojo.
    /// Si hay más opciones que filas, la lista se desplaza de una en una con las flechas ↑/↓.
    /// </summary>
    public class BattleMenu : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] CanvasGroup group;
        [SerializeField] GridLayoutGroup grid;
        [SerializeField] GameObject[] rows;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] TMP_Text[] levels;
        [SerializeField] TMP_Text[] details;
        [SerializeField] GameObject[] cursors;
        [SerializeField] GameObject scrollUp;
        [SerializeField] GameObject scrollDown;

        /// <summary>Fila del menú de cambio de Pokémon (nombre, nivel, PS y situación).</summary>
        public struct SwitchRowData
        {
            public string name;
            public string level;
            public string hp;
            public bool enabled;
            public bool isActive;
            public bool isFainted;
            public int currentHp;
            public int maxHp;
        }

        /// <summary>Vista común de una fila; la usan todos los menús.</summary>
        struct RowView
        {
            public string label;
            public string level;
            public string detail;
            public bool enabled;
            public bool active;
            public bool fainted;
            public bool colorHp;
            public float hpRatio;
        }

        /// <summary>Opacidad de una fila no elegible (el Pokémon activo o uno debilitado).</summary>
        const float DisabledAlpha = 0.65f;

        /// <summary>Ancho de la columna de PS del menú de cambio (NN/NN sin tocar el marco).</summary>
        const float SwitchDetailWidth = 84f;

        /// <summary>Color del texto normal (debe coincidir con el builder).</summary>
        static readonly Color TextBlack = new Color32(0x10, 0x10, 0x10, 0xFF);

        /// <summary>Color de la marca EN COMBATE del Pokémon que ya está en el campo.</summary>
        static readonly Color ActiveMark = new Color32(0x1F, 0x4E, 0x8C, 0xFF);

        InputAction submitAction;
        InputAction navigateAction;
        InputAction cancelAction;

        /// <summary>
        /// Muestra las opciones y llama a <paramref name="onChosen"/> con el índice elegido.
        /// Devuelve −1 si el jugador cancela (X/Backspace/Esc en el mapa de acciones UI).
        /// <paramref name="detailWidth"/> reserva el ancho de la columna derecha (PP o cantidades).
        /// <paramref name="onHighlight"/> avisa del índice resaltado para mostrar su descripción.
        /// <paramref name="enabled"/> deshabilita filas concretas: el cursor las salta y se ven atenuadas.
        /// Si hay más opciones que filas, la lista se desplaza con flechas ↑/↓.
        /// </summary>
        public IEnumerator Choose(string[] options, string[] extraDetails, int columns, Action<int> onChosen,
            float detailWidth = 84f, Action<int> onHighlight = null, bool[] enabled = null)
        {
            int count = options.Length;
            var data = new RowView[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = new RowView
                {
                    label = options[i],
                    detail = extraDetails != null && i < extraDetails.Length ? extraDetails[i] : string.Empty,
                    enabled = IsEnabled(enabled, i),
                };
            }

            yield return ChooseRows(data, columns, detailWidth, onChosen, onHighlight);
        }

        /// <summary>
        /// Lista el equipo para cambiar de Pokémon. Además de lo común, colorea los PS según el porcentaje
        /// y marca la fila del activo (EN COMBATE) y las de los debilitados (DEB).
        /// </summary>
        public IEnumerator ChooseSwitch(SwitchRowData[] switchRows, Action<int> onChosen,
            Action<int> onHighlight = null)
        {
            int count = switchRows.Length;
            var data = new RowView[count];
            for (int i = 0; i < count; i++)
            {
                SwitchRowData row = switchRows[i];
                float ratio = row.maxHp > 0 ? (float)row.currentHp / row.maxHp : 0f;
                data[i] = new RowView
                {
                    label = row.name,
                    level = row.isFainted ? "DEB" : row.isActive ? "EN COMBATE" : row.level,
                    detail = row.hp,
                    enabled = row.enabled,
                    active = row.isActive,
                    fainted = row.isFainted,
                    colorHp = true,
                    hpRatio = ratio,
                };
            }

            yield return ChooseRows(data, 1, SwitchDetailWidth, onChosen, onHighlight);
        }

        /// <summary>Bucle común: rejilla, navegación con salto de filas, desplazamiento y confirmación.</summary>
        IEnumerator ChooseRows(RowView[] data, int columns, float detailWidth, Action<int> onChosen,
            Action<int> onHighlight)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);
            cancelAction ??= controls.FindAction("UI/Cancel", true);

            int count = data.Length;
            columns = Mathf.Max(1, columns);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            FitCells(columns);

            int index = FirstEnabled(data, 0);
            if (index < 0)
            {
                // No hay ninguna opción elegible; el llamador decide qué mensaje mostrar.
                SetVisible(false);
                onChosen(-1);
                yield break;
            }

            int visibleRows = rows.Length;
            int firstVisible = 0;
            ScrollTo(index, ref firstVisible, visibleRows);
            RefreshRows(data, detailWidth, firstVisible);
            UpdateScrollIndicators(firstVisible, visibleRows, count);

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
                        int next = NextEnabled(data, index, nav, columns, rowCount);
                        if (next != index)
                        {
                            index = next;
                            if (ScrollTo(index, ref firstVisible, visibleRows))
                                RefreshRows(data, detailWidth, firstVisible);
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
        void RefreshRows(RowView[] data, float detailWidth, int firstVisible)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int item = firstVisible + i;
                bool used = item < data.Length;
                rows[i].SetActive(used);
                if (!used)
                    continue;

                RowView row = data[item];

                // El activo se ve a plena opacidad porque su marca ya avisa de que no se puede elegir;
                // los debilitados y el resto de filas no elegibles se atenúan a gris medio.
                float alpha = row.active || row.enabled ? 1f : DisabledAlpha;
                labels[i].alpha = alpha;
                labels[i].text = row.label != null ? row.label.ToUpperInvariant() : string.Empty;
                labels[i].overflowMode = TextOverflowModes.Ellipsis;

                TMP_Text level = LevelAt(i);
                if (level != null)
                {
                    bool hasLevel = !string.IsNullOrEmpty(row.level);
                    level.gameObject.SetActive(hasLevel);
                    if (hasLevel)
                    {
                        level.alpha = 1f;
                        level.text = row.level.ToUpperInvariant();
                        level.color = row.fainted ? BattleHUD.HpRed
                            : row.active ? ActiveMark
                            : TextBlack;
                    }
                }

                // La columna de PS de la lista del equipo usa el mismo color que la barra de la ficha.
                details[i].alpha = row.colorHp ? 1f : alpha;
                details[i].text = row.detail ?? string.Empty;
                details[i].color = row.colorHp ? BattleHUD.HpColor(row.hpRatio) : TextBlack;
                details[i].gameObject.SetActive(!string.IsNullOrEmpty(details[i].text));

                // Ancho fijo para que "x5" o "35/35" no toquen el borde del marco.
                var detailLayout = details[i].GetComponent<LayoutElement>();
                if (detailLayout == null)
                    detailLayout = details[i].gameObject.AddComponent<LayoutElement>();
                detailLayout.preferredWidth = detailWidth;
                detailLayout.flexibleWidth = 0f;
            }
        }

        TMP_Text LevelAt(int index) =>
            levels != null && index >= 0 && index < levels.Length ? levels[index] : null;

        static int FirstEnabled(RowView[] data, int start)
        {
            for (int i = start; i < data.Length; i++)
                if (data[i].enabled)
                    return i;
            return -1;
        }

        /// <summary>
        /// Siguiente índice elegible en la dirección pulsada. Mantiene el recorrido en rejilla del menú
        /// (con salto de fila) y salta las filas deshabilitadas, sin quedarse atascado si no hay ninguna.
        /// </summary>
        static int NextEnabled(RowView[] data, int current, Vector2 nav, int columns, int rowCount)
        {
            int count = data.Length;
            int candidate = current;
            for (int step = 0; step < count; step++)
            {
                int row = candidate / columns;
                int col = candidate % columns;
                if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                    col = (col + (nav.x > 0 ? 1 : -1) + columns) % columns;
                else
                    row = (row + (nav.y > 0 ? -1 : 1) + rowCount) % rowCount;

                int next = Mathf.Clamp(row * columns + col, 0, count - 1);
                if (data[next].enabled)
                    return next;
                candidate = next;
            }

            return current;
        }

        static bool IsEnabled(bool[] enabled, int index) =>
            enabled == null || (index >= 0 && index < enabled.Length && enabled[index]);

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
