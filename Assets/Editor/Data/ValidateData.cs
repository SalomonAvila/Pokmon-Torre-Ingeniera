using System;
using System.Collections.Generic;
using TorreIngenieria.Battle;
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
            ValidateTypeMessages();
            ValidateTypeBadges();
            ValidateHpBar();
            ValidateFormulas();
            ValidateSpeciesAssets();
            ValidateMoveAssets();
            ValidateItemAssets();
            ValidateTrainerAssets();
            ValidateTeamMembers();
            ValidateExperience();
            ValidateBattleEngine();
            ValidateCapture();

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

        // ---------- Mensajes y colores de la interfaz ----------

        static void ValidateTypeMessages()
        {
            Check(TypeChart.Describe(1f) == string.Empty,
                "La efectividad normal no debería mostrar ningún mensaje.");
            Check(TypeChart.Describe(2f) == "¡Es muy eficaz!",
                "La efectividad doble debería decir '¡Es muy eficaz!'.");
            Check(TypeChart.Describe(0.5f) == "No es muy eficaz...",
                "La media efectividad debería decir 'No es muy eficaz...'.");
            Check(TypeChart.Describe(0f) == "No afecta...",
                "La inmunidad debería decir 'No afecta...'.");
        }

        static void ValidateTypeBadges()
        {
            Check(PokemonTypeNames.BadgeName(PokemonType.Electric) == "ELECTRICO",
                "La etiqueta de Eléctrico debería evitar la vocal acentuada.");
            Check(PokemonTypeNames.BadgeName(PokemonType.Psychic) == "PSIQUICO",
                "La etiqueta de Psíquico debería evitar la vocal acentuada.");
            Check(PokemonTypeNames.BadgeName(PokemonType.Dragon) == "DRAGON",
                "La etiqueta de Dragón debería evitar la vocal acentuada.");

            // Los fondos oscuros necesitan texto blanco para que la etiqueta se lea.
            Check(PokemonTypeColors.BadgeText(PokemonType.Ghost) == "#FFFFFF",
                "Fantasma debería usar texto blanco en la etiqueta.");
            Check(PokemonTypeColors.BadgeText(PokemonType.Dragon) == "#FFFFFF",
                "Dragón debería usar texto blanco en la etiqueta.");
            Check(PokemonTypeColors.BadgeText(PokemonType.Dark) == "#FFFFFF",
                "Siniestro debería usar texto blanco en la etiqueta.");
            Check(PokemonTypeColors.BadgeText(PokemonType.Water) == "#101010",
                "Agua debería usar texto oscuro en la etiqueta.");
        }

        static void ValidateHpBar()
        {
            Check(BattleHUD.HpColor(1f) == BattleHUD.HpGreen,
                "La barra de PS llena debería ser verde.");
            Check(BattleHUD.HpColor(0.51f) == BattleHUD.HpGreen,
                "Con más del 50 % de PS la barra debería seguir verde.");
            Check(BattleHUD.HpColor(0.5f) == BattleHUD.HpYellow,
                "Con exactamente el 50 % de PS la barra debería ser amarilla.");
            Check(BattleHUD.HpColor(0.4f) == BattleHUD.HpYellow,
                "8/20 PS (40 %) debería pintar la barra de amarillo.");
            Check(BattleHUD.HpColor(0.2f) == BattleHUD.HpYellow,
                "Con exactamente el 20 % de PS la barra debería ser amarilla.");
            Check(BattleHUD.HpColor(0.19f) == BattleHUD.HpRed,
                "Por debajo del 20 % de PS la barra debería ser roja.");
            Check(BattleHUD.HpColor(0f) == BattleHUD.HpRed,
                "Sin PS la barra debería ser roja.");
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
            ValidateBossTrainer();
            ValidateRegularTrainer("kiosco", "1");
            ValidateRegularTrainer("estudiante_concretos", "3");
            ValidateRegularTrainer("monitora_suelos", "3");
        }

        static void ValidateBossTrainer()
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

        /// <summary>
        /// Entrenador normal: su equipo debe caer dentro del rango salvaje de su zona + 0 a +2 niveles
        /// (Doc §8). Los jefes usan otra regla (+4) y se validan aparte.
        /// </summary>
        static void ValidateRegularTrainer(string id, string floorId)
        {
            TrainerData trainer = TrainerDatabase.Get(id);
            Check(trainer != null, $"Falta el asset del entrenador '{id}'. Ejecuta Datos > Construir datos del prototipo.");
            if (trainer == null)
                return;

            Check(!trainer.isBoss, $"'{id}' no debería estar marcado como jefe.");
            Check(trainer.floorId == floorId, $"'{id}' debería ser del piso {floorId} y dice '{trainer.floorId}'.");
            Check(trainer.team.Count > 0, $"'{id}' no tiene equipo.");

            (int min, int max) = Building.WildLevelRange(floorId);
            foreach (TrainerPokemon monster in trainer.team)
            {
                Check(PokemonDatabase.Exists(monster.speciesId), $"'{id}' usa '{monster.speciesId}', que no existe.");
                Check(monster.level >= min && monster.level <= max + 2,
                    $"'{id}' tiene a {monster.speciesId} en nivel {monster.level}; " +
                    $"la zona {floorId} permite entre {min} y {max + 2} (+2 sobre el salvaje).");
                foreach (string move in monster.moveIds)
                    Check(MoveDatabase.Exists(move), $"'{id}' usa el movimiento '{move}', que no existe.");
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

        // ---------- Experiencia, niveles y obediencia (Etapa 3) ----------

        static void ValidateExperience()
        {
            // Doc §7: EXP base × nivel del rival / 7, sin bonus de entrenador.
            CheckEqual(GameRules.ExpReward(64, 5), 45, "Bulbasaur nv 5 debería dar 45 de EXP.");
            CheckEqual(GameRules.ExpReward(64, 10), 91, "Bulbasaur nv 10 debería dar 91 de EXP.");
            CheckEqual(GameRules.ExpReward(45, 5), 32, "Una base 45 a nivel 5 debería dar 32 de EXP.");

            // Un nivel: 125 + 91 = 216 (nivel 6). Los PS actuales suben la diferencia del máximo.
            TeamMember oneLevel = TeamMembers.Create("bulbasaur", 5);
            oneLevel.hp = 10;
            ExpGainResult one = TeamMembers.GainExp(oneLevel, 91);
            CheckEqual(one.OldLevel, 5, "La EXP debe partir del nivel 5.");
            CheckEqual(one.NewLevel, 6, "Con 216 de EXP debería llegar al nivel 6.");
            CheckEqual(one.LevelUps.Count, 1, "Debería registrarse un solo nivel.");
            CheckEqual(oneLevel.exp, 216, "La EXP acumulada debería ser 216.");
            CheckEqual(oneLevel.maxHp, 23, "Bulbasaur nv 6 debería tener 23 PS máximos.");
            CheckEqual(oneLevel.hp, 12, "Los PS actuales deberían subir de 10 a 12 (diferencia de máximos).");
            CheckEqual(one.LevelUps[0].HpGained, 2, "El nivel 6 debería aportar 2 PS máximos.");
            Check(one.LevelUps[0].LearnedMoves.Count == 0,
                "Bulbasaur no aprende movimientos al nivel 6.");

            // Varios niveles en una sola entrega: 125 + 218 = 343 (niveles 6 y 7), con Látigo Cepa.
            TeamMember multi = TeamMembers.Create("bulbasaur", 5);
            multi.hp = 10;
            ExpGainResult several = TeamMembers.GainExp(multi, 218);
            CheckEqual(several.NewLevel, 7, "Con 343 de EXP debería llegar al nivel 7.");
            CheckEqual(several.LevelUps.Count, 2, "Deberían registrarse dos niveles.");
            CheckEqual(multi.exp, 343, "La EXP acumulada debería ser 343.");
            CheckEqual(multi.maxHp, 25, "Bulbasaur nv 7 debería tener 25 PS máximos.");
            CheckEqual(multi.hp, 14, "Los PS actuales deberían subir de 10 a 14.");
            Check(multi.moves.Exists(slot => slot.moveId == "vine_whip"),
                "Bulbasaur debería aprender Látigo Cepa al nivel 7.");
            Check(several.LevelUps[1].LearnedMoves.Contains("vine_whip"),
                "El segundo nivel debería informar Látigo Cepa como aprendido.");

            // Con cuatro movimientos no se aprende el nuevo: se informa y no se reemplaza nada.
            var fullMoves = new TeamMember
            {
                speciesId = "bulbasaur",
                level = 5,
                hp = 21,
                maxHp = 21,
                exp = 125,
                moves = new List<MoveSlot>
                {
                    new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 },
                    new MoveSlot { moveId = "growl", pp = 40, maxPp = 40 },
                    new MoveSlot { moveId = "razor_leaf", pp = 25, maxPp = 25 },
                    new MoveSlot { moveId = "body_slam", pp = 15, maxPp = 15 },
                },
            };
            ExpGainResult full = TeamMembers.GainExp(fullMoves, 218);
            CheckEqual(fullMoves.moves.Count, 4, "El equipo no debería pasar de cuatro movimientos.");
            Check(!fullMoves.moves.Exists(slot => slot.moveId == "vine_whip"),
                "Con cuatro movimientos no debería aprenderse Látigo Cepa.");
            Check(full.LevelUps.Exists(step => step.MissedMoves.Contains("vine_whip")),
                "El aviso debería indicar que Látigo Cepa no se pudo aprender.");

            // Cafeína reutiliza la subida: fija la EXP mínima del siguiente nivel.
            TeamMember candy = TeamMembers.Create("bulbasaur", 5);
            ExpGainResult candyResult = TeamMembers.LevelUp(candy);
            CheckEqual(candy.level, 6, "La Cafeína debería subir un nivel.");
            CheckEqual(candy.exp, 216, "La Cafeína debería dejar la EXP mínima del nivel 6.");
            CheckEqual(candyResult.LevelUps.Count, 1, "La Cafeína debería informar un nivel.");

            // Tope 100: no se sube más y un debilitado no revive.
            var capped = new TeamMember
            {
                speciesId = "bulbasaur",
                level = 99,
                hp = 0,
                maxHp = 0,
                exp = PokemonStats.ExperienceForLevel(99),
                moves = new List<MoveSlot> { new MoveSlot { moveId = "tackle", pp = 35, maxPp = 35 } },
            };
            ExpGainResult cap = TeamMembers.GainExp(capped, 100000);
            CheckEqual(capped.level, 100, "La EXP no debería pasar del nivel 100.");
            CheckEqual(capped.exp, PokemonStats.ExperienceForLevel(100), "La EXP debería topar en la del nivel 100.");
            CheckEqual(capped.hp, 0, "Un Pokémon debilitado no debería revivir al subir de nivel.");
            CheckEqual(cap.ExpGained, PokemonStats.ExperienceForLevel(100) - PokemonStats.ExperienceForLevel(99),
                "La EXP ganada debería recortarse al tope del nivel 100.");

            // Barra de EXP: 0 al inicio del nivel, 1 al máximo.
            CheckNear(BattleHUD.ExpRatio(125, 5), 0f, "La barra debería empezar vacía en el nivel 5.");
            CheckNear(BattleHUD.ExpRatio(216, 6), 0f, "La barra debería reiniciarse al subir al nivel 6.");
            CheckNear(BattleHUD.ExpRatio(170, 5), 45f / 91f, "La barra debería reflejar la EXP del tramo.");
            CheckNear(BattleHUD.ExpRatio(PokemonStats.ExperienceForLevel(100), 100), 1f,
                "En el nivel 100 la barra debería estar llena.");

            // Obediencia (Doc §7): tope 10 + 5 × sellos y 30 % de desobedecer por encima.
            CheckEqual(GameRules.ObedienceLevelCap(0), 10, "Sin sellos el tope debería ser 10.");
            CheckEqual(GameRules.ObedienceLevelCap(1), 15, "Con un sello el tope debería ser 15.");
            CheckEqual(GameRules.ObedienceLevelCap(3), 25, "Con tres sellos el tope debería ser 25.");
            Check(!GameRules.Disobeys(10, 0, 0f), "En el tope de obediencia nunca debería desobedecer.");
            Check(GameRules.Disobeys(11, 0, 0.29f), "Por encima del tope debería desobedecer con tirada baja.");
            Check(!GameRules.Disobeys(11, 0, GameRules.DisobedienceChance),
                "La tirada igual al 30 % no debería desobedecer.");
            Check(!GameRules.Disobeys(15, 1, 0.29f), "Con un sello, nivel 15 todavía debería obedecer.");
            Check(GameRules.Disobeys(16, 1, 0.29f), "Con un sello, nivel 16 debería poder desobedecer.");

            // Integración en el motor: la tirada inyectada evita depender del azar.
            var disobedienceEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("bulbasaur", 11) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 2) },
                BattleKind.Wild)
            {
                ObedienceSeals = 0,
                ObedienceRoll = 0f,
            };
            List<BattleMessage> disobedienceMessages = disobedienceEngine.ResolveTurn(BattleAction.Move(0));
            Check(disobedienceMessages.Exists(message => message.text.Contains("no obedece")),
                "Con la tirada forzada, el motor debería avisar que el Pokémon no obedece.");
        }

        // ---------- Motor de combate (Etapa 3) ----------

        static void ValidateBattleEngine()
        {
            // Estado de un salvaje: PS, tipos y learnset.
            BattlePokemon wild = BattlePokemon.CreateWild("bulbasaur", 5);
            CheckEqual(wild.maxHp, 21, "El salvaje Bulbasaur nv 5 debería tener 21 PS.");
            CheckEqual(wild.currentHp, 21, "El salvaje Bulbasaur nv 5 debería empezar con los PS llenos.");
            Check(wild.HasType(PokemonType.Grass) && wild.HasType(PokemonType.Poison),
                "El salvaje Bulbasaur debería ser Planta/Veneno.");
            CheckEqual(wild.moves.Count, 2, "El salvaje Bulbasaur nv 5 debería conocer 2 movimientos.");

            // Cambios de estadística de combate (Gen 3).
            CheckEqual(BattlePokemon.ModifiedStat(100, 0), 100, "Sin cambios, la estadística debería ser igual.");
            CheckEqual(BattlePokemon.ModifiedStat(100, 2), 200, "+2 debería doblar la estadística.");
            CheckEqual(BattlePokemon.ModifiedStat(100, -2), 50, "−2 debería dejar la mitad de la estadística.");

            // Huida y precisión (propuestas en GameRules).
            CheckNear(GameRules.FleeChance(50, 50), 0.5f, "Con la misma Velocidad se debería huir el 50 %.");
            CheckNear(GameRules.FleeChance(200, 50), GameRules.FleeMaxChance, "Huir rápido debería topar en el máximo.");
            CheckNear(GameRules.FleeChance(10, 200), GameRules.FleeMinChance, "Huir lento debería topar en el mínimo.");
            CheckNear(GameRules.AccuracyStageMultiplier(0), 1f, "Sin cambios, la precisión debería ser la base.");
            CheckNear(GameRules.AccuracyStageMultiplier(-1), 0.75f, "−1 de precisión debería ser 0,75.");

            // Un combate con ventaja clara termina y lo gana el jugador (la variación aleatoria no cambia el final).
            var playerTeam = new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 20) };
            var enemyTeam = new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 2) };
            var engine = new BattleEngine(playerTeam, enemyTeam, BattleKind.Wild);
            int guard = 0;
            while (!engine.IsOver && guard++ < 500)
                engine.ResolveTurn(BattleAction.Move(0));
            Check(engine.IsOver, "Un combate con ventaja clara debería terminar antes de 500 turnos.");
            Check(engine.PlayerWon, "Charmander nv 20 debería ganar a un Rattata nv 2.");

            // Un movimiento de estado (Gruñido, poder 0) no debe pasar por la tabla de efectividad.
            var statusEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("bulbasaur", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 5) },
                BattleKind.Wild);
            List<BattleMessage> statusMessages = statusEngine.ResolveTurn(BattleAction.Move(1));
            Check(statusMessages.Exists(message => message.text == "¡Bulbasaur usó Gruñido!"),
                "El turno de prueba debería usar Gruñido.");
            Check(!statusMessages.Exists(message =>
                    message.text.Contains("eficaz") || message.text.Contains("No afecta")),
                "Un movimiento de estado no debería mostrar mensajes de efectividad.");

            // FromTeamMember conserva PS actuales y PP.
            TeamMember member = TeamMembers.Create("bulbasaur", 5);
            member.hp = 10;
            member.moves[0].pp = 5;
            BattlePokemon fromMember = BattlePokemon.FromTeamMember(member, PokemonDatabase.Get("bulbasaur"));
            CheckEqual(fromMember.currentHp, 10, "FromTeamMember debería respetar los PS actuales.");
            CheckEqual(fromMember.moves[0].currentPp, 5, "FromTeamMember debería respetar los PP actuales.");
        }

        static void ValidateCapture()
        {
            // Bulbasaur nv 5 tiene 21 PS y catchRate 45 (escala oficial 1–255). La fórmula normaliza
            // entre 255 y recorta a los límites del documento. Los bonus por estado son (propuesta).
            CheckNear(GameRules.CaptureChance(21, 1, 45, 1f, 1f), 0.1709f, 0.001f,
                "Captura de Bulbasaur a 1 PS sin estado debería ser ~17,1 %.");
            CheckNear(GameRules.CaptureChance(21, 1, 45, 1f, 2f), 0.3417f, 0.001f,
                "El sueño o la congelación deberían duplicar la captura.");
            CheckNear(GameRules.CaptureChance(21, 1, 45, 1f, 1.5f), 0.2563f, 0.001f,
                "Veneno, quemadura o parálisis deberían subir la captura un 50 %.");
            CheckNear(GameRules.CaptureChance(21, 21, 45, 1f, 1f), 0.0588f, 0.001f,
                "A PS completos la captura debería ser menor que a 1 PS.");
            CheckNear(GameRules.CaptureChance(21, 1, 1, 1f, 1f), GameRules.CaptureChanceMin, 0.0001f,
                "La captura debería respetar el mínimo.");
            CheckNear(GameRules.CaptureChance(21, 1, 255, 1f, 2f), GameRules.CaptureChanceMax, 0.0001f,
                "La captura debería respetar el máximo.");

            // Las sacudidas son presentación y no vuelven a tirar: 3 en una captura y 0–2 en un escape.
            CheckEqual(GameRules.CaptureShakes(0.05f, false), 0,
                "Un escape al 5 % no debería mostrar sacudidas.");
            CheckEqual(GameRules.CaptureShakes(0.95f, false), 2,
                "Un escape al 95 % debería mostrar como máximo dos sacudidas.");
            CheckEqual(GameRules.CaptureShakes(0.50f, false), 1,
                "Un escape al 50 % debería mostrar una sacudida.");
            CheckEqual(GameRules.CaptureShakes(0.05f, true), 3,
                "Una captura debería mostrar siempre tres sacudidas.");

            // La captura solo existe contra salvajes (Doc §7): en un combate de entrenador se rechaza
            // y no gasta el turno.
            var trainerEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 5) },
                BattleKind.Trainer);
            trainerEngine.ResolveTurn(BattleAction.Item("pokeball"));
            Check(!trainerEngine.Captured, "No se debería capturar en un combate de entrenador.");
            Check(!trainerEngine.ItemConsumed, "La Pokébola no debería gastarse en un combate de entrenador.");
            Check(!trainerEngine.CaptureAttempted,
                "En un combate de entrenador ni siquiera debería intentarse la captura.");
            CheckEqual(trainerEngine.CaptureShakes, 0,
                "Un combate de entrenador no debería mostrar sacudidas.");

            // La tirada se inyecta para que las pruebas no dependan del azar. A 1 PS, Rattata (catchRate
            // 255) llega al 95 % y la captura sale con tres sacudidas y el «¡Ya!».
            var captureEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 5) },
                BattleKind.Wild);
            captureEngine.EnemyActive.TakeDamage(captureEngine.EnemyActive.maxHp - 1);
            List<BattleMessage> captureMessages =
                captureEngine.ResolveTurn(BattleAction.Item("pokeball"), captureRoll: 0f);
            Check(captureEngine.Captured, "Con una tirada de 0 la captura debería salir.");
            Check(captureEngine.IsOver, "Una captura exitosa debería cerrar el combate.");
            Check(captureEngine.CaptureAttempted, "La captura debería quedar marcada como intentada.");
            Check(captureEngine.ItemConsumed, "La Pokébola debería gastarse también al capturar.");
            CheckEqual(captureEngine.CaptureShakes, 3,
                "Una captura debería mostrar siempre tres sacudidas.");
            CheckEqual(captureMessages.FindAll(message => message.autoAdvance).Count, 3,
                "La captura debería encolar tres mensajes de sacudida automáticos.");
            Check(captureMessages.Exists(message => message.text == "¡Ya!"),
                "La captura debería terminar con «¡Ya!».");
            Check(captureMessages.Exists(message => message.text == "¡Atrapaste a Rattata!"),
                "La captura debería anunciar el Pokémon atrapado.");

            // Un fallo al 95 % deja dos sacudidas; el mensaje final es «¡Se escapó!».
            var highFailEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 5) },
                BattleKind.Wild);
            highFailEngine.EnemyActive.TakeDamage(highFailEngine.EnemyActive.maxHp - 1);
            List<BattleMessage> highFailMessages =
                highFailEngine.ResolveTurn(BattleAction.Item("pokeball"), captureRoll: 0.99f);
            Check(!highFailEngine.Captured, "Con una tirada de 0,99 la captura no debería salir.");
            Check(highFailEngine.CaptureAttempted, "El fallo debería quedar marcado como intento.");
            CheckEqual(highFailEngine.CaptureShakes, 2,
                "Un fallo al 95 % debería dejar dos sacudidas.");
            CheckEqual(highFailMessages.FindAll(message => message.autoAdvance).Count, 2,
                "El fallo al 95 % debería encolar dos sacudidas automáticas.");
            Check(highFailMessages.Exists(message => message.text == "¡Se escapó!"),
                "El fallo debería terminar con «¡Se escapó!».");

            // A PS completos Bulbasaur cae al 5,88 %, así que un fallo no muestra sacudidas.
            var lowFailEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("bulbasaur", 5) },
                BattleKind.Wild);
            List<BattleMessage> lowFailMessages =
                lowFailEngine.ResolveTurn(BattleAction.Item("pokeball"), captureRoll: 0.99f);
            Check(!lowFailEngine.Captured, "A PS completos la captura de Bulbasaur no debería salir.");
            CheckEqual(lowFailEngine.CaptureShakes, 0,
                "Un fallo al 5,88 % no debería mostrar sacudidas.");
            CheckEqual(lowFailMessages.FindAll(message => message.autoAdvance).Count, 0,
                "Un fallo con pocas probabilidades no debería encolar sacudidas.");
            Check(lowFailMessages.Exists(message => message.text == "¡Se escapó!"),
                "El fallo sin sacudidas debería anunciar «¡Se escapó!».");

            // La Pokébola se gasta haya o no captura; el curativo solo si tenía efecto.
            var ballEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("charmander", 20) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 2) },
                BattleKind.Wild);
            ballEngine.ResolveTurn(BattleAction.Item("pokeball"), captureRoll: 0.99f);
            Check(ballEngine.ItemConsumed, "La Pokébola debería gastarse aunque la captura falle.");
            Check(!ballEngine.Captured, "Con una tirada de 0,99 no debería capturarse.");

            // El capturado conserva los PS y los PP actuales, y la EXP es solo la del nivel (sin bonus).
            var wild = BattlePokemon.CreateWild("bulbasaur", 5);
            wild.TakeDamage(7);
            wild.moves[0].currentPp = 3;
            var wildPp = new int[wild.moves.Count];
            for (int i = 0; i < wild.moves.Count; i++)
                wildPp[i] = wild.moves[i].currentPp;

            TeamMember captured = TeamMembers.CreateCaptured(wild.speciesId, wild.level,
                wild.currentHp, wildPp);
            CheckEqual(captured.hp, wild.currentHp,
                "El capturado debería conservar los PS actuales del salvaje.");
            CheckEqual(captured.maxHp, wild.maxHp,
                "El capturado debería conservar los PS máximos del salvaje.");
            CheckEqual(captured.moves[0].pp, 3,
                "El capturado debería conservar los PP actuales del salvaje.");
            CheckEqual(captured.exp, PokemonStats.ExperienceForLevel(wild.level),
                "La captura no debería dar EXP adicional.");

            // PS y PP fuera de rango se recortan; los PS 0 nunca se guardan.
            TeamMember clamped = TeamMembers.CreateCaptured("bulbasaur", 5, 999, new[] { 999 });
            CheckEqual(clamped.hp, clamped.maxHp,
                "Los PS por encima del máximo deberían recortarse.");
            CheckEqual(clamped.moves[0].pp, clamped.moves[0].maxPp,
                "Los PP por encima del máximo deberían recortarse.");
            TeamMember noZero = TeamMembers.CreateCaptured("bulbasaur", 5, 0, new[] { -1 });
            CheckEqual(noZero.hp, noZero.maxHp,
                "Un capturado no debería guardarse con 0 PS.");
            CheckEqual(noZero.moves[0].pp, 0,
                "Los PP negativos deberían recortarse a 0.");
            TeamMember defaults = TeamMembers.CreateCaptured("bulbasaur", 5);
            CheckEqual(defaults.hp, defaults.maxHp,
                "Sin PS actuales, el capturado debería quedar con los PS llenos, no con el centinela.");
            CheckEqual(defaults.moves[0].pp, defaults.moves[0].maxPp,
                "Sin PP actuales, el capturado debería quedar con los PP máximos.");

            var healEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("bulbasaur", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 2) },
                BattleKind.Wild);
            healEngine.PlayerActive.TakeDamage(10);
            int hpBefore = healEngine.PlayerActive.currentHp;
            healEngine.ResolveTurn(BattleAction.Item("tinto"));
            Check(healEngine.ItemConsumed, "El Tinto debería gastar el turno.");
            Check(healEngine.PlayerActive.currentHp > hpBefore, "El Tinto debería recuperar PS.");

            var fullHpEngine = new BattleEngine(
                new List<BattlePokemon> { BattlePokemon.CreateWild("bulbasaur", 5) },
                new List<BattlePokemon> { BattlePokemon.CreateWild("rattata", 2) },
                BattleKind.Wild);
            fullHpEngine.ResolveTurn(BattleAction.Item("tinto"));
            Check(!fullHpEngine.ItemConsumed, "Un curativo a PS completos no debería gastar el turno.");
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
