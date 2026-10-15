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
    /// Formato de la hoja: celdas de 192x256 px; filas de arriba hacia abajo Abajo, Arriba, Izquierda, Derecha;
    /// columnas Quieto, PasoA, PasoB. Los pies quedan a 8 px del borde inferior de cada celda.
    /// </summary>
    public static class CharacterSpriteSlicer
    {
        const int CellWidth = 192;
        const int CellHeight = 256;
        const int FootMargin = 8;
        // 128 px = 1 unidad = 1 casilla del mapa, así el personaje mide unas 2 casillas (como 16x32 en GBA).
        const float PixelsPerUnit = 128f;

        public static readonly string[] Directions = { "Abajo", "Arriba", "Izquierda", "Derecha" };
        public static readonly string[] Poses = { "Quieto", "PasoA", "PasoB" };

        [MenuItem("Torre Ingeniería/Cortar hoja de caminata de Pedro")]
        static void SlicePedro() => SliceWalkSheet("Assets/Art/Characters/PedroCaminata.png", "Pedro");

        public static void SliceWalkSheet(string path, string characterName)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                EditorUtility.DisplayDialog("Cortar hoja", $"No se encontró {path}.", "OK");
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
