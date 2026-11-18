using System.Collections;
using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.Audio
{
    /// <summary>
    /// Música y efectos del prototipo (Etapa 9). Un objeto persistente que se crea solo al entrar en Play:
    /// cada escena tiene su tema (<see cref="ThemeFor"/>) y el resto del juego pide efectos por id con
    /// <see cref="Sfx"/>. Los clips se cargan de <c>Resources/Audio/Musica/</c> y
    /// <c>Resources/Audio/Efectos/</c>; si un archivo no existe, simplemente no suena (el arte de audio se
    /// puede ir sustituyendo sin tocar código). Un tema puede tener introducción (<c>&lt;tema&gt;_intro</c>):
    /// se toca una vez y el bucle entra justo al terminar, sin hueco (<c>PlayScheduled</c>). El volumen general es el de Opciones
    /// (<see cref="GameSettings.Volume"/>, aplicado a <c>AudioListener</c>).
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        // ---------- Ids de los 7 temas y los efectos (los nombres de archivo son estos ids) ----------

        public const string ThemeTitle = "titulo";
        public const string ThemeAtrium = "atrio";
        public const string ThemeLobby = "ascensores";
        public const string ThemeStairs = "escalera";
        public const string ThemeLabFloor = "piso3";
        public const string ThemeBattle = "combate";
        public const string ThemeVictory = "victoria";

        public static readonly string[] Themes =
            { ThemeTitle, ThemeAtrium, ThemeLobby, ThemeStairs, ThemeLabFloor, ThemeBattle, ThemeVictory };

        /// <summary>Los efectos que usa el juego (Doc §5: pasos, menú, daño, cura, captura, nivel, fanfarria...).</summary>
        public static readonly string[] Effects =
        {
            Cursor, Accept, Cancel, MenuOpen, MenuClose, Error, Bump, Step, Door, Elevator,
            ItemFound, TrainerAlert, Encounter, Heal, Save, Buy, Seal, Hit, HitSuper, HitWeak,
            BallThrow, BallShake, BallOut, Capture, Flee, Faint, LevelUp, Exp, StatDown, StatUp,
        };

        public const string Cursor = "cursor";
        public const string Accept = "aceptar";
        public const string Cancel = "cancelar";
        public const string MenuOpen = "abrir_menu";
        public const string MenuClose = "cerrar_menu";
        public const string Error = "error";
        public const string Bump = "choque";
        public const string Step = "paso";
        public const string Door = "puerta";
        public const string Elevator = "ascensor";
        public const string ItemFound = "objeto";
        public const string TrainerAlert = "alerta";
        public const string Encounter = "encuentro";
        public const string Heal = "curar";
        public const string Save = "guardar";
        public const string Buy = "compra";
        public const string Seal = "sello";
        public const string Hit = "golpe";
        public const string HitSuper = "golpe_eficaz";
        public const string HitWeak = "golpe_poco";
        public const string BallThrow = "pokebola_lanzar";
        public const string BallShake = "pokebola_sacudida";
        public const string BallOut = "pokebola_salir";
        public const string Capture = "captura";
        public const string Flee = "huida";
        public const string Faint = "desmayo";
        public const string LevelUp = "subir_nivel";
        public const string Exp = "exp";
        public const string StatDown = "baja_stat";
        public const string StatUp = "sube_stat";

        const string MusicFolder = "Audio/Musica/";
        const string EffectsFolder = "Audio/Efectos/";
        const float MusicVolume = 0.55f;
        const float FadeTime = 0.4f;

        /// <summary>Volumen relativo de los efectos muy frecuentes (pasos).</summary>
        static readonly Dictionary<string, float> EffectVolume = new Dictionary<string, float>
        {
            [Step] = 0.25f,
            [Cursor] = 0.6f,
            [Bump] = 0.7f,
        };

        static GameAudio instance;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        AudioSource music;
        AudioSource intro;
        AudioSource effects;
        string currentTheme;
        Coroutine fade;
        float lastBump;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject(nameof(GameAudio));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
        }

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = MusicVolume;
            intro = gameObject.AddComponent<AudioSource>();
            intro.playOnAwake = false;
            intro.volume = MusicVolume;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnDestroy() => SceneManager.activeSceneChanged -= OnSceneChanged;

        void Start() => PlayTheme(ThemeFor(SceneManager.GetActiveScene().name));

        void OnSceneChanged(Scene previous, Scene next) => PlayTheme(ThemeFor(next.name));

        /// <summary>Tema de cada escena. Los pisos de laboratorio usan el del piso 3 hasta tener el suyo.</summary>
        public static string ThemeFor(string scene)
        {
            switch (scene)
            {
                case SceneNames.MainMenu:
                case SceneNames.Intro:
                    return ThemeTitle;
                case SceneNames.Atrio: return ThemeAtrium;
                case SceneNames.Ascensores: return ThemeLobby;
                case SceneNames.Escalera: return ThemeStairs;
                case SceneNames.Battle: return ThemeBattle;
            }
            if (scene != null && scene.StartsWith("Piso"))
            {
                string own = "piso" + scene.Substring(4).ToLowerInvariant();
                return Resources.Load<AudioClip>(MusicFolder + own) != null ? own : ThemeLabFloor;
            }
            return null;
        }

        // ---------- API ----------

        /// <summary>Reproduce un efecto por id (no hace nada si falta el archivo).</summary>
        public static void Sfx(string id)
        {
            if (instance == null || string.IsNullOrEmpty(id))
                return;
            instance.PlayEffect(id);
        }

        /// <summary>
        /// Cambia el tema (con un fundido corto). <paramref name="loop"/> = false para fanfarrias como la de
        /// victoria; al terminar, la música queda en silencio hasta el siguiente cambio de escena.
        /// </summary>
        public static void Music(string theme, bool loop = true)
        {
            if (instance != null)
                instance.PlayTheme(theme, loop);
        }

        // ---------- Interno ----------

        void PlayEffect(string id)
        {
            // Chocar contra una pared repite el evento cada fotograma que se empuja: un golpe cada 0,25 s.
            if (id == Bump)
            {
                if (Time.unscaledTime - lastBump < 0.25f)
                    return;
                lastBump = Time.unscaledTime;
            }

            AudioClip clip = Load(EffectsFolder + id);
            if (clip != null)
                effects.PlayOneShot(clip, EffectVolume.TryGetValue(id, out float volume) ? volume : 1f);
        }

        void PlayTheme(string theme, bool loop = true)
        {
            if (theme == currentTheme && (music.isPlaying || intro.isPlaying))
                return;
            currentTheme = theme;

            AudioClip clip = string.IsNullOrEmpty(theme) ? null : Load(MusicFolder + theme);
            AudioClip introClip = string.IsNullOrEmpty(theme) ? null : Load(MusicFolder + theme + "_intro");
            if (fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(SwitchTo(clip, introClip, loop));
        }

        IEnumerator SwitchTo(AudioClip clip, AudioClip introClip, bool loop)
        {
            if (music.isPlaying || intro.isPlaying)
            {
                for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
                {
                    float volume = Mathf.Lerp(MusicVolume, 0f, t / FadeTime);
                    music.volume = volume;
                    intro.volume = volume;
                    yield return null;
                }
                music.Stop();
                intro.Stop();
            }

            music.volume = MusicVolume;
            intro.volume = MusicVolume;
            if (clip == null)
                yield break;
            music.clip = clip;
            music.loop = loop;

            if (introClip == null)
            {
                music.Play();
                yield break;
            }

            // La intro suena una vez y el bucle entra en el instante exacto en que termina.
            double startAt = AudioSettings.dspTime + 0.1;
            intro.clip = introClip;
            intro.PlayScheduled(startAt);
            music.PlayScheduled(startAt + (double)introClip.samples / introClip.frequency);
        }

        AudioClip Load(string path)
        {
            if (cache.TryGetValue(path, out AudioClip clip))
                return clip;
            clip = Resources.Load<AudioClip>(path);
            cache[path] = clip; // también se recuerdan los que faltan, para no buscarlos cada vez
            return clip;
        }
    }
}
