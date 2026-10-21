namespace TorreIngenieria.Core
{
    /// <summary>
    /// Tabla de efectividad de tipos de la franquicia (Gen 3): 17 tipos, matriz fija.
    /// Atacante en la fila y defensor en la columna, en el orden de <see cref="PokemonType"/>.
    /// 0 = sin efecto, 0,5 = poco eficaz, 1 = normal, 2 = supereficaz. No tiene estado mutable.
    ///
    /// Se usa la tabla completa y no solo los tipos del prototipo para no ampliarla después.
    /// </summary>
    public static class TypeChart
    {
        /// <summary>Valores válidos de una celda de la matriz.</summary>
        public static readonly float[] AllowedValues = { 0f, 0.5f, 1f, 2f };

        static readonly float[,] Matrix = Build();

        /// <summary>Multiplicador de un tipo atacante contra un defensor de un solo tipo.</summary>
        public static float Effectiveness(PokemonType attack, PokemonType defend) =>
            Matrix[(int)attack, (int)defend];

        /// <summary>
        /// Multiplicador contra un defensor con uno o dos tipos. Con dos tipos se multiplican
        /// ambos (por ejemplo, Agua contra Roca/Tierra da 2 × 2 = 4).
        /// </summary>
        public static float Effectiveness(PokemonType attack, PokemonType primary, bool hasSecondary, PokemonType secondary)
        {
            float value = Effectiveness(attack, primary);
            return hasSecondary ? value * Effectiveness(attack, secondary) : value;
        }

        /// <summary>Describe el multiplicador como lo muestra el juego: "No afecta", "Poco eficaz"...</summary>
        public static string Describe(float effectiveness) => effectiveness switch
        {
            <= 0f => "No afecta…",
            < 1f => "No es muy eficaz…",
            > 1f => "¡Es supereficaz!",
            _ => "Es eficaz.",
        };

        static float[,] Build()
        {
            // Orden de filas y columnas: Normal, Lucha, Volador, Veneno, Tierra, Roca, Bicho,
            // Fantasma, Acero, Fuego, Agua, Planta, Eléctrico, Psíquico, Hielo, Dragón, Siniestro.
            var rows = new[]
            {
                new[] { 1f, 1f, 1f, 1f, 1f, 0.5f, 1f, 0f, 0.5f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
                new[] { 2f, 1f, 0.5f, 0.5f, 1f, 2f, 0.5f, 0f, 2f, 1f, 1f, 1f, 1f, 0.5f, 2f, 1f, 2f },
                new[] { 1f, 2f, 1f, 1f, 1f, 0.5f, 2f, 1f, 0.5f, 1f, 1f, 2f, 0.5f, 1f, 1f, 1f, 1f },
                new[] { 1f, 1f, 1f, 0.5f, 0.5f, 0.5f, 1f, 0.5f, 0f, 1f, 1f, 2f, 1f, 1f, 1f, 1f, 1f },
                new[] { 1f, 1f, 0f, 2f, 1f, 2f, 0.5f, 1f, 2f, 2f, 1f, 0.5f, 2f, 1f, 1f, 1f, 1f },
                new[] { 1f, 0.5f, 2f, 1f, 0.5f, 1f, 2f, 1f, 0.5f, 2f, 1f, 1f, 1f, 1f, 2f, 1f, 1f },
                new[] { 1f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f, 0.5f, 0.5f, 0.5f, 1f, 2f, 1f, 2f, 1f, 1f, 2f },
                new[] { 0f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 0.5f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 0.5f },
                new[] { 1f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 0.5f, 0.5f, 0.5f, 1f, 0.5f, 1f, 2f, 1f, 1f },
                new[] { 1f, 1f, 1f, 1f, 1f, 0.5f, 2f, 1f, 2f, 0.5f, 0.5f, 2f, 1f, 1f, 2f, 0.5f, 1f },
                new[] { 1f, 1f, 1f, 1f, 2f, 2f, 1f, 1f, 1f, 2f, 0.5f, 0.5f, 1f, 1f, 1f, 0.5f, 1f },
                new[] { 1f, 1f, 0.5f, 0.5f, 2f, 2f, 0.5f, 1f, 0.5f, 0.5f, 2f, 0.5f, 1f, 1f, 1f, 0.5f, 1f },
                new[] { 1f, 1f, 2f, 1f, 0f, 1f, 1f, 1f, 1f, 1f, 2f, 0.5f, 0.5f, 1f, 1f, 0.5f, 1f },
                new[] { 1f, 2f, 1f, 2f, 1f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f, 1f, 0.5f, 1f, 1f, 0f },
                new[] { 1f, 1f, 2f, 1f, 2f, 1f, 1f, 1f, 0.5f, 0.5f, 0.5f, 2f, 1f, 1f, 0.5f, 2f, 1f },
                new[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, 1f },
                new[] { 1f, 0.5f, 1f, 1f, 1f, 1f, 1f, 2f, 0.5f, 1f, 1f, 1f, 1f, 2f, 1f, 1f, 0.5f },
            };

            var matrix = new float[rows.Length, rows.Length];
            for (int attack = 0; attack < rows.Length; attack++)
                for (int defend = 0; defend < rows.Length; defend++)
                    matrix[attack, defend] = rows[attack][defend];
            return matrix;
        }
    }
}
