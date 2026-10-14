using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Paleta base institucional del documento de diseño (sección 5).
    /// </summary>
    public static class Palette
    {
        public static readonly Color EngineeringBlue = Hex(0x1F4E8C);
        public static readonly Color ConcreteGray = Hex(0x8D99A6);
        public static readonly Color DarkGray = Hex(0x3A4252);
        public static readonly Color BoneWhite = Hex(0xF4F1E8);
        public static readonly Color SafetyYellow = Hex(0xF2C230);
        public static readonly Color DarkOutline = Hex(0x1B2230);

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
