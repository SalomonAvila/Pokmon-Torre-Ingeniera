using System;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Cálculo de estadísticas y experiencia (fórmulas Gen 3), sin estado. El prototipo usa IV 31,
    /// EV 0 y naturaleza neutra: así los valores son deterministas y verificables; las naturalezas y
    /// los EV quedan para más adelante.
    ///
    /// La curva de experiencia es la "media rápida" (nivel³) para todas las especies. El documento no
    /// fija curvas por especie; se documenta como simplificación consciente.
    /// </summary>
    public static class PokemonStats
    {
        public const int DefaultIv = 31;
        public const int DefaultEv = 0;
        public const int MinLevel = 1;
        public const int MaxLevel = 100;

        /// <summary>PS máximos: ((2 × base + IV + EV/4) × nivel) / 100 + nivel + 10.</summary>
        public static int MaxHp(int baseHp, int level) =>
            (2 * baseHp + DefaultIv + DefaultEv / 4) * level / 100 + level + 10;

        /// <summary>Estadística distinta de PS: ((2 × base + IV + EV/4) × nivel) / 100 + 5.</summary>
        public static int Stat(int baseStat, int level) =>
            (2 * baseStat + DefaultIv + DefaultEv / 4) * level / 100 + 5;

        /// <summary>Experiencia acumulada para alcanzar un nivel (curva media rápida: nivel³).</summary>
        public static int ExperienceForLevel(int level)
        {
            int clamped = Math.Clamp(level, MinLevel, MaxLevel);
            return clamped * clamped * clamped;
        }

        /// <summary>Nivel que corresponde a una experiencia acumulada.</summary>
        public static int LevelForExperience(int experience)
        {
            int level = MinLevel;
            while (level < MaxLevel && ExperienceForLevel(level + 1) <= experience)
                level++;
            return level;
        }

        /// <summary>Experiencia que falta para el siguiente nivel (0 al llegar al máximo).</summary>
        public static int ExperienceToNextLevel(int experience)
        {
            int level = LevelForExperience(experience);
            return level >= MaxLevel ? 0 : ExperienceForLevel(level + 1) - experience;
        }
    }
}
