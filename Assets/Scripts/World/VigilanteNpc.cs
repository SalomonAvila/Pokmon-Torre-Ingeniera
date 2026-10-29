using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Vigilante Gómez en el Atrio (Doc §8 y §10): cuida la puerta del edificio y cambia su diálogo
    /// según el progreso de la partida:
    /// <list type="bullet">
    /// <item>Sin inicial: manda a hablar con Gabriel.</item>
    /// <item>Con inicial y sin captura: pide capturar un Pokémon en las jardineras de abajo.</item>
    /// <item>Con inicial y captura: deja pasar al lobby (la puerta la resuelve <see cref="WarpZone"/> con
    /// <see cref="GameState.PassesBuildingDoor"/>).</item>
    /// <item>Tras el tutorial: recuerda que la escalera del lobby lleva al piso 3.</item>
    /// </list>
    /// Hereda de <see cref="Interactable"/> para que <see cref="PlayerController"/> lo encuentre y bloquee
    /// su casilla como cualquier NPC; el builder lo crea con <c>ProvisionalMap.CreateVigilante</c>.
    /// </summary>
    public class VigilanteNpc : Interactable
    {
        public override void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

            if (!GameState.HasStarter)
            {
                WorldMessage.Show(
                    "¡Alto ahí, joven! El edificio está cerrado, hay criaturas sueltas.",
                    "Primero hable con Gabriel, junto a la pantalla: él le dará un POKÉMON.");
                return;
            }

            if (!GameState.PassesBuildingDoor)
            {
                WorldMessage.Show(
                    "Ya tiene compañero, pero así no puede entrar.",
                    "Capture al menos un POKÉMON en las jardineras de abajo y vuelva.");
                return;
            }

            if (GameState.TutorialDone)
            {
                WorldMessage.Show(
                    "Ya superó la prueba con Gabriel.",
                    "La escalera del lobby lo lleva al piso 3. ¡Buen viaje!");
                return;
            }

            WorldMessage.Show(
                "Adelante. Don Chucho lo espera en el lobby.",
                "El ascensor no funciona; la escalera del fondo lleva al piso 3.");
        }
    }
}
