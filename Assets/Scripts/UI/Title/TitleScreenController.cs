using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Pantalla de título (pantalla 1). "PRESS START" brilla suavemente invitando a presionar Enter;
    /// al presionarlo destella y pasa a la pantalla que revisa si hay una partida guardada.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Header("Entrada")]
        [SerializeField] InputActionAsset controls;

        [Header("PRESS START")]
        [SerializeField] TMP_Text pressStartText;
        [SerializeField] Color baseColor = new Color32(0xF1, 0xCC, 0xA2, 0xFF);
        [SerializeField] Color shineColor = new Color32(0xFF, 0xF6, 0xE4, 0xFF);
        [SerializeField] Color glowColor = new Color32(0xFF, 0xC8, 0x6E, 0xFF);
        [SerializeField, Tooltip("Pulsos de brillo por segundo.")] float pulseSpeed = 0.8f;
        [SerializeField] float pulseScale = 1.06f;
        [SerializeField, Range(0f, 1f)] float maxGlow = 0.6f;

        [Header("Transiciones")]
        [SerializeField] CanvasGroup fader;
        [SerializeField] CanvasGroup saveScreen;
        [SerializeField] SaveCheckController saveCheck;
        [SerializeField] float fadeDuration = 0.5f;

        InputAction startAction;
        Material textMaterial;
        bool started;

        void Awake()
        {
            startAction = controls.FindAction("UI/Start", true);

            // Material propio del texto para animar el resplandor (Glow del shader de TextMeshPro).
            textMaterial = pressStartText.fontMaterial;
            textMaterial.EnableKeyword(ShaderUtilities.Keyword_Glow);
            textMaterial.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
            textMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, 0.6f);
            textMaterial.SetFloat(ShaderUtilities.ID_GlowInner, 0.05f);
            pressStartText.UpdateMeshPadding();

            saveScreen.alpha = 0f;
        }

        void OnEnable()
        {
            startAction.performed += OnStart;
            startAction.actionMap.Enable();
        }

        void OnDisable()
        {
            startAction.performed -= OnStart;
            startAction.actionMap.Disable();
        }

        void Start() => StartCoroutine(Fade(fader, 1f, 0f));

        void Update()
        {
            if (!started)
                SetShine((Mathf.Sin(Time.unscaledTime * pulseSpeed * 2f * Mathf.PI) + 1f) * 0.5f);
        }

        /// <summary>0 = reposo, 1 = brillo máximo.</summary>
        void SetShine(float amount)
        {
            Color color = Color.Lerp(baseColor, shineColor, amount);
            color.a = Mathf.Lerp(0.75f, 1f, amount);
            pressStartText.color = color;
            pressStartText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, amount);
            textMaterial.SetFloat(ShaderUtilities.ID_GlowPower, Mathf.Lerp(0f, maxGlow, amount));
        }

        void OnStart(InputAction.CallbackContext context)
        {
            if (started)
                return;

            started = true;
            StartCoroutine(StartSequence());
        }

        IEnumerator StartSequence()
        {
            // Destello: brillo máximo y tres parpadeos rápidos, como en los juegos de GBA.
            SetShine(1f);
            pressStartText.rectTransform.localScale = Vector3.one * (pulseScale + 0.04f);
            var flash = new WaitForSecondsRealtime(0.07f);
            for (int i = 0; i < 3; i++)
            {
                pressStartText.alpha = 0f;
                yield return flash;
                pressStartText.alpha = 1f;
                yield return flash;
            }

            yield return Fade(saveScreen, 0f, 1f);
            yield return saveCheck.Run();
        }

        IEnumerator Fade(CanvasGroup group, float from, float to)
        {
            if (group == null)
                yield break;

            group.blocksRaycasts = true;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }

            group.alpha = to;
            group.blocksRaycasts = to > 0f;
        }

        void OnDestroy()
        {
            if (textMaterial != null)
                Destroy(textMaterial);
        }
    }
}
