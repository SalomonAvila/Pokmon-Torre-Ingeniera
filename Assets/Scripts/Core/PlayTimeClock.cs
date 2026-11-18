using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Cuenta el tiempo de juego del Carné y de la pantalla de Guardar (v7). Vive en un objeto persistente
    /// que se crea solo al entrar en Play y suma el tiempo real mientras hay partida cargada y la escena no
    /// es el título ni la introducción. No guarda: el tiempo viaja con el siguiente guardado.
    /// </summary>
    public class PlayTimeClock : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject(nameof(PlayTimeClock));
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<PlayTimeClock>();
        }

        void Update()
        {
            if (!GameState.IsLoaded)
                return;

            string scene = SceneManager.GetActiveScene().name;
            if (scene == SceneNames.MainMenu || scene == SceneNames.Intro)
                return;

            GameState.AddPlayTime(Time.unscaledDeltaTime);
        }
    }
}
