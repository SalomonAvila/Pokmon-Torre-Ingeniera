using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Flecha de borde que señala el destino de la guía cuando queda fuera de cámara. Usa ocho
    /// sprites (arriba, arriba-derecha, derecha... en sentido horario) y se coloca en el borde de
    /// un rectángulo seguro para no pisar la barra ni la franja de objetivo.
    ///
    /// Se oculta durante diálogos, elecciones, menú, fundidos y combate (el combate es otra escena,
    /// así que allí no existe este objeto), y cuando el destino ya se ve en pantalla.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidanceArrow : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Sprite[] directions = new Sprite[8];

        [Header("Bordes de la pantalla (unidades del canvas)")]
        [SerializeField, Min(0f), Tooltip("Margen del centro de la flecha en los laterales y abajo.")]
        float edgeMargin = 48f;
        [SerializeField, Min(0f), Tooltip("Margen superior: barra del HUD + franja + media flecha.")]
        float topMargin = 112f;
        [SerializeField, Range(0f, 0.2f), Tooltip("Margen para considerar que el destino ya está en cámara.")]
        float onScreenMargin = 0.03f;
        [SerializeField, Min(0f), Tooltip("Si el jugador está casi encima del destino, la flecha no aporta.")]
        float minDistance = 1f;

        PlayerController player;
        RectTransform canvasRect;
        string missingTargetId;

        void Awake()
        {
            if (image == null)
                image = GetComponent<Image>();

            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        }

        void LateUpdate()
        {
            if (image == null)
                return;

            if (WorldMessage.IsOpen || DialogueBox.IsOpen || ChoiceBox.IsChoosing || GameMenu.IsOpen ||
                FadeManager.IsFading || SceneTransition.IsTransitioning)
            {
                Hide();
                return;
            }

            GuidanceInfo? info = Guidance.Current;
            if (!info.HasValue)
            {
                Hide();
                return;
            }

            GuidanceTarget target = GuidanceTarget.Find(info.Value.TargetId);
            PlayerController currentPlayer = Player;
            Camera camera = Camera.main;
            if (target == null || currentPlayer == null || camera == null || canvasRect == null)
            {
                if (target == null && missingTargetId != info.Value.TargetId)
                {
                    Debug.LogWarning($"Guía: no hay un GuidanceTarget con id '{info.Value.TargetId}' en la escena.");
                    missingTargetId = info.Value.TargetId;
                }
                Hide();
                return;
            }
            missingTargetId = null;

            if (directions == null || directions.Length < 8)
            {
                Hide();
                return;
            }

            Vector3 targetViewport = camera.WorldToViewportPoint(target.transform.position);
            if (targetViewport.z <= 0f || IsOnScreen(targetViewport))
            {
                Hide();
                return;
            }

            Vector3 playerViewport = camera.WorldToViewportPoint(currentPlayer.transform.position);
            Vector2 size = canvasRect.rect.size;
            Vector2 playerPoint = new Vector2((playerViewport.x - 0.5f) * size.x,
                (playerViewport.y - 0.5f) * size.y);
            Vector2 targetPoint = new Vector2((targetViewport.x - 0.5f) * size.x,
                (targetViewport.y - 0.5f) * size.y);

            // La dirección del sprite sale del vector jugador → destino; la posición, del centro
            // de la pantalla hacia el destino, para que el rayo siempre corte el borde.
            Vector2 direction = targetPoint - playerPoint;
            if (direction.sqrMagnitude < minDistance * minDistance)
            {
                Hide();
                return;
            }

            Vector2 half = size * 0.5f;
            float left = -half.x + edgeMargin;
            float right = half.x - edgeMargin;
            float bottom = -half.y + edgeMargin;
            float top = half.y - topMargin;

            float tx = Mathf.Abs(targetPoint.x) > 0.001f ? right / Mathf.Abs(targetPoint.x) : float.PositiveInfinity;
            float ty = Mathf.Abs(targetPoint.y) > 0.001f ? top / Mathf.Abs(targetPoint.y) : float.PositiveInfinity;
            float t = Mathf.Min(tx, ty);
            if (float.IsInfinity(t) || t <= 0f)
            {
                Hide();
                return;
            }

            Vector2 position = targetPoint * t;
            position.x = Mathf.Clamp(position.x, left, right);
            position.y = Mathf.Clamp(position.y, bottom, top);

            image.rectTransform.anchoredPosition = position;
            image.sprite = directions[DirectionIndex(direction)];
            image.enabled = true;
        }

        bool IsOnScreen(Vector3 viewport) =>
            viewport.x >= onScreenMargin && viewport.x <= 1f - onScreenMargin &&
            viewport.y >= onScreenMargin && viewport.y <= 1f - onScreenMargin;

        PlayerController Player
        {
            get
            {
                if (player == null)
                    player = FindAnyObjectByType<PlayerController>();
                return player;
            }
        }

        void Hide()
        {
            if (image != null)
                image.enabled = false;
        }

        /// <summary>0 arriba, 1 arriba-derecha, 2 derecha... 7 arriba-izquierda.</summary>
        static int DirectionIndex(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt((90f - angle) / 45f) % 8;
            return index < 0 ? index + 8 : index;
        }
    }
}
