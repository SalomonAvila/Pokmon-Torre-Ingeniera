using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Ficha de un Pokémon en la pantalla de Equipo (maqueta 02): icono, nombre, nivel, barra de PS y
    /// PS actuales/máximos. Tiene cuatro aspectos: normal, seleccionado (cursor encima), vacío y
    /// debilitado, más la marca de "elegido para cambiar". Si el builder encuentra los marcos dibujados
    /// los usa; si no, pinta el fondo con colores planos de la paleta.
    /// </summary>
    public class TeamSlotView : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] Image hpBar;
        [SerializeField] GameObject hpRow;

        [Header("Marcos (opcionales; sin ellos se usan los colores)")]
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite selectedSprite;
        [SerializeField] Sprite emptySprite;
        [SerializeField] Sprite faintedSprite;

        [Header("Colores provisionales")]
        [SerializeField] Color normalColor = new Color32(0xF4, 0xF1, 0xE8, 0xFF);
        [SerializeField] Color selectedColor = new Color32(0xF2, 0xC2, 0x30, 0xFF);
        [SerializeField] Color emptyColor = new Color32(0x8D, 0x99, 0xA6, 0xFF);
        [SerializeField] Color faintedColor = new Color32(0xE8, 0xB8, 0xB0, 0xFF);
        [SerializeField] Color swapColor = new Color32(0x9A, 0xD0, 0xE8, 0xFF);

        bool empty;
        bool fainted;

        public void Show(TeamMember member)
        {
            empty = member == null;
            fainted = !empty && TeamMembers.IsFainted(member);

            nameText.gameObject.SetActive(!empty);
            levelText.gameObject.SetActive(!empty);
            if (hpRow != null)
                hpRow.SetActive(!empty);
            if (icon != null)
                icon.enabled = false;

            if (empty)
            {
                SetLook(false, false);
                return;
            }

            PokemonData species = PokemonDatabase.Get(member.speciesId);
            nameText.text = (species != null ? species.displayName : member.speciesId).ToUpperInvariant();
            levelText.text = fainted ? "DEB" : $"Nv{member.level}";
            levelText.color = fainted ? HpBar.Red : Palette.DarkOutline;

            int current = TeamMembers.CurrentHp(member);
            hpText.text = $"{current}/{member.maxHp}";
            HpBar.ShowHp(hpBar, current, member.maxHp);

            if (icon != null && species != null && species.icon != null)
            {
                icon.sprite = species.icon;
                icon.enabled = true;
            }

            SetLook(false, false);
        }

        /// <summary>Repinta el fondo según el cursor y la marca de cambio.</summary>
        public void SetLook(bool selected, bool markedForSwap)
        {
            if (background == null)
                return;

            Sprite sprite = empty ? emptySprite
                : selected ? selectedSprite
                : fainted ? faintedSprite
                : normalSprite;

            if (sprite != null)
            {
                background.sprite = sprite;
                background.color = markedForSwap ? swapColor : Color.white;
                return;
            }

            background.sprite = null;
            background.color = empty ? emptyColor
                : markedForSwap ? swapColor
                : selected ? selectedColor
                : fainted ? faintedColor
                : normalColor;
        }
    }
}
