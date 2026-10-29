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
    /// Al ganar, capturar o huir se conservan los PS y los PP del equipo; al perder se restauran como estaban
    /// antes del combate. Los objetos gastados no se devuelven. El desmayo con pérdida de créditos y el
    /// reaparecer en el lobby son de la segunda mitad.
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

            var engine = new BattleEngine(playerTeam, enemyTeam, BattleRequest.Kind)
            {
                ObedienceSeals = GameState.SealCount,
            };
            ui.Bind(engine.PlayerActive, engine.EnemyActive, SlotFor(slots, engine.PlayerActive)?.member);

            var intro = new List<string>();
            if (BattleRequest.Kind == BattleKind.Wild)
                intro.Add($"¡Ha aparecido un {engine.EnemyActive.DisplayName} salvaje!");
            if (BattleRequest.IsTutorial)
                intro.Add("¡Gabriel quiere comprobar lo que aprendiste!");
            intro.Add($"¡Adelante, {engine.PlayerActive.DisplayName}!");
            yield return ui.Messages(intro);

            while (!engine.IsOver)
            {
                BattleAction action = null;
                yield return ui.ChooseAction(engine.PlayerTeam, engine.PlayerIndex, engine.CanPlayerFlee,
                    value => action = value);
                if (action == null)
                    continue;

                List<BattleMessage> messages = engine.ResolveTurn(action);
                if (action.kind == BattleActionKind.Item && engine.ItemConsumed)
                    GameState.AddItem(action.itemId, -1);

                // En un cambio se muestran primero «¡Vuelve…!» y «¡Adelante…!», se refrescan las fichas
                // y después sigue el turno: así el ataque del rival se ve sobre el Pokémon que entró.
                int switchMessages = action.kind == BattleActionKind.Switch ? engine.SwitchMessageCount : 0;
                if (switchMessages > 0 && switchMessages < messages.Count)
                {
                    yield return ui.Messages(messages.GetRange(0, switchMessages));
                    ui.Bind(engine.PlayerActive, engine.EnemyActive, SlotFor(slots, engine.PlayerActive)?.member);
                    yield return ui.Messages(messages.GetRange(switchMessages, messages.Count - switchMessages));
                }
                else
                {
                    yield return ui.Messages(messages);
                }

                ui.Bind(engine.PlayerActive, engine.EnemyActive, SlotFor(slots, engine.PlayerActive)?.member);
            }

            // La captura cuenta para el Pokédex y se suma al equipo solo si hay sitio (no hay PC en el
            // prototipo). No da EXP ni cuenta como victoria.
            if (engine.Captured)
            {
                bool kept = GameState.CanAddToTeam;
                var pp = new int[engine.EnemyActive.moves.Count];
                for (int i = 0; i < engine.EnemyActive.moves.Count; i++)
                    pp[i] = engine.EnemyActive.moves[i].currentPp;

                GameState.AddCapturedPokemon(engine.CapturedSpeciesId, engine.CapturedLevel,
                    engine.EnemyActive.currentHp, pp);

                yield return ui.Messages(new[]
                {
                    kept
                        ? "Se añadió a tu equipo."
                        : $"{engine.EnemyActive.DisplayName} quedó registrado en el Pokédex, " +
                          "pero no se pudo conservar: el equipo está lleno.",
                });
            }

            // Al ganar, capturar o huir se conservan PS y PP; al perder se deja el equipo como estaba.
            // Los objetos gastados no se devuelven en ningún caso.
            if (engine.PlayerWon || engine.FledAway || engine.Captured)
                WriteBackTeam(slots);
            else
                RestoreTeam(slots);

            // Doc §7: al ganar, el Pokémon activo recibe base × nivel del rival / 7. La captura y la
            // huida no dan EXP. El prototipo no reparte EXP entre los que participaron antes de un cambio:
            // la recibe el que quedó activo al terminar. El tutorial de Gabriel no da EXP (Doc §8).
            if (engine.PlayerWon && !BattleRequest.IsTutorial)
                yield return AwardExperience(engine, slots);

            // El tutorial abre la escalera del lobby en cuanto se gana (GameState.TutorialDone).
            if (BattleRequest.IsTutorial && engine.PlayerWon)
            {
                GameState.CompleteTutorial();
                yield return ui.Messages(new[] { "¡Superaste la prueba de Gabriel!" });
            }

            GameState.NotifyTeamChanged();
            yield return null;

            ReturnToWorld(Building.SceneFor(GameState.Save.location));
        }

        IEnumerator AwardExperience(BattleEngine engine, List<PlayerSlot> slots)
        {
            PlayerSlot winner = SlotFor(slots, engine.PlayerActive);
            if (winner == null)
                yield break;

            PokemonData defeated = PokemonDatabase.Get(engine.EnemyActive.speciesId);
            if (defeated == null)
                yield break;

            int exp = GameRules.ExpReward(defeated.baseExp, engine.EnemyActive.level);
            if (exp <= 0)
                yield break;

            ExpGainResult result = TeamMembers.GainExp(winner.member, exp);
            if (result.ExpGained <= 0)
                yield break;

            // El HUD usa la copia de combate para PS y nivel. La barra de EXP se anima desde los
            // valores viejos y, al terminar, se refresca la ficha con el nivel y los PS nuevos.
            winner.pokemon.level = winner.member.level;
            winner.pokemon.maxHp = winner.member.maxHp;
            winner.pokemon.currentHp = winner.member.hp;
            yield return ui.ShowExpGain(winner.member, result);
            ui.Bind(engine.PlayerActive, engine.EnemyActive, winner.member);
        }

        static PlayerSlot SlotFor(List<PlayerSlot> slots, BattlePokemon pokemon) =>
            slots.Find(slot => ReferenceEquals(slot.pokemon, pokemon));

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
                    if (BattleRequest.IsTutorial)
                        team.AddRange(BuildTutorialTeam());
                    else
                        // Los combates de entrenador y jefe llegan en la segunda mitad de la etapa.
                        Debug.LogWarning("Combate de entrenador aún no implementado.");
                    break;
            }

            return team;
        }

        /// <summary>
        /// Rival del tutorial (Doc §8): el inicial con ventaja de tipo sobre el del jugador (el que gana
        /// a su tipo), a nivel 5 (4 si hubiera dicho "Puedo solo"), con el learnset de ese nivel. A nivel 5
        /// los iniciales todavía no tienen su movimiento de tipo, así que el combate es una prueba de
        /// equipo (el jugador ya trae al menos una captura) y no una derrota anunciada.
        /// </summary>
        static List<BattlePokemon> BuildTutorialTeam()
        {
            string counter = GameRules.CounterStarter(GameState.StarterId);
            if (string.IsNullOrEmpty(counter))
            {
                Debug.LogWarning("Tutorial de Gabriel sin inicial identificable; se usa Charmander.");
                counter = "charmander";
            }

            int level = GameRules.TutorialRivalLevelFor(GameState.Save.gabrielChoice);
            TeamMember member = TeamMembers.Create(counter, level);
            PokemonData species = PokemonDatabase.Get(counter);
            var team = new List<BattlePokemon>(1);
            if (species != null)
                team.Add(BattlePokemon.FromTeamMember(member, species));
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