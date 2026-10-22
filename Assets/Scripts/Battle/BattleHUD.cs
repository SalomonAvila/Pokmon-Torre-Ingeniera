using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Ficha de un combatiente en pantalla: nombre, nivel, barra de PS, PS actuales/máximos y estado.
    /// Se instancia dos veces (jugador y rival) y se refresca con <see cref="Show"/>.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Image hpBar;

        public void Show(BattlePokemon pokemon)
        {
            PokemonData species = PokemonDatabase.Get(pokemon.speciesId);
            nameText.text = species != null ? species.displayName : pokemon.speciesId;
            levelText.text = $"Nv {pokemon.level}";
            hpText.text = $"{pokemon.currentHp}/{pokemon.maxHp}";
            statusText.text = StatusLabel(pokemon.status);

            // La barra no usa sprite: se estira anclando el relleno desde la izquierda (con fillAmount
            // haría falta un sprite, y sin él Unity ignora el recorte).
            if (hpBar != null)
            {
                float ratio = pokemon.maxHp > 0 ? Mathf.Clamp01((float)pokemon.currentHp / pokemon.maxHp) : 0f;
                RectTransform fill = hpBar.rectTransform;
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(ratio, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }
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