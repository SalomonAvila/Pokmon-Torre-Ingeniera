namespace TorreIngenieria.Core
{
    /// <summary>
    /// Tipos de Pokémon de la tercera generación (17 tipos). El documento de diseño solo dice
    /// "según la tabla de tipos de la franquicia"; se fija Gen 3 porque los stats base y la tabla
    /// de efectividad del prototipo salen de esa generación. Incluye Tierra aunque no esté en la
    /// paleta del documento: Geodude, Onix y Rhyhorn son Roca/Tierra.
    /// Los ids coinciden con los nombres en inglés de la franquicia para no traducir datos.
    /// </summary>
    public enum PokemonType
    {
        Normal,
        Fighting,
        Flying,
        Poison,
        Ground,
        Rock,
        Bug,
        Ghost,
        Steel,
        Fire,
        Water,
        Grass,
        Electric,
        Psychic,
        Ice,
        Dragon,
        Dark,
    }

    /// <summary>Nombres de los tipos en español para los mensajes al jugador.</summary>
    public static class PokemonTypeNames
    {
        public static string DisplayName(PokemonType type) => type switch
        {
            PokemonType.Normal => "Normal",
            PokemonType.Fighting => "Lucha",
            PokemonType.Flying => "Volador",
            PokemonType.Poison => "Veneno",
            PokemonType.Ground => "Tierra",
            PokemonType.Rock => "Roca",
            PokemonType.Bug => "Bicho",
            PokemonType.Ghost => "Fantasma",
            PokemonType.Steel => "Acero",
            PokemonType.Fire => "Fuego",
            PokemonType.Water => "Agua",
            PokemonType.Grass => "Planta",
            PokemonType.Electric => "Eléctrico",
            PokemonType.Psychic => "Psíquico",
            PokemonType.Ice => "Hielo",
            PokemonType.Dragon => "Dragón",
            _ => "Siniestro",
        };

        /// <summary>
        /// Nombre en mayúsculas sin vocales acentuadas para la etiqueta de tipo. La fuente pixel
        /// dibuja las mayúsculas acentuadas con un glifo pequeño, así que la etiqueta usa ELECTRICO,
        /// PSIQUICO y DRAGON en ASCII.
        /// </summary>
        public static string BadgeName(PokemonType type) => type switch
        {
            PokemonType.Electric => "ELECTRICO",
            PokemonType.Psychic => "PSIQUICO",
            PokemonType.Dragon => "DRAGON",
            _ => DisplayName(type).ToUpperInvariant(),
        };
    }

    /// <summary>
    /// Colores de la paleta de tipos del Doc §5. Tierra, Hielo y Siniestro no aparecen en el documento,
    /// así que su tono es (propuesta) para completar los 17 tipos de Gen 3.
    /// El texto de la etiqueta se elige por contraste: oscuro sobre fondos claros y blanco sobre los
    /// pocos fondos oscuros (Lucha, Veneno, Fantasma, Dragón y Siniestro).
    /// </summary>
    public static class PokemonTypeColors
    {
        public const string Normal = "#A8A878";
        public const string Fighting = "#C03028";
        public const string Flying = "#A890F0";
        public const string Poison = "#A040A0";
        public const string Ground = "#E0C068"; // (propuesta)
        public const string Rock = "#B8A038";
        public const string Bug = "#A8B820";
        public const string Ghost = "#705898";
        public const string Steel = "#B8B8D0";
        public const string Fire = "#F08030";
        public const string Water = "#6890F0";
        public const string Grass = "#78C850";
        public const string Electric = "#F8D030";
        public const string Psychic = "#F85888";
        public const string Ice = "#98D8D8"; // (propuesta)
        public const string Dragon = "#7038F8";
        public const string Dark = "#705848"; // (propuesta)

        public static string Hex(PokemonType type) => type switch
        {
            PokemonType.Normal => Normal,
            PokemonType.Fighting => Fighting,
            PokemonType.Flying => Flying,
            PokemonType.Poison => Poison,
            PokemonType.Ground => Ground,
            PokemonType.Rock => Rock,
            PokemonType.Bug => Bug,
            PokemonType.Ghost => Ghost,
            PokemonType.Steel => Steel,
            PokemonType.Fire => Fire,
            PokemonType.Water => Water,
            PokemonType.Grass => Grass,
            PokemonType.Electric => Electric,
            PokemonType.Psychic => Psychic,
            PokemonType.Ice => Ice,
            PokemonType.Dragon => Dragon,
            _ => Dark,
        };

        /// <summary>Color del texto de la etiqueta (hex) para que contraste con <see cref="Hex"/>.</summary>
        public static string BadgeText(PokemonType type) => Luminance(Hex(type)) < 0.55f ? "#FFFFFF" : "#101010";

        /// <summary>Luminancia perceptual aproximada de un color "#RRGGBB".</summary>
        static float Luminance(string hex)
        {
            float r = Channel(hex, 1);
            float g = Channel(hex, 3);
            float b = Channel(hex, 5);
            return 0.299f * r + 0.587f * g + 0.114f * b;
        }

        static float Channel(string hex, int start) =>
            (HexDigit(hex[start]) * 16 + HexDigit(hex[start + 1])) / 255f;

        static int HexDigit(char value) => value switch
        {
            >= '0' and <= '9' => value - '0',
            >= 'A' and <= 'F' => value - 'A' + 10,
            >= 'a' and <= 'f' => value - 'a' + 10,
            _ => 0,
        };
    }
}
