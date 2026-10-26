using TMPro;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
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
        // Umbrales y colores compartidos con los menús (ver HpBar).
        public const float HpRedThreshold = HpBar.RedThreshold;
        public const float HpYellowThreshold = HpBar.YellowThreshold;
        public static readonly Color HpGreen = HpBar.Green;
        public static readonly Color HpYellow = HpBar.Yellow;
        public static readonly Color HpRed = HpBar.Red;

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
        public static float ExpRatio(int experience, int level) => HpBar.ExpRatio(experience, level);

        static void SetBar(Image bar, float ratio) => HpBar.SetFill(bar, ratio);

        /// <summary>Color de la barra según el porcentaje de PS restante (0 a 1).</summary>
        public static Color HpColor(float ratio) => HpBar.ColorFor(ratio);

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