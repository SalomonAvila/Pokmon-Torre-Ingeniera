using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Corta las hojas de caminata de los personajes en cuadros con nombre (Personaje_Direccion_Pose),
    /// igual que si se hiciera a mano en el Sprite Editor.
    ///
    /// Formato de la hoja (Doc §5, sprite de 16x32 px como en FireRed): celdas de 16x32 px; filas de arriba
    /// hacia abajo Abajo, Arriba, Izquierda, Derecha; columnas Quieto, PasoA (pie izquierdo adelante),
    /// PasoA2 (piernas pasando), PasoB (pie derecho adelante) y PasoB2 (piernas pasando). Los pies apoyan en
    /// la última fila de píxeles de cada celda.
    ///
    /// Los nombres Quieto, PasoA y PasoB son los de la hoja anterior de 3 columnas: así las escenas guardadas
    /// conservan sus referencias hasta que se regeneren los mapas.
    /// </summary>
    public static class CharacterSpriteSlicer
    {
        public const string PedroSheet = "Assets/Art/Characters/PedroCaminata.png";
        public const string GabrielSheet = "Assets/Art/Characters/GabrielCaminata.png";
        public const string GuardSheet = "Assets/Art/Characters/VigilanteCaminata.png";

        const int CellWidth = 16;
        const int CellHeight = 32;
        const int FootMargin = 0;
        // 16 px = 1 unidad = 1 casilla del mapa, igual que los tiles: la cámara Pixel Perfect no reescala al
        // personaje y la caminata no "baila". Mide 1×2 casillas, como en GBA.
        const float PixelsPerUnit = 16f;

        public static readonly string[] Directions = { "Abajo", "Arriba", "Izquierda", "Derecha" };
        public static readonly string[] Poses = { "Quieto", "PasoA", "PasoA2", "PasoB", "PasoB2" };

        [MenuItem("Torre Ingeniería/Cortar hojas de caminata (Pedro, Gabriel y Vigilante)")]
        static void SliceAll() => SliceCharacters();

        /// <summary>Corta las hojas de los personajes que existan (Pedro siempre; Gabriel y el Vigilante si ya están).</summary>
        public static void SliceCharacters()
        {
            SliceWalkSheet(PedroSheet, "Pedro");
            if (AssetImporter.GetAtPath(GabrielSheet) != null)
                SliceWalkSheet(GabrielSheet, "Gabriel");
            if (AssetImporter.GetAtPath(GuardSheet) != null)
                SliceWalkSheet(GuardSheet, "Vigilante");
        }

        public static void SliceWalkSheet(string path, string characterName)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                EditorUtility.DisplayDialog("Cortar hoja", $"No se encontró {path}.", "OK");
                return;
            }

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width != CellWidth * Poses.Length || height != CellHeight * Directions.Length)
            {
                Debug.LogError($"{path} mide {width}x{height}; la hoja de caminata debe medir " +
                               $"{CellWidth * Poses.Length}x{CellHeight * Directions.Length} " +
                               $"({Poses.Length} cuadros de {CellWidth}x{CellHeight} por dirección).");
                return;
            }

            // Pixel art: filtro Point, sin compresión ni mipmaps.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Se conservan los IDs de cortes previos con el mismo nombre para no romper animaciones existentes.
            Dictionary<string, GUID> previousIds = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);

            int rows = Directions.Length;
            var rects = new List<SpriteRect>();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < Poses.Length; col++)
                {
                    string name = $"{characterName}_{Directions[row]}_{Poses[col]}";
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        // Las coordenadas del Sprite Editor empiezan abajo a la izquierda.
                        rect = new Rect(col * CellWidth, (rows - 1 - row) * CellHeight, CellWidth, CellHeight),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, (float)FootMargin / CellHeight), // pivote en los pies
                        spriteID = previousIds.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    });
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            Debug.Log($"{path}: {rects.Count} cuadros ({string.Join(", ", Directions)} × {string.Join(", ", Poses)}).");
        }
    }
}
