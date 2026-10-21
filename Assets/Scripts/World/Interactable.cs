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

        void Awake()
        {
            // Volver a cargar la escena no revive lo que ya se recogió o se venció.
            // En Awake (no Start) para que no se vea un fotograma antes de ocultarse.
            if (kind == Kind.Item && IsCollected)
                gameObject.SetActive(false);
            else if (kind == Kind.Trainer && IsDefeated)
                gameObject.SetActive(false);
        }

        public Kind InteractionKind => kind;

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

        /// <summary>Habla con el objeto. Lo llama <see cref="PlayerController"/> al pulsar Interact.</summary>
        public void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

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
