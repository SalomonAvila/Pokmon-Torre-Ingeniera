using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Convierte una hoja de tiles (PNG) en todo lo que necesita el Tilemap de Unity:
    /// 1. La importa como pixel art (Point, sin compresión, 16 px por unidad).
    /// 2. La corta en tiles de 16x16, omitiendo las celdas vacías (transparentes).
    /// 3. Crea un asset Tile por cada corte en Assets/Tiles/&lt;hoja&gt;/.
    /// 4. Crea o actualiza su paleta en Assets/Tiles/Paletas/ con la misma distribución de la hoja.
    /// Menú: Torre Ingeniería > Tiles > Preparar tileset seleccionado.
    /// </summary>
    public static class TilesetTools
    {
        public const int TileSize = 16;
        const string TilesRoot = "Assets/Tiles";
        const string PalettesFolder = TilesRoot + "/Paletas";

        [MenuItem("Torre Ingeniería/Tiles/Preparar tileset seleccionado")]
        static void PrepareSelected()
        {
            string[] paths = Selection.objects.OfType<Texture2D>().Select(AssetDatabase.GetAssetPath).ToArray();
            foreach (string path in paths)
                Prepare(path, TileSizeFor(path));
        }

        [MenuItem("Torre Ingeniería/Tiles/Preparar tileset seleccionado", true)]
        static bool CanPrepareSelected() => Selection.objects.OfType<Texture2D>().Any();

        [MenuItem("Torre Ingeniería/Tiles/Preparar todos los tilesets")]
        static void PrepareAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Tilesets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Prepare(path, TileSizeFor(path));
            }
        }

        /// <summary>
        /// Tamaño de corte de una hoja. La flecha de guía y los tiles del mapa usan 16x16;
        /// los pictogramas se dibujan a 24x24 para que ocupen 1,5 casillas en el mapa.
        /// </summary>
        static int TileSizeFor(string path) =>
            path == GuidanceArt.PictogramSheet ? GuidanceArt.PictogramTileSize : TileSize;

        /// <summary>
        /// Prepara una hoja de 16x16. Con <paramref name="createTiles"/> en false solo deja los
        /// sprites (útil para arte de interfaz, como la flecha de guía, que no va a Tile Palette).
        /// </summary>
        public static void Prepare(string path, bool createTiles = true) =>
            Prepare(path, TileSize, createTiles);

        /// <summary>
        /// Prepara una hoja con celdas de <paramref name="tileSize"/> píxeles. Los sprites
        /// conservan 16 PPU: un icono de 24x24 se dibuja a 1,5 unidades (1,5 casillas).
        /// </summary>
        public static void Prepare(string path, int tileSize, bool createTiles = true)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            string sheetName = Path.GetFileNameWithoutExtension(path);
            Texture2D pixels = ReadPixels(path);
            int columns = pixels.width / tileSize;
            int rows = pixels.height / tileSize;
            if (pixels.width % tileSize != 0 || pixels.height % tileSize != 0)
                Debug.LogWarning($"{path}: {pixels.width}x{pixels.height} no es múltiplo de {tileSize}; se ignoran los bordes sobrantes.");

            ConfigureImporter(importer);
            List<SpriteRect> rects = Slice(importer, sheetName, pixels, columns, rows, tileSize);
            Object.DestroyImmediate(pixels);

            if (!createTiles)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Hoja '{sheetName}': {rects.Count} sprites preparados (sin tiles).");
                return;
            }

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            var tilesByCell = new Dictionary<Vector2Int, TileBase>();
            string tileFolder = EnsureFolder($"{TilesRoot}/{sheetName}");
            foreach (SpriteRect rect in rects)
            {
                if (!sprites.TryGetValue(rect.name, out Sprite sprite))
                    continue;
                var cell = new Vector2Int((int)rect.rect.x / tileSize, rows - 1 - (int)rect.rect.y / tileSize);
                tilesByCell[cell] = CreateOrUpdateTile($"{tileFolder}/{rect.name}.asset", sprite);
            }

            UpdatePalette(sheetName, tilesByCell);
            AssetDatabase.SaveAssets();
            Debug.Log($"Tileset '{sheetName}': {tilesByCell.Count} tiles en {tileFolder} y paleta en {PalettesFolder}/{sheetName}.prefab");
        }

        /// <summary>Lee la imagen original desde disco (la textura importada no es legible).</summary>
        static Texture2D ReadPixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        static void ConfigureImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TileSize; // 1 tile = 1 unidad
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // evita costuras entre tiles
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static List<SpriteRect> Slice(TextureImporter importer, string sheetName, Texture2D pixels, int columns,
            int rows, int tileSize)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previousIds = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);

            Color32[] colors = pixels.GetPixels32();
            var rects = new List<SpriteRect>();
            for (int row = 0; row < rows; row++)          // fila 0 = arriba
            {
                for (int col = 0; col < columns; col++)
                {
                    int x = col * tileSize;
                    int y = (rows - 1 - row) * tileSize;  // el Sprite Editor cuenta desde abajo
                    if (IsEmpty(colors, pixels.width, x, y, tileSize))
                        continue;

                    string name = $"{sheetName}_{row * columns + col}";
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        rect = new Rect(x, y, tileSize, tileSize),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = previousIds.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    });
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            return rects;
        }

        static bool IsEmpty(Color32[] colors, int width, int x, int y, int tileSize)
        {
            for (int dy = 0; dy < tileSize; dy++)
                for (int dx = 0; dx < tileSize; dx++)
                    if (colors[(y + dy) * width + x + dx].a > 0)
                        return false;
            return true;
        }

        static TileBase CreateOrUpdateTile(string assetPath, Sprite sprite)
        {
            // Colisión de casilla completa: solo bloquea en las capas con colisionador (Paredes, Colisiones);
            // en Suelo, Decoracion y SobreJugador no tiene efecto.
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, assetPath);
            }
            else if (tile.sprite != sprite || tile.colliderType != Tile.ColliderType.Grid)
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            return tile;
        }

        /// <summary>Paleta con los tiles en la misma posición que en la hoja original.</summary>
        static void UpdatePalette(string sheetName, Dictionary<Vector2Int, TileBase> tilesByCell)
        {
            EnsureFolder(PalettesFolder);
            string palettePath = $"{PalettesFolder}/{sheetName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) == null)
            {
                GridPaletteUtility.CreateNewPalette(PalettesFolder, sheetName, GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(palettePath);
            Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
            tilemap.ClearAllTiles();
            foreach (var pair in tilesByCell)
                tilemap.SetTile(new Vector3Int(pair.Key.x, -pair.Key.y, 0), pair.Value);
            PrefabUtility.SaveAsPrefabAsset(root, palettePath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static string EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return folder;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            return folder;
        }
    }
}
