using System.Collections.Generic;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Construye el pasillo de los ascensores del lobby principal (Figura 23 del documento de diseño):
    /// escalera de madera a la izquierda, nicho con los tres ascensores (fuera de servicio), torniquetes
    /// y la escalera que baja al atrio. Al bajar por esa escalera se vuelve a las gradas del atrio.
    /// Es el piso 2: la escalera de la izquierda sube al piso 3 (escena Escalera).
    /// Menú: Torre Ingeniería > Mapas > Construir pasillo de ascensores provisional.
    /// </summary>
    public static class AscensoresBuilder
    {
        const string TilesetPath = "Assets/Art/Tilesets/Propios/AscensoresProvisional.png";
        const string TilesetName = "AscensoresProvisional";

        /// <summary>Punto de llegada al subir desde el atrio.</summary>
        public const string SpawnFromAtrium = "DesdeAtrio";

        // Fila 0 = norte (arriba). Piso:
        // (espacio) fuera   . baldosa   b piso beige   v escalera de madera (izquierda)
        // a piso del nicho de ascensores   T torniquete   g escalera que baja al atrio
        static readonly string[] Floor =
        {
            "                              ",
            "                              ",
            "vvvbb........Tb...............",
            "vvvbb........Tb...............",
            "vvvbb........Tb...............",
            "vvvbb........Tb...............",
            "     aaaaaaaa   ggggggggg     ",
            "     aaaaaaaa   ggggggggg     ",
            "     aaaaaaaa   ggggggggg     ",
            "                ggggggggg     ",
            "                              ",
        };

        // Objetos:
        // . nada   W pared con moldura   > flecha   | baranda   G panel de llamada
        // E/F ascensor (parte de arriba, izquierda/derecha)   e/f ascensor (parte de abajo)
        static readonly string[] Objects =
        {
            "..............................",
            "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWW",
            "..............................",
            ".>............................",
            "..............................",
            "..............................",
            "............G...|.......|.....",
            "......EFEFEF....|.......|.....",
            "......efefef....|.......|.....",
            "................|.......|.....",
            "..............................",
        };

        // Índices en AscensoresProvisional.png.
        const int TileA = 0, TileB = 1;
        static readonly Dictionary<char, int> FloorIndex = new Dictionary<char, int>
        {
            [' '] = 14, ['b'] = 4, ['v'] = 6, ['g'] = 2, ['a'] = 8, ['T'] = 15,
        };
        static readonly Dictionary<char, int> ObjectIndex = new Dictionary<char, int>
        {
            ['W'] = 5, ['>'] = 7, ['|'] = 3, ['E'] = 9, ['F'] = 10, ['e'] = 11, ['f'] = 12, ['G'] = 13,
        };

        // Los ascensores están fuera de servicio: bloquean el paso. Los torniquetes están abiertos (luz verde).
        const string BlockingFloors = " ";
        const string BlockingObjects = "W|EFefG";

        [MenuItem("Torre Ingeniería/Mapas/Construir pasillo de ascensores provisional")]
        public static void Build()
        {
            ProvisionalMap.PrepareCommonAssets(TilesetPath);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = MapSceneBuilder.CreateScene();
            int width = Floor[0].Length;
            int height = Floor.Length;

            ProvisionalMap.Paint(Floor, Objects, ProvisionalMap.LoadTiles(TilesetName, 16),
                (symbol, col, row) => symbol == '.' ? ((col / 2 + row / 2) % 2 == 0 ? TileA : TileB) : FloorIndex[symbol],
                ObjectIndex, BlockingFloors, BlockingObjects);

            ProvisionalMap.AddFloorInfo("2");
            ProvisionalMap.AddMessageBox();
            ProvisionalMap.AddHud();

            // Escalera de la izquierda → escalera en zigzag hacia el piso 3. Al volver, se llega a su lado.
            // La escalera no se abre hasta superar el combate tutorial de Gabriel (Doc §8).
            WarpZone stairs = ProvisionalMap.AddWarp("SubidaPiso3", 0, 0, 2, 5, height,
                SceneNames.Escalera, StairwellController.SpawnFromBelow);
            stairs.SetTutorialGate(true,
                "—Protocolo de evaluación: nadie sube sin la prueba de Gabriel.\n" +
                "Gabriel te espera en el pasillo.");
            ProvisionalMap.AddSpawn(Building.SpawnFromStairsAbove, 2, 3, height, PlayerController.Direction.Right);

            // Llegada desde el atrio: arriba de la escalera, mirando hacia el pasillo.
            ProvisionalMap.AddSpawn(SpawnFromAtrium, 20, 6, height, PlayerController.Direction.Up);
            // Último escalón de la escalera (entre las barandas) → gradas superiores del atrio.
            // Bajar siempre está permitido: esta salida no lleva la guarda de la puerta del edificio.
            ProvisionalMap.AddWarp("BajadaAtrio", 17, 23, 9, 9, height, SceneNames.Atrio, AtrioBuilder.SpawnFromElevators);

            // Gabriel espera en el pasillo para el combate tutorial antes de la escalera al piso 3 (Doc §8).
            ProvisionalMap.CreateTutorialGabriel(8, 2, height, PlayerController.Direction.Right);

            // Si se prueba esta escena directamente, Pedro empieza donde llega desde el atrio.
            GameObject pedro = ProvisionalMap.CreatePlayer(ProvisionalMap.CellCenter(20, 6, height));
            ProvisionalMap.SetupCamera(pedro.transform, width, height);

            string path = ProvisionalMap.Save(scene, SceneNames.Ascensores);
            Debug.Log($"Pasillo de ascensores creado en {path}.");
        }
    }
}
