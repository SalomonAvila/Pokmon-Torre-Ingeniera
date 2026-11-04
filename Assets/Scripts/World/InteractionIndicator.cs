using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Burbuja que marca a un NPC o entrenador con diálogo nuevo. La crea <c>ProvisionalMap</c>
    /// como hija del personaje y <see cref="Interactable"/> la muestra u oculta. Cuando el jugador
    /// está cerca se ve atenuada; cuando el NPC está en la casilla de enfrente, rebota un píxel.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InteractionIndicator : MonoBehaviour
    {
        [SerializeField, Tooltip("Píxeles que sube y baja la burbuja cuando el NPC es el objetivo.")]
        float bobPixels = 1f;
        [SerializeField, Tooltip("Ciclos por segundo del pequeño rebote.")]
        float bobSpeed = 1.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacidad cuando el NPC está cerca pero no es el objetivo.")]
        float nearbyAlpha = 0.55f;
        [SerializeField, Range(0f, 1f), Tooltip("Opacidad cuando el NPC está en la casilla de enfrente.")]
        float readyAlpha = 1f;

        SpriteRenderer spriteRenderer;
        Vector3 basePosition;
        bool ready;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            basePosition = transform.localPosition;

            // Por si una escena antigua no trae serializados los campos nuevos.
            if (nearbyAlpha <= 0f)
                nearbyAlpha = 0.55f;
            if (readyAlpha <= 0f)
                readyAlpha = 1f;

            ApplyAlpha();
        }

        void OnEnable()
        {
            transform.localPosition = basePosition;
            ApplyAlpha();
        }

        void Update()
        {
            if (!ready || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
            if (pixelsPerUnit <= 0f)
                return;

            float offset = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * bobSpeed) * (bobPixels / pixelsPerUnit);
            transform.localPosition = basePosition + Vector3.up * offset;
        }

        /// <summary>Muestra u oculta la burbuja. Al mostrarla se reinicia el rebote.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
                return;
            gameObject.SetActive(visible);
        }

        /// <summary>Activa el rebote y la opacidad completa cuando el NPC es el objetivo actual.</summary>
        public void SetReady(bool value)
        {
            if (ready == value)
                return;

            ready = value;
            if (!ready)
                transform.localPosition = basePosition;
            ApplyAlpha();
        }

        void ApplyAlpha()
        {
            if (spriteRenderer == null)
                return;

            Color color = spriteRenderer.color;
            color.a = ready ? readyAlpha : nearbyAlpha;
            spriteRenderer.color = color;
        }
    }
}
