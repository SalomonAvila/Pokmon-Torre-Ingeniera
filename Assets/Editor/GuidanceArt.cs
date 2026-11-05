namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Rutas e índices del arte de la guía. La flecha es arte de interfaz (solo sprites); el
    /// resaltado y los pictogramas son hojas de tiles de 16x16.
    /// </summary>
    public static class GuidanceArt
    {
        public const string ArrowSheet = "Assets/Art/UI/FlechaGuia.png";
        public const string PlanterSheet = "Assets/Art/Tilesets/Propios/ResaltadoJardinera.png";
        public const string PictogramSheet = "Assets/Art/Tilesets/Propios/PictogramasGuia.png";

        // Orden de PictogramasGuia.png.
        public const int PictogramUp = 0;
        public const int PictogramDown = 1;
        public const int PictogramStairs = 2;
        public const int PictogramDoor = 3;
        public const int PictogramPlant = 4;

        // Orden de ResaltadoJardinera.png.
        public const int BorderTopLeft = 0;
        public const int BorderTop = 1;
        public const int BorderTopRight = 2;
        public const int BorderLeft = 3;
        public const int BorderRight = 4;
        public const int BorderBottomLeft = 5;
        public const int BorderBottom = 6;
        public const int BorderBottomRight = 7;

        /// <summary>Prepara las tres hojas; se puede llamar en cada builder sin duplicar assets.</summary>
        public static void PrepareAll()
        {
            TilesetTools.Prepare(PlanterSheet);
            TilesetTools.Prepare(PictogramSheet);
            TilesetTools.Prepare(ArrowSheet, createTiles: false);
        }
    }
}
