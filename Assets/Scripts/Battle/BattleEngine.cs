using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Motor de un combate por turnos (Doc §7): resuelve la acción del jugador y la del salvaje en orden
    /// (prioridad y Velocidad), aplica daño con la fórmula de <see cref="GameRules"/> y los efectos de
    /// estado, y decide el final (victoria, captura, derrota o huida).
    ///
    /// Es lógica sin escena: no depende de la UI ni de GameObjects. Devuelve la lista de mensajes del turno
    /// y deja el estado en <see cref="PlayerActive"/>/<see cref="EnemyActive"/>. El cambio de Pokémon
    /// gasta el turno y el rival ataca al que entra. La experiencia la entrega <see cref="BattleSystem"/>
    /// al ganar; los combates de entrenador siguen pendientes.
    /// </summary>
    public class BattleEngine
    {
        public readonly List<BattlePokemon> PlayerTeam;
        public readonly List<BattlePokemon> EnemyTeam;
        public readonly BattleKind Kind;

        int playerIndex;
        int enemyIndex;

        public int TurnCount { get; private set; }
        public bool IsOver { get; private set; }
        public bool PlayerWon { get; private set; }
        public bool FledAway { get; private set; }

        /// <summary>La captura cerró el combate; no cuenta como victoria ni da EXP (Doc §7).</summary>
        public bool Captured { get; private set; }
        public string CapturedSpeciesId { get; private set; }
        public int CapturedLevel { get; private set; }

        /// <summary>En el último turno se lanzó una Pokébola en un combate salvaje (haya capturado o no).</summary>
        public bool CaptureAttempted { get; private set; }

        /// <summary>Sacudidas de la Pokébola: 3 en una captura y 0–2 en un escape.</summary>
        public int CaptureShakes { get; private set; }

        /// <summary>
        /// Mensajes iniciales del último cambio («¡Vuelve…!» y «¡Adelante…!»). La UI los muestra antes
        /// de refrescar las fichas, para que el ataque del rival se vea ya sobre el Pokémon que entró.
        /// </summary>
        public int SwitchMessageCount { get; private set; }

        /// <summary>El último turno gastó un objeto de la mochila (para descontarlo del inventario).</summary>
        public bool ItemConsumed { get; private set; }

        /// <summary>Sellos del jugador; fijan el tope de obediencia (Doc §7). BattleSystem lo asigna al empezar.</summary>
        public int ObedienceSeals { get; set; }

        /// <summary>Tirada de obediencia inyectable para las pruebas; si es nula se usa <see cref="Random.value"/>.</summary>
        public float? ObedienceRoll { get; set; }

        public BattlePokemon PlayerActive => PlayerTeam[playerIndex];
        public BattlePokemon EnemyActive => EnemyTeam[enemyIndex];

        /// <summary>Índice del Pokémon activo del jugador dentro de <see cref="PlayerTeam"/>.</summary>
        public int PlayerIndex => playerIndex;

        public BattleEngine(List<BattlePokemon> playerTeam, List<BattlePokemon> enemyTeam, BattleKind kind)
        {
            PlayerTeam = playerTeam;
            EnemyTeam = enemyTeam;
            Kind = kind;
            playerIndex = FirstHealthy(PlayerTeam);
            enemyIndex = FirstHealthy(EnemyTeam);
        }

        /// <summary>Solo se puede huir de un combate salvaje (Doc §7).</summary>
        public bool CanPlayerFlee => Kind == BattleKind.Wild;

        /// <summary>
        /// Un cambio válido apunta a otro miembro del equipo que no esté debilitado. El Pokémon activo
        /// no puede cambiarse por sí mismo y un índice fuera del equipo no gasta el turno.
        /// </summary>
        public bool CanSwitchTo(int index) =>
            index >= 0 && index < PlayerTeam.Count && index != playerIndex && !PlayerTeam[index].IsFainted;

        /// <summary>
        /// Resuelve un turno completo y devuelve los mensajes, en orden, para la UI. La tirada de captura
        /// se puede fijar desde las pruebas; si se omite, sale de <see cref="Random.value"/>.
        /// </summary>
        public List<BattleMessage> ResolveTurn(BattleAction playerAction, float? captureRoll = null)
        {
            var messages = new List<BattleMessage>();
            ItemConsumed = false;
            SwitchMessageCount = 0;
            if (IsOver)
                return messages;

            // La captura es del turno actual; no se arrastra el intento anterior a la UI.
            CaptureAttempted = false;
            CaptureShakes = 0;

            // Un cambio inválido se avisa sin gastar el turno ni dejar que el rival ataque.
            if (playerAction.kind == BattleActionKind.Switch && !CanSwitchTo(playerAction.switchIndex))
            {
                AddSwitchError(playerAction.switchIndex, messages);
                return messages;
            }

            TurnCount++;
            BattleAction enemyAction = BattleAI.ChooseWild(EnemyActive, PlayerActive);

            if (playerAction.kind == BattleActionKind.Switch)
            {
                int previousIndex = playerIndex;
                BattlePokemon previous = PlayerTeam[previousIndex];
                playerIndex = playerAction.switchIndex;
                previous.ResetStages();

                messages.Add($"¡Vuelve, {previous.DisplayName}!");
                messages.Add($"¡Adelante, {PlayerActive.DisplayName}!");
                SwitchMessageCount = messages.Count;

                // El cambio gasta el turno. El rival ya eligió su movimiento antes de ver entrar al
                // nuevo Pokémon y ataca a este, no al que salió.
                if (!IsOver)
                    UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);
                if (!IsOver)
                    ResolveEndOfTurn(messages);
                return messages;
            }

            if (playerAction.kind == BattleActionKind.Item)
            {
                bool consumed = UseItem(playerAction.itemId, messages, captureRoll);
                ItemConsumed = consumed;

                // Usar un objeto gasta el turno y el salvaje ataca después, salvo que la captura tenga
                // éxito o que el objeto no se haya podido usar.
                if (consumed && !IsOver)
                    UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);
                if (consumed && !IsOver)
                    ResolveEndOfTurn(messages);
                return messages;
            }

            if (playerAction.kind == BattleActionKind.Run)
            {
                if (CanPlayerFlee)
                    TryFlee(messages);
                else
                    messages.Add("¡No puedes huir de un combate de entrenador!");

                if (!IsOver)
                    UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);

                if (!IsOver)
                    ResolveEndOfTurn(messages);
                return messages;
            }

            bool playerFirst = PlayerGoesFirst(playerAction, enemyAction);
            if (playerFirst)
            {
                UseMove(PlayerActive, EnemyActive, playerAction.moveIndex, messages, true);
                if (!IsOver && !EnemyActive.IsFainted)
                    UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);
            }
            else
            {
                UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);
                if (!IsOver && !PlayerActive.IsFainted)
                    UseMove(PlayerActive, EnemyActive, playerAction.moveIndex, messages, true);
            }

            ResolveEndOfTurn(messages);
            return messages;
        }

        // ---------- Turno ----------

        void ResolveEndOfTurn(List<BattleMessage> messages)
        {
            ResolveFaints(messages);
            if (IsOver)
                return;

            ApplyResidual(PlayerActive, messages, true);
            ApplyResidual(EnemyActive, messages, false);
            ResolveFaints(messages);
        }

        /// <summary>Cambia al siguiente Pokémon sano o termina el combate cuando un bando se queda sin ninguno.</summary>
        void ResolveFaints(List<BattleMessage> messages)
        {
            if (PlayerActive.IsFainted)
            {
                if (HasHealthy(PlayerTeam))
                {
                    PlayerActive.ResetStages();
                    playerIndex = FirstHealthy(PlayerTeam);
                    messages.Add($"¡Adelante, {PlayerActive.DisplayName}!");
                }
                else
                {
                    IsOver = true;
                    PlayerWon = false;
                    messages.Add("Todos tus Pokémon se debilitaron…");
                    return;
                }
            }

            if (EnemyActive.IsFainted)
            {
                if (HasHealthy(EnemyTeam))
                {
                    EnemyActive.ResetStages();
                    enemyIndex = FirstHealthy(EnemyTeam);
                    messages.Add($"El rival envió a {EnemyActive.DisplayName}.");
                }
                else
                {
                    IsOver = true;
                    PlayerWon = true;
                    messages.Add("¡Ganaste el combate!");
                }
            }
        }

        void ApplyResidual(BattlePokemon pokemon, List<BattleMessage> messages, bool isPlayer)
        {
            if (pokemon.IsFainted)
                return;

            if (pokemon.status == StatusCondition.Poison)
            {
                pokemon.TakeDamage(Mathf.Max(1, pokemon.maxHp / GameRules.PoisonDamageDivisor));
                messages.Add($"{Name(pokemon, isPlayer)} sufre por el veneno.");
            }
            else if (pokemon.status == StatusCondition.Burn)
            {
                pokemon.TakeDamage(Mathf.Max(1, pokemon.maxHp / GameRules.BurnDamageDivisor));
                messages.Add($"{Name(pokemon, isPlayer)} sufre por la quemadura.");
            }
        }

        bool PlayerGoesFirst(BattleAction player, BattleAction enemy)
        {
            int playerPriority = MoveAt(PlayerActive, player.moveIndex)?.priority ?? 0;
            int enemyPriority = MoveAt(EnemyActive, enemy.moveIndex)?.priority ?? 0;
            if (playerPriority != enemyPriority)
                return playerPriority > enemyPriority;

            if (PlayerActive.Speed != EnemyActive.Speed)
                return PlayerActive.Speed > EnemyActive.Speed;

            return Random.value < 0.5f;
        }

        void TryFlee(List<BattleMessage> messages)
        {
            float chance = GameRules.FleeChance(PlayerActive.Speed, EnemyActive.Speed);
            if (Random.value < chance)
            {
                FledAway = true;
                IsOver = true;
                messages.Add("¡Escapaste sin problemas!");
            }
            else
            {
                messages.Add("¡No pudiste escapar!");
            }
        }

        /// <summary>Aviso de un cambio que la UI ya debería haber bloqueado; no gasta el turno.</summary>
        void AddSwitchError(int index, List<BattleMessage> messages)
        {
            if (index == playerIndex)
                messages.Add($"{PlayerActive.DisplayName} ya está en combate.");
            else if (index < 0 || index >= PlayerTeam.Count)
                messages.Add("Ese Pokémon no está en el equipo.");
            else if (PlayerTeam[index].IsFainted)
                messages.Add($"{PlayerTeam[index].DisplayName} está debilitado y no puede combatir.");
            else
                messages.Add("No puedes cambiar a ese Pokémon.");
        }

        // ---------- Objetos ----------

        /// <summary>
        /// Usa un objeto de la mochila en el Pokémon activo. Devuelve si gastó el turno: la Pokébola
        /// siempre lo gasta (haya o no captura), el curativo solo si tenía efecto y los objetos que no
        /// aplican en combate no lo gastan. La captura se rechaza fuera de un combate salvaje (Doc §7).
        /// </summary>
        bool UseItem(string itemId, List<BattleMessage> messages, float? captureRoll)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null)
            {
                messages.Add("Ese objeto no existe.");
                return false;
            }

            if (item.category == ItemCategory.Ball)
            {
                if (Kind != BattleKind.Wild)
                {
                    messages.Add("No puedes capturar un Pokémon de entrenador.");
                    return false;
                }

                TryCapture(item, messages, captureRoll);
                return true;
            }

            if (item.category == ItemCategory.Heal)
            {
                if (PlayerActive.IsFainted)
                {
                    messages.Add($"{PlayerActive.DisplayName} está debilitado.");
                    return false;
                }

                if (PlayerActive.currentHp >= PlayerActive.maxHp)
                {
                    messages.Add($"{PlayerActive.DisplayName} ya tiene los PS completos.");
                    return false;
                }

                int before = PlayerActive.currentHp;
                PlayerActive.Heal(item.healAmount);
                messages.Add($"{PlayerActive.DisplayName} recuperó {PlayerActive.currentHp - before} PS.");
                return true;
            }

            messages.Add("Ese objeto no se puede usar en combate.");
            return false;
        }

        void TryCapture(ItemData ball, List<BattleMessage> messages, float? captureRoll)
        {
            PokemonData species = PokemonDatabase.Get(EnemyActive.speciesId);
            if (species == null)
            {
                messages.Add("No se pudo leer la especie del rival.");
                return;
            }

            float chance = GameRules.CaptureChance(EnemyActive.maxHp, EnemyActive.currentHp,
                species.catchRate, ball.catchBonus, CaptureStatusBonus(EnemyActive.status));

            // Una sola tirada decide el resultado. Las sacudidas son presentación: una captura
            // siempre muestra tres, y un escape usa la probabilidad recortada a 0–2.
            float roll = captureRoll ?? Random.value;
            bool success = roll < chance;
            CaptureAttempted = true;
            CaptureShakes = GameRules.CaptureShakes(chance, success);

            messages.Add($"¡Lanzaste una {ball.displayName}!");
            for (int i = 0; i < CaptureShakes; i++)
                messages.Add(new BattleMessage("¡La Pokébola se sacude...!", true));

            if (success)
            {
                Captured = true;
                CapturedSpeciesId = EnemyActive.speciesId;
                CapturedLevel = EnemyActive.level;
                IsOver = true;
                messages.Add("¡Ya!");
                messages.Add($"¡Atrapaste a {EnemyActive.DisplayName}!");
                return;
            }

            messages.Add("¡Se escapó!");
        }

        /// <summary>Bonus de captura por estado (propuesta del Doc §7; 1 sin estado).</summary>
        static float CaptureStatusBonus(StatusCondition status) => status switch
        {
            StatusCondition.Sleep or StatusCondition.Freeze => 2f,
            StatusCondition.Poison or StatusCondition.Burn or StatusCondition.Paralysis => 1.5f,
            _ => 1f,
        };

        // ---------- Movimientos ----------

        void UseMove(BattlePokemon attacker, BattlePokemon defender, int moveIndex, List<BattleMessage> messages,
            bool attackerIsPlayer)
        {
            if (moveIndex < 0 || moveIndex >= attacker.moves.Count)
            {
                messages.Add($"{Name(attacker, attackerIsPlayer)} no puede moverse.");
                return;
            }

            if (!CanAct(attacker, messages, attackerIsPlayer))
                return;

            BattleMoveSlot slot = attacker.moves[moveIndex];
            MoveData move = slot.Data;
            if (move == null)
                return;

            if (slot.currentPp <= 0)
            {
                messages.Add($"{Name(attacker, attackerIsPlayer)} no tiene PP de {move.displayName}.");
                return;
            }

            slot.currentPp--;
            messages.Add($"¡{Name(attacker, attackerIsPlayer)} usó {move.displayName}!");

            if (!Hits(move, attacker, defender))
            {
                messages.Add("¡Falló!");
                return;
            }

            // Los movimientos de estado no pasan por la tabla de efectividad.
            if (move.category == MoveCategory.Status || move.power <= 0)
            {
                ApplyEffect(move, attacker, defender, messages, attackerIsPlayer);
                return;
            }

            float effectiveness = defender.GetTypeEffectiveness(move.type);
            if (effectiveness <= 0f)
            {
                // Inmunidad: no hay daño ni mensaje de daño, solo el aviso exacto del documento.
                messages.Add(TypeChart.Describe(effectiveness));
                return;
            }

            bool stab = attacker.HasType(move.type);
            int attackStat = move.category == MoveCategory.Physical ? attacker.Attack : attacker.SpAttack;
            int defenseStat = move.category == MoveCategory.Physical ? defender.Defense : defender.SpDefense;
            bool critical = Random.value < 1f / GameRules.CriticalDenominator;
            float roll = Random.Range(GameRules.DamageRollMin, GameRules.DamageRollMax);

            int damage = GameRules.Damage(attacker.level, move.power, attackStat, defenseStat,
                effectiveness, stab, roll, critical);
            defender.TakeDamage(damage);

            if (critical)
                messages.Add("¡Golpe crítico!");

            // El aviso de efectividad va después del daño y el caso neutro no muestra nada.
            string effectivenessMessage = TypeChart.Describe(effectiveness);
            if (!string.IsNullOrEmpty(effectivenessMessage))
                messages.Add(effectivenessMessage);

            ApplyEffect(move, attacker, defender, messages, attackerIsPlayer);
        }

        /// <summary>
        /// Comprueba la obediencia del jugador (Doc §7: por encima de 10 + 5 × sellos desobedece el 30 %)
        /// y después sueño, congelación y parálisis antes de dejar mover al Pokémon.
        /// </summary>
        bool CanAct(BattlePokemon pokemon, List<BattleMessage> messages, bool isPlayer)
        {
            if (isPlayer && GameRules.Disobeys(pokemon.level, ObedienceSeals, ObedienceRoll ?? Random.value))
            {
                messages.Add($"¡{Name(pokemon, isPlayer)} no obedece!");
                return false;
            }

            switch (pokemon.status)
            {
                case StatusCondition.Sleep:
                    if (pokemon.statusTurns > 0)
                    {
                        pokemon.statusTurns--;
                        messages.Add($"{Name(pokemon, isPlayer)} está dormido.");
                        return false;
                    }
                    pokemon.status = StatusCondition.None;
                    messages.Add($"{Name(pokemon, isPlayer)} despertó.");
                    return true;

                case StatusCondition.Freeze:
                    if (Random.value < GameRules.FreezeThawChance)
                    {
                        pokemon.status = StatusCondition.None;
                        messages.Add($"{Name(pokemon, isPlayer)} se descongeló.");
                        return true;
                    }
                    messages.Add($"{Name(pokemon, isPlayer)} está congelado.");
                    return false;

                case StatusCondition.Paralysis:
                    if (Random.value < GameRules.ParalysisSkipChance)
                    {
                        messages.Add($"{Name(pokemon, isPlayer)} está paralizado y no se movió.");
                        return false;
                    }
                    return true;

                default:
                    return true;
            }
        }

        bool Hits(MoveData move, BattlePokemon attacker, BattlePokemon defender)
        {
            if (move.accuracy <= 0)
                return true; // precisión 0 = nunca falla

            float accuracy = move.accuracy / 100f
                * GameRules.AccuracyStageMultiplier(attacker.accuracyStage - defender.evasionStage);
            return Random.value < accuracy;
        }

        void ApplyEffect(MoveData move, BattlePokemon attacker, BattlePokemon defender, List<BattleMessage> messages,
            bool attackerIsPlayer)
        {
            if (move.effect == MoveEffect.None)
                return;
            if (move.effectChance < 100 && Random.value * 100f >= move.effectChance)
                return;

            bool defenderIsPlayer = !attackerIsPlayer;
            switch (move.effect)
            {
                case MoveEffect.LowerAttack:
                    if (Lower(ref defender.attackStage))
                        messages.Add($"Bajó el Ataque de {Name(defender, defenderIsPlayer)}.");
                    break;
                case MoveEffect.LowerDefense:
                    if (Lower(ref defender.defenseStage))
                        messages.Add($"Bajó la Defensa de {Name(defender, defenderIsPlayer)}.");
                    break;
                case MoveEffect.LowerSpDefense:
                    if (Lower(ref defender.spDefenseStage))
                        messages.Add($"Bajó la Defensa Especial de {Name(defender, defenderIsPlayer)}.");
                    break;
                case MoveEffect.LowerSpeed:
                    if (Lower(ref defender.speedStage))
                        messages.Add($"Bajó la Velocidad de {Name(defender, defenderIsPlayer)}.");
                    break;
                case MoveEffect.LowerAccuracy:
                    if (Lower(ref defender.accuracyStage))
                        messages.Add($"Bajó la Precisión de {Name(defender, defenderIsPlayer)}.");
                    break;
                case MoveEffect.RaiseDefense:
                    if (Raise(ref attacker.defenseStage))
                        messages.Add($"Subió la Defensa de {Name(attacker, attackerIsPlayer)}.");
                    break;
                case MoveEffect.Sleep:
                    if (TrySetStatus(defender, StatusCondition.Sleep))
                    {
                        defender.statusTurns = Random.Range(GameRules.SleepMinTurns, GameRules.SleepMaxTurns + 1);
                        messages.Add($"{Name(defender, defenderIsPlayer)} se durmió.");
                    }
                    break;
                case MoveEffect.Poison:
                    if (TrySetStatus(defender, StatusCondition.Poison))
                        messages.Add($"{Name(defender, defenderIsPlayer)} fue envenenado.");
                    break;
                case MoveEffect.Burn:
                    if (TrySetStatus(defender, StatusCondition.Burn))
                        messages.Add($"{Name(defender, defenderIsPlayer)} fue quemado.");
                    break;
                case MoveEffect.Paralysis:
                    if (TrySetStatus(defender, StatusCondition.Paralysis))
                        messages.Add($"{Name(defender, defenderIsPlayer)} fue paralizado.");
                    break;
            }
        }

        static bool Lower(ref int stage)
        {
            if (stage <= -6)
                return false;
            stage--;
            return true;
        }

        static bool Raise(ref int stage)
        {
            if (stage >= 6)
                return false;
            stage++;
            return true;
        }

        static bool TrySetStatus(BattlePokemon pokemon, StatusCondition status)
        {
            if (pokemon.status != StatusCondition.None)
                return false;
            pokemon.status = status;
            return true;
        }

        // ---------- Utilidades ----------

        static MoveData MoveAt(BattlePokemon pokemon, int index) =>
            index >= 0 && index < pokemon.moves.Count ? pokemon.moves[index].Data : null;

        static int FirstHealthy(List<BattlePokemon> team)
        {
            for (int i = 0; i < team.Count; i++)
                if (!team[i].IsFainted)
                    return i;
            return 0;
        }

        static bool HasHealthy(List<BattlePokemon> team) => team.Exists(pokemon => !pokemon.IsFainted);

        string Name(BattlePokemon pokemon, bool isPlayer)
        {
            if (isPlayer)
                return pokemon.DisplayName;
            return Kind == BattleKind.Wild ? $"El {pokemon.DisplayName} salvaje" : $"El {pokemon.DisplayName} rival";
        }
    }
}