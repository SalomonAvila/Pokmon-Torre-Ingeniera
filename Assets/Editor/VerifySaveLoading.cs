using System;
using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Comprueba el guardado de punta a punta, sin tocar el archivo del jugador:
    /// 1) una partida v1 antigua migra conservando lo viejo y con lo nuevo por defecto;
    /// 2) una partida v2 con 'pokedexByFloor' (clave retirada en la v3) se carga sin fallar y el equipo
    ///    se completa con PS, EXP, movimientos y PP;
    /// 3) una partida actual pasa por ToJson + ParseJson y vuelve igual campo por campo;
    /// 4) la puerta del edificio exige inicial y una captura distinta de él.
    /// La ida y vuelta es en memoria y el menú está deshabilitado en Play, así que no puede corromper
    /// la partida en curso. Menú: Torre Ingeniería > Verificar guardado y carga (también con -executeMethod).
    /// </summary>
    public static class VerifySaveLoading
    {
        /// <summary>Formato v1 tal como lo escribía el juego antes de añadir equipo, Pokédex, etc.</summary>
        const string Version1Json = @"{
  ""version"": 1,
  ""playerName"": ""Pedro"",
  ""location"": ""Atrio"",
  ""credits"": 120,
  ""seals"": 3,
  ""completedFloors"": [ ""3"", ""4"" ],
  ""createdAt"": ""2026-10-01 09:00"",
  ""savedAt"": ""2026-10-02 18:30"",
  ""claveDesconocida"": 42
}";

        /// <summary>Formato v2: equipo sin PS/EXP/movimientos y el retirado 'pokedexByFloor'.</summary>
        const string Version2Json = @"{
  ""version"": 2,
  ""playerName"": ""Pedro"",
  ""location"": ""3"",
  ""credits"": 80,
  ""sealIds"": [ ""3"" ],
  ""bossNotes"": [ { ""floor"": ""3"", ""note"": 4.5 } ],
  ""team"": [ { ""speciesId"": ""bulbasaur"", ""level"": 8 } ],
  ""inventory"": [ { ""id"": ""tinto"", ""amount"": 2 } ],
  ""pokedex"": [ ""bulbasaur"", ""geodude"" ],
  ""pokedexByFloor"": [ { ""floor"": ""3"", ""species"": [ ""geodude"" ] } ],
  ""visitedFloors"": [ ""1"", ""3"" ],
  ""starterId"": ""bulbasaur"",
  ""completedFloors"": [ ""3"" ],
  ""createdAt"": ""2026-10-01 09:00"",
  ""savedAt"": ""2026-10-02 18:30""
}";

        static int checksFailed;

        [MenuItem("Torre Ingeniería/Verificar guardado y carga")]
        public static void Verify()
        {
            checksFailed = 0;

            Debug.Log("--- Carga de partida v1 (SaveSystem) ---");
            VerifyVersion1();

            Debug.Log("--- Carga de partida v2 con 'pokedexByFloor' retirado ---");
            VerifyVersion2();

            Debug.Log("--- Ida y vuelta en memoria de una partida actual ---");
            VerifyRoundTrip();

            Debug.Log("--- Puerta del edificio (inicial y captura) ---");
            VerifyBuildingDoor();

            if (checksFailed == 0)
                Debug.Log("Guardado y carga: verificado (v1 y v2 migran, la partida vuelve igual y la puerta exige captura).");
            else
                Debug.LogError($"Guardado y carga: {checksFailed} problema(s).");
        }

        /// <summary>En Play la prueba tocaría el estado en curso; el menú se deshabilita igual que en Progreso.</summary>
        [MenuItem("Torre Ingeniería/Verificar guardado y carga", true)]
        static bool CanVerify() => !EditorApplication.isPlaying;

        static void VerifyVersion1()
        {
            SaveData data = null;
            try
            {
                data = SaveSystem.ParseJson(Version1Json);
            }
            catch (Exception e)
            {
                Check(false, $"La partida v1 no debería fallar al cargar: {e.Message}");
            }

            if (data != null)
            {
                Check(data.version == SaveData.CurrentVersion,
                    $"La versión debería migrar a {SaveData.CurrentVersion} y quedó en {data.version}.");
                Check(data.playerName == "Pedro" && data.credits == 120,
                    "Se perdió el nombre o los créditos de la partida v1.");
                Check(data.location == "1",
                    $"El 'location' viejo 'Atrio' debe migrar al id de piso '1' y quedó en '{data.location}'.");
                Check(data.completedFloors.Count == 2 &&
                      data.completedFloors.Contains("3") && data.completedFloors.Contains("4"),
                    "Se perdió la lista de pisos completados.");
                Check(data.sealIds.Count == 0, "Los sellos deben arrancar vacíos (el 'seals' numérico viejo se ignora).");
                Check(data.team.Count == 0 && data.inventory.Count == 0 && data.pokedex.Count == 0 &&
                      data.visitedFloors.Count == 0 && data.bossNotes.Count == 0,
                    "Las listas nuevas (equipo, mochila, Pokédex, visitados, notas) deben arrancar vacías.");
                Check(data.starterId == "" && !data.accessCard && !data.tutorialDone &&
                      data.gabrielChoice == GabrielChoice.Undecided &&
                      data.lastBossFloor == "" && data.consecutiveBossLosses == 0,
                    "Las banderas nuevas deben arrancar con su valor inicial.");
            }

            // Un texto que no es JSON sí debe fallar: de eso se encarga TryLoad, que lo trata como "no hay partida".
            Check(Throws(() => SaveSystem.ParseJson("{ esto no es json")), "Un JSON ilegible debería lanzar excepción.");
        }

        static void VerifyVersion2()
        {
            SaveData data = null;
            try
            {
                data = SaveSystem.ParseJson(Version2Json);
            }
            catch (Exception e)
            {
                Check(false, $"La partida v2 no debería fallar al cargar: {e.Message}");
            }

            if (data == null)
                return;

            Check(data.version == SaveData.CurrentVersion,
                $"La partida v2 debería migrar a {SaveData.CurrentVersion} y quedó en {data.version}.");

            // La clave retirada se ignora sin romper el resto: el Pokédex total y lo demás se conservan.
            Check(data.pokedex.Count == 2 && data.pokedex.Contains("bulbasaur") && data.pokedex.Contains("geodude"),
                "Se perdió el Pokédex al ignorar 'pokedexByFloor'.");
            Check(data.location == "3", $"El id de piso '3' no debe alterarse y quedó en '{data.location}'.");
            Check(data.credits == 80 && data.starterId == "bulbasaur" && data.accessCard == false,
                "Se perdieron créditos o banderas de la partida v2.");
            Check(data.sealIds.Count == 1 && data.sealIds.Contains("3"),
                "Se perdieron los sellos de la partida v2.");
            Check(data.bossNotes.Count == 1 && data.bossNotes[0].floor == "3" && data.bossNotes[0].note == 4.5f,
                "Se perdieron las notas de jefes de la partida v2.");
            Check(data.inventory.Count == 1 && data.inventory[0].id == "tinto" && data.inventory[0].amount == 2,
                "Se perdió la mochila de la partida v2.");

            Check(data.team.Count == 1 && data.team[0].speciesId == "bulbasaur" && data.team[0].level == 8,
                "Se perdió el equipo de la partida v2.");
            if (data.team.Count == 1)
            {
                TeamMember member = data.team[0];
                Check(member.hp == member.maxHp && member.maxHp == PokemonStats.MaxHp(45, 8),
                    $"Un Pokémon de la v2 debe migrar con PS completos ({PokemonStats.MaxHp(45, 8)}) y quedó en {member.hp}/{member.maxHp}.");
                Check(member.exp == PokemonStats.ExperienceForLevel(8),
                    $"La EXP de un Pokémon migrado debe ser la de su nivel ({PokemonStats.ExperienceForLevel(8)}) y quedó en {member.exp}.");
                Check(member.moves != null && member.moves.Count == 3 &&
                      member.moves[0].moveId == "vine_whip" &&
                      member.moves.TrueForAll(slot => slot.pp == slot.maxPp && slot.maxPp > 0),
                    "El learnset de un Pokémon migrado debe completarse con sus PP.");
            }
        }

        /// <summary>
        /// Ida y vuelta en memoria: ToJson + ParseJson. No escribe en disco, así que no puede perder la
        /// partida del jugador ni aunque el Editor se cierre a mitad de la prueba.
        /// </summary>
        static void VerifyRoundTrip()
        {
            try
            {
                SaveData original = CreateCurrentSave();
                original.version = SaveData.CurrentVersion;
                original.savedAt = "2026-10-02 18:30";

                string json = JsonUtility.ToJson(original, true);
                SaveData loaded = SaveSystem.ParseJson(json);
                Check(loaded != null, "La partida serializada no se pudo volver a leer.");
                if (loaded == null)
                    return;

                Check(loaded.version == SaveData.CurrentVersion,
                    $"La partida releída debería estar en la versión {SaveData.CurrentVersion} y quedó en {loaded.version}.");
                Check(loaded.playerName == original.playerName && loaded.location == original.location &&
                      loaded.credits == original.credits && loaded.starterId == original.starterId,
                    "Se perdieron datos básicos (nombre, ubicación, créditos o inicial).");
                Check(loaded.team.Count == 5, $"El equipo debería conservar 5 miembros y quedaron {loaded.team.Count}.");

                Check(loaded.team[0].speciesId == "bulbasaur" && loaded.team[0].hp == 21 &&
                      loaded.team[0].maxHp == 27 && loaded.team[0].exp == 512 &&
                      loaded.team[0].moves.Count == 3 && loaded.team[0].moves[0].pp == 10,
                    "Se perdió el primer miembro con sus PS, EXP o movimientos.");
                Check(loaded.team[1].speciesId == "pidgey" && loaded.team[1].hp == 18 && loaded.team[1].maxHp == 20,
                    "Se perdió el segundo miembro con sus PS.");
                // PS 0 = debilitado; debe distinguirse del centinela -1.
                Check(loaded.team[2].speciesId == "rattata" && loaded.team[2].hp == 0 && loaded.team[2].maxHp == 15,
                    "Un Pokémon debilitado (PS = 0) debe conservar el 0 y no confundirse con el centinela.");
                // El centinela de "PS completos" se resuelve al cargar.
                Check(loaded.team[3].speciesId == "meowth" && loaded.team[3].hp == 18 &&
                      loaded.team[3].maxHp == 18 && loaded.team[3].moves.Count == 2,
                    "La carga debe resolver el centinela de PS completos y completar el learnset.");
                // Lista de movimientos vacía: se completa con el learnset.
                Check(loaded.team[4].speciesId == "eevee" && loaded.team[4].moves != null &&
                      loaded.team[4].moves.Count == 2,
                    "Un Pokémon sin movimientos debe recibir los del learnset al cargar.");

                Check(loaded.pokedex.Count == 2 && loaded.sealIds.Count == 1 && loaded.bossNotes.Count == 1,
                    "Se perdieron Pokédex, sellos o notas de jefes.");
                Check(loaded.inventory.Count == 1 && loaded.inventory[0].amount == 3 &&
                      loaded.defeatedTrainers.Count == 1 && loaded.collectedItems.Count == 1,
                    "Se perdió la mochila, los entrenadores derrotados o los ítems recogidos.");
                Check(loaded.accessCard && loaded.tutorialDone && loaded.gabrielChoice == GabrielChoice.Yes &&
                      loaded.lastBossFloor == "3" && loaded.consecutiveBossLosses == 2,
                    "Se perdieron banderas o la racha contra el jefe.");

                // Comparación exhaustiva: mismo JSON = mismos valores en todos los campos serializables.
                string expected = JsonUtility.ToJson(original, true);
                string actual = JsonUtility.ToJson(loaded, true);
                Check(expected == actual, "La partida releída no coincide campo por campo con la escrita.");
                if (expected != actual)
                    Debug.LogWarning($"Esperado:\n{expected}\n\nLeído:\n{actual}");
            }
            catch (Exception e)
            {
                Check(false, $"La ida y vuelta en memoria no debería fallar: {e.Message}");
            }
        }

        /// <summary>
        /// Prueba la condición de la puerta sin guardar ni mutar la partida: se asigna un SaveData de
        /// prueba a GameState y se restaura el anterior al terminar.
        /// </summary>
        static void VerifyBuildingDoor()
        {
            SaveData previous = GameState.Save;
            try
            {
                GameState.Save = new SaveData { starterId = "" };
                Check(!GameState.PassesBuildingDoor,
                    "Sin inicial, la puerta del edificio debe estar cerrada.");

                var onlyStarter = new SaveData { starterId = "bulbasaur" };
                onlyStarter.pokedex.Add("bulbasaur");
                GameState.Save = onlyStarter;
                Check(!GameState.PassesBuildingDoor,
                    "Con el inicial en el Pokédex pero sin otra captura, la puerta debe estar cerrada.");

                onlyStarter.pokedex.Add("pidgey");
                Check(GameState.PassesBuildingDoor,
                    "Con inicial y una captura distinta, la puerta debe abrir.");

                var captureWithoutStarter = new SaveData { starterId = "" };
                captureWithoutStarter.pokedex.Add("pidgey");
                GameState.Save = captureWithoutStarter;
                Check(!GameState.PassesBuildingDoor,
                    "Con una captura pero sin inicial, la puerta debe estar cerrada.");
            }
            finally
            {
                GameState.Save = previous;
            }
        }

        /// <summary>Partida v4 con todos los campos para que la comparación campo por campo cubra el formato entero.</summary>
        static SaveData CreateCurrentSave()
        {
            var data = new SaveData
            {
                playerName = "Ana",
                location = "3",
                credits = 450,
                accessCard = true,
                starterId = "bulbasaur",
                gabrielChoice = GabrielChoice.Yes,
                tutorialDone = true,
                lastBossFloor = "3",
                consecutiveBossLosses = 2,
                createdAt = "2026-10-01 09:00",
            };

            data.sealIds.Add("3");
            data.bossNotes.Add(new FloorNote { floor = "3", note = 4.5f });
            data.team.Add(new TeamMember
            {
                speciesId = "bulbasaur",
                level = 8,
                hp = 21,
                maxHp = 27,
                exp = 512,
                moves =
                {
                    new MoveSlot { moveId = "vine_whip", pp = 10, maxPp = 10 },
                    new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 },
                    new MoveSlot { moveId = "growl", pp = 40, maxPp = 40 },
                },
            });
            data.team.Add(new TeamMember
            {
                speciesId = "pidgey",
                level = 5,
                hp = 18,
                maxHp = 20,
                exp = 125,
                moves =
                {
                    new MoveSlot { moveId = "sand_attack", pp = 15, maxPp = 15 },
                    new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 },
                },
            });
            data.team.Add(new TeamMember
            {
                speciesId = "rattata",
                level = 3,
                hp = 0,
                maxHp = 15,
                exp = 27,
                moves =
                {
                    new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 },
                    new MoveSlot { moveId = "tail_whip", pp = 30, maxPp = 30 },
                },
            });
            data.team.Add(new TeamMember
            {
                speciesId = "meowth",
                level = 4,
                hp = 18,
                maxHp = 18,
                exp = 64,
                moves =
                {
                    new MoveSlot { moveId = "scratch", pp = 35, maxPp = 35 },
                    new MoveSlot { moveId = "growl", pp = 40, maxPp = 40 },
                },
            });
            data.team.Add(new TeamMember
            {
                speciesId = "eevee",
                level = 6,
                hp = 20,
                maxHp = 24,
                exp = 216,
                moves =
                {
                    new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 },
                    new MoveSlot { moveId = "tail_whip", pp = 30, maxPp = 30 },
                },
            });
            data.inventory.Add(new InventoryItem { id = "tinto", amount = 3 });
            data.pokedex.Add("bulbasaur");
            data.pokedex.Add("pidgey");
            data.visitedFloors.Add("1");
            data.visitedFloors.Add("3");
            data.defeatedTrainers.Add("entrenador-1");
            data.collectedItems.Add("apuntes-3");
            data.completedFloors.Add("3");
            return data;
        }

        static bool Throws(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        static void Check(bool ok, string problem)
        {
            if (ok)
                return;

            Debug.LogError(problem);
            checksFailed++;
        }
    }
}
