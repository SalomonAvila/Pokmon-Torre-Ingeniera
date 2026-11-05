using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Enciende el borde amarillo de la jardinera inferior solo cuando la guía apunta a ella y los
    /// encuentros salvajes están activos. Vive en la capa decorativa <c>Resaltado</c> y cambia
    /// <see cref="TilemapRenderer.enabled"/>, nunca desactiva su GameObject: así sigue escuchando
    /// los cambios de la partida.
    /// </summary>
    [RequireComponent(typeof(TilemapRenderer))]
    public class GuidancePlanterHighlight : MonoBehaviour
    {
        TilemapRenderer tilemapRenderer;

        void Awake() => tilemapRenderer = GetComponent<TilemapRenderer>();

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
            if (tilemapRenderer != null)
                tilemapRenderer.enabled = Guidance.HighlightsPlanter;
        }
    }
}
