using System.Collections.Generic;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Construye el atrio con el tileset provisional de colores sólidos, siguiendo el boceto:
    /// gradas superiores, terraza del café con muro verde, atrio con pantalla y mesas, escalera de salida,
    /// gradas inferiores y kiosco del café. Al subir hasta arriba de las gradas superiores se pasa
    /// al pasillo de los ascensores (Figura 23).
    /// Menú: Torre Ingeniería > Mapas > Construir atrio provisional.
    /// </summary>
    public static class AtrioBuilder
    {
        const string TilesetPath = "Assets/Art/Tilesets/Propios/AtrioProvisional.png";
        const string TilesetName = "AtrioProvisional";

        /// <summary>Punto de llegada al bajar desde los ascensores.</summary>
        public const string SpawnFromElevators = "DesdeAscensores";

        // Fila 0 = norte (arriba). Piso:
        // (espacio) fuera del edificio   . baldosa del atrio   g grada de madera   c piso del café   E escalera de salida
        static readonly string[] Floor =
        {
            "                              ",
            " ggggggggggggggggg            ",
            " ggggggggggggggggg            ",
            " ggggggggggggggggg            ",
            " ggggggggggggggggg            ",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            " gggggggggggggggggcccccccccccc",
            "..............................",
            "..............................",
            "..............................",
            ".............................E",
            ".............................E",
            ".............................E",
            ".............................E",
            ".............................E",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            " ggggggggggggg      ......... ",
            " ggggggggggggg      ......... ",
            " ggggggggggggg      ......... ",
            " ggggggggggggg                ",
            " ggggggggggggg                ",
            " ggggggggggggg                ",
            "                              ",
        };

        // Objetos:
        // . nada   | baranda   V muro verde   M mesa   a/y silla azul/amarilla
        // P/p pantalla (marco/imagen)   R toldo del kiosco   C mostrador   L farol
        static readonly string[] Objects =
        {
            "..............................",
            ".|...............|............",
            ".|...............|............",
            ".|...............|............",
            ".|...............|............",
            ".|...............|VVVVVVVVVVVV",
            ".|...............|...........V",
            ".|...............|...y...a...V",
            ".|...............|..aMa.yMy..V",
            ".|...............|...y...a...V",
            ".|...............|.....a...y.V",
            ".|...............|..y.yMy.aMaV",
            ".|...............|.aMa.a...y.V",
            ".|...............|..y........V",
            ".|...............|...........V",
            "..L.............L...........L.",
            "..............................",
            "..............................",
            ".............PPPP.............",
            ".............PppP.............",
            ".............PppP.............",
            ".............PppP.....y...a...",
            ".............PppP....aMa.yMy..",
            ".............PppP.....y...a...",
            ".............PPPP.............",
            "..............PP...y...a...y..",
            "..................aMa.yMy.aMa.",
            "...................y...a...y..",
            "...............L..............",
            ".|...........|......RRRRRRRRR.",
            ".|...........|......CCCCCCCCC.",
            ".|...........|......CCCCCCCCC.",
            ".|...........|................",
            ".|...........|................",
            ".|...........|................",
            "..............................",
        };

        // Índices en AtrioProvisional.png.
        const int TileA = 0, TileB = 1, Outside = 14;
        static readonly Dictionary<char, int> FloorIndex = new Dictionary<char, int> { ['g'] = 2, ['c'] = 4, ['E'] = 11, [' '] = Outside };
        static readonly Dictionary<char, int> ObjectIndex = new Dictionary<char, int>
        {
            ['|'] = 3, ['V'] = 5, ['M'] = 6, ['a'] = 7, ['y'] = 8, ['P'] = 9, ['p'] = 10, ['R'] = 12, ['C'] = 13, ['L'] = 15,
        };

        // Lo que no se puede atravesar: el exterior y los objetos sólidos. Por las gradas se camina,
        // pero sus barandas laterales funcionan como pared. Las sillas no bloquean, para no estorbar el paso.
        const string BlockingFloors = " ";
        const string BlockingObjects = "|VMPpRCL";
        static readonly Vector2Int PlayerStart = new Vector2Int(5, 21); // columna, fila

        /// <summary>
        /// Primera fila de la jardinera inferior, la única con encuentros salvajes. La jardinera de arriba
        /// es el paso obligatorio a la puerta y al Vigilante, así que se deja libre de combates.
        /// </summary>
        const int JardineraInferior = 29;

        // Perímetro del resaltado: filas 29 a 34, las mismas que pinta la capa Encuentros.
        const int JardineraIzquierda = 1;
        const int JardineraDerecha = 13;
        const int JardineraBordeSuperior = JardineraInferior; // 29
        const int JardineraBordeInferior = JardineraInferior + 5; // 34

        [MenuItem("Torre Ingeniería/Mapas/Construir atrio provisional")]
        public static void Build()
        {
            ProvisionalMap.PrepareCommonAssets(TilesetPath);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = MapSceneBuilder.CreateScene();
            int width = Floor[0].Length;
            int height = Floor.Length;

            // Baldosas grandes de 2x2 casillas alternando tono, como en el boceto.
            ProvisionalMap.Paint(Floor, Objects, ProvisionalMap.LoadTiles(TilesetName, 16),
                (symbol, col, row) => symbol == '.' ? ((col / 2 + row / 2) % 2 == 0 ? TileA : TileB) : FloorIndex[symbol],
                ObjectIndex, BlockingFloors, BlockingObjects);
            PaintGuidance(height);

            ProvisionalMap.AddFloorInfo("1");
            ProvisionalMap.AddMessageBox();
            ProvisionalMap.AddHud();
            ProvisionalMap.AddEncounterZone();

            // Zonas de encuentro provisionales de la plazoleta (Doc §7): solo la jardinera de abajo (la de
            // junto al kiosco). La de arriba es el camino obligatorio a la puerta y al Vigilante, así que
            // queda libre de salvajes. Se saltan el muro verde y los objetos sólidos. La capa se oculta al
            // jugar (HideOnPlay), así que la zona de encuentro se ve en el editor, no en el juego.
            // Las jardineras definitivas llegan con el contenido del piso en la Etapa 8.
            TileBase encounterMarker = ProvisionalMap.LoadTiles(TilesetName, 16)[TileA];
            Tilemap encounters = MapSceneBuilder.Layer("Encuentros");
            for (int row = JardineraInferior; row < Floor.Length; row++)
                for (int col = 0; col < Floor[row].Length; col++)
                    if (Floor[row][col] == 'g' && BlockingObjects.IndexOf(Objects[row][col]) < 0)
                        encounters.SetTile(ProvisionalMap.Cell(col, row, height), encounterMarker);

            // Último escalón de las gradas superiores (entre las barandas) → pasillo de los ascensores.
            // Es la puerta del edificio: el Vigilante Gómez solo deja pasar con inicial y un Pokémon capturado.
            ProvisionalMap.AddWarp("SubidaAscensores", 2, 16, 1, 1, height, SceneNames.Ascensores, AscensoresBuilder.SpawnFromAtrium,
                needsBuildingAccess: true,
                lockedMessage: "—Alto. El edificio está en emergencia.\n" +
                               "El vigilante Gómez le dirá qué le falta para entrar.");
            ProvisionalMap.AddSpawn(SpawnFromElevators, 9, 2, height, PlayerController.Direction.Down);

            // Gabriel (Doc §8 y §10): en la plazoleta, junto a la fuente. Entrega el inicial y, si el
            // jugador acepta su ayuda, 5 Pokébolas antes de la puerta del Vigilante. Sus diálogos cambian
            // según el progreso (ver GabrielNpc).
            GabrielNpc gabriel = ProvisionalMap.CreateGabriel(11, 18, height);
            ProvisionalMap.AttachGuidanceTarget(gabriel.gameObject, Guidance.GabrielAtrio);
            ProvisionalMap.AddGuidanceTarget("DestinoJardinera", Guidance.JardineraInferior, 7, 31, height);
            ProvisionalMap.AddGuidanceTarget("DestinoPuerta", Guidance.PuertaEdificio, 9, 1, height);

            // Vigilante Gómez (Doc §8 y §10): cuida la puerta desde el último escalón, al lado de la llegada
            // desde los ascensores. Su diálogo cambia según el progreso (ver VigilanteNpc).
            ProvisionalMap.CreateVigilante(10, 2, height);

            // Entrenador de práctica del kiosco (Doc §10): primer combate de entrenador y primera fuente
            // de créditos. Queda junto al mostrador, mirando al camino; el combate real llega en la Etapa 6.
            ProvisionalMap.CreateTrainer("EntrenadorKiosco", 19, 28, height, "kiosco",
                new[] { "¡Hola! Soy el estudiante del kiosco.", "¿Quieres probar tu equipo en un combate de práctica?" },
                PlayerController.Direction.Up);

            GameObject pedro = ProvisionalMap.CreatePlayer(ProvisionalMap.CellCenter(PlayerStart.x, PlayerStart.y, height));
            ProvisionalMap.SetupCamera(pedro.transform, width, height);

            string path = ProvisionalMap.Save(scene, SceneNames.Atrio);
            Debug.Log($"Atrio provisional creado en {path}. Dale Play y muévete con WASD/flechas (Shift para correr).");
        }

        /// <summary>
        /// Pictogramas del entorno y borde amarillo de la jardinera. Los pictogramas van en
        /// Decoracion (no en Resaltado) para que sigan visibles cuando el borde se apague.
        /// </summary>
        static void PaintGuidance(int height)
        {
            TileBase[] pictograms = ProvisionalMap.LoadTiles("PictogramasGuia", 5);
            Tilemap decoration = MapSceneBuilder.Layer("Decoracion");
            decoration.SetTile(ProvisionalMap.Cell(7, 31, height), pictograms[GuidanceArt.PictogramPlant]);
            decoration.SetTile(ProvisionalMap.Cell(10, 2, height), pictograms[GuidanceArt.PictogramDoor]);
            decoration.SetTile(ProvisionalMap.Cell(9, 15, height), pictograms[GuidanceArt.PictogramUp]);

            Tilemap highlight = MapSceneBuilder.Layer("Resaltado");
            TileBase[] border = ProvisionalMap.LoadTiles("ResaltadoJardinera", 8);

            highlight.SetTile(ProvisionalMap.Cell(JardineraIzquierda, JardineraBordeSuperior, height),
                border[GuidanceArt.BorderTopLeft]);
            for (int col = JardineraIzquierda + 1; col < JardineraDerecha; col++)
                highlight.SetTile(ProvisionalMap.Cell(col, JardineraBordeSuperior, height),
                    border[GuidanceArt.BorderTop]);
            highlight.SetTile(ProvisionalMap.Cell(JardineraDerecha, JardineraBordeSuperior, height),
                border[GuidanceArt.BorderTopRight]);

            for (int row = JardineraBordeSuperior + 1; row < JardineraBordeInferior; row++)
            {
                highlight.SetTile(ProvisionalMap.Cell(JardineraIzquierda, row, height),
                    border[GuidanceArt.BorderLeft]);
                highlight.SetTile(ProvisionalMap.Cell(JardineraDerecha, row, height),
                    border[GuidanceArt.BorderRight]);
            }

            highlight.SetTile(ProvisionalMap.Cell(JardineraIzquierda, JardineraBordeInferior, height),
                border[GuidanceArt.BorderBottomLeft]);
            for (int col = JardineraIzquierda + 1; col < JardineraDerecha; col++)
                highlight.SetTile(ProvisionalMap.Cell(col, JardineraBordeInferior, height),
                    border[GuidanceArt.BorderBottom]);
            highlight.SetTile(ProvisionalMap.Cell(JardineraDerecha, JardineraBordeInferior, height),
                border[GuidanceArt.BorderBottomRight]);

            highlight.gameObject.AddComponent<GuidancePlanterHighlight>();
        }
    }
}
