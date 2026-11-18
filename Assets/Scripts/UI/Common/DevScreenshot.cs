#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Herramienta de revisión (Etapa 9): en el Editor o en una build de desarrollo, F12 guarda una
    /// captura de la pantalla en <c>Docs/Capturas/</c> para compararla con su maqueta de
    /// <c>Docs/Maquetas/</c> a la misma escala. No existe en la build final.
    /// </summary>
    public class DevScreenshot : MonoBehaviour
    {
        const string Folder = "Docs/Capturas";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject(nameof(DevScreenshot)) { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<DevScreenshot>();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f12Key.wasPressedThisFrame)
                return;

            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder,
                $"{SceneManager.GetActiveScene().name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Captura guardada en {path}.");
        }
    }
}
#endif
