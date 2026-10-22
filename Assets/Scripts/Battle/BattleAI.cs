using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Decide qué hace un rival salvaje en su turno. En el prototipo el salvaje no huye (se deja para el
    /// balance de la Etapa 9): elige atacar la mayoría de las veces y, si no, un movimiento al azar.
    /// Los números salen de <see cref="GameRules"/>.
    /// </summary>
    public static class BattleAI
    {
        public static BattleAction ChooseWild(BattlePokemon self, BattlePokemon opponent)
        {
            var action = new BattleAction { kind = BattleActionKind.Move, moveIndex = -1 };

            var usable = UsableMoves(self);
            if (usable.Count == 0)
                return action; // sin PP: el motor lo trata como "no puede moverse"

            if (Random.value < GameRules.WildAttackChance)
                action.moveIndex = BestDamagingMove(self, opponent, usable);

            // Si no ataca (o no hay ningún movimiento con poder), usa uno al azar entre los disponibles.
            if (action.moveIndex < 0)
                action.moveIndex = usable[Random.Range(0, usable.Count)];

            return action;
        }

        static System.Collections.Generic.List<int> UsableMoves(BattlePokemon self)
        {
            var usable = new System.Collections.Generic.List<int>();
            for (int i = 0; i < self.moves.Count; i++)
                if (self.moves[i].CanUse)
                    usable.Add(i);
            return usable;
        }

        /// <summary>Movimiento que hace más daño esperado (poder × STAB × efectividad).</summary>
        static int BestDamagingMove(BattlePokemon self, BattlePokemon opponent,
            System.Collections.Generic.List<int> usable)
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