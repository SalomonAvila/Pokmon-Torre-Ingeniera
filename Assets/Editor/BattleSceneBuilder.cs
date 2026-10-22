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

        /// <summary>Margen interior de los menús: deja libre el borde ornamental de 36 px.</summary>
        const float MenuPadding = 40f;

        /// <summary>Separación entre el cursor y la etiqueta, y entre esta y la columna de detalle.</summary>
        const float MenuRowSpacing = 12f;

        /// <summary>El cursor se dibuja a 3× para que no compita con el texto de 22 px.</summary>
        const float CursorScale = 3f;

        /// <summary>Ancho reservado para las cantidades (x12, x15...).</summary>
        const float ItemDetailWidth = 84f;

        static readonly Color TextBlack = new Color32(0x10, 0x10, 0x10, 0xFF);
        static readonly Color SkyColor = new Color32(0x9A, 0xD0, 0xE8, 0xFF);
        static readonly Color GroundColor = new Color32(0x88, 0xC0, 0x70, 0xFF);
        static readonly Color HpTrackColor = new Color32(0x40, 0x40, 0x48, 0xFF);
        static readonly Color HpFillColor = new Color32(0x50, 0xC8, 0x50, 0xFF);
        static readonly Color ExpTrackColor = new Color32(0x40, 0x40, 0x48, 0xFF);
        static readonly Color ExpFillColor = new Color32(0x58, 0xB8, 0xF0, 0xFF);

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
                new Vector2(16, -16), new Vector2(380, 164), boxSprite, font, false);
            BattleHUD playerHud = CreateHud(canvas, "FichaJugador", new Vector2(1, 0),
                new Vector2(-16, 200), new Vector2(380, 184), boxSprite, font, true, true);

            DialogueBox message = CreateDialogueBox(canvas, controls, boxSprite, arrowSprite, font);

            // La franja inferior se reparte: el mensaje a la izquierda y el menú principal a la derecha.
            // La lista de movimientos/objetos hace lo contrario (lista a la izquierda, mensaje a la derecha).
            float split = DialogueBox.SplitRatio;
            BattleMenu menu = CreateMenu(canvas, "MenuPrincipal", controls, boxSprite, cursorSprite, arrowSprite, font,
                new Vector2(split, 0), new Vector2(1, 0), new Vector2(0, 8), new Vector2(-8, 184),
                new Vector2(183, 60), 2);
            BattleMenu moveMenu = CreateMenu(canvas, "MenuMovimientos", controls, boxSprite, cursorSprite, arrowSprite,
                font, new Vector2(0, 0), new Vector2(split, 0), new Vector2(8, 8), new Vector2(0, 184),
                new Vector2(475, 34), 1);

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
        /// Ficha de PS con tres filas apiladas: nombre/nivel, barra con etiqueta PS y PS/estado.
        /// Usa el mismo borde (36 px) que el cuadro inferior y deja un margen extra, así el texto
        /// nunca toca las Pokébolas.
        /// </summary>
        static BattleHUD CreateHud(Transform canvas, string name, Vector2 anchor, Vector2 position,
            Vector2 size, Sprite boxSprite, TMP_FontAsset font, bool showNumbers, bool showExpBar = false)
        {
            RectTransform panel = CreatePixelBox(canvas, name, boxSprite);
            panel.anchorMin = panel.anchorMax = anchor;
            panel.pivot = anchor;
            panel.anchoredPosition = position;
            panel.sizeDelta = size;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 38, 38);
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

            // Fila 2: etiqueta PS a la izquierda y barra. El relleno se estira desde la izquierda
            // en BattleHUD.Show.
            RectTransform barRow = CreateHudRow(panel, "FilaBarra", 18);
            TextMeshProUGUI psLabel = CreateText(barRow, "EtiquetaPS", "PS", 12, TextBlack);
            psLabel.font = font;
            psLabel.fontStyle = FontStyles.Bold;
            psLabel.alignment = TextAlignmentOptions.MidlineLeft;
            psLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 24f;

            RectTransform track = CreateImage(barRow, "BarraPSFondo", HpTrackColor);
            track.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            RectTransform fill = CreateImage(track, "BarraPS", HpFillColor);
            Stretch(fill);

            // Fila 3 (solo jugador): barra de EXP. Es (propuesta): la Fig. 30 no la muestra.
            Image expFill = null;
            if (showExpBar)
            {
                RectTransform expRow = CreateHudRow(panel, "FilaEXP", 12);
                TextMeshProUGUI expLabel = CreateText(expRow, "EtiquetaEXP", "EXP", 10, TextBlack);
                expLabel.font = font;
                expLabel.fontStyle = FontStyles.Bold;
                expLabel.alignment = TextAlignmentOptions.MidlineLeft;
                expLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 24f;

                RectTransform expTrack = CreateImage(expRow, "BarraEXPFondo", ExpTrackColor);
                expTrack.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                RectTransform expFillRect = CreateImage(expTrack, "BarraEXP", ExpFillColor);
                Stretch(expFillRect);
                expFill = expFillRect.GetComponent<Image>();
            }

            // Fila 4: estado a la izquierda y PS actuales/máximos a la derecha.
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
            if (expFill != null)
                Assign(hud, ("expBar", expFill));
            AssignBool(hud, "showHpNumbers", showNumbers);
            AssignBool(hud, "showExpBar", showExpBar);
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
            Sprite cursorSprite, Sprite scrollSprite, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Vector2 cellSize, int columns)
        {
            RectTransform panel = CreatePixelBox(canvas, name, boxSprite);
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.offsetMin = offsetMin;
            panel.offsetMax = offsetMax;

            var grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(6, 4);
            grid.padding = new RectOffset((int)MenuPadding, (int)MenuPadding, 10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;

            const int maxRows = 4;
            var rows = new Object[maxRows];
            var labels = new Object[maxRows];
            var details = new Object[maxRows];
            var cursors = new Object[maxRows];
            for (int i = 0; i < maxRows; i++)
            {
                var row = new GameObject($"Opcion{i + 1}", typeof(RectTransform));
                row.transform.SetParent(panel, false);
                var layout = row.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = MenuRowSpacing;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                var slot = new GameObject("CursorSlot", typeof(RectTransform));
                slot.transform.SetParent(row.transform, false);
                slot.AddComponent<LayoutElement>().preferredWidth = cursorSprite.rect.width * CursorScale;
                RectTransform cursor = CreatePixelImage(slot.transform, "Cursor", cursorSprite, CursorScale);
                cursor.anchorMin = cursor.anchorMax = cursor.pivot = new Vector2(0.5f, 0.5f);
                cursor.anchoredPosition = Vector2.zero;

                TextMeshProUGUI label = CreateText(row.transform, "Texto", "OPCIÓN", 22, TextBlack);
                label.font = font;
                label.fontStyle = FontStyles.UpperCase;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.overflowMode = TextOverflowModes.Ellipsis; // los nombres largos se recortan con puntos
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                TextMeshProUGUI detail = CreateText(row.transform, "Detalle", string.Empty, 20, TextBlack);
                detail.font = font;
                detail.alignment = TextAlignmentOptions.MidlineRight;
                detail.overflowMode = TextOverflowModes.Ellipsis;
                detail.gameObject.AddComponent<LayoutElement>().preferredWidth = ItemDetailWidth;

                rows[i] = row;
                labels[i] = label;
                details[i] = detail;
                cursors[i] = cursor.gameObject;
            }

            GameObject scrollUp = CreateScrollArrow(panel, "FlechaArriba", scrollSprite,
                new Vector2(1f, 1f), new Vector2(-16f, -9f), 180f);
            GameObject scrollDown = CreateScrollArrow(panel, "FlechaAbajo", scrollSprite,
                new Vector2(1f, 0f), new Vector2(-16f, 9f), 0f);

            var menu = panel.gameObject.AddComponent<BattleMenu>();
            var canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            Assign(menu, ("controls", controls), ("group", canvasGroup), ("grid", grid),
                ("scrollUp", scrollUp), ("scrollDown", scrollDown));
            AssignArray(menu, "rows", rows);
            AssignArray(menu, "labels", labels);
            AssignArray(menu, "details", details);
            AssignArray(menu, "cursors", cursors);
            return menu;
        }

        /// <summary>
        /// Flecha de desplazamiento en la esquina superior o inferior derecha, dentro del marco y
        /// sobre el fondo claro (el borde es oscuro y la flecha no se vería encima).
        /// </summary>
        static GameObject CreateScrollArrow(Transform parent, string name, Sprite sprite, Vector2 anchor,
            Vector2 position, float angle)
        {
            RectTransform rect = CreatePixelImage(parent, name, sprite, 2f);
            rect.anchorMin = rect.anchorMax = anchor;
            // Con el pivote abajo, la flecha de abajo crece hacia dentro; la de arriba se gira 180°
            // alrededor de ese pivote y también queda dentro del marco.
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // no es una celda de la rejilla
            rect.gameObject.SetActive(false);
            return rect.gameObject;
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
            text.lineSpacing = 0f;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            // Mismo margen interior (48 px) en el cuadro completo y en las dos mitades.
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
            // Se ajusta por ancho para que las mitades de la franja inferior midan lo mismo en 16:9 y 3:2.
            scaler.matchWidthOrHeight = 0f;
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
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath);
            if (asset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(PixelFontPath);
                asset = TMP_FontAsset.CreateFontAsset(font, 32, 4, GlyphRenderMode.SDFAA, 512, 512,
                    AtlasPopulationMode.Dynamic, false);
                asset.name = "PressStart2P SDF";
                AssetDatabase.CreateAsset(asset, PixelFontAssetPath);

                asset.atlasTextures[0].name = asset.name + " Atlas";
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            // Los textos de la interfaz van en mayúsculas: asegura las vocales acentuadas y la eñe
            // (por ejemplo POKÉBOLA y POKÉMON) en el atlas dinámico. El texto fuente ya se escribe con
            // las mayúsculas acentuadas, así que el glifo debe existir sí o sí.
            const string UppercaseLatin = "ÁÉÍÓÚÜÑ¿¡";
            if (!asset.TryAddCharacters(UppercaseLatin, out string missing) && !string.IsNullOrEmpty(missing))
                Debug.LogWarning($"La fuente no tiene estos glifos: {missing}");

            if (!asset.HasCharacter('É') || !asset.HasCharacter('Ñ'))
                Debug.LogError("La fuente pixel no incluye É o Ñ; POKÉBOLA/POKÉMON se verían mal.");

            EditorUtility.SetDirty(asset);
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

        static void AssignBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} no tiene el campo '{field}'.");
                return;
            }
            property.boolValue = value;
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

        static void AddSceneToBuild(string path) => BuildScenes.Register(path);
    }
}