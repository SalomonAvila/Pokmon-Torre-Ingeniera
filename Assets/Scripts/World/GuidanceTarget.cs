using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Marca invisible de un destino de la guía (Gabriel, la jardinera, la puerta del edificio o la
    /// escalera del piso 3). Los builders la colocan con un id estable de <c>Guidance</c> y la flecha
    /// de borde usa su posición. No tiene colisión ni dibujo.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidanceTarget : MonoBehaviour
    {
        static readonly Dictionary<string, GuidanceTarget> All = new Dictionary<string, GuidanceTarget>();

        [SerializeField, Tooltip("Id estable de Guidance (gabriel_atrio, jardinera_inferior...).")]
        string id;

        public string Id => id;

        // El proyecto entra a Play sin recargar el dominio: el registro se reinicia a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        void OnEnable()
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (All.TryGetValue(id, out GuidanceTarget other) && other != null && other != this)
            {
                Debug.LogWarning($"Hay más de un GuidanceTarget con id '{id}'; se conserva el primero.");
                return;
            }

            All[id] = this;
        }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(id) && All.TryGetValue(id, out GuidanceTarget target) && target == this)
                All.Remove(id);
        }

        /// <summary>Busca el destino registrado por su id, o null si no está en la escena.</summary>
        public static GuidanceTarget Find(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return null;
            return All.TryGetValue(targetId, out GuidanceTarget target) ? target : null;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
#endif
    }
}
