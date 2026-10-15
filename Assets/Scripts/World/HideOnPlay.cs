using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Oculta el Renderer de este objeto al jugar. Se usa en las capas de ayuda del mapa
    /// (Colisiones, Encuentros), que deben verse al pintar en el editor pero no en el juego.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class HideOnPlay : MonoBehaviour
    {
        void Awake() => GetComponent<Renderer>().enabled = false;
    }
}
