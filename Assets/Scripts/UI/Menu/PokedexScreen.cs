using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Pokédex (maqueta 12): VISTOS y CAPTURADOS arriba, la lista de especies por número a la derecha
    /// (las no vistas salen como «-----» y las capturadas con la Pokébola) y, a la izquierda, la ficha de
    /// la resaltada: sprite, nombre, tipos y descripción. La descripción solo se lee de las capturadas,
    /// como en FireRed; de las vistas se ve el sprite y el tipo.
    /// </summary>
    public class PokedexScreen : MonoBehaviour
    {
        [SerializeField] MenuList list;
        [SerializeField] TMP_Text countsText;
        [SerializeField] Image sprite;
        [SerializeField] TMP_Text unknownText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text typesText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField, Tooltip("Icono de capturado (la Pokébola de la Mochila).")] Sprite caughtIcon;

        public void Hide() => gameObject.SetActive(false);

        public IEnumerator Browse(GameMenu menu)
        {
            List<PokemonData> species = SortedSpecies();
            gameObject.SetActive(true);
            menu.HideMessage();
            countsText.text = $"VISTOS {GameState.SeenSpeciesCount}   CAPTURADOS {GameState.CaughtSpeciesCount}";

            var labels = new string[species.Count];
            for (int i = 0; i < species.Count; i++)
            {
                PokemonData data = species[i];
                labels[i] = $"{data.dexNumber:000} {(GameState.HasSeen(data.speciesId) ? data.displayName : "-----")}";
            }

            list.IconFor = index => GameState.HasCaught(species[index].speciesId) ? caughtIcon : null;
            int chosen = 0;
            while (chosen != MenuList.Cancelled)
            {
                yield return list.Choose(labels, null, value => chosen = value,
                    index => Describe(species[index]), chosen);
            }
            list.IconFor = null;
            Hide();
        }

        void Describe(PokemonData data)
        {
            bool seen = GameState.HasSeen(data.speciesId);
            bool caught = GameState.HasCaught(data.speciesId);

            sprite.sprite = seen ? data.frontSprite : null;
            sprite.enabled = sprite.sprite != null;
            unknownText.gameObject.SetActive(!sprite.enabled);
            unknownText.text = seen ? "SIN SPRITE" : "?";

            nameText.text = seen ? $"{data.dexNumber:000} {data.displayName}" : $"{data.dexNumber:000} -----";
            typesText.text = seen ? Types(data) : string.Empty;
            descriptionText.text = caught
                ? data.description
                : seen ? "Captúralo para leer su ficha completa." : "Todavía no lo has visto.";
        }

        static string Types(PokemonData data)
        {
            var text = new StringBuilder(PokemonTypeColors.BadgeMarkup(data.primaryType));
            if (data.hasSecondaryType)
                text.Append(' ').Append(PokemonTypeColors.BadgeMarkup(data.secondaryType));
            return text.ToString();
        }

        static List<PokemonData> SortedSpecies()
        {
            var species = new List<PokemonData>();
            foreach (PokemonData data in PokemonDatabase.All())
                if (data != null)
                    species.Add(data);
            species.Sort((a, b) => a.dexNumber != b.dexNumber
                ? a.dexNumber.CompareTo(b.dexNumber)
                : string.CompareOrdinal(a.speciesId, b.speciesId));
            return species;
        }
    }
}
