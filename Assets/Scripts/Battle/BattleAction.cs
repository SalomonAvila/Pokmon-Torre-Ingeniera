namespace TorreIngenieria.Battle
{
    /// <summary>Qué hace un combatiente en un turno.</summary>
    public enum BattleActionKind
    {
        None,
        Move,
        Run,
    }

    /// <summary>Acción elegida para un turno. De momento solo hay movimientos y huida.</summary>
    public class BattleAction
    {
        public BattleActionKind kind = BattleActionKind.None;

        /// <summary>Índice del movimiento en <see cref="BattlePokemon.moves"/> (−1 = ninguno).</summary>
        public int moveIndex = -1;

        public static BattleAction Move(int moveIndex) =>
            new BattleAction { kind = BattleActionKind.Move, moveIndex = moveIndex };

        public static BattleAction Run() => new BattleAction { kind = BattleActionKind.Run };
    }
}