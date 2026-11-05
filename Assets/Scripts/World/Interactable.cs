using System.Text;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Objeto del mapa con el que se puede interactuar mirándolo y pulsando Z/E/Enter:
    /// NPC, letrero, objeto recogible o entrenador. Usa el cuadro de texto de
    /// <see cref="WorldMessage"/> y guarda el resultado en <see cref="GameState"/>,
    /// así los objetos recogidos y los entrenadores vencidos no reaparecen al volver a la escena.
    ///
    /// El objeto debe estar centrado en su casilla (como el jugador).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public enum Kind
        {
            Npc,
            Sign,
            Item,
            Trainer,
        }

        [SerializeField] Kind kind = Kind.Npc;

        [Header("Diálogo (se muestra antes de las opciones)")]
        [SerializeField, TextArea(1, 4)] string[] lines = { "..." };

        [Header("Opciones (vacío = sin elección)")]
        [SerializeField, TextArea(1, 3)] string[] options;
        [SerializeField, Tooltip("Índice de la opción que ejecuta la acción (0 = la primera).")]
        int acceptedOption;

        [SerializeField, TextArea(1, 3)] string[] acceptedLines;
        [SerializeField, TextArea(1, 3)] string[] rejectedLines;

        [Header("Objeto recogible")]
        [SerializeField, Tooltip("Id del objeto de la mochila (ItemData).")]
        string itemId;
        [SerializeField] int itemAmount = 1;
        [SerializeField, Tooltip("Id único del objeto en el mundo; si se omite se usa itemId.")]
        string pickupId;

        [Header("Entrenador")]
        [SerializeField] string trainerId;

        [Header("Indicador de diálogo")]
        [SerializeField, Tooltip("Id estable para recordar la conversación vista; si se omite se usa escena/nombre.")]
        string dialogueId;

        InteractionIndicator indicator;
        string contentKey;

        protected virtual void Awake()
        {
            // La burbuja la crea el builder como hija y nace oculta; se busca también en hijos
            // inactivos para poder mostrarla cuando el jugador mire al personaje.
            indicator = GetComponentInChildren<InteractionIndicator>(true);
            indicator?.SetVisible(false);

            // Volver a cargar la escena no revive lo que ya se recogió o se venció.
            // En Awake (no Start) para que no se vea un fotograma antes de ocultarse.
            if (kind == Kind.Item && IsCollected)
                gameObject.SetActive(false);
            else if (kind == Kind.Trainer && IsDefeated)
                gameObject.SetActive(false);
        }

        public Kind InteractionKind => kind;

        /// <summary>NPC y entrenadores muestran la burbuja; letreros y objetos, por ahora no.</summary>
        public bool CanShowIndicator => kind == Kind.Npc || kind == Kind.Trainer;

        /// <summary>Id estable del personaje para recordar qué conversación ya se vio.</summary>
        public string DialogueId =>
            !string.IsNullOrEmpty(dialogueId) ? dialogueId : $"{gameObject.scene.name}/{name}";

        /// <summary>
        /// Clave de la conversación actual. Los NPC genéricos usan un hash de sus textos; los de
        /// historia la sobrescriben para reflejar su estado de progreso.
        /// </summary>
        public virtual string DialogueKey => contentKey ??= BuildContentKey();

        /// <summary>Hay algo nuevo que decir: el diálogo actual no se ha visto todavía.</summary>
        public bool HasUnseenDialogue =>
            CanShowIndicator && !GameState.HasSeenDialogue(DialogueId, DialogueKey);

        /// <summary>Muestra u oculta la burbuja de interacción (la controla <see cref="PlayerController"/>).</summary>
        public void SetIndicatorVisible(bool visible)
        {
            if (!CanShowIndicator)
                return;
            indicator?.SetVisible(visible);
        }

        /// <summary>Marca la burbuja como cercana (estática) o como objetivo actual (con rebote).</summary>
        public void SetIndicatorReady(bool ready)
        {
            if (!CanShowIndicator)
                return;
            indicator?.SetReady(ready);
        }

        /// <summary>Registra la conversación actual como vista; no impide volver a interactuar.</summary>
        protected void MarkDialogueSeen()
        {
            if (!CanShowIndicator)
                return;
            GameState.MarkDialogueSeen(DialogueId, DialogueKey);
        }

        string BuildContentKey()
        {
            var text = new StringBuilder(96);
            text.Append((int)kind).Append('|').Append(acceptedOption).Append('|');
            AppendLines(text, lines);
            AppendLines(text, options);
            AppendLines(text, acceptedLines);
            AppendLines(text, rejectedLines);
            return StableHash(text.ToString());
        }

        static void AppendLines(StringBuilder text, string[] values)
        {
            text.Append('\u001f');
            if (values == null)
                return;
            foreach (string value in values)
                text.Append(value).Append('\u001e');
        }

        /// <summary>FNV-1a de 64 bits: estable entre ejecuciones, a diferencia de string.GetHashCode.</summary>
        static string StableHash(string text)
        {
            ulong hash = 14695981039346656037UL;
            unchecked
            {
                foreach (char character in text)
                {
                    hash ^= character;
                    hash *= 1099511628211UL;
                }
            }
            return hash.ToString("x16");
        }

        string PickupId => string.IsNullOrEmpty(pickupId) ? itemId : pickupId;
        bool IsCollected => kind == Kind.Item && !string.IsNullOrEmpty(PickupId) && GameState.HasCollectedItem(PickupId);
        bool IsDefeated => kind == Kind.Trainer && !string.IsNullOrEmpty(trainerId) && GameState.HasDefeatedTrainer(trainerId);

        /// <summary>Busca el interactuable centrado en la casilla indicada.</summary>
        public static Interactable At(Vector3 cellCenter)
        {
            foreach (Interactable interactable in FindObjectsByType<Interactable>(FindObjectsInactive.Exclude))
            {
                if (interactable.isActiveAndEnabled &&
                    Vector3.Distance(interactable.transform.position, cellCenter) < 0.25f)
                    return interactable;
            }
            return null;
        }

        /// <summary>
        /// Habla con el objeto. Lo llama <see cref="PlayerController"/> al pulsar Interact.
        /// Es virtual para que los NPC de historia (Gabriel, el Vigilante) puedan decidir su
        /// diálogo según el progreso de la partida.
        /// </summary>
        public virtual void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

            // La burbuja desaparece en cuanto el jugador ve esta versión del diálogo; la interacción
            // sigue disponible aunque la vuelva a pedir.
            MarkDialogueSeen();

            switch (kind)
            {
                case Kind.Item:
                    InteractItem();
                    break;
                case Kind.Trainer:
                    InteractTrainer();
                    break;
                default:
                    InteractDialogue();
                    break;
            }
        }

        void InteractDialogue()
        {
            bool hasOptions = options != null && options.Length > 0;
            if (!hasOptions)
            {
                WorldMessage.Show(lines);
                return;
            }

            WorldMessage.Show(lines, options, chosen =>
            {
                if (chosen == acceptedOption)
                    WorldMessage.Show(acceptedLines);
                else
                    WorldMessage.Show(rejectedLines);
            });
        }

        void InteractItem()
        {
            if (IsCollected)
            {
                gameObject.SetActive(false);
                return;
            }

            if (options == null || options.Length == 0)
            {
                WorldMessage.Show(lines, null, _ => Collect());
                return;
            }

            WorldMessage.Show(lines, options, chosen =>
            {
                if (chosen != acceptedOption)
                {
                    WorldMessage.Show(rejectedLines);
                    return;
                }

                WorldMessage.Show(acceptedLines);
                Collect();
            });
        }

        void Collect()
        {
            if (!string.IsNullOrEmpty(itemId))
                GameState.AddItem(itemId, itemAmount);
            if (!string.IsNullOrEmpty(PickupId))
                GameState.CollectItem(PickupId);
            gameObject.SetActive(false);
        }

        void InteractTrainer()
        {
            if (IsDefeated)
            {
                gameObject.SetActive(false);
                return;
            }

            // Hasta la Etapa 6 no hay combate de entrenadores: hablar con él lo marca vencido.
            WorldMessage.Show(lines, options, chosen =>
            {
                if (options != null && options.Length > 0 && chosen != acceptedOption)
                {
                    WorldMessage.Show(rejectedLines);
                    return;
                }

                WorldMessage.Show(acceptedLines);
                if (!string.IsNullOrEmpty(trainerId))
                    GameState.DefeatTrainer(trainerId);
                gameObject.SetActive(false);
            });
        }
    }
}
