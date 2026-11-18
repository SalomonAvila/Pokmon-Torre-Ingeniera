using TorreIngenieria.Audio;
using System;
using System.Collections;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Menú del juego en el mundo (maqueta 01): POKÉDEX, EQUIPO, MOCHILA, MAPA, PEDRO, GUARDAR, OPCIONES y
    /// SALIR, con una línea de ayuda abajo. Cada opción abre su pantalla (Etapas 4 y 7); SALIR cierra el menú.
    /// Además sirve las pantallas que se abren desde el mundo sin pasar por la pausa: el Mapa (tecla M), la
    /// tienda de Don Chucho, el selector de ascensor y las Opciones del título.
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
        [SerializeField] PokedexScreen pokedex;
        [SerializeField] BuildingMapScreen map;
        [SerializeField] CardScreen card;
        [SerializeField] SaveScreen saveScreen;
        [SerializeField] OptionsScreen options;
        [SerializeField] ShopScreen shop;

        static readonly string[] PauseOptions =
            { "POKÉDEX", "EQUIPO", "MOCHILA", "MAPA", "PEDRO", "GUARDAR", "OPCIONES", "SALIR" };

        static readonly string[] PauseHelp =
        {
            "Registro de los POKÉMON vistos y capturados.",
            "Revisa a los POKÉMON de tu equipo.",
            "Revisa los objetos de tu mochila.",
            "Mapa del edificio y tu objetivo actual.",
            "Revisa tu carné estudiantil.",
            "Guarda tu progreso.",
            "Ajusta el volumen y otras opciones.",
            "Cierra este menú.",
        };

        const int PokedexOption = 0;
        const int TeamOption = 1;
        const int BagOption = 2;
        const int MapOption = 3;
        const int CardOption = 4;
        const int SaveOption = 5;
        const int OptionsOption = 6;
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

        // ---------- Puntos de entrada ----------

        /// <summary>Abre el menú de pausa. Devuelve false si falta el prefab (hay que generarlo en el Editor).</summary>
        public static bool Open() => Run(menu => menu.RunPause());

        /// <summary>Mapa del edificio directamente (tecla M).</summary>
        public static bool OpenMap() => Run(menu => menu.map.Browse(menu));

        /// <summary>Tienda de Don Chucho; <paramref name="onClosed"/> se llama al salir.</summary>
        public static bool OpenShop(Action onClosed) => Run(menu => menu.shop.Browse(menu), onClosed);

        /// <summary>
        /// Selector de ascensor; <paramref name="onChosen"/> recibe el piso elegido (o null) una vez que el
        /// menú ya se cerró, para que el viaje pueda empezar el fundido.
        /// </summary>
        public static bool OpenElevator(Action<string> onChosen)
        {
            string destination = null;
            return Run(menu => menu.map.ChooseElevatorFloor(menu, floor => destination = floor),
                () => onChosen(destination));
        }

        /// <summary>
        /// Opciones desde la pantalla de título (sin SALIR AL TÍTULO). Es una corrutina para que el título
        /// espere a que el jugador vuelva.
        /// </summary>
        public static IEnumerator RunTitleOptions()
        {
            bool done = false;
            if (!Run(menu => menu.RunOptions(false), () => done = true))
                yield break;
            while (!done)
                yield return null;
        }

        static bool Run(Func<GameMenu, IEnumerator> screen, Action onClosed = null)
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
            instance.StartCoroutine(instance.RunScreen(screen(instance), onClosed));
            return true;
        }

        IEnumerator RunScreen(IEnumerator screen, Action onClosed)
        {
            IsOpen = true;
            GameAudio.Sfx(GameAudio.MenuOpen);
            controls.FindActionMap("UI", true).Enable();
            HideAll();
            yield return screen;
            GameAudio.Sfx(GameAudio.MenuClose);

            HideAll();
            message.Clear();
            gameObject.SetActive(false);
            IsOpen = false;
            onClosed?.Invoke();
        }

        IEnumerator RunPause()
        {
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
                pauseList.Hide();
                switch (chosen)
                {
                    case PokedexOption:
                        yield return pokedex.Browse(this);
                        break;
                    case TeamOption:
                        yield return team.Browse(this);
                        break;
                    case BagOption:
                        yield return bag.Browse(this);
                        break;
                    case MapOption:
                        yield return map.Browse(this);
                        break;
                    case CardOption:
                        yield return card.Show(this);
                        break;
                    case SaveOption:
                        yield return saveScreen.Run(this);
                        break;
                    case OptionsOption:
                        bool quit = false;
                        yield return options.Run(this, true, result => quit = result == OptionsScreen.Result.QuitToTitle);
                        if (quit)
                        {
                            QuitToTitle();
                            yield break;
                        }
                        break;
                }
            }
        }

        IEnumerator RunOptions(bool inGame)
        {
            yield return options.Run(this, inGame, _ => { });
        }

        /// <summary>SALIR AL TÍTULO: cierra el menú y vuelve a la pantalla de título con fundido.</summary>
        static void QuitToTitle()
        {
            if (!SceneTransition.Go(SceneNames.MainMenu, null))
                Debug.LogWarning("No se pudo volver al título: falta la escena MainMenu en Build Profiles.");
        }

        void HideAll()
        {
            pauseList.Hide();
            actionList.Hide();
            team.Hide();
            summary.Hide();
            bag.Hide();
            pokedex.Hide();
            map.Hide();
            card.Hide();
            saveScreen.Hide();
            options.Hide();
            shop.Hide();
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
