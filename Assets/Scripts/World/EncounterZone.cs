using System.Collections.Generic;
using TorreIngenieria.Battle;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Zonas de encuentro de un piso: al pisar una casilla con tile en la capa <c>Encuentros</c> hay una
    /// probabilidad (10 %, <see cref="GameRules.EncounterChance"/>) de que salga un Pokémon salvaje.
    /// Los pasillos y el lobby no tienen tiles de encuentro, así que son seguros (Doc §7).
    ///
    /// La especie sale de <see cref="SpeciesCatalog"/> y el nivel de <see cref="Building.WildLevelRange"/>.
    /// Empieza el combate con <see cref="BattleRequest"/> y carga la escena <see cref="SceneNames.Battle"/>.
    /// </summary>
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField, Tooltip("Probabilidad por casilla; negativo = la del documento (10 %).")]
        float encounterChance = -1f;

        Tilemap encounters;
        PlayerController player;
        bool busy;

        void Awake()
        {
            GameObject layer = GameObject.Find("Grid/Encuentros");
            encounters = layer != null ? layer.GetComponent<Tilemap>() : null;
            player = FindAnyObjectByType<PlayerController>();
        }

        void OnEnable() => PlayerController.Stepped += OnStepped;
        void OnDisable() => PlayerController.Stepped -= OnStepped;

        void OnStepped(Vector3 position)
        {
            if (busy || encounters == null || player == null || SceneTransition.IsTransitioning)
                return;

            // Una casilla de cambio de escena manda: no se pisa para sacar un combate encima.
            if (WarpZone.At(position) != null)
                return;

            Vector3Int cell = encounters.WorldToCell(position);
            if (!encounters.HasTile(cell))
                return;

            if (GameState.TeamCount == 0)
                return; // sin equipo no hay combate posible

            // Si todo el equipo está debilitado no se puede combatir (hp < 0 es el centinela "lleno").
            if (!GameState.Save.team.Exists(member => member != null && (member.hp < 0 || member.hp > 0)))
                return;

            float chance = encounterChance < 0f ? GameRules.EncounterChance : encounterChance;
            if (Random.value >= chance)
                return;

            string floor = GameState.CurrentFloor;
            IReadOnlyList<string> species = SpeciesCatalog.OnFloor(floor);
            if (species.Count == 0)
                return;

            string chosen = species[Random.Range(0, species.Count)];
            (int min, int max) = Building.WildLevelRange(floor);
            int level = Random.Range(min, max + 1);

            StartBattle(chosen, level);
        }

        void StartBattle(string speciesId, int level)
        {
            busy = true;
            BattleRequest.StartWild(speciesId, level, SceneManager.GetActiveScene().name,
                player.transform.position, (int)player.Facing);

            if (!SceneTransition.Go(SceneNames.Battle, null))
            {
                BattleRequest.Clear();
                busy = false;
            }
        }
    }
}