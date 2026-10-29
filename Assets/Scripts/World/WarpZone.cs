using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Casillas que llevan a otra escena al pisarlas (escaleras, puertas), como en Pokémon FireRed.
    /// El área va desde la casilla de este objeto hacia la derecha y hacia arriba.
    /// </summary>
    public class WarpZone : MonoBehaviour
    {
        static readonly List<WarpZone> All = new List<WarpZone>();

        [SerializeField, Tooltip("Ancho y alto del área, en casillas.")] Vector2Int size = Vector2Int.one;
        [SerializeField] string targetScene;
        [SerializeField, Tooltip("Id del SpawnPoint de la escena destino.")] string targetSpawn;
        [SerializeField, Tooltip("Solo pasa con inicial elegido y un Pokémon capturado (puerta del edificio).")]
        bool needsBuildingAccess;
        [SerializeField, Tooltip("Solo pasa con el combate tutorial de Gabriel superado (escalera del lobby).")]
        bool needsTutorial;
        [SerializeField, TextArea, Tooltip("Mensaje al chocar con la puerta cerrada; cada línea va en un renglón.")]
        string lockedMessage;

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public static WarpZone At(Vector3 position)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            return All.Find(warp => warp.Contains(cell));
        }

        bool Contains(Vector2Int cell)
        {
            var origin = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            return new RectInt(origin, size).Contains(cell);
        }

        /// <summary>Cambia el destino en tiempo de juego (por ejemplo, la escalera según el piso).</summary>
        public void SetTarget(string scene, string spawn)
        {
            targetScene = scene;
            targetSpawn = spawn;
        }

        /// <summary>
        /// Convierte la zona en la puerta del edificio: sin inicial elegido y una captura no deja pasar
        /// (Vigilante Gómez, Doc §8). Las líneas del mensaje se separan con saltos de línea.
        /// </summary>
        public void SetBuildingAccess(bool required, string message)
        {
            needsBuildingAccess = required;
            lockedMessage = message;
        }

        /// <summary>Exige el combate tutorial de Gabriel (escalera del lobby al piso 3).</summary>
        public void SetTutorialGate(bool required, string message)
        {
            needsTutorial = required;
            if (!string.IsNullOrEmpty(message))
                lockedMessage = message;
        }

        public void Trigger()
        {
            if (needsBuildingAccess && !GameState.PassesBuildingDoor)
            {
                WorldMessage.Show(string.IsNullOrEmpty(lockedMessage)
                    ? new[] { "El vigilante no te deja pasar todavía." }
                    : lockedMessage.Split('\n'));
                return;
            }

            if (needsTutorial && !GameState.TutorialDone)
            {
                WorldMessage.Show(string.IsNullOrEmpty(lockedMessage)
                    ? new[] { "Primero tienes que superar la prueba de Gabriel." }
                    : lockedMessage.Split('\n'));
                return;
            }

            if (!SceneTransition.Go(targetScene, targetSpawn))
                WorldMessage.Show("Esta zona del edificio todavía está en construcción.");
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
            var origin = new Vector3(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y), 0);
            Gizmos.DrawCube(origin + new Vector3(size.x, size.y) / 2f, new Vector3(size.x, size.y, 0.1f));
        }
    }
}
