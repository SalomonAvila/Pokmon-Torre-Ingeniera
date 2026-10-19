using System;
using System.Collections.Generic;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Especies que pueden aparecer en cada piso (Doc §3), la fuente de verdad del porcentaje del Pokédex
    /// por piso (Doc §8). Aquí solo está "qué se puede encontrar allí"; cuántos encuentros y a qué nivel se
    /// resuelven con GameRules y Building. Los ids van en minúscula, como el resto del contenido.
    ///
    /// El documento fija las 10 a 12 especies del prototipo y su reparto por piso; se reúnen aquí en un solo
    /// sitio. Los pisos sin encuentros (pasillos, lobby y terraza del piso 14) no aparecen en la tabla.
    /// </summary>
    public static class SpeciesCatalog
    {
        /// <summary>
        /// Especies por piso, con los ids de Building.Floors. Falta completar las listas con el documento:
        /// por ahora solo están los pisos del prototipo (plazoleta y piso 3).
        /// </summary>
        static readonly Dictionary<string, string[]> ByFloor = new Dictionary<string, string[]>
        {
            // Plazoleta (jardineras, niveles 2 a 5).
            // TODO(Doc §3): reemplazar por la lista real de especies de la plazoleta.
            ["1"] = new[] { "caterpie", "pidgey", "rattata", "spearow" },
            // Piso 3 (Roca): Geodude y Onix son también del equipo de la Ing. Cantera, pero también salvajes.
            // TODO(Doc §3, §8): reemplazar por la lista real de las salas-laboratorio del piso 3.
            ["3"] = new[] { "geodude", "onix", "sandshrew", "zubat" },
        };

        /// <summary>Especies que pueden aparecer en el piso; lista vacía si el piso no tiene encuentros.</summary>
        public static IReadOnlyList<string> OnFloor(string floor)
        {
            if (floor == null || !ByFloor.TryGetValue(floor, out string[] species))
                return Array.Empty<string>();
            return species;
        }

        /// <summary>¿La especie aparece en ese piso?</summary>
        public static bool AppearsOn(string speciesId, string floor)
        {
            if (floor == null || !ByFloor.TryGetValue(floor, out string[] species))
                return false;
            return Array.IndexOf(species, speciesId) >= 0;
        }

        /// <summary>Pisos con encuentros definidos, en el orden de Building.Floors.</summary>
        public static IEnumerable<string> FloorsWithEncounters
        {
            get
            {
                foreach (string floor in Building.Floors)
                    if (ByFloor.ContainsKey(floor))
                        yield return floor;
            }
        }
    }
}
