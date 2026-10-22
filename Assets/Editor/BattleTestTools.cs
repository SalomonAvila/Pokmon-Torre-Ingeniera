using TorreIngenieria.Battle;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Herramientas para probar el combate (Etapa 3) mientras no existe la selección del inicial ni la
    /// captura: dan un equipo de prueba y arrancan un encuentro salvaje desde el mapa abierto.
    /// El equipo se guarda en la partida, así que sobrevive a la vuelta del combate.
    /// </summary>
    public static class BattleTestTools
    {
        [MenuItem("Torre Ingeniería/Combate/Preparar equipo de prueba")]
        static void PrepareTeam()
        {
            if (!EditorApplication.isPlaying)
                GameState.Save = null; // fuera de Play, relee la partida del archivo

            if (GameState.TeamCount == 0)
                GameState.TryAddToTeam(TeamMembers.Create("bulbasaur", 5));
            if (GameState.TeamCount < 2)
                GameState.TryAddToTeam(TeamMembers.Create("charmander", 5));

            Debug.Log($"Equipo de prueba listo: {GameState.TeamCount} Pokémon " +
                      $"({string.Join(", ", GameState.Save.team.ConvertAll(member => member.speciesId))}).");
        }

        [MenuItem("Torre Ingeniería/Combate/Iniciar combate salvaje (prueba)")]
        static void StartWildBattle()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("No hay jugador en la escena; abre un mapa y entra en Play.");
                return;
            }
            if (GameState.TeamCount == 0)
            {
                Debug.LogWarning("El equipo está vacío: usa Combate > Preparar equipo de prueba.");
                return;
            }

            string floor = GameState.CurrentFloor;
            var species = SpeciesCatalog.OnFloor(floor);
            if (species.Count == 0)
                species = SpeciesCatalog.OnFloor("1");

            (int min, int max) = Building.WildLevelRange(floor);
            string chosen = species[Random.Range(0, species.Count)];
            int level = Random.Range(min, max + 1);

            BattleRequest.StartWild(chosen, level, SceneManager.GetActiveScene().name,
                player.transform.position, (int)player.Facing);
            SceneTransition.Go(SceneNames.Battle, null);
        }

        [MenuItem("Torre Ingeniería/Combate/Iniciar combate salvaje (prueba)", true)]
        static bool CanStartWildBattle() => EditorApplication.isPlaying;
    }
}