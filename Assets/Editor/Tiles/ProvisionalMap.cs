using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TorreIngenieria.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Utilidades compartidas por los generadores de mapas provisionales (Atrio, Ascensores...):
    /// pintar desde texto, crear a Pedro, la cámara, zonas de cambio de escena y puntos de llegada.
    ///
    /// Los mapas se describen con dos listas de texto del mismo tamaño (fila 0 = norte):
    /// una para el piso y otra para los objetos ('.' = sin objeto).
    /// </summary>
    public static class ProvisionalMap
    {
        const string UtilitiesPath = "Assets/Art/Tilesets/Utilidades/Utilidades.png";
        const string PedroSheetPath = "Assets/Art/Characters/PedroCaminata.png";
        const string ControlsPath = "Assets/Input/ControlesJuego.inputactions";

        /// <summary>Prepara los recursos comunes. Llamar antes de crear la escena.</summary>
        public static void PrepareCommonAssets(params string[] tilesetPaths)
        {
            foreach (string path in tilesetPaths)
                TilesetTools.Prepare(path);
            TilesetTools.Prepare(UtilitiesPath);
            CharacterSpriteSlicer.SliceWalkSheet(PedroSheetPath, "Pedro");
            MainMenuSceneBuilder.PrepareDialogueAssets();
        }

        /// <summary>Indica a qué piso del edificio pertenece la escena (ver Building.Floors).</summary>
        public static void AddFloorInfo(string floor)
        {
            var info = new GameObject($"Piso_{floor}").AddComponent<FloorInfo>();
            var serialized = new SerializedObject(info);
            serialized.FindProperty("floor").stringValue = floor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Cuadro de texto estilo Pokémon para avisos y letreros en el mapa (WorldMessage).</summary>
        public static void AddMessageBox()
        {
            var canvasObject = new GameObject("CanvasMensajes", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var dialogue = MainMenuSceneBuilder.CreateMapDialogueBox(canvasObject.transform, controls);
            var message = canvasObject.AddComponent<WorldMessage>();
            var serialized = new SerializedObject(message);
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("dialogue").objectReferenceValue = dialogue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static TileBase[] LoadTiles(string sheet, int count) =>
            Enumerable.Range(0, count).Select(i => LoadTile(sheet, i)).ToArray();

        static TileBase LoadTile(string sheet, int index)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Tiles/{sheet}/{sheet}_{index}.asset");
            if (tile == null)
                Debug.LogError($"No se encontró el tile {sheet}_{index}.");
            return tile;
        }

        /// <summary>Casilla del Tilemap para una columna y fila del texto (fila 0 = arriba).</summary>
        public static Vector3Int Cell(int col, int row, int height) => new Vector3Int(col, height - 1 - row, 0);

        /// <summary>Centro de la casilla, en coordenadas del mundo.</summary>
        public static Vector3 CellCenter(int col, int row, int height) => Cell(col, row, height) + new Vector3(0.5f, 0.5f);

        /// <summary>
        /// Pinta Suelo, Decoracion y Colisiones. floorTile recibe (símbolo, columna, fila) y devuelve el índice del tile
        /// (o -1 para no pintar esa casilla en Suelo, por ejemplo si va en otra capa).
        /// Además pone un borde de colisión alrededor del mapa.
        /// </summary>
        public static void Paint(string[] floor, string[] objects, TileBase[] tiles,
            Func<char, int, int, int> floorTile, IReadOnlyDictionary<char, int> objectTile,
            string blockingFloors, string blockingObjects)
        {
            Tilemap ground = MapSceneBuilder.Layer("Suelo");
            Tilemap decoration = MapSceneBuilder.Layer("Decoracion");
            Tilemap collisions = MapSceneBuilder.Layer("Colisiones");
            TileBase collisionTile = LoadTile("Utilidades", 0);

            int height = floor.Length;
            int width = floor[0].Length;
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Vector3Int cell = Cell(col, row, height);
                    char floorSymbol = floor[row][col];
                    char item = objects[row][col];

                    int floorIndex = floorTile(floorSymbol, col, row);
                    if (floorIndex >= 0)
                        ground.SetTile(cell, tiles[floorIndex]);
                    if (item != '.')
                        decoration.SetTile(cell, tiles[objectTile[item]]);
                    if (blockingFloors.IndexOf(floorSymbol) >= 0 || blockingObjects.IndexOf(item) >= 0)
                        collisions.SetTile(cell, collisionTile);
                }
            }

            for (int x = -1; x <= width; x++)
            {
                collisions.SetTile(new Vector3Int(x, -1, 0), collisionTile);
                collisions.SetTile(new Vector3Int(x, height, 0), collisionTile);
            }
            for (int y = 0; y < height; y++)
            {
                collisions.SetTile(new Vector3Int(-1, y, 0), collisionTile);
                collisions.SetTile(new Vector3Int(width, y, 0), collisionTile);
            }
        }

        public static GameObject CreatePlayer(Vector3 position)
        {
            var pedro = new GameObject("Pedro");
            pedro.transform.position = position;

            // El pivote del sprite está en los pies: se baja casi media casilla para que pise el borde inferior.
            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(pedro.transform, false);
            spriteObject.transform.localPosition = new Vector3(0, -0.45f, 0);
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = MapSceneBuilder.CharacterSortingOrder;

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(PedroSheetPath)
                .OfType<Sprite>().ToDictionary(s => s.name);
            Sprite[] Frames(string direction) =>
                CharacterSpriteSlicer.Poses.Select(pose => sprites[$"Pedro_{direction}_{pose}"]).ToArray();
            spriteRenderer.sprite = sprites["Pedro_Abajo_Quieto"];

            var controller = pedro.AddComponent<PlayerController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("controls").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            SetArray(serialized.FindProperty("down"), Frames("Abajo"));
            SetArray(serialized.FindProperty("up"), Frames("Arriba"));
            SetArray(serialized.FindProperty("left"), Frames("Izquierda"));
            SetArray(serialized.FindProperty("right"), Frames("Derecha"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return pedro;
        }

        public static void SetupCamera(Transform target, int width, int height)
        {
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10);
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            var serialized = new SerializedObject(follow);
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.FindProperty("mapMin").vector2Value = Vector2.zero;
            serialized.FindProperty("mapMax").vector2Value = new Vector2(width, height);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Zona que lleva a otra escena. Columnas y filas del texto, ambas inclusive.</summary>
        public static WarpZone AddWarp(string name, int colMin, int colMax, int rowMin, int rowMax, int height,
            string targetScene, string targetSpawn, bool needsBuildingAccess = false, string lockedMessage = null)
        {
            var warp = new GameObject(name).AddComponent<WarpZone>();
            warp.transform.position = Cell(colMin, rowMax, height); // esquina inferior izquierda
            var serialized = new SerializedObject(warp);
            serialized.FindProperty("size").vector2IntValue = new Vector2Int(colMax - colMin + 1, rowMax - rowMin + 1);
            serialized.FindProperty("targetScene").stringValue = targetScene;
            serialized.FindProperty("targetSpawn").stringValue = targetSpawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (needsBuildingAccess)
                warp.SetBuildingAccess(true, lockedMessage);
            return warp;
        }

        /// <summary>Punto de llegada desde otra escena.</summary>
        public static void AddSpawn(string id, int col, int row, int height, PlayerController.Direction facing)
        {
            var spawn = new GameObject($"Llegada_{id}").AddComponent<SpawnPoint>();
            spawn.transform.position = CellCenter(col, row, height);
            var serialized = new SerializedObject(spawn);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("facing").enumValueIndex = (int)facing;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Guarda la escena y la agrega al build (quitando las que ya no existen).</summary>
        public static string Save(Scene scene, string sceneName)
        {
            string path = MapSceneBuilder.MapPath(sceneName);
            EditorSceneManager.SaveScene(scene, path);
            var scenes = EditorBuildSettings.scenes.Where(s => File.Exists(s.path) && s.path != path).ToList();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            return path;
        }

        static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
