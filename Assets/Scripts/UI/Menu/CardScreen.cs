using System.Collections;
using System.Globalization;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Carné estudiantil (maqueta 08; opción PEDRO del menú): nombre, ID, créditos, Pokédex, tiempo de
    /// juego y promedio de notas, con los 13 sellos de los jefes abajo (pisos 3 a 14 y el PT). Un sello
    /// obtenido se ve con su arte o, si aún no existe, con el color del piso; los que faltan, apagados.
    /// A o B cierran el carné.
    /// </summary>
    public class CardScreen : MonoBehaviour
    {
        static readonly CultureInfo Colombian = CultureInfo.GetCultureInfo("es-CO");

        [SerializeField] InputActionAsset controls;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text idText;
        [SerializeField] TMP_Text valuesText;

        [Header("Sellos (un elemento por piso con jefe, en el orden de Building.Floors)")]
        [SerializeField] string[] sealFloors;
        [SerializeField] Image[] sealImages;
        [SerializeField] TMP_Text[] sealLabels;
        [SerializeField, Tooltip("Arte del sello obtenido (opcional por piso).")] Sprite[] sealSprites;
        [SerializeField, Tooltip("Color del sello obtenido cuando no hay arte.")] Color[] sealColors;
        [SerializeField] Color missingColor = new Color(0.3f, 0.3f, 0.33f, 1f);

        public void Hide() => gameObject.SetActive(false);

        public IEnumerator Show(GameMenu menu)
        {
            Refresh();
            gameObject.SetActive(true);
            menu.Prompt("Carné estudiantil de la Torre de Ingeniería.");

            var input = new MenuInput(controls);
            yield return null;
            while (!input.Submitted && !input.Cancelled)
                yield return null;
            yield return null; // B no debe cerrar también el menú de pausa
            Hide();
        }

        void Refresh()
        {
            SaveData save = GameState.Save;
            nameText.text = save.playerName;
            idText.text = $"ID No. {save.playerId}";

            string average = save.bossNotes.Count > 0 ? GameState.FormattedAverageNote() : "-";
            valuesText.text = string.Join("\n",
                save.credits.ToString("N0", Colombian),
                $"{GameState.CaughtSpeciesCount}",
                GameState.FormattedPlayTime(),
                average);

            for (int i = 0; i < sealImages.Length; i++)
            {
                string floor = sealFloors != null && i < sealFloors.Length ? sealFloors[i] : null;
                bool owned = floor != null && GameState.HasSeal(floor);
                Sprite art = sealSprites != null && i < sealSprites.Length ? sealSprites[i] : null;
                Image image = sealImages[i];

                image.sprite = owned ? art : null;
                image.color = owned
                    ? art != null ? Color.white : (sealColors != null && i < sealColors.Length ? sealColors[i] : Color.white)
                    : missingColor;
                if (sealLabels != null && i < sealLabels.Length && sealLabels[i] != null)
                    sealLabels[i].gameObject.SetActive(!owned || art == null);
            }
        }
    }
}
