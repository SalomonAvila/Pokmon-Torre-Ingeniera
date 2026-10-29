using System.Collections.Generic;
using TorreIngenieria.Battle;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Gabriel en el lobby (Doc §8): reta al jugador al combate tutorial antes de dejarlo subir al
    /// piso 3. El rival es el inicial con ventaja de tipo sobre el del jugador, a nivel 5, sin objetos
    /// ni huida y sin EXP (lo detecta <see cref="BattleRequest.IsTutorial"/>). Al ganar,
    /// <c>BattleSystem</c> llama a <see cref="GameState.CompleteTutorial"/> y la escalera del lobby se abre.
    ///
    /// Hereda de <see cref="Interactable"/> para que <see cref="PlayerController"/> lo encuentre y bloquee
    /// su casilla como cualquier NPC; el builder lo crea con <c>ProvisionalMap.CreateTutorialGabriel</c>.
    /// </summary>
    public class TutorialGabrielNpc : Interactable
    {
        static readonly string[] ChallengeLines =
        {
            "Antes de subir, quiero ver si tu equipo aguanta.",
            "Te reto a un combate de práctica: sin objetos y sin huir.",
            "¿Empezamos?",
        };

        static readonly string[] Options = { "SÍ", "NO" };

        public override void Interact()
        {
            if (WorldMessage.IsOpen || SceneTransition.IsTransitioning)
                return;

            if (GameState.TutorialDone)
            {
                bool healed = GameState.HealTeam();
                WorldMessage.Show(healed
                    ? new[]
                    {
                        "Ya superaste la prueba.",
                        "La escalera del fondo lleva al piso 3.",
                        "Gabriel curó a tus POKÉMON.",
                    }
                    : new[]
                    {
                        "Ya superaste la prueba.",
                        "La escalera del fondo lleva al piso 3.",
                    });
                return;
            }

            // El lobby aún no tiene el centro de curación de Don Chucho: Gabriel deja el equipo listo
            // antes de la prueba para no empezar el tutorial con PS bajos.
            var lines = new List<string>();
            if (GameState.HealTeam())
                lines.Add("Gabriel curó a tus POKÉMON.");
            lines.AddRange(ChallengeLines);

            WorldMessage.Show(lines.ToArray(), Options, answer =>
            {
                if (answer < 0)
                    return;
                if (answer == 0)
                    StartTutorial();
                else
                    WorldMessage.Show("Te espero. La escalera no se abre hasta superar la prueba.");
            });
        }

        void StartTutorial()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            Vector3 position = player != null ? player.transform.position : transform.position;
            int facing = player != null ? (int)player.Facing : (int)PlayerController.Direction.Down;

            BattleRequest.StartTrainer(GameRules.TutorialTrainerId,
                SceneManager.GetActiveScene().name, position, facing);

            if (!SceneTransition.Go(SceneNames.Battle, null))
            {
                BattleRequest.Clear();
                WorldMessage.Show("El combate tutorial todavía no está disponible.");
            }
        }
    }
}
