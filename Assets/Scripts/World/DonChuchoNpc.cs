using TorreIngenieria.Audio;
using TorreIngenieria.Core;
using TorreIngenieria.UI;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Don Chucho, el señor de los tintos del lobby (Doc §8): su mostrador es la tienda, el punto de
    /// curación gratis y el punto de guardado del edificio. Junto a él se reaparece tras un desmayo
    /// (<see cref="Building.SpawnFromFaint"/>).
    ///
    /// COMPRAR abre la tienda del menú del juego (<see cref="ShopScreen"/>); CURAR cura PS y PP y revive
    /// (<see cref="GameState.HealTeam"/>); GUARDAR pregunta y guarda la partida.
    /// </summary>
    public class DonChuchoNpc : Interactable
    {
        static readonly string[] CounterOptions = { "COMPRAR", "CURAR", "GUARDAR", "SALIR" };
        static readonly string[] YesNo = { "SÍ", "NO" };

        /// <summary>Su saludo no cambia con el progreso: la burbuja solo sale la primera vez.</summary>
        public override string DialogueKey => "mostrador";

        public override void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

            MarkDialogueSeen();
            WorldMessage.Show(
                new[] { "¡Quiubo, mijo! Bienvenido al puesto de Don Chucho.", "¿En qué le puedo servir?" },
                CounterOptions,
                OnCounterChoice);
        }

        void OnCounterChoice(int chosen)
        {
            switch (chosen)
            {
                case 0:
                    if (!GameMenu.OpenShop(() => WorldMessage.Show("¡Que le vaya bien! Vuelva cuando quiera.")))
                        WorldMessage.Show("Uy, la vitrina está cerrada por ahora (falta generar el menú del juego).");
                    break;
                case 1:
                    Heal();
                    break;
                case 2:
                    AskToSave();
                    break;
                default:
                    WorldMessage.Show("¡Que le vaya bien! Vuelva cuando quiera.");
                    break;
            }
        }

        static void Heal()
        {
            if (GameState.TeamCount == 0)
            {
                WorldMessage.Show("Mijo, usted no tiene POKÉMON que atender.");
                return;
            }

            bool healed = GameState.HealTeam();
            if (healed)
                GameAudio.Sfx(GameAudio.Heal);
            WorldMessage.Show(healed
                ? new[] { "Venga, déjeme ver a esos POKÉMON...", "¡Listo! Su equipo quedó como nuevo.", "Aquí no se cobra: la salud es primero." }
                : new[] { "Sus POKÉMON están perfectos, mijo. No necesitan nada." });
        }

        static void AskToSave()
        {
            WorldMessage.Show(new[] { "¿Quiere guardar su progreso aquí en el mostrador?" }, YesNo, answer =>
            {
                if (answer != 0)
                {
                    WorldMessage.Show("Bueno, cuando quiera.");
                    return;
                }

                GameState.SaveNow();
                GameAudio.Sfx(GameAudio.Save);
                WorldMessage.Show($"{GameState.Save.playerName} guardó la partida.");
            });
        }
    }
}
