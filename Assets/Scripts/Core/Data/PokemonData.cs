using System;
using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Movimiento que una especie aprende a un nivel concreto.</summary>
    [Serializable]
    public class LearnsetEntry
    {
        public int level;
        public string moveId;
    }

    /// <summary>
    /// Ficha de una especie del prototipo: tipos, stats base oficiales de Gen 3, EXP base, ratio de
    /// captura y learnset. Los assets se generan desde la tabla del Editor (<c>PrototypeDataBuilder</c>)
    /// y se leen por id con <see cref="PokemonDatabase"/>.
    ///
    /// Los stats actuales no se guardan aquí: se calculan con <see cref="PokemonStats"/> a partir del
    /// nivel y la especie, así el asset es inmutable.
    /// </summary>
    [CreateAssetMenu(fileName = "Pokemon", menuName = "Torre Ingeniería/Datos/Especie")]
    public class PokemonData : ScriptableObject
    {
        [Tooltip("Id en minúscula; coincide con SpeciesCatalog y con el nombre del asset.")]
        public string speciesId;

        public string displayName;

        [Tooltip("Número de la Pokédex nacional (001 Bulbasaur); lo muestran el Resumen y la Pokédex.")]
        public int dexNumber;

        [Header("Tipos")]
        public PokemonType primaryType;
        public bool hasSecondaryType;
        public PokemonType secondaryType;

        [Header("Stats base (Gen 3)")]
        public int baseHp;
        public int baseAttack;
        public int baseDefense;
        public int baseSpAttack;
        public int baseSpDefense;
        public int baseSpeed;

        [Header("Progreso")]
        [Tooltip("EXP base que otorga al ser vencido (Gen 3).")]
        public int baseExp;

        [Tooltip("Ratio de captura oficial, de 1 a 255.")]
        [Range(1, 255)]
        public int catchRate = 45;

        [Tooltip("Movimientos por nivel; el combate aprende/recuerda según esto.")]
        public List<LearnsetEntry> learnset = new List<LearnsetEntry>();

        [Header("Evolución (opcional en el prototipo)")]
        [Tooltip("Id de la especie a la que evoluciona; vacío si no evoluciona en el prototipo.")]
        public string evolvesTo;

        [Tooltip("Nivel de evolución; 0 si no aplica.")]
        public int evolveLevel;

        [Header("Presentación (arte provisional)")]
        public Sprite frontSprite;
        public Sprite backSprite;
        public Sprite icon;

        [TextArea]
        public string description;

        /// <summary>Multiplicador de tipo de un ataque contra esta especie (1, 2, 0,5...).</summary>
        public float TakeEffectiveness(PokemonType attack) =>
            TypeChart.Effectiveness(attack, primaryType, hasSecondaryType, secondaryType);

        /// <summary>¿La especie comparte alguno de esos tipos? Sirve para STAB.</summary>
        public bool HasType(PokemonType type) =>
            primaryType == type || (hasSecondaryType && secondaryType == type);
    }
}
