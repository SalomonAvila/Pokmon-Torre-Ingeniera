using System;
using System.Collections;
using TorreIngenieria.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Cuadro de texto estilo Pokémon dentro de los mapas (letreros, avisos, NPC).
    /// Mientras está abierto el jugador no se mueve: los NPC y objetos lo usan para hablar
    /// (<see cref="Interactable"/>) y los avisos del mundo lo usan con <see cref="Show(string[])"/>.
    /// </summary>
    public class WorldMessage : MonoBehaviour
    {
        static WorldMessage instance;

        [SerializeField] InputActionAsset controls;
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ChoiceBox choices;

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
            if (choices != null)
                choices.gameObject.SetActive(false);
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
        public static bool Show(params string[] lines) => Show(lines, null, null);

        /// <summary>
        /// Muestra los mensajes y, si hay opciones, abre el cuadro de elección. Al cerrarse invoca
        /// <paramref name="onChosen"/> con el índice elegido (o −1 si no hubo opciones).
        /// Devuelve false si no hay cuadro de texto en la escena.
        /// </summary>
        public static bool Show(string[] lines, string[] options, Action<int> onChosen)
        {
            if (instance == null)
            {
                Debug.Log(string.Join(" ", lines));
                onChosen?.Invoke(-1);
                return false;
            }
            if (IsOpen)
                return false;

            instance.StartCoroutine(instance.Run(lines ?? Array.Empty<string>(), options, onChosen));
            return true;
        }

        IEnumerator Run(string[] lines, string[] options, Action<int> onChosen)
        {
            IsOpen = true;
            controls.FindActionMap("UI", true).Enable(); // Z/E/Enter o A para avanzar
            dialogue.gameObject.SetActive(true);
            foreach (string line in lines)
                yield return dialogue.Say(line);

            int chosen = -1;
            if (options != null && options.Length > 0 && choices != null)
                yield return choices.Choose(options, i => chosen = i);

            dialogue.gameObject.SetActive(false);
            IsOpen = false;
            ClosedAt = Time.unscaledTime;
            onChosen?.Invoke(chosen);
        }
    }
}
