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
    /// Construye los pisos de laboratorio (3 a 13 y PT) sobre la planta base de la Figura 38, con salas más anchas:
    /// pasillo central norte-sur; al oeste el ascensor de servicio, la escalera NO, las salas O1-O3 y los tres
    /// ascensores del sur; al este las salas E1-E6 y la escalera SE.
    /// La distribución de las salas cambia en cada piso (ver LabFloorLayout); el piso 3 sigue la Figura 38.
    /// Cada piso usa el color de acento de su tipo en las salas. Las esquinas de la entrada y de la escalera de
    /// subida (el jefe) las fija Building (EntryCorner y GuardedCorner): no se recalculan aquí.
    /// Menú: Torre Ingeniería > Mapas > Construir pisos de laboratorio.
    /// </summary>
    public static class LabFloorBuilder
    {
        const string TilesetPath = "Assets/Art/Tilesets/Propios/LaboratorioProvisional.png";
        const string TilesetName = "LaboratorioProvisional";

        /// <summary>Pisos con esta planta y el color de acento de su tipo (documento de diseño, sección 5).</summary>
        static readonly (string floor, string type, string accent)[] LabFloors =
        {
            ("3", "Roca", "#B8A038"), ("4", "Acero", "#B8B8D0"), ("5", "Eléctrico", "#F8D030"),
            ("6", "Bicho", "#A8B820"), ("7", "Lucha", "#C03028"), ("PT", "Fantasma", "#705898"),
            ("8", "Planta", "#78C850"), ("9", "Veneno", "#A040A0"), ("10", "Agua", "#6890F0"),
            ("11", "Fuego", "#F08030"), ("12", "Volador", "#A890F0"), ("13", "Psíquico", "#F85888"),
        };

        static readonly Dictionary<char, int> FloorIndex = new Dictionary<char, int>
        {
            ['W'] = 4, ['d'] = 5, ['X'] = 6, ['s'] = 7, ['E'] = 8, ['F'] = 9, ['e'] = 10, ['f'] = 11, ['c'] = 17,
        };
        static readonly Dictionary<char, int> ObjectIndex = new Dictionary<char, int>
        {
            ['M'] = 12, ['D'] = 13, ['P'] = 14, ['o'] = 15, ['K'] = 18, ['Y'] = 19,
        };
        const int CorridorA = 0, CorridorB = 1, RoomFloor = 2, EncounterFloor = 3;
        const string BlockingFloors = "WXEFefc";
        const string BlockingObjects = "MDPoKY";

        // Escaleras: casilla de salida (columna, filas) y punto de llegada (columna, fila, dirección).
        const int NorthwestWarpColumn = 6, SoutheastWarpColumn = 23;
        static readonly (int min, int max) NorthwestRows = (7, 9), SoutheastRows = (43, 45);
        static readonly (int col, int row, PlayerController.Direction facing) NorthwestArrival = (10, 8, PlayerController.Direction.Right);
        static readonly (int col, int row, PlayerController.Direction facing) SoutheastArrival = (19, 44, PlayerController.Direction.Left);

        [MenuItem("Torre Ingeniería/Mapas/Construir pisos de laboratorio")]
        public static void BuildAll()
        {
            ProvisionalMap.PrepareCommonAssets(TilesetPath);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (var (floor, type, accent) in LabFloors)
                BuildFloor(floor, type, accent);
            Debug.Log($"Pisos de laboratorio creados: {LabFloors.Length} escenas en Assets/Scenes/Mapas.");
        }

        static void BuildFloor(string floor, string type, string accent)
        {
            Scene scene = MapSceneBuilder.CreateScene();
            LabFloorLayout.Result layout = LabFloorLayout.Generate(floor);
            string[] floorRows = layout.Floor;
            int width = LabFloorLayout.Width;
            int height = LabFloorLayout.Height;
            TileBase[] tiles = ProvisionalMap.LoadTiles(TilesetName, 20);

            // Pasillo en baldosas de 2x2; el piso de las salas va en su propia capa, teñida con el color del piso.
            ProvisionalMap.Paint(floorRows, layout.Objects, tiles,
                (symbol, col, row) => symbol switch
                {
                    '.' => (col / 2 + row / 2) % 2 == 0 ? CorridorA : CorridorB,
                    'r' or 'z' => -1,
                    _ => FloorIndex[symbol],
                },
                ObjectIndex, BlockingFloors, BlockingObjects);
            PaintRooms(floorRows, tiles, accent, height);

            // La escalera de subida es la que custodia el jefe: la fija Building, no este builder.
            bool upAtNorthwest = Building.GuardedCorner(floor) == Corner.NorthWest;
            AddStairs(upAtNorthwest, height);

            ProvisionalMap.AddFloorInfo(floor);
            ProvisionalMap.AddMessageBox();
            ProvisionalMap.AddHud();
            ProvisionalMap.AddEncounterZone();

            // Si se prueba el piso directamente, Pedro empieza donde se llega subiendo.
            var start = upAtNorthwest ? SoutheastArrival : NorthwestArrival;
            GameObject pedro = ProvisionalMap.CreatePlayer(ProvisionalMap.CellCenter(start.col, start.row, height));
            ProvisionalMap.SetupCamera(pedro.transform, width, height);

            ProvisionalMap.Save(scene, Building.SceneFor(floor));
            Debug.Log($"{Building.DisplayName(floor)} ({type}) creado: " +
                string.Join(", ", layout.Rooms.ConvertAll(room => $"{room.Name} {room.Kind}")));
        }

        /// <summary>Piso de las salas y zonas de encuentro de los laboratorios.</summary>
        static void PaintRooms(string[] floorRows, TileBase[] tiles, string accent, int height)
        {
            var go = new GameObject("SueloSalas", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(GameObject.Find("Grid").transform, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = -9;
            var rooms = go.GetComponent<Tilemap>();
            ColorUtility.TryParseHtmlString(accent, out Color color);
            rooms.color = Color.Lerp(Color.white, color, 0.55f); // acento suave

            Tilemap encounters = MapSceneBuilder.Layer("Encuentros");
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < floorRows[row].Length; col++)
                {
                    char symbol = floorRows[row][col];
                    if (symbol != 'r' && symbol != 'z')
                        continue;
                    Vector3Int cell = ProvisionalMap.Cell(col, row, height);
                    rooms.SetTile(cell, tiles[symbol == 'z' ? EncounterFloor : RoomFloor]);
                    if (symbol == 'z')
                        encounters.SetTile(cell, tiles[EncounterFloor]);
                }
            }
        }

        /// <summary>
        /// Escalera de subida y de bajada según el piso. Ambas llevan a la escena Escalera:
        /// la de subida entra por abajo (DesdeAbajo) y la de bajada entra por arriba (DesdeArriba).
        /// </summary>
        static void AddStairs(bool upAtNorthwest, int height)
        {
            var nw = (col: NorthwestWarpColumn, rows: NorthwestRows, arrival: NorthwestArrival);
            var se = (col: SoutheastWarpColumn, rows: SoutheastRows, arrival: SoutheastArrival);
            var up = upAtNorthwest ? nw : se;
            var down = upAtNorthwest ? se : nw;

            ProvisionalMap.AddWarp("EscaleraSubida", up.col, up.col, up.rows.min, up.rows.max, height,
                SceneNames.Escalera, StairwellController.SpawnFromBelow);
            ProvisionalMap.AddWarp("EscaleraBajada", down.col, down.col, down.rows.min, down.rows.max, height,
                SceneNames.Escalera, StairwellController.SpawnFromAbove);

            // Al llegar subiendo se aparece en la escalera de bajada; al llegar bajando, en la de subida.
            ProvisionalMap.AddSpawn(Building.SpawnFromStairsBelow, down.arrival.col, down.arrival.row, height, down.arrival.facing);
            ProvisionalMap.AddSpawn(Building.SpawnFromStairsAbove, up.arrival.col, up.arrival.row, height, up.arrival.facing);
        }
    }
}
