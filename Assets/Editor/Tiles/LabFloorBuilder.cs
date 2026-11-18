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
            // En el piso 3 los objetos del suelo son recogibles de verdad (AddFloor3Content): se quitan
            // las cajas decorativas 'o' para que no parezcan objetos que no se pueden coger.
            if (floor == "3")
                for (int row = 0; row < layout.Objects.Length; row++)
                    layout.Objects[row] = layout.Objects[row].Replace('o', '.');
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
            AddFloorTrainers(floor, height);
            AddElevators(height);
            if (floor == "3")
                AddFloor3Content(height);

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
        /// Entrenadores del piso 3 (Doc §10): los dos estudiantes del pasillo central y la Ing. Cantera
        /// custodiando la escalera del noroeste. El resto de los pisos aún no tiene entrenadores.
        /// </summary>
        static void AddFloorTrainers(string floor, int height)
        {
            if (floor != "3")
                return;

            // Pasillo central: el estudiante mira al sur (recibe al jugador que sube desde el SE) y la
            // monitora al norte, delante de la escalera del jefe.
            ProvisionalMap.CreateTrainer("EstudianteConcretos", 14, 20, height, "estudiante_concretos",
                new[]
                {
                    "¡Alto! Soy el estudiante de Concretos.",
                    "Aquí se practica con rocas: veamos cómo aguanta tu equipo.",
                },
                PlayerController.Direction.Down);

            ProvisionalMap.CreateTrainer("MonitoraSuelos", 15, 32, height, "monitora_suelos",
                new[]
                {
                    "Soy la monitora de Suelos.",
                    "La Ing. Cantera no deja subir a cualquiera. Demuéstrame que puedes.",
                },
                PlayerController.Direction.Up);

            // La jefa del piso espera en su escalera (Doc §8: «Todo gran edificio empieza por los cimientos»).
            ProvisionalMap.CreateTrainer("IngCantera", 8, 8, height, "cantera",
                new[]
                {
                    "Todo gran edificio empieza por los cimientos.",
                    "Veamos cuáles son los tuyos.",
                },
                PlayerController.Direction.Right, ProvisionalMap.Character.Cantera);
        }

        /// <summary>
        /// Contenido del piso 3 (Doc §10, Figura 38), con las salas de <see cref="LabFloorLayout"/>:
        /// <list type="bullet">
        /// <item>Encuentros por laboratorio: E6 Concretos (Geodude, Rhyhorn), O3 Suelos (Geodude, Onix),
        /// E3 Geología (Geodude, Kabuto, Omanyte) y O1 Estructuras (Geodude, Rhyhorn, Onix).</item>
        /// <item>Objetos: 2 Tintos en el pasillo, 1 Tinto en E6, 5 Pokébolas en los 4 laboratorios, Agua de
        /// panela en E4, Apuntes en E2 y la Cafeína escondida en O3.</item>
        /// <item>Pistas en las aulas O2 y E5, placas de cada sala, E1 cerrada y algunos estudiantes.</item>
        /// </list>
        /// Las coordenadas son (columna, fila) con la fila 0 al norte, las mismas del layout.
        /// </summary>
        static void AddFloor3Content(int height)
        {
            // ---------- Encuentros por laboratorio ----------
            ProvisionalMap.AddEncounterArea("EncuentrosO1Estructuras", 1, 11, 11, 19, height, "geodude", "rhyhorn", "onix");
            ProvisionalMap.AddEncounterArea("EncuentrosO3Suelos", 1, 11, 31, 39, height, "geodude", "onix");
            ProvisionalMap.AddEncounterArea("EncuentrosE3Geologia", 18, 30, 15, 20, height, "geodude", "kabuto", "omanyte");
            ProvisionalMap.AddEncounterArea("EncuentrosE6Concretos", 18, 30, 36, 41, height, "geodude", "rhyhorn");

            // ---------- Objetos (ids estables: no reaparecen al volver) ----------
            Pickup("TintoPasillo1", 13, 12, height, "piso3_tinto_pasillo_1", "tinto");
            Pickup("TintoPasillo2", 16, 26, height, "piso3_tinto_pasillo_2", "tinto");
            Pickup("TintoE6", 29, 40, height, "piso3_tinto_e6", "tinto");
            Pickup("PokebolaO1", 10, 12, height, "piso3_pokeball_o1", "pokeball");
            Pickup("PokebolaO3a", 10, 32, height, "piso3_pokeball_o3_a", "pokeball");
            Pickup("PokebolaO3b", 2, 39, height, "piso3_pokeball_o3_b", "pokeball");
            Pickup("PokebolaE3", 29, 16, height, "piso3_pokeball_e3", "pokeball");
            Pickup("PokebolaE6", 19, 37, height, "piso3_pokeball_e6", "pokeball");
            Pickup("AguaPanelaE4", 28, 26, height, "piso3_agua_panela_e4", "agua_panela", "AGUA DE PANELA");
            Pickup("ApuntesE2", 28, 12, height, "piso3_apuntes_e2", "apuntes", "APUNTES",
                "Son unos apuntes de Mecánica de Suelos, llenos de subrayados.");
            ProvisionalMap.CreateHiddenPickup("CafeinaO3", 1, 31, height, "piso3_cafeina_o3", "cafeina", 1,
                new[]
                {
                    "Detrás del rack hay una lata olvidada...",
                    "¡Encontraste 1 CAFEÍNA!",
                });

            // ---------- Placas de las salas (pared junto a cada puerta) ----------
            ProvisionalMap.CreateSign("LetreroPiso", 15, 0, height,
                "PISO 3 – INGENIERÍA CIVIL Y GEOLOGÍA.",
                "«Todo gran edificio empieza por los cimientos.»");
            ProvisionalMap.CreateSign("PlacaO1", 12, 14, height,
                "O1 – LABORATORIO DE ESTRUCTURAS.",
                "Aviso: no apoyar POKÉMON de roca sobre las vigas de prueba.");
            ProvisionalMap.CreateSign("PlacaO2", 12, 24, height,
                "O2 – AULA DE GEOLOGÍA.",
                "Hoy: «Minerales, fósiles y por qué el Onix no cabe en el ascensor».");
            ProvisionalMap.CreateSign("PlacaO3", 12, 34, height,
                "O3 – LABORATORIO DE SUELOS.",
                "Prohibido excavar sin permiso de la Ing. Cantera.");
            ProvisionalMap.CreateSign("PuertaE1", 17, 3, height,
                "SALA E1 – CERRADA.",
                "Un papel pegado dice: «Cerrada por filtraciones tras la tormenta. Mantenimiento».");
            ProvisionalMap.CreateSign("PlacaE2", 17, 9, height,
                "E2 – BODEGA DE MATERIALES.",
                "Inventario: cemento, arena, grava y apuntes que nadie reclamó.");
            ProvisionalMap.CreateSign("PlacaE3", 17, 16, height,
                "E3 – LABORATORIO DE GEOLOGÍA.",
                "Colección de fósiles. Algunos se movieron después de la tormenta.");
            ProvisionalMap.CreateSign("PlacaE4", 17, 23, height,
                "E4 – SALA DE ESTUDIO.",
                "Silencio, por favor: hay parcial de Resistencia de Materiales.");
            ProvisionalMap.CreateSign("PlacaE5", 17, 30, height,
                "E5 – AULA DE ESTRUCTURAS.",
                "Monitoría de Estática: martes y jueves, o cuando haya emergencia.");
            ProvisionalMap.CreateSign("PlacaE6", 17, 37, height,
                "E6 – LABORATORIO DE CONCRETOS.",
                "Cilindros de prueba en curado. No tocar... ni morder, Rhyhorn.");

            // ---------- Pistas (aulas O2 y E5) ----------
            ProvisionalMap.CreateNpc("EstudianteGeologia", 7, 22, height, new[]
            {
                "¿Vas a enfrentar a la Ing. Cantera? Sus POKÉMON son de tipo ROCA.",
                "La roca no aguanta el agua ni las plantas: con SQUIRTLE o BULBASAUR lo tienes más fácil.",
                "Si tu compañero es CHARMANDER, busca los Apuntes de la bodega E2.",
                "Enseñan un ataque de TIERRA, y la tierra también le gana a la roca.",
            });
            ProvisionalMap.CreateNpc("MonitorEstructuras", 24, 30, height, new[]
            {
                "Dato de monitoría: ONIX tiene muchísima DEFENSA...",
                "...pero su DEFENSA ESPECIAL es baja. PISTOLA AGUA o BURBUJA le hacen mucho daño.",
                "Y si te falta nivel, dicen que alguien escondió CAFEÍNA en el Laboratorio de Suelos.",
                "Revisa las esquinas, detrás de los racks.",
            });

            // ---------- Estudiantes del piso (ambiente) ----------
            ProvisionalMap.CreateNpc("EstudianteEscalera", 13, 42, height, new[]
            {
                "Subí por la escalera porque los ascensores no sirven sin carné.",
                "Dicen que la Ing. Cantera tiene el CARNÉ DE ACCESO. Si la vences, te lo da.",
            }, facing: PlayerController.Direction.Right);
            ProvisionalMap.CreateNpc("EstudianteE4", 23, 23, height, new[]
            {
                "Shh... estoy repasando para el parcial.",
                "Con la tormenta lo aplazaron, pero el profe dijo que «la naturaleza no da prórroga».",
                "Si te hace falta, en esta sala dejaron un AGUA DE PANELA.",
            });
            ProvisionalMap.CreateNpc("AuxiliarBodega", 21, 9, height, new[]
            {
                "Soy el auxiliar de la bodega. Aquí guardamos todo lo que sobra de los laboratorios.",
                "Esos apuntes de la esquina no son de nadie. Llévatelos, a alguien le servirán.",
            });
            ProvisionalMap.CreateNpc("TecnicoConcretos", 25, 39, height, new[]
            {
                "¡Cuidado! Un RHYHORN se metió entre los cilindros de prueba.",
                "Aquí también salen GEODUDE. Si quieres capturarlos, debilítalos primero.",
            }, facing: PlayerController.Direction.Left);
            ProvisionalMap.CreateNpc("EstudianteFosiles", 24, 18, height, new[]
            {
                "¡Los fósiles del laboratorio cobraron vida! Vi un KABUTO y un OMANYTE.",
                "Solo salen aquí, en Geología. No los vas a encontrar en otro lado del piso.",
            }, facing: PlayerController.Direction.Down);
        }

        static void Pickup(string name, int col, int row, int height, string pickupId, string itemId,
            string itemName = null, string intro = null)
        {
            string label = itemName ?? itemId switch
            {
                "tinto" => "TINTO",
                "pokeball" => "POKÉBOLA",
                _ => itemId.ToUpperInvariant(),
            };
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(intro))
                lines.Add(intro);
            lines.Add($"¡Encontraste 1 {label}!");
            ProvisionalMap.CreatePickup(name, col, row, height, pickupId, itemId, 1, lines.ToArray());
        }

        /// <summary>
        /// Ascensores del sur (filas 41-42 de <see cref="LabFloorLayout"/>): cada puerta abre el selector de
        /// piso desde la fila 43. Al llegar en ascensor se aparece delante, mirando al pasillo. El ascensor
        /// de servicio del norte sigue sin funcionar.
        /// </summary>
        static void AddElevators(int height)
        {
            foreach (int col in new[] { 3, 4, 6, 7, 9, 10 })
                ProvisionalMap.CreateElevatorPanel($"Ascensor{col}", col, 42, height);
            ProvisionalMap.AddSpawn(Building.SpawnFromElevator, 7, 43, height, PlayerController.Direction.Down);
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
