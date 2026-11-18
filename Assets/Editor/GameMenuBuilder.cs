using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Equipo, Resumen, Mochila, Pokédex, Mapa (y ascensor), Carné, Guardar, Opciones, la tienda de
    /// Don Chucho, cuadro de mensajes y submenú de acciones. <see cref="GameMenu"/> lo
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

        // Arte opcional de las pantallas de la Etapa 7 (fondos en mosaico y sellos).
        const string PokedexBackgroundPath = MenuArtFolder + "/FondoPokedex.png";
        const string MapBackgroundPath = MenuArtFolder + "/FondoMapa.png";
        const string CardBackgroundPath = MenuArtFolder + "/FondoCarne.png";
        const string OptionsBackgroundPath = MenuArtFolder + "/FondoOpciones.png";
        const string ShopBackgroundPath = MenuArtFolder + "/FondoTienda.png";
        const string SealFolder = MenuArtFolder + "/Sellos";
        const string PanelFramePath = MenuArtFolder + "/Panel.png";
        const int PanelFrameBorder = 4;
        const string PokeballItemPath = "Assets/Resources/Data/Items/pokeball.asset";

        /// <summary>
        /// Los 13 sellos (pisos con jefe, en el orden de Building.Floors) con el color del tipo de cada piso
        /// (Doc §5, los mismos acentos de LabFloorBuilder; el 14 es Dragón). El arte, si existe, es
        /// <c>Sellos/Sello{piso}.png</c>.
        /// </summary>
        static readonly (string floor, string accent)[] Seals =
        {
            ("3", "#B8A038"), ("4", "#B8B8D0"), ("5", "#F8D030"), ("6", "#A8B820"), ("7", "#C03028"),
            ("PT", "#705898"), ("8", "#78C850"), ("9", "#A040A0"), ("10", "#6890F0"), ("11", "#F08030"),
            ("12", "#A890F0"), ("13", "#F85888"), ("14", "#7038F8"),
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
                PokedexScreen pokedex = CreatePokedexScreen(root.transform);
                BuildingMapScreen map = CreateMapScreen(root.transform);
                CardScreen card = CreateCardScreen(root.transform);
                SaveScreen saveScreen = CreateSaveScreen(root.transform);
                OptionsScreen options = CreateOptionsScreen(root.transform);
                ShopScreen shop = CreateShopScreen(root.transform);
                MenuList pauseList = CreatePauseList(root.transform);
                DialogueBox message = CreateMessageBox(root.transform);
                MenuList actionList = CreateActionList(root.transform);

                var menu = root.AddComponent<GameMenu>();
                Assign(menu, ("controls", controls), ("pauseList", pauseList), ("message", message),
                    ("actionList", actionList), ("team", team), ("summary", summary), ("bag", bag),
                    ("pokedex", pokedex), ("map", map), ("card", card), ("saveScreen", saveScreen),
                    ("options", options), ("shop", shop));

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
                if (slot.TryGetComponent(out Outline slotOutline))
                    Object.DestroyImmediate(slotOutline); // el marco dibujado ya trae borde
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

        // ---------- Pokédex (maqueta 12) ----------

        static PokedexScreen CreatePokedexScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Pokedex", Navy, PokedexBackgroundPath);
            RectTransform header = CreateHeader(screen, "POKÉDEX");
            TextMeshProUGUI counts = CreateText(header, "Cuentas", "VISTOS 0   CAPTURADOS 0", 16, TextWhite);
            counts.alignment = TextAlignmentOptions.MidlineRight;
            TopLeft(counts.rectTransform, 400, 0, 608, 52);

            RectTransform portrait = CreatePanel(screen, "Ficha", SpriteBackdrop);
            TopLeft(portrait, 16, 68, 400, 300);
            RectTransform sprite = CreateImage(portrait, "Sprite", Color.white);
            sprite.anchorMin = sprite.anchorMax = new Vector2(0.5f, 1f);
            sprite.pivot = new Vector2(0.5f, 1f);
            sprite.anchoredPosition = new Vector2(0, -12);
            sprite.sizeDelta = new Vector2(192, 192);
            sprite.GetComponent<Image>().preserveAspect = true;
            sprite.GetComponent<Image>().enabled = false;
            TextMeshProUGUI unknown = CreateText(portrait, "Desconocido", "?", 48, Palette.DarkGray);
            unknown.alignment = TextAlignmentOptions.Center;
            TopLeft(unknown.rectTransform, 0, 12, 400, 192);
            TextMeshProUGUI nameText = CreateText(portrait, "Nombre", "000 -----", 20, TextBlack);
            TopLeft(nameText.rectTransform, 16, 216, 368, 28);
            TextMeshProUGUI types = CreateText(portrait, "Tipos", string.Empty, 14, TextBlack);
            TopLeft(types.rectTransform, 16, 256, 368, 28);

            MenuList list = CreateMenuList(screen, "ListaEspecies", 6, 40, true, withIcons: true);
            TopLeft((RectTransform)list.transform, 432, 60, 576, 332);
            list.gameObject.SetActive(true);

            TextMeshProUGUI description = CreateDescriptionPanel(screen);

            Sprite caught = AssetDatabase.LoadAssetAtPath<ItemData>(PokeballItemPath)?.icon;
            var pokedex = screen.gameObject.AddComponent<PokedexScreen>();
            Assign(pokedex, ("list", list), ("countsText", counts), ("sprite", sprite.GetComponent<Image>()),
                ("unknownText", unknown), ("nameText", nameText), ("typesText", types),
                ("descriptionText", description));
            AssignOptional(pokedex, ("caughtIcon", caught));
            screen.gameObject.SetActive(false);
            return pokedex;
        }

        // ---------- Mapa del edificio y ascensor (maqueta 09) ----------

        static BuildingMapScreen CreateMapScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "MapaEdificio", Navy, MapBackgroundPath);
            RectTransform header = CreateHeader(screen, "MAPA DEL EDIFICIO");
            var title = header.GetComponentInChildren<TextMeshProUGUI>();

            MenuList list = CreateMenuList(screen, "ListaPisos", 6, 40, true, withIcons: true);
            TopLeft((RectTransform)list.transform, 16, 60, 480, 332);
            list.gameObject.SetActive(true);

            RectTransform panel = CreatePanel(screen, "Detalle", BoneWhite);
            TopLeft(panel, 512, 60, 496, 332);
            TextMeshProUGUI floorText = CreateText(panel, "Piso", "PISO 3 – CIVIL", 18, TextBlack);
            floorText.textWrappingMode = TextWrappingModes.Normal;
            TopLeft(floorText.rectTransform, 24, 24, 448, 64);
            TextMeshProUGUI detail = CreateText(panel, "Estado", string.Empty, 16, TextBlack);
            detail.textWrappingMode = TextWrappingModes.Normal;
            detail.lineSpacing = 40;
            TopLeft(detail.rectTransform, 24, 112, 448, 196);

            var map = screen.gameObject.AddComponent<BuildingMapScreen>();
            Assign(map, ("list", list), ("titleText", title), ("floorText", floorText), ("detailText", detail));
            AssignSeals(map);
            screen.gameObject.SetActive(false);
            return map;
        }

        // ---------- Carné (maqueta 08) ----------

        static CardScreen CreateCardScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Carne", SkyBlue, CardBackgroundPath);

            RectTransform cardPanel = CreatePanel(screen, "Tarjeta", BoneWhite);
            TopLeft(cardPanel, 48, 20, 928, 400);
            RectTransform strip = CreateImage(cardPanel, "Franja", Palette.EngineeringBlue);
            TopLeft(strip, 0, 0, 928, 48);
            TextMeshProUGUI stripText = CreateText(strip, "Texto", "CARNÉ ESTUDIANTIL · TORRE DE INGENIERÍA", 16, TextWhite);
            stripText.alignment = TextAlignmentOptions.Center;
            Stretch(stripText.rectTransform);

            RectTransform photo = CreatePanel(cardPanel, "Foto", SpriteBackdrop);
            TopLeft(photo, 24, 68, 152, 184);
            RectTransform pedro = CreateImage(photo, "Pedro", Color.white);
            Stretch(pedro);
            pedro.offsetMin = new Vector2(24, 12);
            pedro.offsetMax = new Vector2(-24, -12);
            Image pedroImage = pedro.GetComponent<Image>();
            pedroImage.preserveAspect = true;
            pedroImage.sprite = AssetDatabase.LoadAllAssetsAtPath(CharacterSpriteSlicer.PedroSheet)
                .OfType<Sprite>().FirstOrDefault(sprite => sprite.name == "Pedro_Abajo_Quieto");
            pedroImage.enabled = pedroImage.sprite != null;

            TextMeshProUGUI nameText = CreateText(cardPanel, "Nombre", "PEDRO", 24, TextBlack);
            TopLeft(nameText.rectTransform, 200, 72, 400, 32);
            TextMeshProUGUI idText = CreateText(cardPanel, "ID", "ID No. 00000", 18, TextBlack);
            idText.alignment = TextAlignmentOptions.TopRight;
            TopLeft(idText.rectTransform, 600, 76, 304, 28);

            TextMeshProUGUI labels = CreateText(cardPanel, "Etiquetas", "CRÉDITOS\nPOKÉDEX\nTIEMPO\nPROMEDIO", 18, TextBlack);
            labels.lineSpacing = 36;
            TopLeft(labels.rectTransform, 200, 124, 320, 136);
            TextMeshProUGUI values = CreateText(cardPanel, "Valores", "0\n0\n0:00\n-", 18, TextBlack);
            values.lineSpacing = 36;
            values.alignment = TextAlignmentOptions.TopRight;
            TopLeft(values.rectTransform, 520, 124, 384, 136);

            TextMeshProUGUI sealsTitle = CreateText(cardPanel, "TituloSellos", "SELLOS", 16, TextBlack);
            TopLeft(sealsTitle.rectTransform, 24, 272, 400, 24);

            var floors = new string[Seals.Length];
            var images = new Object[Seals.Length];
            var sealLabels = new Object[Seals.Length];
            var sprites = new Object[Seals.Length];
            var colors = new Color[Seals.Length];
            for (int i = 0; i < Seals.Length; i++)
            {
                RectTransform seal = CreateImage(cardPanel, $"Sello{Seals[i].floor}", Color.gray);
                TopLeft(seal, 24 + i * 68, 304, 60, 60);
                seal.GetComponent<Image>().preserveAspect = true;
                TextMeshProUGUI label = CreateText(seal, "Piso", Seals[i].floor, 14, TextWhite);
                label.alignment = TextAlignmentOptions.Center;
                Stretch(label.rectTransform);

                floors[i] = Seals[i].floor;
                images[i] = seal.GetComponent<Image>();
                sealLabels[i] = label;
                sprites[i] = LoadOptionalSprite($"{SealFolder}/Sello{Seals[i].floor}.png", 0);
                ColorUtility.TryParseHtmlString(Seals[i].accent, out colors[i]);
            }

            var card = screen.gameObject.AddComponent<CardScreen>();
            Assign(card, ("controls", controls), ("nameText", nameText), ("idText", idText), ("valuesText", values));
            AssignStrings(card, "sealFloors", floors);
            AssignArray(card, "sealImages", images);
            AssignArray(card, "sealLabels", sealLabels);
            AssignArray(card, "sealSprites", sprites);
            AssignColors(card, "sealColors", colors);
            screen.gameObject.SetActive(false);
            return card;
        }

        // ---------- Guardar (maqueta 10) ----------

        static SaveScreen CreateSaveScreen(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "Guardar", BoneWhite);
            TopLeft(panel, 16, 16, 560, 248);
            TextMeshProUGUI labels = CreateText(panel, "Etiquetas", "JUGADOR\nLUGAR\nSELLOS\nPOKÉDEX\nTIEMPO", 18, TextBlack);
            labels.lineSpacing = 28;
            TopLeft(labels.rectTransform, 24, 24, 200, 208);
            TextMeshProUGUI values = CreateText(panel, "Valores", string.Empty, 18, Palette.EngineeringBlue);
            values.lineSpacing = 28;
            values.alignment = TextAlignmentOptions.TopRight;
            values.overflowMode = TextOverflowModes.Ellipsis;
            TopLeft(values.rectTransform, 200, 24, 336, 208);

            var save = panel.gameObject.AddComponent<SaveScreen>();
            Assign(save, ("valuesText", values));
            panel.gameObject.SetActive(false);
            return save;
        }

        // ---------- Opciones (maqueta 11) ----------

        static OptionsScreen CreateOptionsScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Opciones", Navy, OptionsBackgroundPath);
            CreateHeader(screen, "OPCIONES");

            RectTransform panel = CreatePanel(screen, "Panel", BoneWhite);
            TopLeft(panel, 64, 72, 896, 344);

            string[] names = { "VOLUMEN", "PANTALLA COMPLETA", "VELOCIDAD DEL TEXTO", "SALIR AL TÍTULO", "VOLVER" };
            var rows = new Object[names.Length];
            var cursors = new Object[names.Length];
            var values = new Object[names.Length];
            var segments = new Object[GameSettings.MaxVolume];
            for (int i = 0; i < names.Length; i++)
            {
                var row = new GameObject($"Fila{i + 1}", typeof(RectTransform));
                row.transform.SetParent(panel, false);
                TopLeft((RectTransform)row.transform, 0, 24 + i * 62, 896, 56);

                RectTransform cursor = CreatePixelImage(row.transform, "Cursor", cursorSprite);
                cursor.anchorMin = cursor.anchorMax = cursor.pivot = new Vector2(0, 0.5f);
                cursor.anchoredPosition = new Vector2(28, 0);
                cursor.gameObject.SetActive(false);

                TextMeshProUGUI label = CreateText(row.transform, "Texto", names[i], 20, TextBlack);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                TopLeft(label.rectTransform, 72, 0, 440, 56);

                TextMeshProUGUI value = null;
                if (i < 3)
                {
                    value = CreateText(row.transform, "Valor", string.Empty, 20, Palette.EngineeringBlue);
                    value.alignment = TextAlignmentOptions.MidlineRight;
                    TopLeft(value.rectTransform, 520, 0, 344, 56);
                }

                if (i == 0)
                {
                    // Barra de 10 pasos; el número va a su derecha.
                    for (int j = 0; j < segments.Length; j++)
                    {
                        RectTransform segment = CreateImage(row.transform, $"Paso{j + 1}", Palette.ConcreteGray);
                        TopLeft(segment, 520 + j * 26, 16, 20, 24);
                        segments[j] = segment.GetComponent<Image>();
                    }
                }

                rows[i] = row;
                cursors[i] = cursor.gameObject;
                values[i] = value;
            }

            var options = screen.gameObject.AddComponent<OptionsScreen>();
            Assign(options, ("controls", controls));
            AssignArray(options, "rows", rows);
            AssignArray(options, "cursors", cursors);
            AssignArray(options, "values", values);
            AssignArray(options, "volumeSegments", segments);
            AssignColor(options, "segmentOn", Palette.EngineeringBlue);
            AssignColor(options, "segmentOff", Palette.ConcreteGray);
            screen.gameObject.SetActive(false);
            return options;
        }

        // ---------- Tienda de Don Chucho ----------

        static ShopScreen CreateShopScreen(Transform parent)
        {
            RectTransform screen = CreateBackground(parent, "Tienda", Navy, ShopBackgroundPath);

            RectTransform creditsBox = CreatePanel(screen, "Creditos", BoneWhite);
            TopLeft(creditsBox, 16, 16, 320, 96);
            TextMeshProUGUI credits = CreateText(creditsBox, "Texto", "CRÉDITOS\n0", 20, TextBlack);
            credits.lineSpacing = 24;
            Stretch(credits.rectTransform);
            credits.rectTransform.offsetMin = new Vector2(20, 12);
            credits.rectTransform.offsetMax = new Vector2(-20, -16);

            TextMeshProUGUI owned = CreateText(screen, "EnMochila", string.Empty, 16, TextWhite);
            TopLeft(owned.rectTransform, 16, 128, 320, 28);

            RectTransform quantityBox = CreatePanel(screen, "Cantidad", BoneWhite);
            TopLeft(quantityBox, 16, 176, 320, 96);
            TextMeshProUGUI quantity = CreateText(quantityBox, "Cantidad", "x01", 24, TextBlack);
            quantity.alignment = TextAlignmentOptions.MidlineLeft;
            TopLeft(quantity.rectTransform, 24, 0, 120, 96);
            TextMeshProUGUI total = CreateText(quantityBox, "Total", "0", 20, TextBlack);
            total.alignment = TextAlignmentOptions.MidlineRight;
            TopLeft(total.rectTransform, 144, 0, 152, 96);
            quantityBox.gameObject.SetActive(false);

            MenuList list = CreateMenuList(screen, "ListaTienda", 5, 44, true);
            TopLeft((RectTransform)list.transform, 352, 16, 656, 308);
            list.gameObject.SetActive(true);

            (TextMeshProUGUI description, Image icon) = CreateDescriptionWithIcon(screen);

            var shop = screen.gameObject.AddComponent<ShopScreen>();
            Assign(shop, ("controls", controls), ("itemList", list), ("creditsText", credits),
                ("itemIcon", icon), ("descriptionText", description), ("quantityBox", quantityBox.gameObject),
                ("quantityText", quantity), ("totalText", total), ("ownedText", owned));
            screen.gameObject.SetActive(false);
            return shop;
        }

        /// <summary>Barra superior azul con el título de la pantalla.</summary>
        static RectTransform CreateHeader(Transform screen, string titleText)
        {
            RectTransform header = CreateImage(screen, "Cabecera", Palette.EngineeringBlue);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = Vector2.one;
            header.offsetMin = new Vector2(0, -52);
            header.offsetMax = Vector2.zero;
            TextMeshProUGUI title = CreateText(header, "Titulo", titleText, 24, TextWhite);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            TopLeft(title.rectTransform, 16, 0, 600, 52);
            return header;
        }

        /// <summary>Recuadro de descripción de abajo (como el de la Mochila), sin icono.</summary>
        static TextMeshProUGUI CreateDescriptionPanel(Transform screen)
        {
            RectTransform panel = CreatePanel(screen, "Descripcion", BoneWhite);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = new Vector2(1, 0);
            panel.offsetMin = new Vector2(16, 12);
            panel.offsetMax = new Vector2(-16, 12 + MessageHeight - 8);
            TextMeshProUGUI text = CreateText(panel, "Texto", string.Empty, 18, TextBlack);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 20;
            text.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24, 16);
            text.rectTransform.offsetMax = new Vector2(-24, -20);
            return text;
        }

        /// <summary>Recuadro de descripción con icono a la izquierda (Mochila y tienda).</summary>
        static (TextMeshProUGUI text, Image icon) CreateDescriptionWithIcon(Transform screen)
        {
            TextMeshProUGUI text = CreateDescriptionPanel(screen);
            text.fontSize = 20;
            text.rectTransform.offsetMin = new Vector2(112, 16);
            RectTransform icon = CreateImage(text.rectTransform.parent, "Icono", Color.white);
            icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
            icon.pivot = new Vector2(0, 0.5f);
            icon.anchoredPosition = new Vector2(24, 0);
            icon.sizeDelta = new Vector2(64, 64);
            Image image = icon.GetComponent<Image>();
            image.preserveAspect = true;
            image.enabled = false;
            return (text, image);
        }

        static void AssignSeals(BuildingMapScreen map)
        {
            var floors = new string[Seals.Length];
            var sprites = new Object[Seals.Length];
            for (int i = 0; i < Seals.Length; i++)
            {
                floors[i] = Seals[i].floor;
                sprites[i] = LoadOptionalSprite($"{SealFolder}/Sello{Seals[i].floor}.png", 0);
            }
            AssignStrings(map, "sealFloors", floors);
            AssignArray(map, "sealSprites", sprites);
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
        static MenuList CreateMenuList(Transform parent, string name, int rowCount, float rowHeight, bool scrollArrows,
            bool withIcons = false)
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
            var icons = new Object[withIcons ? rowCount : 0];
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

                if (withIcons)
                {
                    RectTransform icon = CreateImage(row.transform, "Icono", Color.white);
                    var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
                    iconLayout.preferredWidth = 32;
                    iconLayout.preferredHeight = 32;
                    Image iconImage = icon.GetComponent<Image>();
                    iconImage.preserveAspect = true;
                    iconImage.enabled = false;
                    icons[i] = iconImage;
                }

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
            AssignArray(list, "icons", icons);
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

        /// <summary>
        /// Panel de color con borde oscuro de 4 px (1 px del arte a escala 4). Si existe el marco 9-slice
        /// <c>Menu/Panel.png</c> (Etapa 9), se usa ese arte teñido con el color del panel.
        /// </summary>
        static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateImage(parent, name, color);
            Sprite frame = LoadOptionalSprite(PanelFramePath, PanelFrameBorder);
            if (frame != null)
            {
                Image image = rect.GetComponent<Image>();
                image.sprite = frame;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = frame.pixelsPerUnit / 100f / PixelScale;
                return rect;
            }
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

        static void AssignStrings(Object target, string field, IReadOnlyList<string> values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignColors(Object target, string field, IReadOnlyList<Color> values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).colorValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignColor(Object target, string field, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).colorValue = value;
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
