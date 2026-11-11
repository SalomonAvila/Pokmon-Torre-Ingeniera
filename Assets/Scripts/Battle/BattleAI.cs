using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Decide qué hace el rival en su turno (Doc §8):
    /// <list type="bullet">
    /// <item>Salvaje: ataca la mayoría de las veces y, si no, elige un movimiento al azar.</item>
    /// <item>Entrenador: el 60 % de las veces su mejor movimiento y, con menos del 25 % de PS, un Tinto
    /// una sola vez.</item>
    /// <item>Jefe: siempre su mejor movimiento (IA determinista, Doc §7) y, con menos del 30 % de PS, un
    /// Agua de panela una sola vez.</item>
    /// </list>
    /// Los números salen de <see cref="GameRules"/>. La elección del jefe no tira el azar del "mejor
    /// movimiento": su comportamiento es el mismo en cada intento, que es lo que pide el Doc §7.
    /// </summary>
    public static class BattleAI
    {
        /// <summary>Objeto con el que el entrenador se cura una vez (Doc §8).</summary>
        public const string TrainerHealItem = "tinto";

        /// <summary>Objeto con el que el jefe se cura una vez (Doc §8).</summary>
        public const string BossHealItem = "agua_panela";

        /// <summary>Elige la acción del rival según su tipo de IA. <paramref name="canHeal"/> es "todavía no usó su objeto".</summary>
        public static BattleAction Choose(BattlePokemon self, BattlePokemon opponent, TrainerAi ai, bool canHeal)
        {
            switch (ai)
            {
                case TrainerAi.Boss:
                    return ChooseBoss(self, opponent, canHeal);
                case TrainerAi.Trainer:
                    return ChooseTrainer(self, opponent, canHeal);
                default:
                    return ChooseWild(self, opponent);
            }
        }

        public static BattleAction ChooseWild(BattlePokemon self, BattlePokemon opponent)
        {
            return ChooseMove(self, opponent, GameRules.WildAttackChance);
        }

        /// <summary>Entrenador: cura bajo el umbral y, si no, su mejor movimiento el 60 % de las veces.</summary>
        static BattleAction ChooseTrainer(BattlePokemon self, BattlePokemon opponent, bool canHeal)
        {
            BattleAction heal = TryHeal(self, canHeal, TrainerHealItem, GameRules.TrainerHealThreshold);
            return heal ?? ChooseMove(self, opponent, GameRules.BestMoveChance);
        }

        /// <summary>Jefe: cura bajo el umbral y, si no, siempre el mejor movimiento (sin azar en la elección).</summary>
        static BattleAction ChooseBoss(BattlePokemon self, BattlePokemon opponent, bool canHeal)
        {
            BattleAction heal = TryHeal(self, canHeal, BossHealItem, GameRules.BossHealThreshold);
            return heal ?? ChooseMove(self, opponent, 1f);
        }

        /// <summary>
        /// Movimiento del turno: con <paramref name="bestMoveChance"/> usa el de mayor daño esperado; si
        /// no, uno al azar entre los que tengan PP. Sin movimientos con PP devuelve −1 y el motor lo trata
        /// como "no puede moverse".
        /// </summary>
        static BattleAction ChooseMove(BattlePokemon self, BattlePokemon opponent, float bestMoveChance)
        {
            var action = new BattleAction { kind = BattleActionKind.Move, moveIndex = -1 };

            List<int> usable = UsableMoves(self);
            if (usable.Count == 0)
                return action;

            if (Random.value < bestMoveChance)
                action.moveIndex = BestDamagingMove(self, opponent, usable);

            // Si no ataca (o no hay ningún movimiento con poder), usa uno al azar entre los disponibles.
            if (action.moveIndex < 0)
                action.moveIndex = usable[Random.Range(0, usable.Count)];

            return action;
        }

        /// <summary>
        /// Objeto curativo del rival cuando sus PS bajan del umbral. Devuelve null si todavía no le toca,
        /// si ya usó su objeto o si el asset del objeto no existe.
        /// </summary>
        static BattleAction TryHeal(BattlePokemon self, bool canHeal, string itemId, float threshold)
        {
            if (!canHeal || self.IsFainted || self.maxHp <= 0)
                return null;

            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.category != ItemCategory.Heal)
                return null;

            float ratio = (float)self.currentHp / self.maxHp;
            return ratio < threshold ? BattleAction.Item(itemId) : null;
        }

        static List<int> UsableMoves(BattlePokemon self)
        {
            var usable = new List<int>();
            for (int i = 0; i < self.moves.Count; i++)
                if (self.moves[i].CanUse)
                    usable.Add(i);
            return usable;
        }

        /// <summary>Movimiento que hace más daño esperado (poder × STAB × efectividad).</summary>
        static int BestDamagingMove(BattlePokemon self, BattlePokemon opponent, List<int> usable)
        {
            int best = -1;
            float bestScore = 0f;
            foreach (int index in usable)
            {
                MoveData move = self.moves[index].Data;
                if (move == null || move.power <= 0)
                    continue;

                float score = move.power
                    * (self.HasType(move.type) ? GameRules.StabMultiplier : 1f)
                    * opponent.GetTypeEffectiveness(move.type);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            return best;
        }
    }
}
