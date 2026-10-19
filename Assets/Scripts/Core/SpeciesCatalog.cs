using System;
using System.Collections.Generic;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Especies que pueden aparecer en cada piso (Doc §3), la fuente de verdad del porcentaje del Pokédex
    /// por piso (Doc §8). Aquí solo está "qué se puede encontrar allí"; cuántos encuentros y a qué nivel se
    /// resuelven con GameRules y Building. Los ids van en minúscula, como el resto del contenido.
    ///
    /// El documento fija las especies del prototipo y su reparto por piso; se reúnen aquí en un solo sitio.
    /// Los pisos sin encuentros (pasillos, lobby y terraza del piso 14) no aparecen en la tabla. Los iniciales
    /// se listan aparte en <see cref="Starters"/>: se eligen, no se encuentran salvajes.
    /// </summary>
    public static class SpeciesCatalog
    {
        /// <summary>
        /// Especies por piso, con los ids de Building.Floors (Doc §3).
        /// </summary>
        static readonly Dictionary<string, string[]> ByFloor = new Dictionary<string, string[]>
        {
            // Plazoleta (jardineras, niveles 2 a 5). Eevee es raro (Doc §3).
            ["1"] = new[] { "rattata", "pidgey", "meowth", "eevee" },
            // Piso 3 (Roca). Geodude y Onix son también del equipo de la Ing. Cantera (Doc §8, §10).
            ["3"] = new[] { "geodude", "rhyhorn", "onix", "kabuto", "omanyte" },
        };

        /// <summary>
        /// Iniciales que el jugador elige en la plazoleta (Doc §3). No son encuentros salvajes: no cuentan
        /// para el porcentaje del Pokédex por piso, pero sí como especies conocidas.
        /// </summary>
        public static readonly string[] Starters = { "bulbasaur", "charmander", "squirtle" };

        /// <summary>¿La especie es uno de los iniciales?</summary>
        public static bool IsStarter(string speciesId) => Array.IndexOf(Starters, speciesId) >= 0;

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
