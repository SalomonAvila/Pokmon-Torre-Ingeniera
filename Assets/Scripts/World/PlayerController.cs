using System;
using System.Collections;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Movimiento por casillas en 4 direcciones, como en Pokémon FireRed (documento de diseño, secciones 7 y 9):
    /// - Un toque breve solo gira al personaje; mantener la tecla lo hace avanzar casilla por casilla.
    /// - Shift corre. Contra una pared camina en el sitio.
    /// - Cada paso alterna pie: PasoA → PasoA2 en una casilla y PasoB → PasoB2 en la siguiente (4 cuadros de
    ///   caminata, Doc §5). Con una hoja antigua de 3 cuadros, la segunda mitad del paso vuelve a Quieto.
    /// El objeto se mantiene en el centro de la casilla; el sprite hijo tiene el pivote en los pies.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public enum Direction { Down, Up, Left, Right }

        [Header("Entrada")]
        [SerializeField] InputActionAsset controls;

        [Header("Sprites (Quieto, PasoA, PasoA2, PasoB, PasoB2)")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] down = new Sprite[5];
        [SerializeField] Sprite[] up = new Sprite[5];
        [SerializeField] Sprite[] left = new Sprite[5];
        [SerializeField] Sprite[] right = new Sprite[5];

        [Header("Movimiento")]
        [SerializeField, Tooltip("Casillas por segundo al caminar.")] float walkSpeed = 4f;
        [SerializeField, Tooltip("Casillas por segundo al correr.")] float runSpeed = 8f;
        [SerializeField, Tooltip("Tiempo que hay que mantener la tecla tras girar para empezar a caminar.")] float turnDelay = 0.1f;
        [SerializeField, Tooltip("Capas del mapa que bloquean el paso.")]
        string[] blockingLayerNames = { "Paredes", "Colisiones", "Bloqueo" };

        [Header("Indicadores de diálogo")]
        [SerializeField, Min(0.1f), Tooltip("Radio en casillas para ver la burbuja de NPCs con diálogo nuevo.")]
        float indicatorRadius = 8f;

        // NonSerialized: si Unity recarga los scripts durante el Play, se vuelven a buscar en Awake/OnEnable
        // en lugar de quedar como acciones vacías.
        [System.NonSerialized] InputAction moveAction;
        [System.NonSerialized] InputAction runAction;
        [System.NonSerialized] InputAction interactAction;
        [System.NonSerialized] InputAction cancelAction;
        [System.NonSerialized] InputAction menuAction;
        [System.NonSerialized] InputAction mapAction;
        Tilemap[] blockingTilemaps;
        Direction facing = Direction.Down;
        bool moving;
        bool nextStepIsA = true;
        float turnTimer;
        Interactable[] interactables;

        /// <summary>Se invoca al chocar con una casilla bloqueada (posición del centro de esa casilla).</summary>
        public static event Action<Vector3> Bumped;

        /// <summary>Se invoca al terminar un paso hasta una casilla nueva (posición del centro de esa casilla).</summary>
        public static event Action<Vector3> Stepped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Bumped = null;
            Stepped = null;
        }

        public Direction Facing => facing;
        public bool IsMoving => moving;

        void Awake()
        {
            moveAction = controls.FindAction("Juego/Move", true);
            runAction = controls.FindAction("Juego/Run", true);
            interactAction = controls.FindAction("Juego/Interact", true);
            cancelAction = controls.FindAction("Juego/Cancel", true);
            menuAction = controls.FindAction("Juego/Menu", true);
            mapAction = controls.FindAction("Juego/Map", true);
            // Las escenas ya generadas guardaron el radio anterior (4); el nuevo mínimo es 8.
            // Al regenerar los mapas, el builder escribirá 8 directamente.
            if (indicatorRadius < 8f)
                indicatorRadius = 8f;
            transform.position = CellCenter(transform.position);
            ShowFrame(0);
        }

        // El mapa de acciones no se desactiva en OnDisable: al cambiar de escena, el jugador anterior
        // podría apagarlo justo después de que el nuevo lo encendiera.
        void OnEnable()
        {
            moveAction ??= controls.FindAction("Juego/Move", true);
            runAction ??= controls.FindAction("Juego/Run", true);
            interactAction ??= controls.FindAction("Juego/Interact", true);
            cancelAction ??= controls.FindAction("Juego/Cancel", true);
            menuAction ??= controls.FindAction("Juego/Menu", true);
            mapAction ??= controls.FindAction("Juego/Map", true);
            moveAction.actionMap.Enable();

            // Al recargar scripts durante el Play se pierde la lista cacheada; Start la vuelve a
            // calcular tras colocar al jugador, pero aquí se recupera antes del primer fotograma.
            CacheInteractables();
            RefreshDialogueMarkers();
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
            else if (SceneTransition.ConsumePendingPosition(out Vector3 returnPosition, out int returnFacing))
            {
                // Vuelta de un combate: cae en la misma casilla donde lo sorprendió el encuentro.
                transform.position = CellCenter(returnPosition);
                facing = (Direction)returnFacing;
                ShowFrame(0);
            }
            else if (!string.IsNullOrEmpty(spawnId))
            {
                Debug.LogWarning($"No hay un SpawnPoint con id '{spawnId}' en esta escena.");
            }

            // La orientación puede venir del SpawnPoint o de la vuelta de un combate: hay que
            // señalar a los NPC cercanos desde el primer fotograma, sin esperar a moverse.
            CacheInteractables();
            RefreshDialogueMarkers();
        }

        void Update()
        {
            // La lista de interactuables está cacheada: recorrerla cada fotograma es barato y permite
            // que la burbuja siga al jugador, desaparezca al ver el diálogo y cambie con el progreso.
            RefreshDialogueMarkers();

            // Mientras hay texto, opciones, un fundido o un paso en curso, el jugador no se mueve ni interactúa.
            if (moving || SceneTransition.IsTransitioning ||
                WorldMessage.IsOpen || DialogueBox.IsOpen || ChoiceBox.IsChoosing || GameMenu.IsOpen)
                return;

            if (interactAction.WasPressedThisFrame())
            {
                Interact();
                return;
            }

            // Menú del juego (Equipo y Mochila en la Etapa 4; el resto de opciones llega en la Etapa 7).
            if (menuAction.WasPressedThisFrame())
            {
                if (!GameMenu.Open())
                    WorldMessage.Show("El menú todavía no está generado (Torre Ingeniería > Construir menú del juego).");
                return;
            }
            // Mientras no exista la pantalla de mapa, M recuerda el objetivo actual.
            if (mapAction.WasPressedThisFrame())
            {
                WorldMessage.Show(Guidance.MessageText());
                return;
            }
            if (cancelAction.WasPressedThisFrame())
                return; // en el mundo no hay nada que cancelar; lo usan la UI y los menús

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
                RefreshDialogueMarkers();
                return;
            }

            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
                return;
            }

            StartCoroutine(Step());
        }

        /// <summary>Habla con el objeto de la casilla que tiene delante (NPC, letrero, ítem o entrenador).</summary>
        void Interact()
        {
            TargetInFront()?.Interact();
            // La conversación se marca como vista dentro de Interact; refrescar aquí oculta la
            // burbuja en el mismo fotograma, incluso si el personaje sigue delante.
            RefreshDialogueMarkers();
        }

        /// <summary>Objeto de la casilla a la que mira; la misma que usa <see cref="Interact"/>.</summary>
        Interactable TargetInFront()
        {
            Vector3 cell = transform.position + (Vector3)ToVector(facing);
            return Interactable.At(cell);
        }

        void CacheInteractables()
        {
            // Se incluyen los inactivos (entrenadores vencidos, objetos recogidos) para no tener que
            // volver a buscar cuando cambie la escena o se recarguen los scripts.
            interactables = FindObjectsByType<Interactable>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Muestra la burbuja de cada NPC o entrenador con diálogo nuevo dentro del radio. La del
        /// personaje que está en la casilla de enfrente se marca como lista (rebote y opacidad completa).
        /// No oculta la burbuja por caminar: es una guía para descubrir a los NPC.
        /// </summary>
        void RefreshDialogueMarkers()
        {
            if (interactables == null)
                return;

            bool blocked = SceneTransition.IsTransitioning ||
                           WorldMessage.IsOpen || DialogueBox.IsOpen || ChoiceBox.IsChoosing || GameMenu.IsOpen;
            Vector3 currentCell = CellCenter(transform.position);
            Vector3 facingCell = currentCell + (Vector3)ToVector(facing);
            float radiusSquared = indicatorRadius * indicatorRadius;

            foreach (Interactable interactable in interactables)
            {
                if (interactable == null)
                    continue;

                bool show = false;
                if (!blocked && interactable.isActiveAndEnabled && interactable.CanShowIndicator)
                {
                    Vector3 offset = interactable.transform.position - currentCell;
                    show = offset.sqrMagnitude <= radiusSquared && interactable.HasUnseenDialogue;
                }

                bool ready = show &&
                             (interactable.transform.position - facingCell).sqrMagnitude < 0.0625f;
                interactable.SetIndicatorVisible(show);
                interactable.SetIndicatorReady(ready);
            }
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
            (int firstHalf, int secondHalf) = StepFrames(nextStepIsA);
            nextStepIsA = !nextStepIsA;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, target, t / duration);
                ShowFrame(t < duration / 2f ? firstHalf : secondHalf);
                yield return null;
            }

            transform.position = target;
            ShowFrame(0);
            moving = false;

            if (!blocked)
            {
                WarpZone.At(transform.position)?.Trigger();
                Stepped?.Invoke(transform.position);
            }

            // Actualiza el radio y el objetivo listo al terminar el paso (también tras chocar con un NPC).
            RefreshDialogueMarkers();
        }

        bool IsBlocked(Vector3 cellCenter)
        {
            foreach (Tilemap tilemap in blockingTilemaps)
                if (tilemap.gameObject.activeInHierarchy && tilemap.HasTile(tilemap.WorldToCell(cellCenter)))
                    return true;

            // NPC, letreros, objetos y entrenadores también ocupan su casilla. Los objetos recogidos
            // se desactivan, así que la casilla vuelve a quedar libre sin tocar el tilemap.
            return Interactable.At(cellCenter) != null;
        }

        /// <summary>
        /// Cuadros de cada mitad del paso. Hoja de 5 cuadros: PasoA → PasoA2 o PasoB → PasoB2. Hoja antigua de
        /// 3 cuadros (escenas sin regenerar): PasoA o PasoB y luego Quieto.
        /// </summary>
        (int firstHalf, int secondHalf) StepFrames(bool stepA)
        {
            if (down.Length >= 5)
                return stepA ? (1, 2) : (3, 4);
            return (stepA ? 1 : 2, 0);
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
