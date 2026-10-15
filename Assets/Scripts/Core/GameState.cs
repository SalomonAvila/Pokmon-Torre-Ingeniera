using System;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Estado de la partida en curso: los datos guardados y el piso donde está el jugador.
    /// Si se prueba una escena directamente (sin pasar por el menú), carga la partida guardada o crea una vacía.
    /// </summary>
    public static class GameState
    {
        static SaveData save;

        public static SaveData Save
        {
            get
            {
                if (save == null)
                    save = SaveSystem.TryLoad(out SaveData loaded) ? loaded : new SaveData();
                return save;
            }
            set => save = value;
        }

        /// <summary>Piso donde está el jugador (lo actualiza FloorInfo al entrar a cada piso).</summary>
        public static string CurrentFloor { get; set; } = Building.Floors[0];

        /// <summary>Se invoca cuando cambia el progreso (por ejemplo, al completar un piso).</summary>
        public static event Action ProgressChanged;

        // El proyecto entra a Play sin recargar el dominio: los valores estáticos se reinician a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            save = null;
            CurrentFloor = Building.Floors[0];
            ProgressChanged = null;
        }

        /// <summary>
        /// Un piso está completado cuando se vence a su jefe. Los pisos sin reto (1 y 2) cuentan como completados,
        /// así que la escalera del 2 al 3 siempre está abierta; desde el 3 hay que completar cada piso para subir.
        /// </summary>
        public static bool IsFloorCompleted(string floor) =>
            !Building.HasChallenge(floor) || Save.completedFloors.Contains(floor);

        public static void CompleteFloor(string floor)
        {
            if (!Save.completedFloors.Contains(floor))
                Save.completedFloors.Add(floor);
            SaveSystem.Save(Save);
            ProgressChanged?.Invoke();
        }

        public static void ResetProgress()
        {
            Save.completedFloors.Clear();
            SaveSystem.Save(Save);
            ProgressChanged?.Invoke();
        }
    }
}
