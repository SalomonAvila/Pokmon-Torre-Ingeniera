using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Fundido a negro único y persistente entre escenas. Todas las transiciones del juego pasan por aquí
    /// (WarpZone, StairwellController y el paso del título al mundo). Mientras dura el fundido o la pantalla
    /// está negra, <see cref="IsFading"/> es true y el jugador no se mueve.
    ///
    /// Vive en <c>DontDestroyOnLoad</c> y se crea solo la primera vez que se necesita. El Canvas del fundido
    /// tapa cualquier UI y bloquea los raycasts del puntero.
    /// </summary>
    public class FadeManager : MonoBehaviour
    {
        const int SortingOrder = 30000;
        const float DefaultDuration = 0.25f;

        public static FadeManager Instance { get; private set; }

        /// <summary>Hay un fundido en curso o la pantalla está en negro: hay que bloquear el input.</summary>
        public static bool IsFading => Instance != null && Instance.busy;

        [SerializeField, Tooltip("Duración del fundido, en segundos (tiempo sin escalar).")]
        float duration = DefaultDuration;

        CanvasGroup group;
        bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        /// <summary>Crea el fundido si aún no existe y lo conserva entre escenas.</summary>
        public static FadeManager Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("FadeManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<FadeManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            // Necesario para que el negro bloquee los clics mientras está visible.
            gameObject.AddComponent<GraphicRaycaster>();

            var image = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(transform, false);
            image.color = Color.black;
            image.raycastTarget = true;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        /// <summary>Pone la pantalla en negro (o la aclara) de inmediato, sin fundido.</summary>
        public void SetBlack(bool black)
        {
            group.alpha = black ? 1f : 0f;
            group.blocksRaycasts = black;
            busy = black;
        }

        /// <summary>Fundido a negro. Al terminar, la pantalla queda negra y el input bloqueado.</summary>
        public IEnumerator FadeToBlack(float seconds = -1f)
        {
            busy = true;
            group.blocksRaycasts = true;
            yield return FadeRoutine(1f, seconds);
        }

        /// <summary>Fundido desde negro. Al terminar, libera el input.</summary>
        public IEnumerator FadeFromBlack(float seconds = -1f)
        {
            yield return FadeRoutine(0f, seconds);
            busy = false;
            group.blocksRaycasts = false;
        }

        /// <summary>Funde a negro, carga la escena y vuelve a aclarar. Bloquea el input todo el tiempo.</summary>
        public void FadeAndLoad(string sceneName, string spawnId)
        {
            if (busy)
                return;

            StartCoroutine(FadeAndLoadRoutine(sceneName, spawnId));
        }

        IEnumerator FadeAndLoadRoutine(string sceneName, string spawnId)
        {
            busy = true;
            group.blocksRaycasts = true;

            yield return FadeRoutine(1f, -1f);
            SceneTransition.SetPendingSpawn(spawnId);

            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null; // deja que el jugador se ubique en su SpawnPoint

            yield return FadeRoutine(0f, -1f);

            busy = false;
            group.blocksRaycasts = false;
        }

        IEnumerator FadeRoutine(float target, float seconds)
        {
            float time = seconds < 0f ? duration : seconds;
            float start = group.alpha;

            if (time <= 0f)
            {
                group.alpha = target;
                yield break;
            }

            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / time);
                yield return null;
            }

            group.alpha = target;
        }
    }
}
