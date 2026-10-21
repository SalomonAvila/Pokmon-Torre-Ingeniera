using System;
using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Tipo de IA que usa el entrenador en combate.</summary>
    public enum TrainerAi
    {
        Wild,
        Trainer,
        Boss,
    }

    /// <summary>Un Pokémon del equipo de un entrenador o jefe.</summary>
    [Serializable]
    public class TrainerPokemon
    {
        public string speciesId;
        public int level;

        [Tooltip("Movimientos fijos; vacío = los últimos que conozca por learnset.")]
        public List<string> moveIds = new List<string>();
    }

    /// <summary>
    /// Entrenador o jefe del prototipo. Los datos viven en assets generados por el Editor
    /// (<c>PrototypeDataBuilder</c>) y se leen por id con <see cref="TrainerDatabase"/>.
    /// El jefe lleva <c>isBoss</c>, su piso y el sello que entrega.
    /// </summary>
    [CreateAssetMenu(fileName = "Trainer", menuName = "Torre Ingeniería/Datos/Entrenador")]
    public class TrainerData : ScriptableObject
    {
        [Tooltip("Id en minúscula; coincide con el nombre del asset y con GameState.DefeatTrainer.")]
        public string trainerId;

        public string displayName;

        [Tooltip("Clase que se muestra en el desafío (Ingeniera, Aprendiz, Vigilante...).")]
        public string trainerClass;

        public TrainerAi ai = TrainerAi.Trainer;

        public List<TrainerPokemon> team = new List<TrainerPokemon>();

        [Tooltip("Créditos que entrega al ser vencido; 0 = los calcula GameRules por nivel.")]
        public int rewardCredits;

        [Header("Jefe")]
        public bool isBoss;

        [Tooltip("Id del piso que completa al vencerlo (id de Building.Floors).")]
        public string floorId;

        [Tooltip("Id del sello que entrega; normalmente el id del piso.")]
        public string sealId;

        [TextArea]
        public string defeatDialogue;

        [TextArea]
        public string hintDialogue;
    }
}
