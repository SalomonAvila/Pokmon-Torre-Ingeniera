using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.Battle
{
    /// <summary>Tipo de combate en curso.</summary>
    public enum BattleKind
    {
        Wild,
        Trainer,
    }

    /// <summary>
    /// Datos que el mundo deja antes de cargar la escena de combate (que no es aditiva ni persiste).
    /// Guarda qué combate empieza y adónde y en qué casilla debe volver el jugador al terminar.
    /// Todo es estático y se reinicia al entrar en Play, como el resto de estáticos del proyecto.
    /// </summary>
    public static class BattleRequest
    {
        public static bool HasPending { get; private set; }
        public static BattleKind Kind { get; private set; }

        /// <summary>Especie y nivel del encuentro salvaje.</summary>
        public static string WildSpeciesId { get; private set; }
        public static int WildLevel { get; private set; }

        /// <summary>Id del entrenador o jefe (para la segunda mitad de la etapa).</summary>
        public static string TrainerId { get; private set; }

        /// <summary>Escena del mundo, casilla y orientación a las que se vuelve al acabar.</summary>
        public static string ReturnScene { get; private set; }
        public static Vector3 ReturnPosition { get; private set; }
        public static int ReturnFacing { get; private set; }

        /// <summary>
        /// ¿Es el combate tutorial de Gabriel? No da EXP ni permite objetos ni huida (Doc §8); al
        /// ganar, <see cref="GameState.CompleteTutorial"/> abre la escalera del lobby.
        /// </summary>
        public static bool IsTutorial =>
            Kind == BattleKind.Trainer && TrainerId == GameRules.TutorialTrainerId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();

        /// <summary>Prepara un combate salvaje. <paramref name="facing"/> es el valor de PlayerController.Direction.</summary>
        public static void StartWild(string speciesId, int level, string returnScene, Vector3 returnPosition, int facing)
        {
            HasPending = true;
            Kind = BattleKind.Wild;
            WildSpeciesId = speciesId;
            WildLevel = level;
            TrainerId = null;
            ReturnScene = returnScene;
            ReturnPosition = returnPosition;
            ReturnFacing = facing;
        }

        /// <summary>Prepara un combate de entrenador o jefe (sin huida). Se usará en la segunda mitad.</summary>
        public static void StartTrainer(string trainerId, string returnScene, Vector3 returnPosition, int facing)
        {
            HasPending = true;
            Kind = BattleKind.Trainer;
            WildSpeciesId = null;
            WildLevel = 0;
            TrainerId = trainerId;
            ReturnScene = returnScene;
            ReturnPosition = returnPosition;
            ReturnFacing = facing;
        }

        public static void Clear()
        {
            HasPending = false;
            Kind = BattleKind.Wild;
            WildSpeciesId = null;
            WildLevel = 0;
            TrainerId = null;
            ReturnScene = null;
            ReturnPosition = default;
            ReturnFacing = 0;
        }
    }
}