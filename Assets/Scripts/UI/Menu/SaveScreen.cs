using TorreIngenieria.Audio;
using System.Collections;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Guardar (maqueta 10): recuadro con el resumen de la partida (jugador, lugar, sellos, Pokédex y tiempo)
    /// y la pregunta SÍ / NO. Guarda con <see cref="GameState.SaveNow"/>, el mismo archivo del guardado
    /// automático; también lo usa el mostrador de Don Chucho.
    /// </summary>
    public class SaveScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text valuesText;

        public void Hide() => gameObject.SetActive(false);

        public IEnumerator Run(GameMenu menu)
        {
            Refresh();
            gameObject.SetActive(true);

            bool yes = false;
            yield return menu.Confirm("¿Quieres guardar la partida?", value => yes = value);
            if (yes)
            {
                GameState.SaveNow();
                GameAudio.Sfx(GameAudio.Save);
                Refresh(); // el tiempo de juego avanzó mientras se decidía
                yield return menu.Say($"{GameState.Save.playerName} guardó la partida.");
            }
            Hide();
        }

        void Refresh()
        {
            SaveData save = GameState.Save;
            valuesText.text = string.Join("\n",
                save.playerName,
                Building.HudTitle(save.location),
                GameState.SealCount.ToString(),
                $"{GameState.CaughtSpeciesCount}/{GameState.SeenSpeciesCount}",
                GameState.FormattedPlayTime());
        }
    }
}
