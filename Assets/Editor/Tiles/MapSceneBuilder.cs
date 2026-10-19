using System.IO;
using TorreIngenieria.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Crea una escena de mapa lista para pintar con el Tile Palette: cámara Pixel Perfect estilo GBA (240x160)
    /// y las capas de Tilemap del juego (Suelo, Paredes, Decoracion, SobreJugador, Colisiones y Encuentros).
    /// Menú: Torre Ingeniería > Mapas > Crear escena de mapa.
    /// </summary>
    public static class MapSceneBuilder
    {
        const string MapsFolder = "Assets/Scenes/Mapas";

        /// <summary>Orden de dibujo del jugador y los NPC: entre Decoracion y SobreJugador.</summary>
        public const int CharacterSortingOrder = 5;

        [MenuItem("Torre Ingeniería/Mapas/Crear escena de mapa")]
        static void CreateMapScene()
        {
            if (!AssetDatabase.IsValidFolder(MapsFolder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(MapsFolder), Path.GetFileName(MapsFolder));

            string path = EditorUtility.SaveFilePanelInProject("Nueva escena de mapa", "Piso3", "unity",
                "Nombre de la escena (por ejemplo Atrio, Lobby, Piso3, PisoTecnico).", MapsFolder);
            if (string.IsNullOrEmpty(path) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = CreateScene();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Escena de mapa creada en {path}. Abre Window > 2D > Tile Palette para pintar.");
        }

        /// <summary>Escena nueva (sin guardar) con cámara, luz y las capas del mapa.</summary>
        public static Scene CreateScene()
        {
            if (!AssetDatabase.IsValidFolder(MapsFolder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(MapsFolder), Path.GetFileName(MapsFolder));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateGlobalLight();
            CreateGrid();
            return scene;
        }

        /// <summary>Busca una capa del mapa (Suelo, Paredes, Decoracion, SobreJugador, Colisiones, Encuentros).</summary>
        public static Tilemap Layer(string name) => GameObject.Find($"Grid/{name}").GetComponent<Tilemap>();

        public static string MapPath(string sceneName) => $"{MapsFolder}/{sceneName}.unity";

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0, 0, -10);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 160f / 2f / TilesetTools.TileSize; // 10 tiles de alto
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            go.AddComponent<AudioListener>();

            // Resolución de GBA (240x160) escalada sin perder nitidez.
            var pixelPerfect = go.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = TilesetTools.TileSize;
            pixelPerfect.refResolutionX = 240;
            pixelPerfect.refResolutionY = 160;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        }

        static void CreateGlobalLight()
        {
            // El renderer 2D de URP necesita una luz global para que los sprites se vean con su color original.
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }

        static void CreateGrid()
        {
            var grid = new GameObject("Grid", typeof(Grid));
            grid.GetComponent<Grid>().cellSize = Vector3.one;

            CreateLayer(grid.transform, "Suelo", -10);
            Tilemap walls = CreateLayer(grid.transform, "Paredes", -5);
            AddCollision(walls.gameObject);
            CreateLayer(grid.transform, "Decoracion", 0);
            CreateLayer(grid.transform, "SobreJugador", 10);

            Tilemap collisions = CreateLayer(grid.transform, "Colisiones", 20);
            AddCollision(collisions.gameObject);
            collisions.gameObject.AddComponent<HideOnPlay>();

            Tilemap encounters = CreateLayer(grid.transform, "Encuentros", 21);
            encounters.gameObject.AddComponent<HideOnPlay>();
        }

        static Tilemap CreateLayer(Transform grid, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return go.GetComponent<Tilemap>();
        }

        /// <summary>Colisión de la capa como una sola forma (más eficiente que una caja por tile).</summary>
        static void AddCollision(GameObject layer)
        {
            var body = layer.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = layer.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            // Polígonos (no contornos) para que se pueda consultar si una casilla está ocupada.
            layer.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
    }
}
