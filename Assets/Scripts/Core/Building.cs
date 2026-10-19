using System;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Esquina de un mapa. En los pisos con reto, la escalera de subida y la entrada por la que se llega
    /// están en SE o NO según la posición en la secuencia (Doc §2).
    /// </summary>
    public enum Corner { NorthWest, NorthEast, SouthWest, SouthEast }

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

        // ---------- Secuencia de retos (Doc §2) ----------

        /// <summary>
        /// Posición en la secuencia de retos (k): piso 3 = 0, 4 = 1, 5 = 2, 6 = 3, 7 = 4, PT = 5,
        /// 8 = 6, 9 = 7, 10 = 8, 11 = 9, 12 = 10, 13 = 11 y 14 = 12.
        /// Los pisos sin reto (1 y 2) y cualquier piso desconocido devuelven -1.
        /// </summary>
        public static int SequenceIndex(string floor)
        {
            int index = Array.IndexOf(Floors, floor);
            return index < 0 || !HasChallenge(floor) ? -1 : index - FloorsWithoutChallenge.Length;
        }

        /// <summary>Esquina por la que se entra al piso: SE si k es par y NO si es impar.</summary>
        public static Corner EntryCorner(string floor) =>
            ChallengeIndex(floor) % 2 == 0 ? Corner.SouthEast : Corner.NorthWest;

        /// <summary>
        /// Esquina que custodia el jefe: la opuesta a la de entrada (Doc §2). El documento pone la cinta de
        /// precaución en la otra escalera (la de entrada), cerrada de forma permanente; el prototipo sirve todos
        /// los tramos con una sola escena <c>Escalera</c> y reutiliza la cinta para cerrar la subida hasta
        /// completar el piso (ver <c>StairwellController</c>).
        /// </summary>
        public static Corner GuardedCorner(string floor) => Opposite(EntryCorner(floor));

        // ---------- Dificultad por piso (los números viven en GameRules) ----------

        /// <summary>Rango de niveles salvajes del piso; los pisos sin reto usan el de la plazoleta.</summary>
        public static (int min, int max) WildLevelRange(string floor)
        {
            int k = SequenceIndex(floor);
            return k < 0
                ? (GameRules.PlazaWildMinLevel, GameRules.PlazaWildMaxLevel)
                : (GameRules.WildMinLevel(k), GameRules.WildMaxLevel(k));
        }

        /// <summary>Pokémon del equipo del jefe del piso.</summary>
        public static int BossTeamSize(string floor) => GameRules.BossTeamSize(ChallengeIndex(floor));

        /// <summary>Nivel del jefe del piso (el salvaje máximo del piso + 4).</summary>
        public static int BossLevel(string floor) => GameRules.BossLevel(ChallengeIndex(floor));

        /// <summary>k del piso, exigiendo que tenga reto; en los demás pisa fuerte para no inventarse reglas.</summary>
        static int ChallengeIndex(string floor)
        {
            int k = SequenceIndex(floor);
            if (k < 0)
                throw new InvalidOperationException($"El piso '{floor}' no tiene reto, así que no tiene entrada por escalera ni jefe.");
            return k;
        }

        static Corner Opposite(Corner corner) => corner switch
        {
            Corner.NorthWest => Corner.SouthEast,
            Corner.SouthEast => Corner.NorthWest,
            Corner.NorthEast => Corner.SouthWest,
            _ => Corner.NorthEast,
        };
    }
}
