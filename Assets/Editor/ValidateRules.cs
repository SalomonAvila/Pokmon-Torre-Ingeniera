using System;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Comprueba, piso por piso, que Building y GameRules reproduzcan las tablas del documento
    /// (Doc §2, §7 y §8): posición en la secuencia, esquina de entrada, esquina que custodia el jefe,
    /// rango de niveles salvajes y tamaño del equipo del jefe. Como el proyecto no tiene suite de tests,
    /// esta es la verificación ejecutable de esas reglas.
    /// Menú: Torre Ingeniería > Validar reglas del documento.
    /// </summary>
    public static class ValidateRules
    {
        /// <summary>Pisos con reto en el orden del documento; su índice es la k esperada.</summary>
        static readonly string[] ChallengeFloors = { "3", "4", "5", "6", "7", "PT", "8", "9", "10", "11", "12", "13", "14" };

        /// <summary>Pisos en los que se entra por la escalera del NO (los demás pisos con reto, por la del SE).</summary>
        static readonly string[] EnterByNorthWest = { "4", "6", "PT", "9", "11", "13" };

        /// <summary>Discrepancias encontradas en la pasada actual; el validador se ejecuta de una sola pasada.</summary>
        static int checksFailed;

        [MenuItem("Torre Ingeniería/Validar reglas del documento")]
        static void Validate()
        {
            checksFailed = 0;
            Debug.Log("--- Reglas del documento (Doc §2, §7 y §8) ---");

            // Pisos 1 y 2: sin reto, sin entrada por escalera y sin jefe; su rango salvaje es el de la plazoleta.
            Check(Building.SequenceIndex("1") == -1 && Building.SequenceIndex("2") == -1, "Los pisos 1 y 2 no deben tener k.");
            Check(Building.WildLevelRange("1") == (GameRules.PlazaWildMinLevel, GameRules.PlazaWildMaxLevel),
                $"El piso 1 debe usar el rango de la plazoleta ({GameRules.PlazaWildMinLevel}-{GameRules.PlazaWildMaxLevel}).");
            Check(Throws(() => Building.BossTeamSize("1")), "El piso 1 no debe tener jefe: BossTeamSize debería lanzar excepción.");

            for (int k = 0; k < ChallengeFloors.Length; k++)
            {
                string floor = ChallengeFloors[k];
                Corner expectedEntry = Array.IndexOf(EnterByNorthWest, floor) >= 0 ? Corner.NorthWest : Corner.SouthEast;
                Corner expectedGuarded = expectedEntry == Corner.SouthEast ? Corner.NorthWest : Corner.SouthEast;

                int index = Building.SequenceIndex(floor);
                Corner entry = Building.EntryCorner(floor);
                Corner guarded = Building.GuardedCorner(floor);
                (int min, int max) wild = Building.WildLevelRange(floor);
                int team = Building.BossTeamSize(floor);
                int bossLevel = Building.BossLevel(floor);

                Debug.Log($"{Building.DisplayName(floor),-14} k={index,-2} entrada={entry,-9} custodia={guarded,-9} " +
                          $"salvajes={wild.min}-{wild.max,-2} equipo del jefe={team} nivel del jefe={bossLevel}");

                Check(index == k, $"{floor}: k debería ser {k} y es {index}.");
                Check(entry == expectedEntry, $"{floor}: se entra por {expectedEntry} y Building dice {entry}.");
                Check(guarded == expectedGuarded, $"{floor}: el jefe custodia {expectedGuarded} y Building dice {guarded}.");
                Check(wild.min == 3 + 3 * k && wild.max == 6 + 3 * k,
                    $"{floor}: los salvajes deberían ser {3 + 3 * k}-{6 + 3 * k} y Building dice {wild.min}-{wild.max}.");
                Check(team == DocumentBossTeamSize(floor),
                    $"{floor}: el equipo del jefe debería ser {DocumentBossTeamSize(floor)} y Building dice {team}.");
                Check(bossLevel == wild.max + GameRules.BossLevelBonus,
                    $"{floor}: el jefe debería ir en nivel {wild.max + GameRules.BossLevelBonus} y Building dice {bossLevel}.");
            }

            // Montoya: piso 14, 4 Pokémon y el más flojo igual al nivel de jefe que calcula la regla.
            Check(GameRules.MontoyaLevels.Length == Building.BossTeamSize("14"),
                $"Montoya debería llevar {Building.BossTeamSize("14")} Pokémon y la tabla tiene {GameRules.MontoyaLevels.Length}.");
            Check(GameRules.MontoyaLevels[0] == Building.BossLevel("14"),
                $"El primer Pokémon de Montoya debería estar en el nivel del jefe del piso 14 ({Building.BossLevel("14")}).");

            if (checksFailed == 0)
                Debug.Log($"Reglas del documento: todo coincide ({ChallengeFloors.Length} pisos con reto + plazoleta).");
            else
                Debug.LogError($"Reglas del documento: {checksFailed} discrepancia(s) por revisar.");
        }

        /// <summary>Tamaño del equipo del jefe tal como lo lista el documento, copiado a mano para comparar.</summary>
        static int DocumentBossTeamSize(string floor) => floor switch
        {
            "3" => 2,
            "4" or "5" or "6" or "7" or "PT" => 3,
            _ => 4,
        };

        static bool Throws(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (InvalidOperationException)
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
