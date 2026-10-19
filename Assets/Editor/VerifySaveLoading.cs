using System;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Comprueba que cargar una partida antigua no rompa nada: toma un JSON con el formato v1 (con el antiguo
    /// 'seals' numérico, sin ningún campo nuevo y con una clave desconocida), lo pasa por SaveSystem.ParseJson
    /// y verifica que conserva lo que ya existía y que lo nuevo arranca con sus valores iniciales.
    /// Menú: Torre Ingeniería > Verificar carga de partida v1 (también sirve con -executeMethod).
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

        static int checksFailed;

        [MenuItem("Torre Ingeniería/Verificar carga de partida v1")]
        public static void Verify()
        {
            checksFailed = 0;
            Debug.Log("--- Carga de partida v1 (SaveSystem) ---");

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

            if (checksFailed == 0)
                Debug.Log("Carga de partida v1: verificada (lo viejo se conserva, lo nuevo arranca por defecto y la versión migra).");
            else
                Debug.LogError($"Carga de partida v1: {checksFailed} problema(s).");
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
