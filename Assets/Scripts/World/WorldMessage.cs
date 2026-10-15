using System.Collections;
using TorreIngenieria.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Cuadro de texto estilo Pokémon dentro de los mapas (letreros, avisos, NPC).
    /// Mientras está abierto el jugador no se mueve.
    /// </summary>
    public class WorldMessage : MonoBehaviour
    {
        static WorldMessage instance;

        [SerializeField] InputActionAsset controls;
        [SerializeField] DialogueBox dialogue;

        public static bool IsOpen { get; private set; }

        /// <summary>Momento en que se cerró el último mensaje (para no reabrirlo al instante).</summary>
        public static float ClosedAt { get; private set; } = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            IsOpen = false;
            ClosedAt = -1f;
        }

        void Awake()
        {
            instance = this;
            dialogue.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsOpen = false;
            }
        }

        /// <summary>Muestra los mensajes uno tras otro. Devuelve false si no hay cuadro de texto en la escena.</summary>
        public static bool Show(params string[] lines)
        {
            if (instance == null)
            {
                Debug.Log(string.Join(" ", lines));
                return false;
            }
            if (!IsOpen)
                instance.StartCoroutine(instance.Run(lines));
            return true;
        }

        IEnumerator Run(string[] lines)
        {
            IsOpen = true;
            controls.FindActionMap("UI", true).Enable(); // Z/E/Enter o A para avanzar
            dialogue.gameObject.SetActive(true);
            foreach (string line in lines)
                yield return dialogue.Say(line);
            dialogue.gameObject.SetActive(false);
            IsOpen = false;
            ClosedAt = Time.unscaledTime;
        }
    }
}
