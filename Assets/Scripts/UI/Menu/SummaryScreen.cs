using System;
using System.Collections;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Resumen de un Pokémon del equipo (maquetas 04, 05 y 06), con las tres páginas de FireRed: INFO,
    /// ESTADÍSTICAS y MOVIMIENTOS. ◀ ▶ cambian de página, ▲ ▼ pasan al Pokémon anterior o siguiente y B
    /// vuelve al Equipo. En MOVIMIENTOS, A (Z/Enter) entra a ver el detalle de cada movimiento.
    ///
    /// Las tres páginas comparten la misma tabla de seis filas (etiqueta, valor y detalle a la derecha) y el
    /// recuadro inferior (título y texto), así el builder arma una sola vez la pantalla.
    /// Sin género, naturaleza, habilidad ni objeto equipado: no existen en el documento de diseño.
    /// </summary>
    public class SummaryScreen : MonoBehaviour
    {
        enum Page { Info, Stats, Moves }

        static readonly string[] PageTitles = { "INFO POKÉMON", "ESTADÍSTICAS", "MOVIMIENTOS" };

        [SerializeField] InputActionAsset controls;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Image[] tabBackgrounds;
        [SerializeField] Color tabActiveColor = new Color32(0xF2, 0xC2, 0x30, 0xFF);
        [SerializeField] Color tabInactiveColor = new Color32(0x8D, 0x99, 0xA6, 0xFF);

        [Header("Pokémon")]
        [SerializeField] Image sprite;
        [SerializeField] TMP_Text noSpriteText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;

        [Header("Tabla")]
        [SerializeField] GameObject[] rows;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] TMP_Text[] values;
        [SerializeField] TMP_Text[] details;
        [SerializeField] GameObject[] cursors;

        [Header("Recuadro inferior")]
        [SerializeField] TMP_Text footerTitle;
        [SerializeField] TMP_Text footerText;
        [SerializeField] GameObject expRow;
        [SerializeField] Image expBar;

        InputAction submitAction;
        InputAction cancelAction;
        InputAction navigateAction;

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Abre el Resumen en <paramref name="index"/> y devuelve el último Pokémon mostrado.</summary>
        public IEnumerator Run(int index, Action<int> onClosed)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            cancelAction ??= controls.FindAction("UI/Cancel", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

            int count = GameState.Save.team.Count;
            index = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
            var page = Page.Info;
            gameObject.SetActive(true);
            Render(index, page, -1);
            yield return null;

            bool held = true;
            while (count > 0)
            {
                if (cancelAction.WasPressedThisFrame())
                    break;

                if (submitAction.WasPressedThisFrame() && page == Page.Moves && MoveCount(index) > 0)
                {
                    yield return null;
                    yield return BrowseMoves(index);
                    Render(index, page, -1);
                    held = true;
                    continue;
                }

                Vector2 nav = navigateAction.ReadValue<Vector2>();
                if (nav.sqrMagnitude > 0.25f)
                {
                    if (!held)
                    {
                        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                            page = (Page)Mathf.Clamp((int)page + (nav.x > 0 ? 1 : -1), 0, PageTitles.Length - 1);
                        else
                            index = (index + (nav.y < 0 ? 1 : -1) + count) % count;
                        Render(index, page, -1);
                    }
                    held = true;
                }
                else
                {
                    held = false;
                }

                yield return null;
            }

            yield return null;
            Hide();
            onClosed(index);
        }

        /// <summary>Detalle de movimientos: ▲ ▼ recorren los cuatro y B vuelve a la página.</summary>
        IEnumerator BrowseMoves(int index)
        {
            int moves = MoveCount(index);
            int selected = 0;
            Render(index, Page.Moves, selected);

            bool held = true;
            while (!cancelAction.WasPressedThisFrame())
            {
                float y = navigateAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(y) > 0.5f)
                {
                    if (!held)
                    {
                        selected = (selected + (y < 0 ? 1 : -1) + moves) % moves;
                        Render(index, Page.Moves, selected);
                    }
                    held = true;
                }
                else
                {
                    held = false;
                }
                yield return null;
            }
            yield return null;
        }

        static int MoveCount(int index) => GameState.Save.team[index].moves.Count;

        void Render(int index, Page page, int selectedMove)
        {
            TeamMember member = GameState.Save.team[index];
            PokemonData species = PokemonDatabase.Get(member.speciesId);
            string name = (species != null ? species.displayName : member.speciesId).ToUpperInvariant();

            titleText.text = PageTitles[(int)page];
            for (int i = 0; i < tabBackgrounds.Length; i++)
                tabBackgrounds[i].color = i == (int)page ? tabActiveColor : tabInactiveColor;

            nameText.text = name;
            levelText.text = $"Nv{member.level}";
            Sprite front = species != null ? species.frontSprite : null;
            sprite.sprite = front;
            sprite.enabled = front != null;
            if (noSpriteText != null)
                noSpriteText.gameObject.SetActive(front == null);

            ClearTable();
            expRow.SetActive(false);
            switch (page)
            {
                case Page.Info:
                    RenderInfo(member, species, name);
                    break;
                case Page.Stats:
                    RenderStats(member, species);
                    break;
                default:
                    RenderMoves(member, selectedMove);
                    break;
            }
        }

        void RenderInfo(TeamMember member, PokemonData species, string name)
        {
            SetRow(0, "N.º", species != null && species.dexNumber > 0 ? species.dexNumber.ToString("000") : "???");
            SetRow(1, "NOMBRE", name);
            SetRow(2, "TIPO", species == null ? "???"
                : species.hasSecondaryType
                    ? $"{PokemonTypeColors.BadgeMarkup(species.primaryType)} {PokemonTypeColors.BadgeMarkup(species.secondaryType)}"
                    : PokemonTypeColors.BadgeMarkup(species.primaryType));
            SetRow(3, "ENTRENADOR", GameState.Save.playerName.ToUpperInvariant());
            SetRow(4, "NIVEL", member.level.ToString());
            SetRow(5, "ESTADO", TeamMembers.IsFainted(member) ? "DEBILITADO" : "SANO");

            footerTitle.text = "DESCRIPCIÓN";
            footerText.text = species != null ? species.description : string.Empty;
        }

        void RenderStats(TeamMember member, PokemonData species)
        {
            int current = TeamMembers.CurrentHp(member);
            float ratio = member.maxHp > 0 ? (float)current / member.maxHp : 0f;
            string hpColor = ColorUtility.ToHtmlStringRGB(HpBar.ColorFor(ratio));
            SetRow(0, "PS", $"<color=#{hpColor}>{current}</color>/{member.maxHp}");

            if (species != null)
            {
                SetRow(1, "ATAQUE", PokemonStats.Stat(species.baseAttack, member.level).ToString());
                SetRow(2, "DEFENSA", PokemonStats.Stat(species.baseDefense, member.level).ToString());
                SetRow(3, "AT. ESP.", PokemonStats.Stat(species.baseSpAttack, member.level).ToString());
                SetRow(4, "DEF. ESP.", PokemonStats.Stat(species.baseSpDefense, member.level).ToString());
                SetRow(5, "VELOCIDAD", PokemonStats.Stat(species.baseSpeed, member.level).ToString());
            }

            int toNext = member.level >= PokemonStats.MaxLevel
                ? 0
                : Mathf.Max(0, PokemonStats.ExperienceForLevel(member.level + 1) - member.exp);
            footerTitle.text = "EXPERIENCIA";
            footerText.text = $"PUNTOS EXP.  {member.exp}\nSIG. NV.     {toNext}";
            expRow.SetActive(true);
            HpBar.SetFill(expBar, HpBar.ExpRatio(member.exp, member.level));
        }

        void RenderMoves(TeamMember member, int selected)
        {
            for (int i = 0; i < TeamMembers.MaxMoves; i++)
            {
                if (i >= member.moves.Count)
                {
                    SetRow(i, string.Empty, "-----", "PP --/--");
                    continue;
                }

                MoveSlot slot = member.moves[i];
                MoveData move = MoveDatabase.Get(slot.moveId);
                SetRow(i, move != null ? PokemonTypeColors.BadgeMarkup(move.type) : string.Empty,
                    (move != null ? move.displayName : slot.moveId).ToUpperInvariant(),
                    $"PP {slot.pp}/{slot.maxPp}");
                cursors[i].SetActive(i == selected);
            }

            if (selected < 0 || selected >= member.moves.Count)
            {
                footerTitle.text = "MOVIMIENTOS";
                footerText.text = "Z: VER DETALLE   X: VOLVER";
                return;
            }

            MoveData data = MoveDatabase.Get(member.moves[selected].moveId);
            footerTitle.text = data != null ? data.displayName.ToUpperInvariant() : member.moves[selected].moveId;
            if (data == null)
            {
                footerText.text = string.Empty;
                return;
            }

            string category = data.category switch
            {
                // En ASCII, como en el combate: la fuente pixel dibuja pequeña la Í mayúscula.
                MoveCategory.Physical => "FISICO",
                MoveCategory.Special => "ESPECIAL",
                _ => "ESTADO",
            };
            string power = data.power > 0 ? data.power.ToString() : "---";
            string accuracy = data.accuracy > 0 ? data.accuracy.ToString() : "---";
            footerText.text = $"POTENCIA {power}   PRECISIÓN {accuracy}   {category}\n{data.description}";
        }

        void ClearTable()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].SetActive(false);
                cursors[i].SetActive(false);
            }
        }

        void SetRow(int i, string label, string value, string detail = "")
        {
            rows[i].SetActive(true);
            labels[i].text = label;
            values[i].text = value;
            details[i].text = detail;
        }
    }
}
