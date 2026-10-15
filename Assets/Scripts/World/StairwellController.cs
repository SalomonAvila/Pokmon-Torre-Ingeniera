using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Escalera en zigzag entre dos pisos. Una sola escena sirve para todos los tramos del edificio:
    /// el tramo se deduce del piso desde el que se llega (GameState.CurrentFloor).
    /// La subida está bloqueada con cinta de precaución hasta completar el piso de abajo
    /// (protocolo "Evaluación Total"). Bajar siempre está permitido.
    /// </summary>
    public class StairwellController : MonoBehaviour
    {
        public const string SpawnFromBelow = "DesdeAbajo";
        public const string SpawnFromAbove = "DesdeArriba";

        [SerializeField] WarpZone bottomExit;
        [SerializeField] WarpZone topExit;
        [SerializeField, Tooltip("Capa con la cinta de precaución; se oculta cuando la subida está habilitada.")]
        Tilemap lockLayer;
        [SerializeField, Tooltip("Segundos sin volver a mostrar el aviso tras cerrarlo.")]
        float messageCooldown = 0.4f;

        string lowerFloor;
        string upperFloor;

        public bool IsLocked => upperFloor == null || !GameState.IsFloorCompleted(lowerFloor);

        void Awake()
        {
            // Si se llega bajando, el piso actual es el de arriba del tramo.
            bool cameFromAbove = SceneTransition.PendingSpawn == SpawnFromAbove;
            string current = GameState.CurrentFloor;
            lowerFloor = cameFromAbove ? Building.Previous(current) ?? current : current;
            upperFloor = Building.Next(lowerFloor);

            bottomExit.SetTarget(Building.SceneFor(lowerFloor), Building.SpawnFromStairsAbove);
            if (upperFloor != null)
                topExit.SetTarget(Building.SceneFor(upperFloor), Building.SpawnFromStairsBelow);

            Refresh();
        }

        void OnEnable()
        {
            GameState.ProgressChanged += Refresh;
            PlayerController.Bumped += OnPlayerBumped;
        }

        void OnDisable()
        {
            GameState.ProgressChanged -= Refresh;
            PlayerController.Bumped -= OnPlayerBumped;
        }

        void Refresh() => lockLayer.gameObject.SetActive(IsLocked);

        void OnPlayerBumped(Vector3 position)
        {
            if (!IsLocked || !lockLayer.HasTile(lockLayer.WorldToCell(position)))
                return;
            if (Time.unscaledTime - WorldMessage.ClosedAt < messageCooldown)
                return;

            if (upperFloor == null)
                WorldMessage.Show("Esta es la última escalera del edificio.");
            else
                WorldMessage.Show(
                    "La escalera está cerrada con cinta de precaución.",
                    $"Protocolo \"Evaluación Total\": completa el {Building.DisplayName(lowerFloor)} para subir al {Building.DisplayName(upperFloor)}.");
        }
    }
}
