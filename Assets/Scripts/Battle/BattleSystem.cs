using System.Collections;
using System.Collections.Generic;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Orquesta un combate dentro de la escena <c>Battle</c>: lee la petición (<see cref="BattleRequest"/>),
    /// arma los equipos, conduce el bucle de turnos con <see cref="BattleEngine"/> y la <see cref="BattleUI"/>,
    /// y vuelve al mundo con el fundido de <see cref="SceneTransition"/>.
    ///
    /// Al ganar o huir se conservan los PS y los PP del equipo; al perder se restauran como estaban antes
    /// del combate (el desmayo con pérdida de créditos y el reaparecer en el lobby son de la segunda mitad).
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] BattleUI ui;

        /// <summary>Miembro del equipo y su copia de combate, para devolver PS y PP al terminar.</summary>
        class PlayerSlot
        {
            public TeamMember member;
            public BattlePokemon pokemon;
            public int startHp;
            public int[] startPp;
        }

        void Awake()
        {
            // El combate usa el mapa de acciones de la UI (Z/E/Enter y flechas).
            controls.FindActionMap("UI", true).Enable();
        }

        void Start()
        {
            if (!BattleRequest.HasPending)
            {
                Debug.LogWarning("La escena de combate se abrió sin una petición de combate; se vuelve al mundo.");
                ReturnToWorld(Building.SceneFor(GameState.Save.location));
                return;
            }

            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var slots = new List<PlayerSlot>();
            var playerTeam = new List<BattlePokemon>();
            foreach (TeamMember member in GameState.Save.team)
            {
                if (member == null)
                    continue;

                PokemonData species = PokemonDatabase.Get(member.speciesId);
                if (species == null)
                    continue;

                BattlePokemon pokemon = BattlePokemon.FromTeamMember(member, species);
                slots.Add(new PlayerSlot
                {
                    member = member,
                    pokemon = pokemon,
                    startHp = pokemon.currentHp,
                    startPp = CapturePp(member),
                });
                playerTeam.Add(pokemon);
            }

            var enemyTeam = BuildEnemyTeam();

            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
            {
                Debug.LogError("No se pudo armar el combate: falta el equipo del jugador o el rival.");
                ReturnToWorld(Building.SceneFor(GameState.Save.location));
                yield break;
            }

            var engine = new BattleEngine(playerTeam, enemyTeam, BattleRequest.Kind);
            ui.Bind(engine.PlayerActive, engine.EnemyActive);

            var intro = new List<string>();
            if (BattleRequest.Kind == BattleKind.Wild)
                intro.Add($"¡Ha aparecido un {engine.EnemyActive.DisplayName} salvaje!");
            intro.Add($"¡Adelante, {engine.PlayerActive.DisplayName}!");
            yield return ui.Messages(intro);

            while (!engine.IsOver)
            {
                BattleAction action = null;
                yield return ui.ChooseAction(engine.PlayerActive, engine.CanPlayerFlee, value => action = value);
                if (action == null)
                    continue;

                List<string> messages = engine.ResolveTurn(action);
                yield return ui.Messages(messages);
                ui.Bind(engine.PlayerActive, engine.EnemyActive);
            }

            // Al ganar o huir se conservan PS y PP; al perder se deja el equipo como estaba.
            if (engine.PlayerWon || engine.FledAway)
                WriteBackTeam(slots);
            else
                RestoreTeam(slots);

            GameState.NotifyTeamChanged();
            yield return null;

            ReturnToWorld(Building.SceneFor(GameState.Save.location));
        }

        List<BattlePokemon> BuildEnemyTeam()
        {
            var team = new List<BattlePokemon>();
            switch (BattleRequest.Kind)
            {
                case BattleKind.Wild:
                    if (!string.IsNullOrEmpty(BattleRequest.WildSpeciesId))
                        team.Add(BattlePokemon.CreateWild(BattleRequest.WildSpeciesId, BattleRequest.WildLevel));
                    break;

                case BattleKind.Trainer:
                    // Los combates de entrenador y jefe llegan en la segunda mitad de la etapa.
                    Debug.LogWarning("Combate de entrenador aún no implementado.");
                    break;
            }

            return team;
        }

        // ---------- PS y PP ----------

        static int[] CapturePp(TeamMember member)
        {
            var pp = new int[member.moves.Count];
            for (int i = 0; i < member.moves.Count; i++)
                pp[i] = member.moves[i].pp;
            return pp;
        }

        static void WriteBackTeam(List<PlayerSlot> slots)
        {
            foreach (PlayerSlot slot in slots)
            {
                slot.member.hp = slot.pokemon.currentHp;
                foreach (BattleMoveSlot battleMove in slot.pokemon.moves)
                {
                    MoveSlot memberMove = slot.member.moves.Find(move => move != null && move.moveId == battleMove.moveId);
                    if (memberMove != null)
                        memberMove.pp = battleMove.currentPp;
                }
            }
        }

        static void RestoreTeam(List<PlayerSlot> slots)
        {
            foreach (PlayerSlot slot in slots)
            {
                slot.member.hp = slot.startHp;
                for (int i = 0; i < slot.startPp.Length && i < slot.member.moves.Count; i++)
                    slot.member.moves[i].pp = slot.startPp[i];
            }
        }

        // ---------- Vuelta al mundo ----------

        /// <summary>Recuerda la casilla y vuelve al mundo con fundido. Limpia la petición de combate.</summary>
        void ReturnToWorld(string fallbackScene)
        {
            Vector3 position = BattleRequest.ReturnPosition;
            int facing = BattleRequest.ReturnFacing;
            bool hasReturn = !string.IsNullOrEmpty(BattleRequest.ReturnScene);
            string scene = hasReturn ? BattleRequest.ReturnScene : fallbackScene;
            BattleRequest.Clear();

            // Sin petición (por ejemplo, abrir la escena de combate a mano) no se fuerza casilla:
            // el jugador aparece donde lo deje el mapa.
            if (hasReturn)
                SceneTransition.SetPendingPosition(position, facing);

            if (!SceneTransition.Go(scene, null))
                Debug.LogError($"No se pudo volver a la escena '{scene}' al terminar el combate.");
        }
    }
}