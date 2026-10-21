using System.Linq;
using TMPro;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Genera la escena de la pantalla de título: imagen a pantalla completa con "PRESS START" brillando
    /// y, al presionar Enter, la pantalla que revisa la partida guardada con un cuadro de texto estilo
    /// Pokémon clásico.
    /// Se ejecuta desde el menú del editor: Torre Ingeniería > Construir escena Menú Principal.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/" + SceneNames.MainMenu + ".unity";
        const string ControlsPath = "Assets/Input/ControlesJuego.inputactions";
        const string TitleImagePath = "Assets/Art/UI/PantallaTitulo.png";
        const string MontoyaPath = "Assets/Art/Characters/AurelioMontoya.png";
        const string PedroPath = "Assets/Art/Characters/PedroPerez.png";
        const string DialogBoxPath = "Assets/Art/UI/CuadroDialogo.png";
        const string ContinueArrowPath = "Assets/Art/UI/FlechaContinuar.png";
        const string OptionCursorPath = "Assets/Art/UI/CursorOpcion.png";
        const string PixelFontPath = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
        const string PixelFontAssetPath = "Assets/Art/Fonts/PressStart2P SDF.asset";

        // Medidas de la imagen de título (1024x572) y de su franja oscura inferior, en píxeles.
        static readonly Vector2 ReferenceResolution = new Vector2(1024, 576);
        const float ImageAspect = 1024f / 572f;
        const float BandBottom = 6f;
        const float BandHeight = 50f;

        // Los sprites pixel art se dibujan a 1 px = 4 unidades del canvas (como 256x144 escalado x4).
        const float PixelScale = 4f;
        const int DialogBoxBorder = 9;

        // Colores tomados de la imagen de título y del cuadro de texto.
        static readonly Color Beige = new Color32(0xF1, 0xCC, 0xA2, 0xFF);
        static readonly Color BandColor = new Color32(0x0A, 0x07, 0x05, 0xF0);
        static readonly Color TextBlack = new Color32(0x10, 0x10, 0x10, 0xFF);
        static readonly Color ScreenWhite = new Color32(0xF8, 0xF8, 0xF8, 0xFF); // mismo blanco del cuadro de texto

        // HUD de los mapas: azul ingeniería y amarillo de seguridad (Doc §5).
        const float HudBarHeight = 40f;
        static readonly Color HudBarColor = new Color32(0x0A, 0x1F, 0x3A, 0xE6);
        static readonly Color HudTextColor = new Color32(0xF8, 0xD0, 0x30, 0xFF);

        [MenuItem("Torre Ingeniería/Construir escena Menú Principal")]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                EditorUtility.DisplayDialog("TextMeshPro",
                    "Se están importando los recursos esenciales de TextMeshPro. " +
                    "Cuando termine, vuelve a ejecutar Torre Ingeniería > Construir escena Menú Principal.", "OK");
                return;
            }

            var missing = new[] { ControlsPath, TitleImagePath, MontoyaPath, PedroPath, DialogBoxPath, ContinueArrowPath, OptionCursorPath, PixelFontPath }
                .Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Menú principal", "Faltan recursos:\n" + string.Join("\n", missing), "OK");
                return;
            }

            ConfigureSprite(TitleImagePath);
            ConfigureSprite(MontoyaPath);
            ConfigurePixelSprite(DialogBoxPath, DialogBoxBorder);
            ConfigurePixelSprite(ContinueArrowPath, 0);
            ConfigurePixelSprite(OptionCursorPath, 0);
            EnsurePixelFont();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene descarga los assets sin uso, así que se cargan después para no guardar referencias vacías.
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath);
            var dialogBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DialogBoxPath);
            var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ContinueArrowPath);
            var cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OptionCursorPath);

            CreateCamera();
            Transform canvas = CreateCanvas();
            CreateBackground(canvas, AssetDatabase.LoadAssetAtPath<Sprite>(TitleImagePath));

            // Franja propia que tapa el "PRESS START" dibujado en la imagen, para poder animarlo.
            RectTransform band = CreateImage(canvas, "Franja", BandColor);
            band.anchorMin = Vector2.zero;
            band.anchorMax = new Vector2(1, 0);
            band.offsetMin = new Vector2(0, BandBottom);
            band.offsetMax = new Vector2(0, BandBottom + BandHeight);

            TextMeshProUGUI pressStart = CreateText(band, "PressStart", "PULSA ENTER", 32, Beige);
            pressStart.fontStyle = FontStyles.Bold;
            pressStart.alignment = TextAlignmentOptions.Center;
            pressStart.characterSpacing = 8;
            Stretch(pressStart.rectTransform);

            // Pantalla de partida: Aurelio Montoya (y Pedro, que aparece en una partida nueva),
            // cuadro de texto y cuadro de opciones.
            CanvasGroup saveScreen = CreateOverlay(canvas, "PantallaPartida", ScreenWhite, 0f);
            CanvasGroup montoya = CreateCharacter(saveScreen.transform, "AurelioMontoya", LoadSprite(MontoyaPath), 1f);
            CanvasGroup pedro = CreateCharacter(saveScreen.transform, "PedroPerez", LoadSprite(PedroPath), 0f);
            DialogueBox dialogue = CreateDialogueBox(saveScreen.transform, controls, dialogBoxSprite, arrowSprite, pixelFont);
            ChoiceBox choices = CreateChoiceBox(saveScreen.transform, controls, dialogBoxSprite, cursorSprite, pixelFont);
            var saveCheck = saveScreen.gameObject.AddComponent<SaveCheckController>();
            Assign(saveCheck, ("dialogue", dialogue), ("choices", choices),
                ("hostPortrait", montoya), ("playerPortrait", pedro));

            CanvasGroup fader = CreateOverlay(canvas, "Fundido", Color.black, 1f);

            var controller = new GameObject("TitleScreenController").AddComponent<TitleScreenController>();
            Assign(controller, ("controls", controls), ("pressStartText", pressStart),
                ("fader", fader), ("saveScreen", saveScreen), ("saveCheck", saveCheck));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildFirst(ScenePath);
            Debug.Log($"Escena creada en {ScenePath} y agregada como primera escena del build.");
        }

        // ---------- Cuadro de texto para los mapas ----------

        /// <summary>Prepara el marco, la flecha y la fuente del cuadro de texto. Llamar antes de crear la escena.</summary>
        public static void PrepareDialogueAssets()
        {
            ConfigurePixelSprite(DialogBoxPath, DialogBoxBorder);
            ConfigurePixelSprite(ContinueArrowPath, 0);
            EnsurePixelFont();
        }

        /// <summary>El mismo cuadro de texto de la pantalla de partida, para usarlo en los mapas (WorldMessage).</summary>
        public static DialogueBox CreateMapDialogueBox(Transform parent, InputActionAsset controls) =>
            CreateDialogueBox(parent, controls,
                AssetDatabase.LoadAssetAtPath<Sprite>(DialogBoxPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(ContinueArrowPath),
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath));

        /// <summary>Cuadro de opciones (SÍ / NO…) para los NPC de los mapas (WorldMessage).</summary>
        public static ChoiceBox CreateMapChoiceBox(Transform parent, InputActionAsset controls) =>
            CreateChoiceBox(parent, controls,
                AssetDatabase.LoadAssetAtPath<Sprite>(DialogBoxPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(OptionCursorPath),
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath));

        /// <summary>
        /// Barra superior de los mapas (piso, sellos y créditos). Vive en su propio Canvas
        /// para que el fundido la tape y el cuadro de texto no la mueva.
        /// </summary>
        public static HUD CreateMapHud(Transform parent)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath);

            RectTransform bar = CreateImage(parent, "BarraHUD", HudBarColor);
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = Vector2.one;
            bar.offsetMin = new Vector2(0, -HudBarHeight);
            bar.offsetMax = Vector2.zero;
            bar.GetComponent<Image>().raycastTarget = false;

            TextMeshProUGUI floor = CreateHudText(bar, "Piso", "PISO 1", font, TextAlignmentOptions.MidlineLeft);
            floor.rectTransform.anchorMin = floor.rectTransform.anchorMax = new Vector2(0, 0.5f);
            floor.rectTransform.pivot = new Vector2(0, 0.5f);
            floor.rectTransform.anchoredPosition = new Vector2(24, 0);
            floor.rectTransform.sizeDelta = new Vector2(480, HudBarHeight);

            TextMeshProUGUI seals = CreateHudText(bar, "Sellos", "SELLOS 0", font, TextAlignmentOptions.MidlineRight);
            seals.rectTransform.anchorMin = seals.rectTransform.anchorMax = new Vector2(1, 0.5f);
            seals.rectTransform.pivot = new Vector2(1, 0.5f);
            seals.rectTransform.anchoredPosition = new Vector2(-256, 0);
            seals.rectTransform.sizeDelta = new Vector2(220, HudBarHeight);

            TextMeshProUGUI credits = CreateHudText(bar, "Creditos", "CRÉDITOS 0", font, TextAlignmentOptions.MidlineRight);
            credits.rectTransform.anchorMin = credits.rectTransform.anchorMax = new Vector2(1, 0.5f);
            credits.rectTransform.pivot = new Vector2(1, 0.5f);
            credits.rectTransform.anchoredPosition = new Vector2(-24, 0);
            credits.rectTransform.sizeDelta = new Vector2(220, HudBarHeight);

            var hud = bar.gameObject.AddComponent<HUD>();
            Assign(hud, ("floorText", floor), ("sealsText", seals), ("creditsText", credits));
            return hud;
        }

        static TextMeshProUGUI CreateHudText(Transform parent, string name, string text, TMP_FontAsset font,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = CreateText(parent, name, text, 20, HudTextColor);
            label.font = font;
            label.fontStyle = FontStyles.Bold;
            label.alignment = alignment;
            return label;
        }

        // ---------- Importación de recursos ----------

        /// <summary>
        /// Importa la imagen como Sprite (editable en el Sprite Editor), sin compresión ni mipmaps
        /// y sin reducir su resolución.
        /// </summary>
        static void ConfigureSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            if (importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed && importer.maxTextureSize >= 4096)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Importa un sprite pixel art: filtro Point (píxeles nítidos) y, si tiene borde, 9-slice para estirarlo.
        /// </summary>
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

        /// <summary>Crea (una sola vez) la fuente TextMeshPro de Press Start 2P.</summary>
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

        static void CreateBackground(Transform canvas, Sprite sprite)
        {
            // Cubre toda la pantalla sin deformar la imagen (recorta los lados si hace falta).
            RectTransform background = CreateImage(canvas, "Fondo", Color.white);
            var image = background.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0.5f);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ImageAspect;
        }

        /// <summary>
        /// Personaje centrado por encima del cuadro de texto: la parte visible del sprite (sin los márgenes
        /// transparentes) ocupa del 33 % al 98 % del alto de la pantalla, sea cual sea el tamaño de la imagen.
        /// </summary>
        static CanvasGroup CreateCharacter(Transform parent, string name, Sprite sprite, float alpha)
        {
            const float screenBottom = 0.33f;
            const float screenTop = 0.98f;

            // Límites de la parte visible, en fracción del sprite, a partir de su malla ajustada.
            Vector2 min = new Vector2(sprite.vertices.Min(v => v.x), sprite.vertices.Min(v => v.y)) * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 max = new Vector2(sprite.vertices.Max(v => v.x), sprite.vertices.Max(v => v.y)) * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 visibleMin = min / sprite.rect.size;
            Vector2 visibleMax = max / sprite.rect.size;

            float height = (screenTop - screenBottom) / (visibleMax.y - visibleMin.y); // fracción de la pantalla
            float bottom = screenBottom - visibleMin.y * height;
            float width = sprite.rect.width / sprite.rect.height * ReferenceResolution.y * height;

            RectTransform rect = CreateImage(parent, name, Color.white);
            rect.anchorMin = new Vector2(0.5f, bottom);
            rect.anchorMax = new Vector2(0.5f, bottom + height);
            rect.sizeDelta = new Vector2(width, 0);
            rect.anchoredPosition = new Vector2((0.5f - (visibleMin.x + visibleMax.x) / 2f) * width, 0);

            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            return group;
        }

        /// <summary>Primer sprite de la imagen (sirve tanto en modo Single como Multiple).</summary>
        static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

        static DialogueBox CreateDialogueBox(Transform parent, InputActionAsset controls, Sprite boxSprite, Sprite arrowSprite, TMP_FontAsset font)
        {
            // Ancho completo en la parte inferior, como en Pokémon Rojo/Azul.
            RectTransform box = CreatePixelBox(parent, "CuadroTexto", boxSprite);
            box.anchorMin = Vector2.zero;
            box.anchorMax = new Vector2(1, 0);
            box.offsetMin = new Vector2(8, 8);
            box.offsetMax = new Vector2(-8, 8 + 176);

            TextMeshProUGUI text = CreateText(box, "Texto", string.Empty, 24, TextBlack);
            text.font = font;
            text.fontStyle = FontStyles.UpperCase;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 60;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(60, 40);
            text.rectTransform.offsetMax = new Vector2(-60, -48);

            RectTransform arrow = CreatePixelImage(box, "FlechaContinuar", arrowSprite);
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(1, 0);
            arrow.anchoredPosition = new Vector2(-48, 36);

            var dialogue = box.gameObject.AddComponent<DialogueBox>();
            Assign(dialogue, ("controls", controls), ("text", text), ("continueArrow", arrow));
            return dialogue;
        }

        static ChoiceBox CreateChoiceBox(Transform parent, InputActionAsset controls, Sprite boxSprite, Sprite cursorSprite, TMP_FontAsset font)
        {
            // Esquina inferior derecha, justo encima del cuadro de texto.
            RectTransform box = CreatePixelBox(parent, "CuadroOpciones", boxSprite);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(1, 0);
            box.anchoredPosition = new Vector2(-8, 8 + 176 + 4);
            box.sizeDelta = new Vector2(400, 160);

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 40, 40, 40);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var labels = new Object[2];
            var cursors = new Object[2];
            for (int i = 0; i < labels.Length; i++)
            {
                var row = new GameObject($"Opcion{i + 1}", typeof(RectTransform));
                row.transform.SetParent(box, false);
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 12;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;

                // El cursor vive dentro de un espacio fijo para que el texto no se mueva al ocultarlo.
                var slot = new GameObject("CursorSlot", typeof(RectTransform));
                slot.transform.SetParent(row.transform, false);
                slot.AddComponent<LayoutElement>().preferredWidth = cursorSprite.rect.width * PixelScale;
                RectTransform cursor = CreatePixelImage(slot.transform, "Cursor", cursorSprite);
                cursor.anchorMin = cursor.anchorMax = cursor.pivot = new Vector2(0.5f, 0.5f);
                cursor.anchoredPosition = Vector2.zero;

                TextMeshProUGUI label = CreateText(row.transform, "Texto", "OPCIÓN", 24, TextBlack);
                label.font = font;
                label.fontStyle = FontStyles.UpperCase;
                label.alignment = TextAlignmentOptions.MidlineLeft;

                labels[i] = label;
                cursors[i] = cursor.gameObject;
            }

            var choiceBox = box.gameObject.AddComponent<ChoiceBox>();
            Assign(choiceBox, ("controls", controls));
            AssignArray(choiceBox, "optionLabels", labels);
            AssignArray(choiceBox, "cursors", cursors);
            box.gameObject.SetActive(false);
            return choiceBox;
        }

        static CanvasGroup CreateOverlay(Transform canvas, string name, Color color, float alpha)
        {
            RectTransform rect = CreateImage(canvas, name, color);
            Stretch(rect);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = false;
            return group;
        }

        // ---------- Utilidades ----------

        static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        /// <summary>Caja 9-slice con los píxeles del borde ampliados a la escala del juego.</summary>
        static RectTransform CreatePixelBox(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = sprite.pixelsPerUnit / 100f / PixelScale;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>Imagen pixel art a su tamaño nativo multiplicado por la escala del juego.</summary>
        static RectTransform CreatePixelImage(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            rect.sizeDelta = sprite.rect.size * PixelScale;
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

        static void AddSceneToBuildFirst(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
