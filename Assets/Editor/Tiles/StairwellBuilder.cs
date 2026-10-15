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
    /// Construye la escalera en zigzag entre pisos: escalones grises, bordes rojo carmesí y tramos que alternan
    /// de lado unidos por descansos. Se entra por abajo a la izquierda (desde el piso de abajo) y se sale por arriba
    /// a la izquierda (al piso de arriba). Una sola escena sirve para todos los tramos (ver StairwellController).
    /// Menú: Torre Ingeniería > Mapas > Construir escalera provisional.
    /// </summary>
    public static class StairwellBuilder
    {
        const string TilesetPath = "Assets/Art/Tilesets/Propios/EscaleraProvisional.png";
        const string TilesetName = "EscaleraProvisional";

        // Fila 0 = arriba. R borde carmesí   s escalón (tramo horizontal)   v escalón (entrada/salida)   d descanso
        static readonly string[] Floor =
        {
            "RRRRRRRRRRRRRRRRRRRR",
            "RvvvRRRRRRRRRRRRRRRR",
            "RvvvRRRRRRRRRRRRRRRR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddssssssssssssdddR",
            "RdddssssssssssssdddR",
            "RRRRRRRRRRRRRRRRdddR",
            "RRRRRRRRRRRRRRRRdddR",
            "RdddssssssssssssdddR",
            "RdddssssssssssssdddR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddRRRRRRRRRRRRRRRR",
            "RdddssssssssssssdddR",
            "RdddssssssssssssdddR",
            "RRRRRRRRRRRRRRRRdddR",
            "RRRRRRRRRRRRRRRRdddR",
            "RdddssssssssssssdddR",
            "RdddssssssssssssdddR",
            "RvvvRRRRRRRRRRRRRRRR",
            "RRRRRRRRRRRRRRRRRRRR",
        };

        static readonly string[] NoObjects = BuildEmpty(Floor);

        // Índices en EscaleraProvisional.png.
        static readonly Dictionary<char, int> FloorIndex = new Dictionary<char, int> { ['s'] = 0, ['v'] = 1, ['d'] = 2, ['R'] = 3 };
        const int WallShadow = 6, Tape = 4;

        // Cinta de precaución: al inicio del primer tramo (columna, filas).
        const int TapeColumn = 4;
        static readonly int[] TapeRows = { 19, 20 };

        [MenuItem("Torre Ingeniería/Mapas/Construir escalera provisional")]
        public static void Build()
        {
            ProvisionalMap.PrepareCommonAssets(TilesetPath);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = MapSceneBuilder.CreateScene();
            int width = Floor[0].Length;
            int height = Floor.Length;
            TileBase[] tiles = ProvisionalMap.LoadTiles(TilesetName, 7);

            // El borde que tiene piso justo debajo se dibuja más oscuro, como la cara del muro.
            ProvisionalMap.Paint(Floor, NoObjects, tiles,
                (symbol, col, row) => symbol == 'R' && row + 1 < height && Floor[row + 1][col] != 'R' ? WallShadow : FloorIndex[symbol],
                new Dictionary<char, int>(), "R", "");

            Tilemap lockLayer = CreateLockLayer();
            foreach (int row in TapeRows)
                lockLayer.SetTile(ProvisionalMap.Cell(TapeColumn, row, height), tiles[Tape]);

            // Salidas: abajo → piso de abajo; arriba → piso de arriba. El destino real lo fija StairwellController.
            WarpZone bottom = ProvisionalMap.AddWarp("SalidaAbajo", 1, 3, 21, 21, height, SceneNames.Ascensores, Building.SpawnFromStairsAbove);
            WarpZone top = ProvisionalMap.AddWarp("SalidaArriba", 1, 3, 1, 1, height, Building.SceneFor("3"), Building.SpawnFromStairsBelow);
            ProvisionalMap.AddSpawn(StairwellController.SpawnFromBelow, 2, 20, height, PlayerController.Direction.Right);
            ProvisionalMap.AddSpawn(StairwellController.SpawnFromAbove, 2, 3, height, PlayerController.Direction.Down);

            var controller = new GameObject("StairwellController").AddComponent<StairwellController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("bottomExit").objectReferenceValue = bottom;
            serialized.FindProperty("topExit").objectReferenceValue = top;
            serialized.FindProperty("lockLayer").objectReferenceValue = lockLayer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ProvisionalMap.AddMessageBox();
            GameObject pedro = ProvisionalMap.CreatePlayer(ProvisionalMap.CellCenter(2, 20, height));
            ProvisionalMap.SetupCamera(pedro.transform, width, height);

            string path = ProvisionalMap.Save(scene, SceneNames.Escalera);
            Debug.Log($"Escalera provisional creada en {path}.");
        }

        /// <summary>Capa "Bloqueo" con la cinta; bloquea el paso solo mientras está activa.</summary>
        static Tilemap CreateLockLayer()
        {
            var go = new GameObject("Bloqueo", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(GameObject.Find("Grid").transform, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = 1;
            return go.GetComponent<Tilemap>();
        }

        static string[] BuildEmpty(string[] layout)
        {
            var rows = new string[layout.Length];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = new string('.', layout[i].Length);
            return rows;
        }
    }
}
