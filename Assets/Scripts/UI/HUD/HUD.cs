using System.Globalization;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Barra superior del mundo: piso actual, sellos y créditos (Doc §8).
    /// Se actualiza sola al cambiar cualquier dato de la partida o el progreso.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] TMP_Text floorText;
        [SerializeField] TMP_Text sealsText;
        [SerializeField] TMP_Text creditsText;

        static readonly CultureInfo Colombian = CultureInfo.GetCultureInfo("es-CO");

        void OnEnable()
        {
            GameState.StateChanged += Refresh;
            GameState.ProgressChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameState.StateChanged -= Refresh;
            GameState.ProgressChanged -= Refresh;
        }

        void Start() => Refresh();

        /// <summary>Vuelve a leer el estado de la partida y repinta los tres campos.</summary>
        public void Refresh()
        {
            if (floorText != null)
                floorText.text = Building.HudTitle(GameState.CurrentFloor);

            if (sealsText != null)
                sealsText.text = $"SELLOS {GameState.SealCount}";

            if (creditsText != null)
                creditsText.text = $"CRÉDITOS {GameState.Save.credits.ToString("N0", Colombian)}";
        }
    }
}
