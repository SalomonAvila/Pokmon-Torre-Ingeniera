using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Rectángulo de casillas con su propia tabla de especies (un laboratorio del piso 3: Concretos saca
    /// Geodude y Rhyhorn, Suelos Geodude y Onix...). <see cref="EncounterZone"/> lo consulta al pisar una
    /// casilla de encuentro; fuera de cualquier área usa la lista del piso de <c>SpeciesCatalog</c>.
    /// El área va desde la casilla de este objeto hacia la derecha y hacia arriba, como <see cref="WarpZone"/>.
    /// </summary>
    public class EncounterArea : MonoBehaviour
    {
        static readonly List<EncounterArea> All = new List<EncounterArea>();

        [SerializeField, Tooltip("Ancho y alto del área, en casillas.")] Vector2Int size = Vector2Int.one;
        [SerializeField, Tooltip("Ids de especie que pueden salir aquí (SpeciesCatalog del piso).")]
        string[] species;

        public IReadOnlyList<string> Species => species;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Área que contiene la casilla, o null.</summary>
        public static EncounterArea At(Vector3 position)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            return All.Find(area => area.Contains(cell));
        }

        bool Contains(Vector2Int cell)
        {
            var origin = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            return new RectInt(origin, size).Contains(cell);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.25f);
            var origin = new Vector3(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y), 0);
            Gizmos.DrawCube(origin + new Vector3(size.x, size.y) / 2f, new Vector3(size.x, size.y, 0.1f));
        }
    }
}
