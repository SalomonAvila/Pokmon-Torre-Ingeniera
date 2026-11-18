using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Agrupa el arte de la interfaz (<c>Assets/Art/UI/</c>: marcos, cursores, menús, iconos de objetos y
    /// sellos) en un Sprite Atlas con filtro de punto y sin compresión, para que los menús se dibujen en
    /// pocas llamadas (Etapa 9). Se regenera con Construir todo; volver a ejecutarlo tras añadir arte lo
    /// incluye solo, porque el atlas empaqueta la carpeta entera.
    /// Menú: Torre Ingeniería > Construir atlas de la interfaz.
    /// </summary>
    public static class UiAtlasBuilder
    {
        const string UiFolder = "Assets/Art/UI";
        const string AtlasPath = UiFolder + "/AtlasInterfaz.spriteatlasv2";

        [MenuItem("Torre Ingeniería/Construir atlas de la interfaz")]
        public static void Build()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(UiFolder);
            if (folder == null)
            {
                Debug.LogWarning($"No existe {UiFolder}: no se crea el atlas de la interfaz.");
                return;
            }

            if (!File.Exists(AtlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new[] { folder });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }

            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)
            {
                importer.includeInBuild = true;
                importer.packingSettings = new SpriteAtlasPackingSettings
                {
                    enableRotation = false,
                    enableTightPacking = false,
                    padding = 2,
                };
                importer.textureSettings = new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Point,
                    generateMipMaps = false,
                    sRGB = true,
                };
                var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
                platform.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformSettings(platform);
                importer.SaveAndReimport();
            }

            Debug.Log($"Atlas de la interfaz listo en {AtlasPath} (empaqueta {UiFolder}).");
        }
    }
}
