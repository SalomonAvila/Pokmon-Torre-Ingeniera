using System;
using System.Collections;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Menú del juego en el mundo (maqueta 01): POKÉDEX, EQUIPO, MOCHILA, MAPA, PEDRO, GUARDAR, OPCIONES y
    /// SALIR, con una línea de ayuda abajo. Abre las pantallas de Equipo, Resumen y Mochila (Etapa 4); las
    /// demás muestran un aviso hasta la Etapa 7.
    ///
    /// Es un prefab (<c>Resources/UI/MenuJuego</c>, lo genera <c>GameMenuBuilder</c>) que se instancia en la
    /// escena la primera vez que se abre, así ningún mapa necesita regenerarse. Cada pantalla es una corrutina
    /// que espera a la siguiente: la pila de corrutinas hace de pila de pantallas y B siempre vuelve atrás.
    /// </summary>
    public class GameMenu : MonoBehaviour
    {
        const string PrefabPath = "UI/MenuJuego";

        static GameMenu instance;

        [SerializeField] InputActionAsset controls;
        [SerializeField] MenuList pauseList;
        [SerializeField] DialogueBox message;
        [SerializeField] MenuList actionList;
        [SerializeField] TeamScreen team;
        [SerializeField] SummaryScreen summary;
        [SerializeField] BagScreen bag;

        static readonly string[] PauseOptions =
            { "POKÉDEX", "EQUIPO", "MOCHILA", "MAPA", "PEDRO", "GUARDAR", "OPCIONES", "SALIR" };

        static readonly string[] PauseHelp =
        {
            "Registro de los POKÉMON vistos y capturados.",
            "Revisa a los POKÉMON de tu equipo.",
            "Revisa los objetos de tu mochila.",
            "Mira el mapa del edificio.",
            "Revisa tu carné estudiantil.",
            "Guarda tu progreso.",
            "Ajusta el volumen y otras opciones.",
            "Cierra este menú.",
        };

        const int TeamOption = 1;
        const int BagOption = 2;
        const int SaveOption = 5;
        const int CloseOption = 7;

        /// <summary>True mientras cualquier pantalla del menú está abierta (el jugador no se mueve).</summary>
        public static bool IsOpen { get; private set; }

        // El proyecto entra a Play sin recargar el dominio: los estáticos se reinician a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            IsOpen = false;
        }

        public TeamScreen Team => team;
        public SummaryScreen Summary => summary;
        public BagScreen Bag => bag;

        void Awake()
        {
            HideAll();
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                IsOpen = false;
            }
        }

        /// <summary>Abre el menú de pausa. Devuelve false si falta el prefab (hay que generarlo en el Editor).</summary>
        public static bool Open()
        {
            if (IsOpen)
                return false;

            if (instance == null)
            {
                var prefab = Resources.Load<GameMenu>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning("Falta el menú del juego: usa Torre Ingeniería > Construir menú del juego.");
                    return false;
                }
                instance = Instantiate(prefab);
                instance.name = prefab.name;
            }

            instance.gameObject.SetActive(true);
            instance.StartCoroutine(instance.RunPause());
            return true;
        }

        IEnumerator RunPause()
        {
            IsOpen = true;
            controls.FindActionMap("UI", true).Enable();
            int option = TeamOption;

            while (true)
            {
                HideAll();
                message.gameObject.SetActive(true);
                int chosen = MenuList.Cancelled;
                yield return pauseList.Choose(PauseOptions, null, value => chosen = value,
                    index => message.ShowText(PauseHelp[index], false), option);

                if (chosen == MenuList.Cancelled || chosen == CloseOption)
                    break;

                option = chosen;
                if (chosen == TeamOption)
                {
                    pauseList.Hide();
                    yield return team.Browse(this);
                }
                else if (chosen == BagOption)
                {
                    pauseList.Hide();
                    yield return bag.Browse(this);
                }
                else if (chosen == SaveOption)
                    yield return SaveGame();
                else
                    yield return Say("Esta opción todavía está en construcción.");
            }

            HideAll();
            message.Clear();
            gameObject.SetActive(false);
            IsOpen = false;
        }

        IEnumerator SaveGame()
        {
            bool yes = false;
            yield return Confirm("¿Quieres guardar la partida?", value => yes = value);
            if (!yes)
                yield break;

            SaveSystem.Save(GameState.Save);
            yield return Say($"{GameState.Save.playerName} guardó la partida.");
        }

        void HideAll()
        {
            pauseList.Hide();
            actionList.Hide();
            team.Hide();
            summary.Hide();
            bag.Hide();
        }

        // ---------- Utilidades para las pantallas ----------

        /// <summary>Muestra un mensaje en el cuadro inferior y espera a que el jugador lo pase.</summary>
        public IEnumerator Say(string text)
        {
            message.gameObject.SetActive(true);
            yield return message.Say(text);
        }

        /// <summary>Texto fijo en el cuadro inferior, sin esperar (instrucción de la pantalla).</summary>
        public void Prompt(string text)
        {
            message.gameObject.SetActive(true);
            message.ShowText(text, false);
        }

        public void HideMessage() => message.gameObject.SetActive(false);

        /// <summary>
        /// Submenú pequeño de la esquina inferior derecha (DATOS / CAMBIAR / SALIR, USAR / TIRAR...).
        /// Devuelve el índice o <see cref="MenuList.Cancelled"/>.
        /// </summary>
        public IEnumerator ChooseAction(string prompt, string[] options, Action<int> onChosen)
        {
            Prompt(prompt);
            int chosen = MenuList.Cancelled;
            yield return actionList.Choose(options, null, value => chosen = value);
            actionList.Hide();
            onChosen(chosen);
        }

        /// <summary>Pregunta SÍ / NO; cancelar cuenta como NO.</summary>
        public IEnumerator Confirm(string question, Action<bool> onAnswered)
        {
            int chosen = MenuList.Cancelled;
            yield return ChooseAction(question, new[] { "SÍ", "NO" }, value => chosen = value);
            onAnswered(chosen == 0);
        }
    }
}
