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
    }
}
