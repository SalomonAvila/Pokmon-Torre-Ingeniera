using System.Collections.Generic;
using TorreIngenieria.Core;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Ayuda por la racha de derrotas contra un jefe (Doc §8): con 3 derrotas seguidas contra el mismo
    /// jefe, Gabriel ofrece una pista y un Agua de panela. La racha la lleva <see cref="GameState"/>; la
    /// pista sale del asset del jefe (<see cref="TrainerData.hintDialogue"/>) y el objeto solo se entrega
    /// una vez por racha, así que no se puede acumular hablando con Gabriel en bucle.
    /// </summary>
    public static class BossHintOffer
    {
        /// <summary>¿Hay una racha pendiente de atender?</summary>
        public static bool Available => GameState.BossHintAvailable;

        /// <summary>Clave de diálogo de Gabriel cuando la ayuda está disponible.</summary>
        public const string DialogueKey = "boss-hint";

        /// <summary>Muestra la pista, entrega el Agua de panela y cura al equipo, como el resto de sus charlas.</summary>
        public static void Show()
        {
            string floor = GameState.BossStreakFloor;
            TrainerData boss = TrainerDatabase.FindBoss(floor);
            string name = boss != null && !string.IsNullOrEmpty(boss.displayName) ? boss.displayName : "el jefe";

            var lines = new List<string>
            {
                $"Ya van {GameState.BossLossStreak(floor)} derrotas seguidas contra {name}.",
                boss != null && !string.IsNullOrWhiteSpace(boss.hintDialogue)
                    ? boss.hintDialogue
                    : "Cambia de estrategia: el tipo del rival manda.",
                "Toma un Agua de panela de mi parte.",
            };

            GameState.AddItem("agua_panela", 1);
            if (GameState.HealTeam())
                lines.Add("Gabriel curó a tus POKÉMON.");

            GameState.MarkBossHintGiven();
            WorldMessage.Show(lines.ToArray());
        }
    }
}
