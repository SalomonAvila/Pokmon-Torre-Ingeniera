using System.Collections.Generic;
using System.IO;
using TMPro;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Genera el prefab del menú del juego (<c>Assets/Resources/UI/MenuJuego.prefab</c>): menú de pausa,
    /// Equipo, Resumen, Mochila, cuadro de mensajes y submenú de acciones. <see cref="GameMenu"/> lo
    /// instancia la primera vez que el jugador abre el menú, así los mapas no necesitan regenerarse.
    ///
    /// Distribución de las maquetas de <c>Docs/Maquetas/</c> (01 a 07) en el canvas de 1024×576 (256×144 px
    /// a escala 4, como el título y el combate). El arte de <c>Assets/Art/UI/Menu/</c> es opcional: si un
    /// archivo existe se usa (fondos en mosaico, fichas del equipo, mochila por bolsillo); si no, se pintan
    /// colores planos de la paleta. Volver a ejecutar el builder tras añadir arte lo aplica.
    ///
    /// Menú: Torre Ingeniería > Construir menú del juego.
    /// </summary>
    public static class GameMenuBuilder
    {
        const string PrefabFolder = "Assets/Resources/UI";
        const string PrefabPath = PrefabFolder + "/MenuJuego.prefab";
        const string ControlsPath = "Assets/Input/ControlesJuego.inputactions";
        const string DialogBoxPath = "Assets/Art/UI/CuadroDialogo.png";
        const string OptionCursorPath = "Assets/Art/UI/CursorOpcion.png";
        const string ContinueArrowPath = "Assets/Art/UI/FlechaContinuar.png";
        const string PixelFontAssetPath = "Assets/Art/Fonts/PressStart2P SDF.asset";

        // Arte opcional de los menús (lo genera el equipo de arte; ver Docs/Plan de fases.md, Etapa 4).
        const string MenuArtFolder = "Assets/Art/UI/Menu";
        const string TeamBackgroundPath = MenuArtFolder + "/FondoEquipo.png";
        const string SummaryBackgroundPath = MenuArtFolder + "/FondoResumen.png";
        const string BagBackgroundPath = MenuArtFolder + "/FondoMochila.png";
        const string SlotNormalPath = MenuArtFolder + "/FichaEquipo.png";
        const string SlotSelectedPath = MenuArtFolder + "/FichaEquipoSeleccionada.png";
        const string SlotEmptyPath = MenuArtFolder + "/FichaEquipoVacia.png";
        const string SlotFaintedPath = MenuArtFolder + "/FichaEquipoDebilitada.png";
        static readonly string[] BagPaths =
        {
            MenuArtFolder + "/MochilaObjetos.png",
            MenuArtFolder + "/MochilaPokebolas.png",
            MenuArtFolder + "/MochilaClave.png",
        };

        /// <summary>Borde 9-slice de las fichas del equipo, en píxeles del PNG.</summary>
        const int SlotBorder = 4;

        const float PixelScale = 4f;
        const int DialogBoxBorder = 9;

        /// <summary>Alto del cuadro de mensajes del menú (más bajo que el de los mapas: deja sitio al equipo).</summary>
        const float MessageHeight = 136f;

        static readonly Color TextBlack = new Color32(0x10, 0x10, 0x10, 0xFF);
        static readonly Color TextWhite = new Color32(0xF8, 0xF8, 0xF8, 0xFF);
        static readonly Color Navy = new Color32(0x18, 0x3A, 0x68, 0xFF);
        static readonly Color SkyBlue = new Color32(0x4A, 0x86, 0xC8, 0xFF);
        static readonly Color SpriteBackdrop = new Color32(0x9A, 0xD0, 0xE8, 0xFF);
        static readonly Color BarTrack = new Color32(0x40, 0x40, 0x48, 0xFF);
        static readonly Color ExpFill = new Color32(0x58, 0xB8, 0xF0, 0xFF);

        static TMP_FontAsset font;
        static Sprite boxSprite;
        static Sprite cursorSprite;
        static Sprite arrowSprite;
        static InputActionAsset controls;

        [MenuItem("Torre Ingeniería/Construir menú del juego")]
        public static void Build()
        {
            MainMenuSceneBuilder.PrepareDialogueAssets(); // marco, flecha y fuente pixel
            ConfigurePixelSprite(OptionCursorPath, 0);

            controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            boxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DialogBoxPath);
            cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OptionCursorPath);
            arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ContinueArrowPath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontAssetPath);
            if (controls == null || boxSprite == null || cursorSprite == null || arrowSprite == null || font == null)
            {
                EditorUtility.DisplayDialog("Menú del juego",
                    "Faltan recursos de la interfaz (controles, cuadro de diálogo, cursor o fuente pixel).", "OK");
                return;
            }

            // El prefab se arma en una escena de vista previa para no tocar la escena abierta.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = CreateCanvasRoot();
                SceneManager.MoveGameObjectToScene(root, preview);

                TeamScreen team = CreateTeamScreen(root.transform);
                SummaryScreen summary = CreateSummaryScreen(root.transform);
                BagScreen bag = CreateBagScreen(root.transform);
                MenuList pauseList = CreatePauseList(root.transform);
                DialogueBox message = CreateMessageBox(root.transform);
                MenuList actionList = CreateActionList(root.transform);

                var menu = root.AddComponent<GameMenu>();
                Assign(menu, ("controls", controls), ("pauseList", pauseList), ("message", message),
                    ("actionList", actionList), ("team", team), ("summary", summary), ("bag", bag));

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Menú del juego creado en {PrefabPath}. En un mapa, en Play, ábrelo con Esc/Tab.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static GameObject CreateCanvasRoot()
        {
            var root = new GameObject("MenuJuego", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // encima del HUD (10) y debajo del fundido (FadeManager)
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            return root;
        }

        // ---------- Equipo (maquetas 02 y 03) ----------

        static TeamScreen CreateTeamScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Equipo", Navy, TeamBackgroundPath);

            Sprite normal = LoadOptionalSprite(SlotNormalPath, SlotBorder);
            Sprite selected = LoadOptionalSprite(SlotSelectedPath, SlotBorder);
            Sprite empty = LoadOptionalSprite(SlotEmptyPath, SlotBorder);
            Sprite fainted = LoadOptionalSprite(SlotFaintedPath, SlotBorder);

            var slots = new Object[GameRules.MaxTeamSize];
            slots[0] = CreateSlot(screen, "Lider", 16, 56, 400, 200, true, normal, selected, empty, fainted);
            for (int i = 1; i < slots.Length; i++)
                slots[i] = CreateSlot(screen, $"Pokemon{i + 1}", 432, 12 + (i - 1) * 68, 576, 62, false,
                    normal, selected, empty, fainted);

            RectTransform exit = CreatePanel(screen, "Salir", BoneWhite);
            TopLeft(exit, 808, 356, 200, 52);
            TextMeshProUGUI exitLabel = CreateText(exit, "Texto", "SALIR", 22, TextBlack);
            Stretch(exitLabel.rectTransform);
            exitLabel.alignment = TextAlignmentOptions.Center;

            var team = screen.gameObject.AddComponent<TeamScreen>();
            Assign(team, ("controls", controls), ("exitBackground", exit.GetComponent<Image>()));
            AssignArray(team, "slots", slots);
            screen.gameObject.SetActive(false);
            return team;
        }

        static TeamSlotView CreateSlot(Transform parent, string name, float x, float y, float width, float height,
            bool lead, Sprite normal, Sprite selected, Sprite empty, Sprite fainted)
        {
            RectTransform slot = CreatePanel(parent, name, BoneWhite);
            TopLeft(slot, x, y, width, height);
            if (normal != null)
            {
                Image background = slot.GetComponent<Image>();
                background.sprite = normal;
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = normal.pixelsPerUnit / 100f / PixelScale;
                Object.DestroyImmediate(slot.GetComponent<Outline>()); // el marco dibujado ya trae borde
            }

            float iconSize = lead ? 64 : 48;
            RectTransform icon = CreateImage(slot, "Icono", Color.white);
            TopLeft(icon, lead ? 16 : 8, lead ? 16 : (height - iconSize) / 2f, iconSize, iconSize);
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().enabled = false;

            float textX = lead ? 96 : 64;
            TextMeshProUGUI nameText = CreateText(slot, "Nombre", "POKÉMON", lead ? 22 : 20, TextBlack);
            TopLeft(nameText.rectTransform, textX, lead ? 20 : 6, lead ? 290 : 300, 28);
            TextMeshProUGUI levelText = CreateText(slot, "Nivel", "Nv5", 16, TextBlack);
            TopLeft(levelText.rectTransform, textX, lead ? 58 : 36, 140, 22);

            // Fila de PS: etiqueta, barra y números. En la ficha del líder va debajo; en las demás, a la derecha.
            var hpRow = new GameObject("FilaPS", typeof(RectTransform));
            hpRow.transform.SetParent(slot, false);
            var row = (RectTransform)hpRow.transform;
            if (lead)
                TopLeft(row, 24, 112, width - 48, 72);
            else
                TopLeft(row, width - 220, 6, 204, 52);

            TextMeshProUGUI hpLabel = CreateText(row, "EtiquetaPS", "PS", 14, TextBlack);
            hpLabel.fontStyle = FontStyles.Bold;
            TopLeft(hpLabel.rectTransform, 0, 0, 40, 16);
            RectTransform track = CreateImage(row, "BarraPSFondo", BarTrack);
            TopLeft(track, 40, 2, row.sizeDelta.x - 40, lead ? 12 : 10);
            RectTransform fill = CreateImage(track, "BarraPS", HpBar.Green);
            Stretch(fill);
            TextMeshProUGUI hpText = CreateText(row, "TextoPS", "0/0", lead ? 22 : 18, TextBlack);
            hpText.alignment = TextAlignmentOptions.TopRight;
            TopLeft(hpText.rectTransform, 0, lead ? 32 : 24, row.sizeDelta.x, 28);

            var view = slot.gameObject.AddComponent<TeamSlotView>();
            Assign(view, ("background", slot.GetComponent<Image>()), ("icon", icon.GetComponent<Image>()),
                ("nameText", nameText), ("levelText", levelText), ("hpText", hpText),
                ("hpBar", fill.GetComponent<Image>()), ("hpRow", hpRow));
            AssignOptional(view, ("normalSprite", normal), ("selectedSprite", selected),
                ("emptySprite", empty), ("faintedSprite", fainted));
            return view;
        }

        // ---------- Resumen (maquetas 04, 05 y 06) ----------

        static SummaryScreen CreateSummaryScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Resumen", SkyBlue, SummaryBackgroundPath);

            RectTransform header = CreateImage(screen, "Cabecera", Palette.EngineeringBlue);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = Vector2.one;
            header.offsetMin = new Vector2(0, -52);
            header.offsetMax = Vector2.zero;
            TextMeshProUGUI title = CreateText(header, "Titulo", "INFO POKÉMON", 24, TextWhite);
            TopLeft(title.rectTransform, 16, 12, 420, 32);

            string[] tabNames = { "INFO", "ESTADÍSTICAS", "MOVIMIENTOS" };
            var tabs = new Object[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                RectTransform tab = CreateImage(header, $"Pestana{i + 1}", Palette.ConcreteGray);
                TopLeft(tab, 512 + i * 168, 8, 160, 36);
                TextMeshProUGUI label = CreateText(tab, "Texto", tabNames[i], 14, TextBlack);
                Stretch(label.rectTransform);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 8;
                label.fontSizeMax = 14;
                tabs[i] = tab.GetComponent<Image>();
            }

            // Izquierda: sprite frontal, nombre y nivel.
            RectTransform portrait = CreatePanel(screen, "Retrato", SpriteBackdrop);
            TopLeft(portrait, 16, 68, 400, 300);
            RectTransform sprite = CreateImage(portrait, "Sprite", Color.white);
            sprite.anchorMin = sprite.anchorMax = new Vector2(0.5f, 0.5f);
            sprite.anchoredPosition = new Vector2(0, 20);
            sprite.sizeDelta = new Vector2(192, 192);
            sprite.GetComponent<Image>().preserveAspect = true;
            sprite.GetComponent<Image>().enabled = false;
            TextMeshProUGUI noSprite = CreateText(portrait, "SinSprite", "SIN SPRITE", 14, Palette.DarkGray);
            noSprite.alignment = TextAlignmentOptions.Center;
            noSprite.rectTransform.anchorMin = noSprite.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            noSprite.rectTransform.anchoredPosition = new Vector2(0, 20);
            noSprite.rectTransform.sizeDelta = new Vector2(360, 24);
            TextMeshProUGUI nameText = CreateText(portrait, "Nombre", "POKÉMON", 22, TextBlack);
            TopLeft(nameText.rectTransform, 16, 256, 260, 32);
            TextMeshProUGUI levelText = CreateText(portrait, "Nivel", "Nv5", 22, TextBlack);
            levelText.alignment = TextAlignmentOptions.TopRight;
            TopLeft(levelText.rectTransform, 276, 256, 108, 32);

            // Derecha: tabla de seis filas (etiqueta, valor y detalle).
            const int rowCount = 6;
            var rows = new Object[rowCount];
            var labels = new Object[rowCount];
            var values = new Object[rowCount];
            var details = new Object[rowCount];
            var cursors = new Object[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                var row = new GameObject($"Fila{i + 1}", typeof(RectTransform));
                row.transform.SetParent(screen, false);
                var rect = (RectTransform)row.transform;
                TopLeft(rect, 452, 68 + i * 50, 556, 44);

                RectTransform labelCell = CreateImage(rect, "Etiqueta", Palette.EngineeringBlue);
                TopLeft(labelCell, 0, 0, 196, 44);
                TextMeshProUGUI label = CreateText(labelCell, "Texto", "ETIQUETA", 16, TextWhite);
                Stretch(label.rectTransform);
                label.alignment = TextAlignmentOptions.Center;

                RectTransform valueCell = CreateImage(rect, "Valor", BoneWhite);
                TopLeft(valueCell, 200, 0, 356, 44);
                TextMeshProUGUI value = CreateText(valueCell, "Texto", "VALOR", 18, TextBlack);
                Stretch(value.rectTransform);
                value.rectTransform.offsetMin = new Vector2(12, 0);
                value.alignment = TextAlignmentOptions.MidlineLeft;
                value.overflowMode = TextOverflowModes.Ellipsis;
                TextMeshProUGUI detail = CreateText(valueCell, "Detalle", string.Empty, 16, TextBlack);
                Stretch(detail.rectTransform);
                detail.rectTransform.offsetMax = new Vector2(-12, 0);
                detail.alignment = TextAlignmentOptions.MidlineRight;

                RectTransform cursor = CreatePixelImage(rect, "Cursor", cursorSprite);
                cursor.anchorMin = cursor.anchorMax = new Vector2(0, 0.5f);
                cursor.pivot = new Vector2(1, 0.5f);
                cursor.anchoredPosition = new Vector2(-4, 0);
                cursor.gameObject.SetActive(false);

                rows[i] = row;
                labels[i] = label;
                values[i] = value;
                details[i] = detail;
                cursors[i] = cursor.gameObject;
            }

            // Abajo: recuadro con título, texto y barra de EXP (solo en ESTADÍSTICAS).
            RectTransform footer = CreatePanel(screen, "Recuadro", BoneWhite);
            footer.anchorMin = Vector2.zero;
            footer.anchorMax = new Vector2(1, 0);
            footer.offsetMin = new Vector2(16, 12);
            footer.offsetMax = new Vector2(-16, 192);
            RectTransform footerTitleCell = CreateImage(footer, "Titulo", Palette.EngineeringBlue);
            TopLeft(footerTitleCell, 0, 0, 420, 40);
            TextMeshProUGUI footerTitle = CreateText(footerTitleCell, "Texto", "DESCRIPCIÓN", 18, TextWhite);
            Stretch(footerTitle.rectTransform);
            footerTitle.rectTransform.offsetMin = new Vector2(16, 0);
            footerTitle.alignment = TextAlignmentOptions.MidlineLeft;
            TextMeshProUGUI footerText = CreateText(footer, "Texto", string.Empty, 18, TextBlack);
            footerText.textWrappingMode = TextWrappingModes.Normal;
            footerText.lineSpacing = 20;
            TopLeft(footerText.rectTransform, 24, 52, 944, 96);

            var expRow = new GameObject("FilaEXP", typeof(RectTransform));
            expRow.transform.SetParent(footer, false);
            TopLeft((RectTransform)expRow.transform, 24, 152, 944, 12);
            RectTransform expTrack = CreateImage(expRow.transform, "BarraEXPFondo", BarTrack);
            Stretch(expTrack);
            RectTransform expFill = CreateImage(expTrack, "BarraEXP", ExpFill);
            Stretch(expFill);

            var summary = screen.gameObject.AddComponent<SummaryScreen>();
            Assign(summary, ("controls", controls), ("titleText", title), ("sprite", sprite.GetComponent<Image>()),
                ("noSpriteText", noSprite), ("nameText", nameText), ("levelText", levelText),
                ("footerTitle", footerTitle), ("footerText", footerText), ("expRow", expRow),
                ("expBar", expFill.GetComponent<Image>()));
            AssignArray(summary, "tabBackgrounds", tabs);
            AssignArray(summary, "rows", rows);
            AssignArray(summary, "labels", labels);
            AssignArray(summary, "values", values);
            AssignArray(summary, "details", details);
            AssignArray(summary, "cursors", cursors);
            screen.gameObject.SetActive(false);
            return summary;
        }

        // ---------- Mochila (maqueta 07) ----------

        static BagScreen CreateBagScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Mochila", Navy, BagBackgroundPath);

            TextMeshProUGUI title = CreateText(screen, "Titulo", "MOCHILA", 24, TextWhite);
            title.alignment = TextAlignmentOptions.Center;
            TopLeft(title.rectTransform, 16, 16, 384, 32);

            RectTransform bagBox = CreatePanel(screen, "CuadroMochila", SkyBlue);
            TopLeft(bagBox, 16, 56, 384, 288);
            RectTransform bagImage = CreateImage(bagBox, "Mochila", Color.white);
            bagImage.anchorMin = bagImage.anchorMax = new Vector2(0.5f, 0.5f);
            bagImage.sizeDelta = new Vector2(224, 224);
            bagImage.GetComponent<Image>().preserveAspect = true;
            bagImage.GetComponent<Image>().enabled = false;

            TextMeshProUGUI pocket = CreateText(screen, "Bolsillo", "< OBJETOS >", 22, TextWhite);
            pocket.alignment = TextAlignmentOptions.Center;
            TopLeft(pocket.rectTransform, 16, 356, 384, 32);

            MenuList itemList = CreateMenuList(screen, "ListaObjetos", 6, 44, true);
            RectTransform listRect = (RectTransform)itemList.transform;
            TopLeft(listRect, 416, 16, 592, 400);
            itemList.gameObject.SetActive(true); // la lista vive dentro de la pantalla: se oculta con ella

            RectTransform description = CreatePanel(screen, "Descripcion", BoneWhite);
            description.anchorMin = Vector2.zero;
            description.anchorMax = new Vector2(1, 0);
            description.offsetMin = new Vector2(16, 12);
            description.offsetMax = new Vector2(-16, 12 + MessageHeight - 8);
            RectTransform icon = CreateImage(description, "Icono", Color.white);
            icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
            icon.pivot = new Vector2(0, 0.5f);
            icon.anchoredPosition = new Vector2(24, 0);
            icon.sizeDelta = new Vector2(64, 64);
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().enabled = false;
            TextMeshProUGUI text = CreateText(description, "Texto", string.Empty, 20, TextBlack);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 20;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(112, 16);
            text.rectTransform.offsetMax = new Vector2(-24, -20);

            var bags = new Object[BagPaths.Length];
            for (int i = 0; i < BagPaths.Length; i++)
                bags[i] = LoadOptionalSprite(BagPaths[i], 0);

            var bag = screen.gameObject.AddComponent<BagScreen>();
            Assign(bag, ("itemList", itemList), ("pocketText", pocket), ("bagImage", bagImage.GetComponent<Image>()),
                ("itemIcon", icon.GetComponent<Image>()), ("descriptionText", text));
            AssignArray(bag, "bagSprites", bags);
            screen.gameObject.SetActive(false);
            return bag;
        }

        // ---------- Menú de pausa, mensajes y submenú (maquetas 01 y 03) ----------

        static MenuList CreatePauseList(Transform parent)
        {
            MenuList list = CreateMenuList(parent, "MenuPausa", 8, 34, false);
            var rect = (RectTransform)list.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-8, -48); // debajo de la barra del HUD
            rect.sizeDelta = new Vector2(320, 0);
            FitHeight(list.gameObject);
            return list;
        }

        static MenuList CreateActionList(Transform parent)
        {
            MenuList list = CreateMenuList(parent, "Acciones", 4, 34, false);
            var rect = (RectTransform)list.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-8, 8 + MessageHeight + 4);
            rect.sizeDelta = new Vector2(300, 0);
            FitHeight(list.gameObject);
            return list;
        }

        /// <summary>El mismo cuadro de texto de los mapas, más bajo para dejar sitio a las pantallas.</summary>
        static DialogueBox CreateMessageBox(Transform parent)
        {
            DialogueBox box = MainMenuSceneBuilder.CreateMapDialogueBox(parent, controls);
            box.name = "CuadroMensajes";
            var rect = (RectTransform)box.transform;
            rect.offsetMin = new Vector2(8, 8);
            rect.offsetMax = new Vector2(-8, 8 + MessageHeight);

            var text = box.GetComponentInChildren<TMP_Text>();
            text.fontSize = 22;
            text.rectTransform.offsetMin = new Vector2(48, 28);
            text.rectTransform.offsetMax = new Vector2(-48, -28);
            return box;
        }

        /// <summary>
        /// Cuadro con el marco del diálogo y <paramref name="rowCount"/> filas: cursor, texto y detalle a la
        /// derecha (cantidades). <paramref name="scrollArrows"/> añade las flechas de desplazamiento.
        /// </summary>
        static MenuList CreateMenuList(Transform parent, string name, int rowCount, float rowHeight, bool scrollArrows)
        {
            RectTransform box = CreatePixelBox(parent, name, boxSprite);
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(44, 40, 36, 36);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rows = new Object[rowCount];
            var labels = new Object[rowCount];
            var details = new Object[rowCount];
            var cursors = new Object[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                var row = new GameObject($"Opcion{i + 1}", typeof(RectTransform));
                row.transform.SetParent(box, false);
                row.AddComponent<LayoutElement>().preferredHeight = rowHeight;
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 12;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;

                var slot = new GameObject("CursorSlot", typeof(RectTransform));
                slot.transform.SetParent(row.transform, false);
                slot.AddComponent<LayoutElement>().preferredWidth = cursorSprite.rect.width * PixelScale;
                RectTransform cursor = CreatePixelImage(slot.transform, "Cursor", cursorSprite);
                cursor.anchorMin = cursor.anchorMax = cursor.pivot = new Vector2(0.5f, 0.5f);
                cursor.anchoredPosition = Vector2.zero;
                cursor.gameObject.SetActive(false);

                TextMeshProUGUI label = CreateText(row.transform, "Texto", "OPCIÓN", 22, TextBlack);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                TextMeshProUGUI detail = CreateText(row.transform, "Detalle", string.Empty, 20, TextBlack);
                detail.alignment = TextAlignmentOptions.MidlineRight;
                detail.gameObject.AddComponent<LayoutElement>().preferredWidth = 84;
                detail.gameObject.SetActive(false);

                rows[i] = row;
                labels[i] = label;
                details[i] = detail;
                cursors[i] = cursor.gameObject;
            }

            GameObject up = null;
            GameObject down = null;
            if (scrollArrows)
            {
                up = CreateScrollArrow(box, "FlechaArriba", new Vector2(1, 1), new Vector2(-24, -14), 180f);
                down = CreateScrollArrow(box, "FlechaAbajo", new Vector2(1, 0), new Vector2(-24, 14), 0f);
            }

            var list = box.gameObject.AddComponent<MenuList>();
            Assign(list, ("controls", controls));
            AssignOptional(list, ("scrollUp", up), ("scrollDown", down));
            AssignArray(list, "rows", rows);
            AssignArray(list, "labels", labels);
            AssignArray(list, "details", details);
            AssignArray(list, "cursors", cursors);
            box.gameObject.SetActive(false);
            return list;
        }

        static GameObject CreateScrollArrow(Transform parent, string name, Vector2 anchor, Vector2 position, float angle)
        {
            RectTransform rect = CreatePixelImage(parent, name, arrowSprite, 3f);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0, 0, angle);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rect.gameObject.SetActive(false);
            return rect.gameObject;
        }

        /// <summary>La caja crece hacia arriba según las filas activas (2 para SÍ/NO, 3 para DATOS/CAMBIAR/SALIR...).</summary>
        static void FitHeight(GameObject box)
        {
            var fitter = box.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        // ---------- Utilidades ----------

        static readonly Color BoneWhite = Palette.BoneWhite;

        /// <summary>Pantalla completa con fondo de color o, si existe, el patrón dibujado en mosaico.</summary>
        static RectTransform CreateBackground(Transform parent, string name, Color color, string patternPath)
        {
            RectTransform rect = CreateImage(parent, name, color);
            Stretch(rect);
            Sprite pattern = LoadOptionalSprite(patternPath, 0);
            if (pattern != null)
            {
                Image image = rect.GetComponent<Image>();
                image.sprite = pattern;
                image.color = Color.white;
                image.type = Image.Type.Tiled;
                image.pixelsPerUnitMultiplier = pattern.pixelsPerUnit / 100f / PixelScale;
            }
            return rect;
        }

        /// <summary>Panel de color con borde oscuro de 4 px (1 px del arte a escala 4).</summary>
        static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateImage(parent, name, color);
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.DarkOutline;
            outline.effectDistance = new Vector2(4, -4);
            return rect;
        }

        static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static Sprite LoadOptionalSprite(string path, int border)
        {
            if (!File.Exists(path))
                return null;
            ConfigurePixelSprite(path, border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ConfigurePixelSprite(string path, int border)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            var borderVector = new Vector4(border, border, border, border);
            if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point &&
                importer.spriteBorder == borderVector &&
                importer.textureCompression == TextureImporterCompression.Uncompressed)
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

        static RectTransform CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        static RectTransform CreatePixelBox(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = sprite.pixelsPerUnit / 100f / PixelScale;
            return rect;
        }

        static RectTransform CreatePixelImage(Transform parent, string name, Sprite sprite, float scale = PixelScale)
        {
            RectTransform rect = CreateImage(parent, name, Color.white);
            rect.GetComponent<Image>().sprite = sprite;
            rect.sizeDelta = sprite.rect.size * scale;
            return rect;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.alignment = TextAlignmentOptions.TopLeft;
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
            foreach (var (field, value) in values)
                if (value == null)
                    Debug.LogError($"{target.GetType().Name}.{field} quedó sin asignar.");
            AssignOptional(target, values);
        }

        /// <summary>Como <see cref="Assign"/>, pero acepta vacíos (arte que todavía no existe).</summary>
        static void AssignOptional(Object target, params (string field, Object value)[] values)
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
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignArray(Object target, string field, IReadOnlyList<Object> values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
