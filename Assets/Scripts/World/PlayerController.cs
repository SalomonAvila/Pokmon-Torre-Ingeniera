using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Movimiento por casillas en 4 direcciones, como en Pokémon FireRed (documento de diseño, secciones 7 y 9):
    /// - Un toque breve solo gira al personaje; mantener la tecla lo hace avanzar casilla por casilla.
    /// - Shift corre. Contra una pared camina en el sitio.
    /// - Cada paso alterna pie (PasoA / PasoB) y vuelve a Quieto a mitad de casilla.
    /// El objeto se mantiene en el centro de la casilla; el sprite hijo tiene el pivote en los pies.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public enum Direction { Down, Up, Left, Right }

        [Header("Entrada")]
        [SerializeField] InputActionAsset controls;

        [Header("Sprites (Quieto, PasoA, PasoB)")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] down = new Sprite[3];
        [SerializeField] Sprite[] up = new Sprite[3];
        [SerializeField] Sprite[] left = new Sprite[3];
        [SerializeField] Sprite[] right = new Sprite[3];

        [Header("Movimiento")]
        [SerializeField, Tooltip("Casillas por segundo al caminar.")] float walkSpeed = 4f;
        [SerializeField, Tooltip("Casillas por segundo al correr.")] float runSpeed = 8f;
        [SerializeField, Tooltip("Tiempo que hay que mantener la tecla tras girar para empezar a caminar.")] float turnDelay = 0.1f;
        [SerializeField, Tooltip("Capas del mapa que bloquean el paso.")]
        string[] blockingLayerNames = { "Paredes", "Colisiones", "Bloqueo" };

        // NonSerialized: si Unity recarga los scripts durante el Play, se vuelven a buscar en Awake/OnEnable
        // en lugar de quedar como acciones vacías.
        [System.NonSerialized] InputAction moveAction;
        [System.NonSerialized] InputAction runAction;
        Tilemap[] blockingTilemaps;
        Direction facing = Direction.Down;
        bool moving;
        bool nextStepIsA = true;
        float turnTimer;

        /// <summary>Se invoca al chocar con una casilla bloqueada (posición del centro de esa casilla).</summary>
        public static event Action<Vector3> Bumped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Bumped = null;

        public Direction Facing => facing;
        public bool IsMoving => moving;

        void Awake()
        {
            moveAction = controls.FindAction("Juego/Move", true);
            runAction = controls.FindAction("Juego/Run", true);
            transform.position = CellCenter(transform.position);
            ShowFrame(0);
        }

        // El mapa de acciones no se desactiva en OnDisable: al cambiar de escena, el jugador anterior
        // podría apagarlo justo después de que el nuevo lo encendiera.
        void OnEnable()
        {
            moveAction ??= controls.FindAction("Juego/Move", true);
            runAction ??= controls.FindAction("Juego/Run", true);
            moveAction.actionMap.Enable();
        }

        void Start()
        {
            // Las casillas bloqueadas se consultan directamente en las capas del mapa (como en FireRed),
            // sin depender de la física: es exacto y no cambia según cómo se cargue la escena.
            // Se incluyen las capas inactivas (como Bloqueo) porque pueden activarse durante el juego.
            blockingTilemaps = Array.FindAll(
                FindObjectsByType<Tilemap>(FindObjectsInactive.Include),
                tilemap => Array.IndexOf(blockingLayerNames, tilemap.name) >= 0);

            // Si se llegó desde otra escena (escalera, puerta), aparece en el SpawnPoint indicado.
            string spawnId = SceneTransition.ConsumePendingSpawn();
            SpawnPoint spawn = string.IsNullOrEmpty(spawnId) ? null : SpawnPoint.Find(spawnId);
            if (spawn != null)
            {
                transform.position = CellCenter(spawn.transform.position);
                facing = spawn.Facing;
                ShowFrame(0);
            }
            else if (!string.IsNullOrEmpty(spawnId))
            {
                Debug.LogWarning($"No hay un SpawnPoint con id '{spawnId}' en esta escena.");
            }
        }

        void Update()
        {
            if (moving || SceneTransition.IsTransitioning || WorldMessage.IsOpen)
                return;

            Direction? input = ReadDirection();
            if (input == null)
            {
                turnTimer = 0f;
                ShowFrame(0);
                return;
            }

            if (input.Value != facing)
            {
                // Toque breve: solo gira. Si se mantiene, empieza a caminar después de turnDelay.
                facing = input.Value;
                turnTimer = turnDelay;
                ShowFrame(0);
                return;
            }

            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
                return;
            }

            StartCoroutine(Step());
        }

        /// <summary>Dirección pulsada; sin diagonales (gana el eje con más fuerza).</summary>
        Direction? ReadDirection()
        {
            Vector2 value = moveAction.ReadValue<Vector2>();
            if (value.sqrMagnitude < 0.25f)
                return null;
            if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
                return value.x > 0 ? Direction.Right : Direction.Left;
            return value.y > 0 ? Direction.Up : Direction.Down;
        }

        IEnumerator Step()
        {
            moving = true;
            Vector3 start = transform.position;
            Vector3 target = start + (Vector3)ToVector(facing);
            bool blocked = IsBlocked(target);
            if (blocked)
            {
                Bumped?.Invoke(target);
                target = start; // camina en el sitio, como al chocar en los juegos originales
            }

            float duration = 1f / (runAction.IsPressed() ? runSpeed : walkSpeed);
            int stepFrame = nextStepIsA ? 1 : 2;
            nextStepIsA = !nextStepIsA;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, target, t / duration);
                ShowFrame(t < duration / 2f ? stepFrame : 0);
                yield return null;
            }

            transform.position = target;
            ShowFrame(0);
            moving = false;

            if (!blocked)
                WarpZone.At(transform.position)?.Trigger();
        }

        bool IsBlocked(Vector3 cellCenter)
        {
            foreach (Tilemap tilemap in blockingTilemaps)
                if (tilemap.gameObject.activeInHierarchy && tilemap.HasTile(tilemap.WorldToCell(cellCenter)))
                    return true;
            return false;
        }

        void ShowFrame(int index)
        {
            Sprite[] frames = facing switch
            {
                Direction.Up => up,
                Direction.Left => left,
                Direction.Right => right,
                _ => down,
            };
            if (frames.Length > index && frames[index] != null)
                spriteRenderer.sprite = frames[index];
        }

        static Vector2 ToVector(Direction direction) => direction switch
        {
            Direction.Up => Vector2.up,
            Direction.Left => Vector2.left,
            Direction.Right => Vector2.right,
            _ => Vector2.down,
        };

        static Vector3 CellCenter(Vector3 position) =>
            new Vector3(Mathf.Floor(position.x) + 0.5f, Mathf.Floor(position.y) + 0.5f, position.z);
    }
}
