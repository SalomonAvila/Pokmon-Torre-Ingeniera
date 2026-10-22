using System.Linq;
using TMPro;
using TorreIngenieria.Battle;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Genera la escena de combate (<c>Assets/Scenes/Battle.unity</c>): cámara, fondo, los sprites de los dos
    /// combatientes, sus fichas de PS, el cuadro de mensajes y los menús principal y de movimientos.
    /// El estado del combate lo lleva <see cref="BattleSystem"/> dentro de la escena.
    ///
    /// Menú: Torre Ingeniería > Construir escena de combate.
    /// </summary>
    public static class BattleSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/" + SceneNames.Battle + ".unity";
        const string ControlsPath = "Assets/Input/ControlesJuego.inputactions";
        const string DialogBoxPath = "Assets/Art/UI/CuadroDialogo.png";
        const string ContinueArrowPath = "Assets/Art/UI/FlechaContinuar.png";
        const string OptionCursorPath = "Assets/Art/UI/CursorOpcion.png";
        const string PixelFontPath = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
        const string PixelFontAssetPath = "Assets/Art/Fonts/PressStart2P SDF.asset";

        static readonly Vector2 ReferenceResolution = new Vector2(1024, 576);
        const float PixelScale = 4f;
        const int DialogBoxBorder = 9;

        static readonly Color TextBlack = new Color32(0x10, 0x10, 0x10, 0xFF);
        static readonly Color SkyColor = new Color32(0x9A, 0xD0, 0xE8, 0xFF);
        static readonly Color GroundColor = new Color32(0x88, 0xC0, 0x70, 0xFF);
        static readonly Color HpTrackColor = new Color32(0x40, 0x40, 0x48, 0xFF);
        static readonly Color HpFillColor = new Color32(0x50, 0xC8, 0x50, 0xFF);

        [MenuItem("Torre Ingeniería/Construir escena de combate")]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                EditorUtility.DisplayDialog("TextMeshPro",
                    "Se están importando los recursos esenciales de TextMeshPro. " +
                    "Cuando termine, vuelve a ejecutar Torre Ingeniería > Construir escena de combate.", "OK");
                return;
            }

            var missing = new[] { ControlsPath, DialogBoxPath, ContinueArrowPath, OptionCursorPath, PixelFontPath }
                .Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Escena de combate", "Faltan recursos:\n" + string.Join("\n", missing), "OK");
                return;
            }

            ConfigurePixelSprite(DialogBoxPath, DialogBoxBorder);
            ConfigurePixelSprite(ContinueArrowPath, 0);
            ConfigurePixelSprite(OptionCursorPath, 0);
            EnsurePixelFont();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var boxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DialogBoxPath);
            var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ContinueArrowPath);
            var cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OptionCursorPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath);

            CreateCamera();
            Transform canvas = CreateCanvas();
            CreateBackground(canvas);

            Image enemySprite = CreateSpriteImage(canvas, "SpriteRival",
                new Vector2(1, 1), new Vector2(-180, -120), new Vector2(200, 200));
            Image playerSprite = CreateSpriteImage(canvas, "SpriteJugador",
                new Vector2(0, 0), new Vector2(220, 290), new Vector2(200, 200));

            BattleHUD enemyHud = CreateHud(canvas, "FichaRival", new Vector2(0, 1),
                new Vector2(16, -16), new Vector2(360, 136), boxSprite, font);
            BattleHUD playerHud = CreateHud(canvas, "FichaJugador", new Vector2(1, 0),
                new Vector2(-16, 200), new Vector2(360, 136), boxSprite, font);

            DialogueBox message = CreateDialogueBox(canvas, controls, boxSprite, arrowSprite, font);

            BattleMenu menu = CreateMenu(canvas, "MenuPrincipal", controls, boxSprite, cursorSprite, font,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(608, 176),
                new Vector2(260, 60), 2);
            BattleMenu moveMenu = CreateMenu(canvas, "MenuMovimientos", controls, boxSprite, cursorSprite, font,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(608, 176),
                new Vector2(560, 34), 1);

            var ui = canvas.gameObject.AddComponent<BattleUI>();
            Assign(ui, ("enemyHud", enemyHud), ("playerHud", playerHud),
                ("enemySprite", enemySprite), ("playerSprite", playerSprite),
                ("message", message), ("menu", menu), ("moveMenu", moveMenu));

            var system = new GameObject("BattleSystem").AddComponent<BattleSystem>();
            Assign(system, ("controls", controls), ("ui", ui));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Debug.Log($"Escena de combate creada en {ScenePath} y agregada al build.");
        }

        // ---------- Fichas de PS ----------

        /// <summary>
        /// Ficha de PS con tres filas apiladas: nombre/nivel, barra y PS/estado. El padding descuenta
        /// el borde del marco (9 px) y deja un margen extra, así el texto nunca toca las Pokébolas.
        /// </summary>
        static BattleHUD CreateHud(Transform canvas, string name, Vector2 anchor, Vector2 position,
            Vector2 size, Sprite boxSprite, TMP_FontAsset font)
        {
            // A 2× el borde del marco mide 18 px (9 px de sprite); el relleno deja 6 px más de margen.
            RectTransform panel = CreatePixelBox(canvas, name, boxSprite, 2f);
            panel.anchorMin = panel.anchorMax = anchor;
            panel.pivot = anchor;
            panel.anchoredPosition = position;
            panel.sizeDelta = size;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Fila 1: nombre a la izquierda y nivel a la derecha (como en la Fig. 30 del documento).
            RectTransform topRow = CreateHudRow(panel, "FilaNombre", 26);
            TextMeshProUGUI nameText = CreateText(topRow, "Nombre", "—", 16, TextBlack);
            nameText.font = font;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI levelText = CreateText(topRow, "Nivel", "Nv 5", 14, TextBlack);
            levelText.font = font;
            levelText.alignment = TextAlignmentOptions.MidlineRight;

            // Fila 2: barra de PS. El relleno se estira desde la izquierda en BattleHUD.Show.
            RectTransform track = CreateImage(panel, "BarraPSFondo", HpTrackColor);
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
            RectTransform fill = CreateImage(track, "BarraPS", HpFillColor);
            Stretch(fill);

            // Fila 3: estado a la izquierda y PS actuales/máximos a la derecha.
            RectTransform bottomRow = CreateHudRow(panel, "FilaPS", 22);
            TextMeshProUGUI statusText = CreateText(bottomRow, "Estado", string.Empty, 12, TextBlack);
            statusText.font = font;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI hpText = CreateText(bottomRow, "TextoPS", "0/0", 14, TextBlack);
            hpText.font = font;
            hpText.alignment = TextAlignmentOptions.MidlineRight;

            var hud = panel.gameObject.AddComponent<BattleHUD>();
            Assign(hud, ("nameText", nameText), ("levelText", levelText), ("hpText", hpText),
                ("statusText", statusText), ("hpBar", fill.GetComponent<Image>()));
            return hud;
        }

        /// <summary>Fila horizontal de una ficha de PS, con altura fija para que no se pisen las filas.</summary>
        static RectTransform CreateHudRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = height;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return (RectTransform)row.transform;
        }

        // ---------- Menús ----------

        static BattleMenu CreateMenu(Transform canvas, string name, InputActionAsset controls, Sprite boxSprite,
            Sprite cursorSprite, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Vector2 size, Vector2 cellSize, int columns)
        {
            RectTransform panel = CreatePixelBox(canvas, name, boxSprite);
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.pivot = anchorMin;
            panel.anchoredPosition = position;
            panel.sizeDelta = size;

            var grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(6, 4);
            grid.padding = new RectOffset(20, 16, 10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;

            const int maxRows = 4;
            const float cursorScale = PixelScale;
            var rows = new Object[maxRows];
            var labels = new Object[maxRows];
            var details = new Object[maxRows];
            var cursors = new Object[maxRows];
            for (int i = 0; i < maxRows; i++)
            {
                var row = new GameObject($"Opcion{i + 1}", typeof(RectTransform));
                row.transform.SetParent(panel, false);
                var layout = row.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                var slot = new GameObject("CursorSlot", typeof(RectTransform));
                slot.transform.SetParent(row.transform, false);
                slot.AddComponent<LayoutElement>().preferredWidth = cursorSprite.rect.width * cursorScale;
                RectTransform cursor = CreatePixelImage(slot.transform, "Cursor", cursorSprite, cursorScale);
                cursor.anchorMin = cursor.anchorMax = cursor.pivot = new Vector2(0.5f, 0.5f);
                cursor.anchoredPosition = Vector2.zero;

                TextMeshProUGUI label = CreateText(row.transform, "Texto", "OPCIÓN", 22, TextBlack);
                label.font = font;
                label.fontStyle = FontStyles.UpperCase;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                TextMeshProUGUI detail = CreateText(row.transform, "Detalle", string.Empty, 20, TextBlack);
                detail.font = font;
                detail.alignment = TextAlignmentOptions.MidlineRight;

                rows[i] = row;
                labels[i] = label;
                details[i] = detail;
                cursors[i] = cursor.gameObject;
            }

            var menu = panel.gameObject.AddComponent<BattleMenu>();
            var canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            Assign(menu, ("controls", controls), ("group", canvasGroup), ("grid", grid));
            AssignArray(menu, "rows", rows);
            AssignArray(menu, "labels", labels);
            AssignArray(menu, "details", details);
            AssignArray(menu, "cursors", cursors);
            return menu;
        }

        // ---------- Cuadro de texto ----------

        static DialogueBox CreateDialogueBox(Transform parent, InputActionAsset controls, Sprite boxSprite,
            Sprite arrowSprite, TMP_FontAsset font)
        {
            RectTransform box = CreatePixelBox(parent, "CuadroMensajes", boxSprite);
            box.anchorMin = Vector2.zero;
            box.anchorMax = new Vector2(1, 0);
            box.offsetMin = new Vector2(8, 8);
            box.offsetMax = new Vector2(-8, 8 + 176);

            TextMeshProUGUI text = CreateText(box, "Texto", string.Empty, 22, TextBlack);
            text.font = font;
            text.fontStyle = FontStyles.UpperCase;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(48, 32);
            text.rectTransform.offsetMax = new Vector2(-48, -40);

            RectTransform arrow = CreatePixelImage(box, "FlechaContinuar", arrowSprite);
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(1, 0);
            arrow.anchoredPosition = new Vector2(-40, 30);

            var dialogue = box.gameObject.AddComponent<DialogueBox>();
            Assign(dialogue, ("controls", controls), ("text", text), ("continueArrow", arrow));
            return dialogue;
        }

        // ---------- Escena ----------

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0, 0, -10);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            go.AddComponent<AudioListener>();
        }

        static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            return go.transform;
        }

        static void CreateBackground(Transform canvas)
        {
            RectTransform sky = CreateImage(canvas, "Cielo", SkyColor);
            sky.anchorMin = new Vector2(0, 0.42f);
            sky.anchorMax = Vector2.one;
            sky.offsetMin = sky.offsetMax = Vector2.zero;
            sky.GetComponent<Image>().raycastTarget = false;

            RectTransform ground = CreateImage(canvas, "Suelo", GroundColor);
            ground.anchorMin = Vector2.zero;
            ground.anchorMax = new Vector2(1, 0.42f);
            ground.offsetMin = ground.offsetMax = Vector2.zero;
            ground.GetComponent<Image>().raycastTarget = false;
        }

        static Image CreateSpriteImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = rect.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false; // se rellena en tiempo de ejecución con el sprite de la especie
            return image;
        }

        // ---------- Importación de recursos ----------

        static void ConfigurePixelSprite(string path, int border)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            var borderVector = new Vector4(border, border, border, border);
            if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point &&
                importer.spriteBorder == borderVector)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = borderVector;
            importer.SaveAndReimport();
        }

        static void EnsurePixelFont()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath) != null)
                return;

            var font = AssetDatabase.LoadAssetAtPath<Font>(PixelFontPath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 32, 4, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic, false);
            asset.name = "PressStart2P SDF";
            AssetDatabase.CreateAsset(asset, PixelFontAssetPath);

            asset.atlasTextures[0].name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
        }

        // ---------- Utilidades ----------

        static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        static RectTransform CreatePixelBox(Transform parent, string name, Sprite sprite, float scale = PixelScale)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = sprite.pixelsPerUnit / 100f / scale;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform CreatePixelImage(Transform parent, string name, Sprite sprite, float scale = PixelScale)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            rect.sizeDelta = sprite.rect.size * scale;
            return rect;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Assign(Object target, params (string field, Object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name} no tiene el campo '{field}'.");
                    continue;
                }
                if (value == null)
                    Debug.LogError($"{target.GetType().Name}.{field} quedó sin asignar.");
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}