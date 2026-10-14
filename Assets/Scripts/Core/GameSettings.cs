using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Opciones del jugador, guardadas en PlayerPrefs y aplicadas al iniciar el juego.
    /// </summary>
    public static class GameSettings
    {
        public const int MaxVolume = 10;

        const string VolumeKey = "opciones.volumen";
        const string FullscreenKey = "opciones.pantallaCompleta";
        const int DefaultVolume = 8;

        public static int Volume
        {
            get => PlayerPrefs.GetInt(VolumeKey, DefaultVolume);
            set
            {
                int volume = Mathf.Clamp(value, 0, MaxVolume);
                PlayerPrefs.SetInt(VolumeKey, volume);
                ApplyVolume(volume);
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplySaved()
        {
            ApplyVolume(Volume);
            if (PlayerPrefs.HasKey(FullscreenKey))
                Screen.fullScreen = Fullscreen;
        }

        static void ApplyVolume(int volume) => AudioListener.volume = volume / (float)MaxVolume;
    }
}
