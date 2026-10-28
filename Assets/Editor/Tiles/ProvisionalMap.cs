using System;
using System.Collections.Generic;
using System.Linq;
using TorreIngenieria.World;
using Unity.Cinemachine;
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
        const string PedroSheetPath = CharacterSpriteSlicer.PedroSheet;
        const string ControlsPath = "Assets/Input/ControlesJuego.inputactions";

        /// <summary>Prepara los recursos comunes. Llamar antes de crear la escena.</summary>
        public static void PrepareCommonAssets(params string[] tilesetPaths)
        {
            foreach (string path in tilesetPaths)
                TilesetTools.Prepare(path);
            TilesetTools.Prepare(UtilitiesPath);
            CharacterSpriteSlicer.SliceCharacters(); // Pedro, y Gabriel y el Vigilante si ya existen
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
            var choices = MainMenuSceneBuilder.CreateMapChoiceBox(canvasObject.transform, controls);
            var message = canvasObject.AddComponent<WorldMessage>();
            var serialized = new SerializedObject(message);
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("dialogue").objectReferenceValue = dialogue;
            serialized.FindProperty("choices").objectReferenceValue = choices;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Barra superior con piso, sellos y créditos (HUD).</summary>
        public static void AddHud()
        {
            var canvasObject = new GameObject("CanvasHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // encima del cuadro de texto y debajo del fundido (FadeManager)
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            MainMenuSceneBuilder.CreateMapHud(canvasObject.transform);
        }

        /// <summary>
        /// Zonas de encuentro del piso: revisa la capa <c>Encuentros</c> al dar cada paso y puede empezar un
        /// combate salvaje (10 % por casilla; ver <see cref="EncounterZone"/>). Los pisos sin tiles de
        /// encuentro (pasillos, lobby) son seguros.
        /// </summary>
        public static void AddEncounterZone() =>
            new GameObject("ZonaEncuentros").AddComponent<EncounterZone>();

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

        const string PickupTilePath = "Assets/Tiles/AtrioProvisional/AtrioProvisional_8.asset";

        /// <summary>Personajes con hoja de caminata propia (ver <see cref="CharacterSpriteSlicer"/>).</summary>
        public enum Character { Pedro, Gabriel, Vigilante }

        /// <summary>
        /// NPC centrado en la casilla; habla con el jugador (Interactable). Con <paramref name="character"/>
        /// usa la hoja de ese personaje; sin él, la de Pedro teñida de verde (NPC genérico de prueba).
        /// </summary>
        public static Interactable CreateNpc(string name, int col, int row, int height, string[] lines,
            string[] options = null, string[] acceptedLines = null, string[] rejectedLines = null,
            Character character = Character.Pedro,
            PlayerController.Direction facing = PlayerController.Direction.Down)
        {
            var go = new GameObject(name);
            go.transform.position = CellCenter(col, row, height);
            SpriteRenderer sprite = AddCharacterSprite(go, character, facing);
            if (character == Character.Pedro)
                sprite.color = new Color(0.72f, 1f, 0.78f); // tinte para distinguirlo de Pedro

            var interactable = go.AddComponent<Interactable>();
            ConfigureInteractable(interactable, Interactable.Kind.Npc, lines, options, 0,
                acceptedLines, rejectedLines, null, 0, null, null);
            return interactable;
        }

        /// <summary>
        /// Gabriel de la plazoleta: NPC de historia que entrega el inicial y las Pokébolas (ver
        /// <see cref="GabrielNpc"/>). Usa su hoja de caminata y no necesita líneas fijas.
        /// </summary>
        public static GabrielNpc CreateGabriel(int col, int row, int height,
            PlayerController.Direction facing = PlayerController.Direction.Down)
        {
            var go = new GameObject("Gabriel");
            go.transform.position = CellCenter(col, row, height);
            AddCharacterSprite(go, Character.Gabriel, facing);
            return go.AddComponent<GabrielNpc>();
        }

        /// <summary>
        /// Entrenador de prueba centrado en la casilla, con el cuadro de caminata de Pedro y otro tinte.
        /// El combate de entrenadores llega en la Etapa 6; hasta entonces <see cref="Interactable"/> lo
        /// marca como vencido al hablar (comportamiento provisional). La orientación se usa para el
        /// sprite y deja preparada la casilla para la visión de <c>TrainerController</c>.
        /// </summary>
        public static Interactable CreateTrainer(string name, int col, int row, int height, string trainerId,
            string[] lines, PlayerController.Direction facing = PlayerController.Direction.Down)
        {
            var go = new GameObject(name);
            go.transform.position = CellCenter(col, row, height);
            SpriteRenderer sprite = AddCharacterSprite(go, Character.Pedro, facing);
            sprite.color = new Color(1f, 0.78f, 0.72f); // tinte para distinguirlo de Pedro y del NPC

            var interactable = go.AddComponent<Interactable>();
            ConfigureInteractable(interactable, Interactable.Kind.Trainer, lines, null, 0,
                null, null, null, 0, null, trainerId);
            return interactable;
        }

        /// <summary>
        /// Sprite hijo con el cuadro Quieto del personaje en la orientación pedida. El pivote está en los pies:
        /// se baja casi media casilla para que pise el borde inferior, igual que Pedro.
        /// </summary>
        static SpriteRenderer AddCharacterSprite(GameObject owner, Character character,
            PlayerController.Direction facing)
        {
            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(owner.transform, false);
            spriteObject.transform.localPosition = new Vector3(0, -0.45f, 0);

            string sheet = character switch
            {
                Character.Gabriel => CharacterSpriteSlicer.GabrielSheet,
                Character.Vigilante => CharacterSpriteSlicer.GuardSheet,
                _ => PedroSheetPath,
            };
            string direction = facing switch
            {
                PlayerController.Direction.Up => "Arriba",
                PlayerController.Direction.Left => "Izquierda",
                PlayerController.Direction.Right => "Derecha",
                _ => "Abajo",
            };

            var sprite = spriteObject.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = MapSceneBuilder.CharacterSortingOrder;
            sprite.sprite = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>()
                .FirstOrDefault(s => s.name == $"{character}_{direction}_Quieto");
            if (sprite.sprite == null)
                Debug.LogError($"No se encontró {character}_{direction}_Quieto en {sheet}: corta las hojas de caminata.");
            return sprite;
        }

        /// <summary>Objeto recogible en el suelo; usa el sprite de un tile provisional.</summary>
        public static Interactable CreatePickup(string name, int col, int row, int height,
            string pickupId, string itemId, int amount, string[] lines)
        {
            var go = new GameObject(name);
            go.transform.position = CellCenter(col, row, height);

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = MapSceneBuilder.CharacterSortingOrder - 1;
            if (AssetDatabase.LoadAssetAtPath<TileBase>(PickupTilePath) is Tile tile)
                sprite.sprite = tile.sprite;
            go.transform.localScale = Vector3.one * 0.7f;

            var interactable = go.AddComponent<Interactable>();
            ConfigureInteractable(interactable, Interactable.Kind.Item, lines, null, 0,
                null, null, itemId, amount, pickupId, null);
            return interactable;
        }

        static void ConfigureInteractable(Interactable target, Interactable.Kind kind, string[] lines,
            string[] options, int acceptedOption, string[] acceptedLines, string[] rejectedLines,
            string itemId, int itemAmount, string pickupId, string trainerId)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            SetStringArray(serialized.FindProperty("lines"), lines);
            SetStringArray(serialized.FindProperty("options"), options);
            serialized.FindProperty("acceptedOption").intValue = acceptedOption;
            SetStringArray(serialized.FindProperty("acceptedLines"), acceptedLines);
            SetStringArray(serialized.FindProperty("rejectedLines"), rejectedLines);
            serialized.FindProperty("itemId").stringValue = itemId ?? "";
            serialized.FindProperty("itemAmount").intValue = itemAmount;
            serialized.FindProperty("pickupId").stringValue = pickupId ?? "";
            serialized.FindProperty("trainerId").stringValue = trainerId ?? "";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStringArray(SerializedProperty property, string[] values)
        {
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
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

        /// <summary>
        /// Cámara virtual de Cinemachine que sigue al jugador, confinada al rectángulo del piso
        /// (de (0,0) a (width,height)). La Main Camera lleva el CinemachineBrain (lo crea MapSceneBuilder).
        /// </summary>
        public static void SetupCamera(Transform target, int width, int height)
        {
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(target.position.x, target.position.y, -10);

            // Límite del piso para el Confiner 2D. El confiner usa el tamaño del BoxCollider2D
            // y la posición del objeto (ignora el offset), así que el objeto va centrado en el mapa.
            var bounds = new GameObject("LimitePiso");
            bounds.transform.position = new Vector3(width / 2f, height / 2f, 0f);
            var box = bounds.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, height);
            box.offset = Vector2.zero;
            box.isTrigger = true;

            var virtualCamera = new GameObject("CamaraVirtual");
            virtualCamera.transform.position = new Vector3(target.position.x, target.position.y, -10f);
            var vcam = virtualCamera.AddComponent<CinemachineCamera>();
            vcam.Follow = target;
            vcam.Lens = new LensSettings
            {
                ModeOverride = LensSettings.OverrideModes.Orthographic,
                OrthographicSize = camera.orthographicSize,
                NearClipPlane = camera.nearClipPlane,
                FarClipPlane = camera.farClipPlane,
            };

            // Sin amortiguación: el movimiento por casillas debe verse firme, como en GBA.
            var follow = virtualCamera.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 0f, -10f);
            follow.TrackerSettings.PositionDamping = Vector3.zero;

            // Ajusta el tamaño ortográfico al Pixel Perfect Camera de URP (debe ir antes del Confiner).
            virtualCamera.AddComponent<CinemachinePixelPerfect>();

            var confiner = virtualCamera.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = box;
            confiner.Damping = 0f;
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

        /// <summary>Guarda la escena y la registra en Build Profiles sin alterar el orden de las demás.</summary>
        public static string Save(Scene scene, string sceneName)
        {
            string path = MapSceneBuilder.MapPath(sceneName);
            EditorSceneManager.SaveScene(scene, path);
            BuildScenes.Register(path);
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
