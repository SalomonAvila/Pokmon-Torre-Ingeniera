using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Barras de PS y EXP compartidas por el combate (<c>BattleHUD</c>) y los menús (Equipo y Resumen):
    /// mismos umbrales y colores que FireRed (verde, amarillo hasta el 50 %, rojo por debajo del 20 %).
    /// </summary>
    public static class HpBar
    {
        /// <summary>Por debajo de este porcentaje la barra de PS se pinta en rojo.</summary>
        public const float RedThreshold = 0.2f;

        /// <summary>Hasta este porcentaje (incluido) la barra de PS se pinta en amarillo.</summary>
        public const float YellowThreshold = 0.5f;

        public static readonly Color Green = new Color32(0x50, 0xC8, 0x50, 0xFF);
        public static readonly Color Yellow = new Color32(0xF8, 0xD0, 0x30, 0xFF);
        public static readonly Color Red = new Color32(0xE0, 0x40, 0x40, 0xFF);

        /// <summary>Color de la barra según el porcentaje de PS restante (0 a 1).</summary>
        public static Color ColorFor(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio < RedThreshold)
                return Red;
            return ratio <= YellowThreshold ? Yellow : Green;
        }

        /// <summary>
        /// Estira el relleno desde la izquierda. La barra no usa sprite: con fillAmount haría falta uno,
        /// y sin él Unity ignora el recorte.
        /// </summary>
        public static void SetFill(Image bar, float ratio)
        {
            if (bar == null)
                return;

            RectTransform fill = bar.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        /// <summary>Relleno y color de una barra de PS.</summary>
        public static void ShowHp(Image bar, int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            SetFill(bar, ratio);
            if (bar != null)
                bar.color = ColorFor(ratio);
        }

        /// <summary>Fracción de la barra de EXP entre el nivel actual y el siguiente (0 a 1).</summary>
        public static float ExpRatio(int experience, int level)
        {
            int floor = PokemonStats.ExperienceForLevel(level);
            int next = level >= PokemonStats.MaxLevel
                ? floor
                : PokemonStats.ExperienceForLevel(level + 1);

            return next > floor
                ? Mathf.Clamp01((float)(experience - floor) / (next - floor))
                : 1f;
        }
    }
}
