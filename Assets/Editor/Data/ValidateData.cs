using System;
using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Verificación ejecutable de la Etapa 1: datos del prototipo, fórmulas del combate y equipo.
    /// Como el proyecto no tiene suite de tests, este validador del menú comprueba valores conocidos
    /// de daño, captura, stats, experiencia y efectividad de tipos, que cada especie del catálogo
    /// tenga su asset y que los learnsets y equipos apunten a movimientos y especies existentes,
    /// además de la factoría <see cref="TeamMembers"/> y su normalización al cargar.
    ///
    /// Se ejecuta después de "Datos > Construir datos del prototipo".
    /// Menú: Torre Ingeniería > Validar datos y fórmulas.
    /// </summary>
    public static class ValidateData
    {
        static int checksFailed;

        [MenuItem("Torre Ingeniería/Validar datos y fórmulas")]
        public static void Validate()
        {
            checksFailed = 0;
            Debug.Log("--- Datos, fórmulas y equipo (Etapa 1) ---");

            ValidateTypeChart();
            ValidateFormulas();
            ValidateSpeciesAssets();
            ValidateMoveAssets();
            ValidateItemAssets();
            ValidateTrainerAssets();
            ValidateTeamMembers();

            if (checksFailed == 0)
                Debug.Log("Datos y fórmulas: todo coincide.");
            else
                Debug.LogError($"Datos y fórmulas: {checksFailed} discrepancia(s) por revisar.");
        }

        // ---------- Tabla de tipos ----------

        static void ValidateTypeChart()
        {
            // Casos conocidos de la franquicia (Gen 3).
            CheckNear(TypeChart.Effectiveness(PokemonType.Water, PokemonType.Rock, true, PokemonType.Ground), 4f,
                "Agua contra Roca/Tierra debería ser 4×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Grass, PokemonType.Rock, true, PokemonType.Ground), 4f,
                "Planta contra Roca/Tierra debería ser 4×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Electric, PokemonType.Ground), 0f,
                "Eléctrico contra Tierra debería ser 0×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Normal, PokemonType.Ghost), 0f,
                "Normal contra Fantasma debería ser 0×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Ghost, PokemonType.Normal), 0f,
                "Fantasma contra Normal debería ser 0×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Fire, PokemonType.Grass), 2f,
                "Fuego contra Planta debería ser 2×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Dark, PokemonType.Psychic), 2f,
                "Siniestro contra Psíquico debería ser 2×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Steel, PokemonType.Ice), 2f,
                "Acero contra Hielo debería ser 2×.");
            CheckNear(TypeChart.Effectiveness(PokemonType.Poison, PokemonType.Steel), 0f,
                "Veneno contra Acero debería ser 0× en Gen 3.");

            // Toda la matriz debe usar solo 0, 0,5, 1 o 2.
            foreach (PokemonType attack in Enum.GetValues(typeof(PokemonType)))
                foreach (PokemonType defend in Enum.GetValues(typeof(PokemonType)))
                {
                    float value = TypeChart.Effectiveness(attack, defend);
                    if (Array.IndexOf(TypeChart.AllowedValues, value) < 0)
                        Check(false, $"{PokemonTypeNames.DisplayName(attack)} contra " +
                                     $"{PokemonTypeNames.DisplayName(defend)} tiene un valor inválido ({value}).");
                }
        }

        // ---------- Fórmulas ----------

        static void ValidateFormulas()
        {
            // Daño: nivel 50, poder 80, ataque 100, defensa 100, sin STAB, efectividad 1 y variación 1.
            // El crítico es (propuesta) y se inyecta; las pruebas lo fijan en false salvo el caso explícito.
            CheckEqual(GameRules.Damage(50, 80, 100, 100, 1f, false, 1f, critical: false), 37,
                "El daño base de referencia debería ser 37.");
            CheckEqual(GameRules.Damage(50, 80, 100, 100, 1f, true, 1f), 55,
                "El daño con STAB debería ser 55.");
            CheckEqual(GameRules.Damage(50, 80, 100, 100, 2f, false, 1f), 74,
                "El daño supereficaz debería ser 74.");
            CheckEqual(GameRules.Damage(50, 80, 100, 100, 1f, false, 1f, critical: true), 74,
                "El daño crítico debería ser 74.");
            CheckEqual(GameRules.Damage(50, 0, 100, 100, 1f, false, 1f), 0,
                "Un movimiento de estado no debe hacer daño.");
            CheckEqual(GameRules.Damage(5, 35, 11, 10, 0.5f, true, GameRules.DamageRollMin), 3,
                "El daño con poco eficaz y STAB a nivel 5 debería ser 3.");

            // Stats: IV 31, EV 0, naturaleza neutra.
            CheckEqual(PokemonStats.MaxHp(45, 5), 21, "Bulbasaur nv 5 debería tener 21 PS.");
            CheckEqual(PokemonStats.Stat(49, 5), 11, "Bulbasaur nv 5 debería tener 11 de Ataque.");
            CheckEqual(PokemonStats.Stat(160, 10), 40, "Onix nv 10 debería tener 40 de Defensa.");

            // Experiencia: curva media rápida.
            CheckEqual(PokemonStats.ExperienceForLevel(5), 125, "El nivel 5 debería pedir 125 de EXP.");
            CheckEqual(PokemonStats.LevelForExperience(124), 4, "124 de EXP deberían ser nivel 4.");
            CheckEqual(PokemonStats.LevelForExperience(125), 5, "125 de EXP deberían ser nivel 5.");
            CheckEqual(GameRules.ExpReward(64, 5), 45, "Bulbasaur nv 5 debería dar 45 de EXP.");

            // Captura: fórmula de la franquicia recortada al 5 %–95 %. El estado es (propuesta)
            // y se deja en 1 (sin estado) para que el valor sea fijo.
            CheckNear(GameRules.CaptureChance(21, 1, 45, statusBonus: 1f), 0.1709f, 0.001f,
                "La captura de un Bulbasaur a 1 PS debería rondar el 17,1 %.");
            Check(GameRules.CaptureChance(21, 21, 255) <= GameRules.CaptureChanceMax + 0.0001f,
                "La captura no debería pasar del máximo.");
            Check(GameRules.CaptureChance(21, 21, 1) >= GameRules.CaptureChanceMin - 0.0001f,
                "La captura no debería bajar del mínimo.");
        }

        // ---------- Assets ----------

        static void ValidateSpeciesAssets()
        {
            var expected = new List<string>(SpeciesCatalog.Starters);
            foreach (string floor in SpeciesCatalog.FloorsWithEncounters)
                expected.AddRange(SpeciesCatalog.OnFloor(floor));

            var seen = new HashSet<string>();
            foreach (string id in expected)
            {
                if (!seen.Add(id))
                    continue;

                PokemonData data = PokemonDatabase.Get(id);
                Check(data != null, $"Falta el asset de la especie '{id}'. Ejecuta Datos > Construir datos del prototipo.");
                if (data == null)
                    continue;

                Check(data.speciesId == id, $"El asset de '{id}' declara speciesId '{data.speciesId}'.");
                Check(id == id.ToLowerInvariant(), $"El id '{id}' debe ir en minúscula.");
                Check(data.catchRate >= 1 && data.catchRate <= 255, $"'{id}' tiene catchRate fuera de rango.");
                Check(data.baseHp > 0 && data.baseAttack > 0 && data.baseDefense > 0 &&
                      data.baseSpAttack > 0 && data.baseSpDefense > 0 && data.baseSpeed > 0,
                    $"'{id}' tiene stats base en cero.");

                foreach (LearnsetEntry entry in data.learnset)
                {
                    Check(entry.level >= PokemonStats.MinLevel && entry.level <= PokemonStats.MaxLevel,
                        $"'{id}' aprende un movimiento en el nivel {entry.level}, fuera de rango.");
                    Check(MoveDatabase.Exists(entry.moveId), $"'{id}' aprende '{entry.moveId}', que no existe.");
                }

                // Futuro: `TeamMembers.Create` ya rechaza una pre-evolución en su nivel de evolución o
                // superior; cuando haya evoluciones, validar también que las tablas de encuentros no la
                // generen en ese rango. En el prototipo todo es ≤ 10 y ninguna especie evoluciona.
            }
        }

        static void ValidateMoveAssets()
        {
            foreach (string id in new[]
            {
                "tackle", "scratch", "quick_attack", "body_slam", "growl", "tail_whip", "harden",
                "vine_whip", "razor_leaf", "sleep_powder", "ember", "flamethrower", "water_gun",
                "bubble", "gust", "wing_attack", "rock_throw", "rock_slide", "mud_slap",
                "poison_sting", "sand_attack", "acid",
            })
            {
                MoveData data = MoveDatabase.Get(id);
                Check(data != null, $"Falta el movimiento '{id}'.");
                if (data == null)
                    continue;

                Check(data.moveId == id, $"El asset de '{id}' declara moveId '{data.moveId}'.");
                Check(id == id.ToLowerInvariant(), $"El id '{id}' debe ir en minúscula.");
                Check(data.category == MoveCategory.Status || data.power > 0,
                    $"'{id}' es de ataque y no tiene poder.");
                Check(data.maxPp > 0, $"'{id}' no tiene PP.");
            }
        }

        static void ValidateItemAssets()
        {
            foreach (string id in new[] { "tinto", "agua_panela", "pokeball" })
            {
                ItemData data = ItemDatabase.Get(id);
                Check(data != null, $"Falta el objeto '{id}'.");
                if (data == null)
                    continue;

                Check(data.itemId == id, $"El asset de '{id}' declara itemId '{data.itemId}'.");
                Check(id == id.ToLowerInvariant(), $"El id '{id}' debe ir en minúscula.");
                Check(data.category != ItemCategory.Heal || data.healAmount > 0,
                    $"'{id}' es curativo y no cura nada.");
            }
        }

        static void ValidateTrainerAssets()
        {
            TrainerData cantera = TrainerDatabase.Get("cantera");
            Check(cantera != null, "Falta el asset del jefe 'cantera'.");
            if (cantera == null)
                return;

            Check(cantera.isBoss, "'cantera' debería estar marcado como jefe.");
            Check(cantera.floorId == "3", $"Cantera debería ser del piso 3 y dice '{cantera.floorId}'.");
            Check(cantera.sealId == cantera.floorId, "El sello de Cantera debería ser el id de su piso.");
            Check(cantera.team.Count == Building.BossTeamSize("3"),
                $"Cantera debería llevar {Building.BossTeamSize("3")} Pokémon y lleva {cantera.team.Count}.");

            foreach (TrainerPokemon monster in cantera.team)
            {
                Check(PokemonDatabase.Exists(monster.speciesId), $"Cantera usa '{monster.speciesId}', que no existe.");
                Check(monster.level > 0, $"Cantera tiene un Pokémon en nivel {monster.level}.");
                foreach (string move in monster.moveIds)
                    Check(MoveDatabase.Exists(move), $"Cantera usa el movimiento '{move}', que no existe.");
            }
        }

        // ---------- Equipo ----------

        static void ValidateTeamMembers()
        {
            // Bulbasaur nv 5: PS y EXP de la fórmula; learnset con tackle y growl (mismo nivel, orden de tabla).
            TeamMember bulbasaur = TryCreate("bulbasaur", 5, "Bulbasaur nv 5");
            if (bulbasaur != null)
            {
                CheckEqual(bulbasaur.maxHp, 21, "Bulbasaur nv 5 debería tener 21 PS máximos.");
                CheckEqual(bulbasaur.hp, 21, "Bulbasaur nv 5 debería arrancar con los PS completos.");
                CheckEqual(bulbasaur.exp, 125, "Bulbasaur nv 5 debería tener 125 de EXP (5³).");
                CheckEqual(bulbasaur.moves.Count, 2, "Bulbasaur nv 5 debería conocer 2 movimientos.");
                if (bulbasaur.moves.Count == 2)
                {
                    Check(bulbasaur.moves[0].moveId == "tackle" && bulbasaur.moves[1].moveId == "growl",
                        "Bulbasaur nv 5 debería llevar tackle y growl, en el orden de la tabla.");
                    CheckEqual(bulbasaur.moves[0].maxPp, 35, "Tackle debería tener 35 PP.");
                    CheckEqual(bulbasaur.moves[1].maxPp, 40, "Growl debería tener 40 PP.");
                    CheckEqual(bulbasaur.moves[0].pp, 35, "Tackle debería arrancar con los PP llenos.");
                    CheckEqual(bulbasaur.moves[1].pp, 40, "Growl debería arrancar con los PP llenos.");
                }
            }

            // Geodude nv 8: los dos movimientos de mayor nivel ≤ 8 y sus PP.
            TeamMember geodude = TryCreate("geodude", 8, "Geodude nv 8");
            if (geodude != null)
            {
                CheckEqual(geodude.maxHp, 26, "Geodude nv 8 debería tener 26 PS máximos.");
                CheckEqual(geodude.exp, 512, "Geodude nv 8 debería tener 512 de EXP (8³).");
                CheckEqual(geodude.moves.Count, 2, "Geodude nv 8 debería conocer 2 movimientos.");
                if (geodude.moves.Count == 2)
                {
                    Check(geodude.moves[0].moveId == "rock_throw" && geodude.moves[1].moveId == "tackle",
                        "Geodude nv 8 debería llevar rock_throw y tackle, del nivel más alto al más bajo.");
                    CheckEqual(geodude.moves[0].maxPp, 15, "Rock Throw debería tener 15 PP.");
                    CheckEqual(geodude.moves[1].maxPp, 35, "Tackle debería tener 35 PP.");
                    CheckEqual(geodude.moves[0].pp, 15, "Rock Throw debería arrancar con los PP llenos.");
                    CheckEqual(geodude.moves[1].pp, 35, "Tackle debería arrancar con los PP llenos.");
                }
            }

            // Equipo de un entrenador: respeta los movimientos fijos de TrainerData.
            TrainerData cantera = TrainerDatabase.Get("cantera");
            if (cantera != null)
            {
                try
                {
                    List<TeamMember> team = TeamMembers.CreateTeam(cantera);
                    CheckEqual(team.Count, cantera.team.Count, "El equipo de Cantera debería materializarse completo.");
                    if (team.Count == cantera.team.Count && team.Count >= 2)
                    {
                        Check(team[0].speciesId == "geodude" && team[0].moves.Count == 3 &&
                              team[0].moves[0].moveId == "tackle",
                            "Cantera: Geodude debería conservar sus 3 movimientos fijos.");
                        Check(team[1].speciesId == "onix" && team[1].moves.Count == 4,
                            "Cantera: Onix debería conservar sus 4 movimientos fijos.");
                    }
                }
                catch (Exception e)
                {
                    Check(false, $"No se pudo materializar el equipo de Cantera: {e.Message}");
                }
            }

            // Recalcular una partida vieja: centinela, EXP 0 y movimientos vacíos.
            var legacy = new TeamMember
            {
                speciesId = "bulbasaur",
                level = 8,
                hp = TeamMember.FullHp,
                maxHp = 0,
                exp = 0,
                moves = new List<MoveSlot>(),
            };
            TeamMembers.RecalculateOnLoad(legacy);
            CheckEqual(legacy.maxHp, 27, "Al cargar, Bulbasaur nv 8 debería recuperar sus 27 PS máximos.");
            CheckEqual(legacy.hp, 27, "El centinela de PS completos debería resolverse al máximo.");
            CheckEqual(legacy.exp, 512, "La EXP 0 de una partida vieja debería pasar a la del nivel (8³).");
            CheckEqual(legacy.moves.Count, 3, "El learnset vacío de una partida vieja debería completarse.");

            // PS por encima del nuevo máximo: se recortan.
            var overMax = new TeamMember
            {
                speciesId = "pidgey",
                level = 5,
                hp = 999,
                maxHp = 0,
                exp = 0,
                moves = new List<MoveSlot>(),
            };
            TeamMembers.RecalculateOnLoad(overMax);
            CheckEqual(overMax.maxHp, 20, "Pidgey nv 5 debería tener 20 PS máximos.");
            CheckEqual(overMax.hp, 20, "Los PS por encima del máximo deberían recortarse.");

            // PS 0 = debilitado: no se debe confundir con el centinela.
            var fainted = new TeamMember
            {
                speciesId = "rattata",
                level = 3,
                hp = 0,
                maxHp = 0,
                exp = 0,
                moves = new List<MoveSlot>(),
            };
            TeamMembers.RecalculateOnLoad(fainted);
            CheckEqual(fainted.maxHp, 15, "Rattata nv 3 debería tener 15 PS máximos.");
            CheckEqual(fainted.hp, 0, "Los PS 0 (debilitado) deberían conservarse.");

            // Un movimiento que ya no existe se descarta y se completa con el learnset.
            var staleMove = new TeamMember
            {
                speciesId = "eevee",
                level = 6,
                hp = 24,
                maxHp = 0,
                exp = 0,
                moves = new List<MoveSlot> { new MoveSlot { moveId = "no_existe", pp = 5, maxPp = 5 } },
            };
            TeamMembers.RecalculateOnLoad(staleMove);
            Check(staleMove.moves.Count > 0 && staleMove.moves.TrueForAll(slot => slot.moveId != "no_existe"),
                "Un movimiento inexistente debería descartarse y reemplazarse por el learnset.");

            // Catálogo: cada especie, en su nivel mínimo de aparición, debe tener al menos un ataque con poder.
            foreach (string speciesId in CatalogSpecies())
            {
                int level = MinimumAppearanceLevel(speciesId);
                TeamMember member = TryCreate(speciesId, level, $"{speciesId} nv {level} (nivel mínimo)");
                if (member == null)
                    continue;

                bool hasDamagingMove = false;
                foreach (MoveSlot slot in member.moves)
                {
                    MoveData move = MoveDatabase.Get(slot.moveId);
                    if (move != null && move.power > 0)
                    {
                        hasDamagingMove = true;
                        break;
                    }
                }

                Check(hasDamagingMove,
                    $"'{speciesId}' en su nivel mínimo ({level}) no tiene ningún movimiento con poder; " +
                    "la IA salvaje se quedaría sin atacar.");
            }
        }

        static TeamMember TryCreate(string speciesId, int level, string context)
        {
            try
            {
                return TeamMembers.Create(speciesId, level);
            }
            catch (Exception e)
            {
                Check(false, $"{context}: {e.Message}");
                return null;
            }
        }

        static IEnumerable<string> CatalogSpecies()
        {
            var species = new HashSet<string>(SpeciesCatalog.Starters);
            foreach (string floor in SpeciesCatalog.FloorsWithEncounters)
                foreach (string id in SpeciesCatalog.OnFloor(floor))
                    species.Add(id);
            return species;
        }

        /// <summary>Nivel mínimo al que aparece la especie; los iniciales se validan desde el nivel 1.</summary>
        static int MinimumAppearanceLevel(string speciesId)
        {
            int min = int.MaxValue;
            foreach (string floor in SpeciesCatalog.FloorsWithEncounters)
                if (SpeciesCatalog.AppearsOn(speciesId, floor))
                    min = Math.Min(min, Building.WildLevelRange(floor).min);

            return min == int.MaxValue ? PokemonStats.MinLevel : min;
        }

        // ---------- Utilidades ----------

        static void CheckEqual(int actual, int expected, string problem) =>
            Check(actual == expected, $"{problem} (esperado {expected}, obtenido {actual})");

        static void CheckNear(float actual, float expected, float tolerance, string problem) =>
            Check(Mathf.Abs(actual - expected) <= tolerance, $"{problem} (esperado {expected}, obtenido {actual})");

        static void CheckNear(float actual, float expected, string problem) =>
            CheckNear(actual, expected, 0.0001f, problem);

        static void Check(bool ok, string problem)
        {
            if (ok)
                return;

            Debug.LogError(problem);
            checksFailed++;
        }
    }
}
