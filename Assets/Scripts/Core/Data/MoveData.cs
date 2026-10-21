using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Categoría de un movimiento según el reparto moderno por movimiento (no por tipo).</summary>
    public enum MoveCategory
    {
        Physical,
        Special,
        Status,
    }

    /// <summary>
    /// Efecto secundario del movimiento. El prototipo solo necesita estados y cambios de estadística;
    /// el combate los interpreta, aquí solo se declaran.
    /// </summary>
    public enum MoveEffect
    {
        None,
        LowerAttack,
        LowerDefense,
        LowerSpeed,
        LowerAccuracy,
        LowerSpDefense,
        RaiseDefense,
        Sleep,
        Poison,
        Burn,
        Paralysis,
    }

    /// <summary>
    /// Movimiento del prototipo. Los datos viven en assets generados por el Editor a partir de una
    /// sola tabla (ver <c>PrototypeDataBuilder</c>); el combate solo lee este asset.
    ///
    /// La categoría es por movimiento (Physical/Special/Status) en lugar de la regla de Gen 3 que la
    /// deriva del tipo: el documento no fija el reparto y así los datos son más flexibles.
    /// </summary>
    [CreateAssetMenu(fileName = "Move", menuName = "Torre Ingeniería/Datos/Movimiento")]
    public class MoveData : ScriptableObject
    {
        [Tooltip("Id en minúscula; coincide con el nombre del asset.")]
        public string moveId;

        public string displayName;

        public PokemonType type;

        public MoveCategory category;

        [Tooltip("Poder base; 0 en movimientos de estado.")]
        public int power;

        [Tooltip("Precisión de 0 a 100; 0 = nunca falla.")]
        [Range(0, 100)]
        public int accuracy = 100;

        [Tooltip("PP máximos del movimiento.")]
        public int maxPp = 10;

        [Tooltip("Prioridad de -6 a 6; los valores altos se ejecutan antes.")]
        [Range(-6, 6)]
        public int priority;

        public MoveEffect effect;

        [Tooltip("Probabilidad del efecto secundario, de 0 a 100.")]
        [Range(0, 100)]
        public int effectChance;

        [TextArea]
        public string description;
    }
}
