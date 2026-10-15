using System;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Estructura del edificio, de abajo hacia arriba: 1 (atrio), 2 (ascensores), 3 a 7, PT (piso técnico), 8 a 14.
    /// Las escaleras siempre conectan un piso con el siguiente de esta lista.
    /// </summary>
    public static class Building
    {
        public static readonly string[] Floors = { "1", "2", "3", "4", "5", "6", "7", "PT", "8", "9", "10", "11", "12", "13", "14" };

        /// <summary>Pisos sin reto (atrio y ascensores): su escalera de subida siempre está abierta.</summary>
        public static readonly string[] FloorsWithoutChallenge = { "1", "2" };

        public static bool HasChallenge(string floor) => Array.IndexOf(FloorsWithoutChallenge, floor) < 0;

        /// <summary>SpawnPoint de cada piso al llegar subiendo por la escalera (desde el piso de abajo).</summary>
        public const string SpawnFromStairsBelow = "DesdeEscaleraAbajo";

        /// <summary>SpawnPoint de cada piso al llegar bajando por la escalera (desde el piso de arriba).</summary>
        public const string SpawnFromStairsAbove = "DesdeEscaleraArriba";

        public static bool IsValid(string floor) => Array.IndexOf(Floors, floor) >= 0;

        /// <summary>Piso de arriba, o null si es el último.</summary>
        public static string Next(string floor)
        {
            int index = Array.IndexOf(Floors, floor);
            return index >= 0 && index < Floors.Length - 1 ? Floors[index + 1] : null;
        }

        /// <summary>Piso de abajo, o null si es el primero.</summary>
        public static string Previous(string floor)
        {
            int index = Array.IndexOf(Floors, floor);
            return index > 0 ? Floors[index - 1] : null;
        }

        public static string DisplayName(string floor) => floor == "PT" ? "PISO TÉCNICO" : $"PISO {floor}";

        /// <summary>Escena de cada piso (los pisos 3 en adelante se llamarán Piso3, Piso4, ..., PisoPT).</summary>
        public static string SceneFor(string floor) => floor switch
        {
            "1" => SceneNames.Atrio,
            "2" => SceneNames.Ascensores,
            _ => $"Piso{floor}",
        };
    }
}
