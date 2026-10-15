using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Indica a qué piso del edificio pertenece la escena (ver Building.Floors).
    /// Al cargarla actualiza GameState.CurrentFloor, que usa la escalera para saber qué tramo es.
    /// </summary>
    public class FloorInfo : MonoBehaviour
    {
        [SerializeField] string floor = "1";

        public string Floor => floor;

        void Awake()
        {
            if (Building.IsValid(floor))
                GameState.CurrentFloor = floor;
            else
                Debug.LogWarning($"'{floor}' no es un piso válido del edificio.");
        }
    }
}
