using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// La cámara sigue al jugador sin salirse de los límites del mapa (como el Confiner 2D del documento).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField, Tooltip("Esquina inferior izquierda del mapa, en casillas.")] Vector2 mapMin;
        [SerializeField, Tooltip("Esquina superior derecha del mapa, en casillas.")] Vector2 mapMax;

        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (target == null)
                return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            float x = Clamp(target.position.x, mapMin.x + halfWidth, mapMax.x - halfWidth);
            float y = Clamp(target.position.y, mapMin.y + halfHeight, mapMax.y - halfHeight);
            transform.position = new Vector3(x, y, transform.position.z);
        }

        // Si el mapa es más pequeño que la pantalla, se centra.
        static float Clamp(float value, float min, float max) =>
            min > max ? (min + max) / 2f : Mathf.Clamp(value, min, max);
    }
}
