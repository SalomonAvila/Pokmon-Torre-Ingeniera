namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Línea del registro de combate. <see cref="autoAdvance"/> lo usan las sacudidas de la Pokébola:
    /// se muestran y pasan solas, sin esperar a que el jugador pulse una tecla.
    /// </summary>
    public readonly struct BattleMessage
    {
        public readonly string text;
        public readonly bool autoAdvance;

        public BattleMessage(string text, bool autoAdvance = false)
        {
            this.text = text;
            this.autoAdvance = autoAdvance;
        }

        /// <summary>Permite añadir textos normales a una lista de <see cref="BattleMessage"/>.</summary>
        public static implicit operator BattleMessage(string text) => new BattleMessage(text);
    }

    /// <summary>Qué hace un combatiente en un turno.</summary>
    public enum BattleActionKind
    {
        None,
        Move,
        Run,
        Item,
        Switch,
    }

    /// <summary>
    /// Acción elegida para un turno: movimiento, huida, uso de un objeto de la mochila o cambio de
    /// Pokémon. El cambio ocupa el turno y el rival ataca al Pokémon que entra.
    /// </summary>
    public class BattleAction
    {
        public BattleActionKind kind = BattleActionKind.None;

        /// <summary>Índice del movimiento en <see cref="BattlePokemon.moves"/> (−1 = ninguno).</summary>
        public int moveIndex = -1;

        /// <summary>Id del objeto de la mochila que se usa (solo con <see cref="BattleActionKind.Item"/>).</summary>
        public string itemId;

        /// <summary>Índice del compañero que entra (solo con <see cref="BattleActionKind.Switch"/>).</summary>
        public int switchIndex = -1;

        public static BattleAction Move(int moveIndex) =>
            new BattleAction { kind = BattleActionKind.Move, moveIndex = moveIndex };

        public static BattleAction Run() => new BattleAction { kind = BattleActionKind.Run };

        public static BattleAction Item(string itemId) =>
            new BattleAction { kind = BattleActionKind.Item, itemId = itemId };

        public static BattleAction Switch(int switchIndex) =>
            new BattleAction { kind = BattleActionKind.Switch, switchIndex = switchIndex };
    }
}