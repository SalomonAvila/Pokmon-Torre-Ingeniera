using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Cambio de escena con fundido a negro (delegado en <see cref="FadeManager"/>).
    /// Guarda el SpawnPoint donde debe aparecer el jugador en la escena nueva (PlayerController lo lee al iniciar).
    /// </summary>
    public static class SceneTransition
    {
        public static string PendingSpawn { get; private set; }

        /// <summary>Casilla y orientación a las que vuelve el jugador tras un combate (si se piden).</summary>
        public static bool HasPendingPosition { get; private set; }
        public static Vector3 PendingPosition { get; private set; }
        public static int PendingFacing { get; private set; }

        /// <summary>Hay un fundido o cambio de escena en curso: el jugador no debe moverse.</summary>
        public static bool IsTransitioning => FadeManager.IsFading;

        // El proyecto entra a Play sin recargar el dominio: los valores estáticos se reinician a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PendingSpawn = null;
            HasPendingPosition = false;
            PendingPosition = default;
            PendingFacing = 0;
        }

        /// <summary>
        /// Pide que el jugador aparezca en una casilla concreta (no en un SpawnPoint) al cargar la escena.
        /// Lo usa el combate para devolver al jugador a la casilla donde lo sorprendió el encuentro.
        /// </summary>
        public static void SetPendingPosition(Vector3 position, int facing)
        {
            PendingPosition = position;
            PendingFacing = facing;
            HasPendingPosition = true;
            PendingSpawn = null;
        }

        /// <summary>Lee y limpia la casilla de retorno. Devuelve false si no se pidió ninguna.</summary>
        public static bool ConsumePendingPosition(out Vector3 position, out int facing)
        {
            position = PendingPosition;
            facing = PendingFacing;
            bool has = HasPendingPosition;
            HasPendingPosition = false;
            return has;
        }

        /// <summary>Va a otra escena con fundido. Devuelve false si la escena no existe (todavía).</summary>
        public static bool Go(string sceneName, string spawnId)
        {
            if (IsTransitioning)
                return true;

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"La escena '{sceneName}' no existe o no está en Build Profiles.");
                return false;
            }

            FadeManager.Ensure().FadeAndLoad(sceneName, spawnId);
            return true;
        }

        /// <summary>El jugador llama esto al aparecer para no volver a usar el mismo destino.</summary>
        public static string ConsumePendingSpawn()
        {
            string spawn = PendingSpawn;
            PendingSpawn = null;
            return spawn;
        }

        /// <summary>Solo lo usa el fundido, justo antes de cargar la escena.</summary>
        internal static void SetPendingSpawn(string spawnId) => PendingSpawn = spawnId;
    }
}
