using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Ficha de un combatiente en pantalla: nombre, nivel, barra de PS, PS actuales/máximos y estado.
    /// La ficha del jugador añade la barra de EXP (propuesta). Se instancia dos veces (jugador y rival)
    /// y se refresca con <see cref="Show"/>.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        /// <summary>Por debajo de este porcentaje la barra de PS se pinta en rojo.</summary>
        public const float HpRedThreshold = 0.2f;

        /// <summary>Hasta este porcentaje (incluido) la barra de PS se pinta en amarillo.</summary>
        public const float HpYellowThreshold = 0.5f;

        public static readonly Color HpGreen = new Color32(0x50, 0xC8, 0x50, 0xFF);
        public static readonly Color HpYellow = new Color32(0xF8, 0xD0, 0x30, 0xFF);
        public static readonly Color HpRed = new Color32(0xE0, 0x40, 0x40, 0xFF);

        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Image hpBar;
        [SerializeField] Image expBar;
        [SerializeField] bool showHpNumbers = true;
        [SerializeField] bool showExpBar;

        public void Show(BattlePokemon pokemon, TeamMember member = null)
        {
            PokemonData species = PokemonDatabase.Get(pokemon.speciesId);
            nameText.text = species != null
                ? species.displayName.ToUpperInvariant()
                : pokemon.speciesId.ToUpperInvariant();
            levelText.text = $"Nv {pokemon.level}";

            // El documento solo muestra los PS numéricos del jugador; la ficha rival deja la barra sola.
            hpText.gameObject.SetActive(showHpNumbers);
            hpText.text = showHpNumbers ? $"{pokemon.currentHp}/{pokemon.maxHp}" : string.Empty;
            statusText.text = StatusLabel(pokemon.status);

            // La barra no usa sprite: se estira anclando el relleno desde la izquierda (con fillAmount
            // haría falta un sprite, y sin él Unity ignora el recorte).
            if (hpBar != null)
            {
                float ratio = pokemon.maxHp > 0 ? Mathf.Clamp01((float)pokemon.currentHp / pokemon.maxHp) : 0f;
                SetBar(hpBar, ratio);

                // Se recalcula en cada Show (inicio, daño, curación y cambio), así el color nunca
                // se queda pegado al estado anterior. 8/20 = 40 % debe verse amarillo.
                hpBar.color = HpColor(ratio);
            }

            // (propuesta) La Fig. 30 del documento solo muestra nombre, nivel y PS; la barra de EXP
            // se añade para que el jugador vea el avance hacia el siguiente nivel.
            if (member != null && showExpBar)
                ShowExp(member.exp, pokemon.level);
        }

        /// <summary>Pinta la barra de EXP y actualiza el nivel mostrado (solo en la ficha del jugador).</summary>
        public void ShowExp(int experience, int level)
        {
            levelText.text = $"Nv {level}";
            if (!showExpBar || expBar == null)
                return;

            SetBar(expBar, ExpRatio(experience, level));
        }

        /// <summary>Fracción de la barra de EXP entre el nivel actual y el siguiente (0 a 1).</summary>
        public static float ExpRatio(int experience, int level)
        {
            int floor = PokemonStats.ExperienceForLevel(level);
            int next = level >= PokemonStats.MaxLevel
                ? floor
                : PokemonStats.ExperienceForLevel(level + 1);

            return next > floor
                ? Mathf.Clamp01((float)(experience - floor) / (next - floor))
                : 1f;
        }

        static void SetBar(Image bar, float ratio)
        {
            RectTransform fill = bar.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        /// <summary>Color de la barra según el porcentaje de PS restante (0 a 1).</summary>
        public static Color HpColor(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio < HpRedThreshold)
                return HpRed;
            return ratio <= HpYellowThreshold ? HpYellow : HpGreen;
        }

        public static string StatusLabel(StatusCondition status) => status switch
        {
            StatusCondition.Sleep => "DORMIDO",
            StatusCondition.Poison => "ENVENENADO",
            StatusCondition.Burn => "QUEMADO",
            StatusCondition.Paralysis => "PARALIZADO",
            StatusCondition.Freeze => "CONGELADO",
            _ => string.Empty,
        };
    }
}