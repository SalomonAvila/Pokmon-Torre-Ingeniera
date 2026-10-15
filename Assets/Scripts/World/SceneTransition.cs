using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Cambio de escena con fundido a negro. Guarda el SpawnPoint donde debe aparecer el jugador
    /// en la escena nueva (PlayerController lo lee al iniciar).
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        const float FadeDuration = 0.25f;

        public static string PendingSpawn { get; private set; }
        public static bool IsTransitioning { get; private set; }

        CanvasGroup fader;

        // El proyecto entra a Play sin recargar el dominio: los valores estáticos se reinician a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PendingSpawn = null;
            IsTransitioning = false;
        }

        public static void Go(string sceneName, string spawnId)
        {
            if (IsTransitioning)
                return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"La escena '{sceneName}' no existe o no está en Build Profiles.");
                return;
            }

            var runner = new GameObject("SceneTransition").AddComponent<SceneTransition>();
            DontDestroyOnLoad(runner.gameObject);
            runner.StartCoroutine(runner.Run(sceneName, spawnId));
        }

        /// <summary>El jugador llama esto al aparecer para no volver a usar el mismo destino.</summary>
        public static string ConsumePendingSpawn()
        {
            string spawn = PendingSpawn;
            PendingSpawn = null;
            return spawn;
        }

        IEnumerator Run(string sceneName, string spawnId)
        {
            IsTransitioning = true;
            CreateFader();

            yield return Fade(0f, 1f);
            PendingSpawn = spawnId;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null; // deja que el jugador se ubique en su SpawnPoint
            yield return Fade(1f, 0f);

            IsTransitioning = false;
            Destroy(gameObject);
        }

        void CreateFader()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var image = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(transform, false);
            image.color = Color.black;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            fader = gameObject.AddComponent<CanvasGroup>();
            fader.alpha = 0f;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                fader.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }
            fader.alpha = to;
        }
    }
}
