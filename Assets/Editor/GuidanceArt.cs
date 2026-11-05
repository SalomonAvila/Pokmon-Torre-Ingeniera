namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Rutas e índices del arte de la guía. La flecha es arte de interfaz (solo sprites); el
    /// resaltado usa tiles de 16x16 y los pictogramas de 24x24 (1,5 casillas a 16 PPU).
    /// </summary>
    public static class GuidanceArt
    {
        public const string ArrowSheet = "Assets/Art/UI/FlechaGuia.png";
        public const string PlanterSheet = "Assets/Art/Tilesets/Propios/ResaltadoJardinera.png";
        public const string PictogramSheet = "Assets/Art/Tilesets/Propios/PictogramasGuia.png";

        // La hoja de pictogramas usa iconos de 24x24: 1,5 casillas a 16 PPU.
        public const int PictogramTileSize = 24;
        public const int PictogramCount = 5;

        // Orden de PictogramasGuia.png.
        public const int PictogramUp = 0;
        public const int PictogramDown = 1;
        public const int PictogramStairs = 2;
        // La puerta no se pinta en el Atrio: el Vigilante ocupa esa casilla y la guía ya la marca.
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
            TilesetTools.Prepare(PictogramSheet, PictogramTileSize);
            TilesetTools.Prepare(ArrowSheet, createTiles: false);
        }
    }
}
