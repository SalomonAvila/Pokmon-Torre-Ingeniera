using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Opciones del jugador, guardadas en PlayerPrefs y aplicadas al iniciar el juego.
    /// </summary>
    public static class GameSettings
    {
        /// <summary>Velocidad de la máquina de escribir de los cuadros de texto.</summary>
        public enum TextSpeed { Slow, Normal, Fast }

        public static readonly string[] TextSpeedNames = { "LENTA", "NORMAL", "RÁPIDA" };

        public const int MaxVolume = 10;

        const string VolumeKey = "opciones.volumen";
        const string FullscreenKey = "opciones.pantallaCompleta";
        const string TextSpeedKey = "opciones.velocidadTexto";
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

        public static TextSpeed Speed
        {
            get => (TextSpeed)Mathf.Clamp(PlayerPrefs.GetInt(TextSpeedKey, (int)TextSpeed.Normal), 0, 2);
            set => PlayerPrefs.SetInt(TextSpeedKey, (int)value);
        }

        /// <summary>
        /// Factor sobre las letras por segundo del cuadro de texto (30 en NORMAL). (propuesta) LENTA ≈ 18,
        /// RÁPIDA ≈ 75 letras por segundo.
        /// </summary>
        public static float TextSpeedMultiplier => Speed switch
        {
            TextSpeed.Slow => 0.6f,
            TextSpeed.Fast => 2.5f,
            _ => 1f,
        };

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
