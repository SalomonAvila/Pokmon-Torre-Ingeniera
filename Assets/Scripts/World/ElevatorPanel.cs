using TorreIngenieria.Audio;
using TorreIngenieria.Core;
using TorreIngenieria.UI;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Puerta de un ascensor del edificio (lobby y ascensores del sur de cada piso). Mirándola y pulsando
    /// aceptar abre el selector de piso (<see cref="BuildingMapScreen"/>): sin el Carné de acceso que
    /// entrega la Ing. Cantera los ascensores no viajan, y con él solo van a pisos ya visitados.
    /// Al elegir, el jugador aparece frente a los ascensores del piso destino
    /// (<see cref="Building.SpawnFromElevator"/>).
    /// </summary>
    public class ElevatorPanel : Interactable
    {
        public override void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

            if (!GameState.Save.accessCard)
            {
                WorldMessage.Show(
                    "El ascensor está bloqueado por la emergencia.",
                    "La pantalla dice: «Acerque su CARNÉ DE ACCESO».");
                return;
            }

            if (!GameMenu.OpenElevator(Travel))
                WorldMessage.Show("El panel del ascensor no responde (falta generar el menú del juego).");
        }

        static void Travel(string floor)
        {
            if (string.IsNullOrEmpty(floor))
                return;

            GameAudio.Sfx(GameAudio.Elevator);
            if (!SceneTransition.Go(Building.SceneFor(floor), Building.SpawnFromElevator))
                WorldMessage.Show("Ese piso todavía está en construcción.");
        }
    }
}
