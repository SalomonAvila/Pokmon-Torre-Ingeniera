using System.IO;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Atajos para probar el juego completo desde el editor:
    /// - Jugar desde el título (Ctrl+Shift+P): entra a Play empezando por MainMenu, sin cambiar la escena abierta.
    /// - Borrar partida guardada: para volver a ver el flujo de partida nueva.
    /// El botón ▶ normal sigue probando la escena que esté abierta.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromStart
    {
        const string MainMenuPath = "Assets/Scenes/" + SceneNames.MainMenu + ".unity";

        static PlayFromStart()
        {
            // Al salir de Play se quita la escena de inicio forzada, para que ▶ vuelva a su comportamiento normal.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorSceneManager.playModeStartScene = null;
            };
        }

        [MenuItem("Torre Ingeniería/Jugar desde el título %#p")]
        static void Play()
        {
            var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            if (mainMenu == null)
            {
                EditorUtility.DisplayDialog("Jugar", "Primero construye la escena con Torre Ingeniería > Construir escena Menú Principal.", "OK");
                return;
            }

            EditorSceneManager.playModeStartScene = mainMenu;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Torre Ingeniería/Jugar desde el título %#p", true)]
        static bool CanPlay() => !EditorApplication.isPlaying;

        [MenuItem("Torre Ingeniería/Borrar partida guardada")]
        static void DeleteSave()
        {
            if (!SaveSystem.HasSave())
            {
                EditorUtility.DisplayDialog("Partida guardada", "No hay ninguna partida guardada.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Partida guardada", $"¿Borrar la partida guardada?\n{SaveSystem.SavePath}", "Borrar", "Cancelar"))
            {
                File.Delete(SaveSystem.SavePath);
                Debug.Log("Partida guardada borrada.");
            }
        }
    }
}
