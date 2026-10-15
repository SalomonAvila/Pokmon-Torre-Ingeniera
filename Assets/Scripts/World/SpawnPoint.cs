using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Lugar donde aparece el jugador al llegar desde otra escena (por ejemplo, arriba de una escalera).
    /// El id debe coincidir con el "targetSpawn" del WarpZone que lleva hasta aquí.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        static readonly List<SpawnPoint> All = new List<SpawnPoint>();

        [SerializeField] string id;
        [SerializeField] PlayerController.Direction facing = PlayerController.Direction.Down;

        public string Id => id;
        public PlayerController.Direction Facing => facing;

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public static SpawnPoint Find(string id) => All.Find(spawn => spawn.id == id);
    }
}
