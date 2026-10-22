using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Motor de un combate por turnos (Doc §7): resuelve la acción del jugador y la del salvaje en orden
    /// (prioridad y Velocidad), aplica daño con la fórmula de <see cref="GameRules"/> y los efectos de
    /// estado, y decide el final (victoria, derrota o huida).
    ///
    /// Es lógica sin escena: no depende de la UI ni de GameObjects. Devuelve la lista de mensajes del turno
    /// y deja el estado en <see cref="PlayerActive"/>/<see cref="EnemyActive"/>. La captura, la experiencia y
    /// los combates de entrenador son de la segunda mitad de la etapa.
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

        public BattlePokemon PlayerActive => PlayerTeam[playerIndex];
        public BattlePokemon EnemyActive => EnemyTeam[enemyIndex];

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

        /// <summary>Resuelve un turno completo y devuelve los mensajes, en orden, para la UI.</summary>
        public List<string> ResolveTurn(BattleAction playerAction)
        {
            var messages = new List<string>();
            if (IsOver)
                return messages;

            TurnCount++;
            BattleAction enemyAction = BattleAI.ChooseWild(EnemyActive, PlayerActive);

            if (playerAction.kind == BattleActionKind.Run)
            {
                if (CanPlayerFlee)
                    TryFlee(messages);
                else
                    messages.Add("¡No puedes huir de un combate de entrenador!");

                if (!IsOver)
                    UseMove(EnemyActive, PlayerActive, enemyAction.moveIndex, messages, false);

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

        void ResolveEndOfTurn(List<string> messages)
        {
            ResolveFaints(messages);
            if (IsOver)
                return;

            ApplyResidual(PlayerActive, messages, true);
            ApplyResidual(EnemyActive, messages, false);
            ResolveFaints(messages);
        }

        /// <summary>Cambia al siguiente Pokémon sano o termina el combate cuando un bando se queda sin ninguno.</summary>
        void ResolveFaints(List<string> messages)
        {
            if (PlayerActive.IsFainted)
            {
                if (HasHealthy(PlayerTeam))
                {
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

        void ApplyResidual(BattlePokemon pokemon, List<string> messages, bool isPlayer)
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

        void TryFlee(List<string> messages)
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

        // ---------- Movimientos ----------

        void UseMove(BattlePokemon attacker, BattlePokemon defender, int moveIndex, List<string> messages,
            bool attackerIsPlayer)
        {
            bool defenderIsPlayer = !attackerIsPlayer;

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

            if (move.power <= 0)
            {
                ApplyEffect(move, attacker, defender, messages, attackerIsPlayer);
                return;
            }

            float effectiveness = defender.GetTypeEffectiveness(move.type);
            if (effectiveness <= 0f)
            {
                messages.Add($"No afecta a {Name(defender, defenderIsPlayer)}…");
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
            if (!Mathf.Approximately(effectiveness, 1f))
                messages.Add(TypeChart.Describe(effectiveness));

            ApplyEffect(move, attacker, defender, messages, attackerIsPlayer);
        }

        /// <summary>Comprueba sueño, congelación y parálisis antes de dejar mover al Pokémon.</summary>
        bool CanAct(BattlePokemon pokemon, List<string> messages, bool isPlayer)
        {
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

        void ApplyEffect(MoveData move, BattlePokemon attacker, BattlePokemon defender, List<string> messages,
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