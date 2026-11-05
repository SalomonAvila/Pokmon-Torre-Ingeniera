using TMPro;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Franja de objetivo bajo la barra superior del mapa. Se oculta con el <see cref="CanvasGroup"/>
    /// cuando la escena no tiene guía, pero el objeto sigue activo para recibir los eventos de
    /// <see cref="GameState"/>. A diferencia de la flecha, no se oculta durante diálogos ni menús.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidanceHud : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;

        void Awake()
        {
            if (group == null)
                return;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        void OnEnable()
        {
            GameState.StateChanged += Refresh;
            GameState.ProgressChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameState.StateChanged -= Refresh;
            GameState.ProgressChanged -= Refresh;
        }

        void Start() => Refresh();

        void Refresh()
        {
            GuidanceInfo? info = Guidance.Current;

            if (group != null)
                group.alpha = info.HasValue ? 1f : 0f;

            if (label != null)
                label.text = Guidance.StripText(info);
        }
    }
}
