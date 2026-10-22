using System;
using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.Battle
{
    /// <summary>Estados de alteración (Gen 3 simplificado).</summary>
    public enum StatusCondition
    {
        None,
        Sleep,
        Poison,
        Burn,
        Paralysis,
        Freeze,
    }

    /// <summary>Movimiento en combate con sus PP actuales.</summary>
    [Serializable]
    public class BattleMoveSlot
    {
        public string moveId;
        public int currentPp;
        public int maxPp;

        public MoveData Data => MoveDatabase.Get(moveId);

        public bool CanUse => currentPp > 0 && Data != null;
    }

    /// <summary>
    /// Estado de un Pokémon dentro de un combate: PS actuales, cambios de estadística, estado y movimientos
    /// con PP. Se crea desde <see cref="TeamMember"/> o desde un <see cref="TrainerPokemon"/> al empezar la
    /// batalla y se descarta al salir: no se guarda en el JSON (solo vuelven al equipo los PS y los PP).
    /// </summary>
    [Serializable]
    public class BattlePokemon
    {
        public string speciesId;
        public int level;

        public int maxHp;
        public int currentHp;

        public int attack;
        public int defense;
        public int spAttack;
        public int spDefense;
        public int speed;

        public PokemonType primaryType;
        public bool hasSecondaryType;
        public PokemonType secondaryType;

        public List<BattleMoveSlot> moves = new List<BattleMoveSlot>();

        public StatusCondition status = StatusCondition.None;

        /// <summary>Turnos de sueño restantes (solo si <see cref="status"/> es Sleep).</summary>
        public int statusTurns;

        public int attackStage;
        public int defenseStage;
        public int spAttackStage;
        public int spDefenseStage;
        public int speedStage;
        public int accuracyStage;
        public int evasionStage;

        public bool IsFainted => currentHp <= 0;

        /// <summary>Nombre para los mensajes de combate.</summary>
        public string DisplayName
        {
            get
            {
                PokemonData species = PokemonDatabase.Get(speciesId);
                return species != null ? species.displayName : speciesId;
            }
        }

        /// <summary>Crea el Pokémon de combate desde un miembro del equipo del jugador.</summary>
        public static BattlePokemon FromTeamMember(TeamMember member, PokemonData species)
        {
            var pokemon = new BattlePokemon
            {
                speciesId = member.speciesId,
                level = member.level,
                maxHp = member.maxHp,
                currentHp = member.hp == TeamMember.FullHp ? member.maxHp : member.hp,
                primaryType = species.primaryType,
                hasSecondaryType = species.hasSecondaryType,
                secondaryType = species.secondaryType,
            };

            pokemon.attack = PokemonStats.Stat(species.baseAttack, member.level);
            pokemon.defense = PokemonStats.Stat(species.baseDefense, member.level);
            pokemon.spAttack = PokemonStats.Stat(species.baseSpAttack, member.level);
            pokemon.spDefense = PokemonStats.Stat(species.baseSpDefense, member.level);
            pokemon.speed = PokemonStats.Stat(species.baseSpeed, member.level);

            foreach (MoveSlot slot in member.moves)
            {
                MoveData move = MoveDatabase.Get(slot.moveId);
                if (move == null)
                    continue;

                pokemon.moves.Add(new BattleMoveSlot
                {
                    moveId = slot.moveId,
                    currentPp = slot.pp,
                    maxPp = move.maxPp,
                });
            }

            return pokemon;
        }

        /// <summary>Crea el Pokémon salvaje de un encuentro (un solo miembro, movimientos del learnset).</summary>
        public static BattlePokemon CreateWild(string speciesId, int level)
        {
            PokemonData species = PokemonDatabase.Get(speciesId);
            if (species == null)
                throw new ArgumentException($"No existe la especie '{speciesId}'.", nameof(speciesId));

            var pokemon = new BattlePokemon
            {
                speciesId = speciesId,
                level = level,
                maxHp = PokemonStats.MaxHp(species.baseHp, level),
                primaryType = species.primaryType,
                hasSecondaryType = species.hasSecondaryType,
                secondaryType = species.secondaryType,
            };
            pokemon.currentHp = pokemon.maxHp;

            pokemon.attack = PokemonStats.Stat(species.baseAttack, level);
            pokemon.defense = PokemonStats.Stat(species.baseDefense, level);
            pokemon.spAttack = PokemonStats.Stat(species.baseSpAttack, level);
            pokemon.spDefense = PokemonStats.Stat(species.baseSpDefense, level);
            pokemon.speed = PokemonStats.Stat(species.baseSpeed, level);

            foreach (string moveId in TeamMembers.LearnsetMoves(species, level))
            {
                MoveData move = MoveDatabase.Get(moveId);
                if (move == null)
                    continue;

                pokemon.moves.Add(new BattleMoveSlot
                {
                    moveId = moveId,
                    currentPp = move.maxPp,
                    maxPp = move.maxPp,
                });
            }

            return pokemon;
        }

        public float GetTypeEffectiveness(PokemonType attackType) =>
            TypeChart.Effectiveness(attackType, primaryType, hasSecondaryType, secondaryType);

        public bool HasType(PokemonType type) =>
            primaryType == type || (hasSecondaryType && secondaryType == type);

        /// <summary>Estadística con el cambio de nivel de combate aplicado (Gen 3: +n → (2+n)/2).</summary>
        public static int ModifiedStat(int baseStat, int stage)
        {
            int clamped = Mathf.Clamp(stage, -6, 6);
            return clamped >= 0
                ? Mathf.FloorToInt(baseStat * (2f + clamped) / 2f)
                : Mathf.FloorToInt(baseStat * 2f / (2f - clamped));
        }

        public int Attack => ModifiedStat(attack, attackStage);
        public int Defense => ModifiedStat(defense, defenseStage);
        public int SpAttack => ModifiedStat(spAttack, spAttackStage);
        public int SpDefense => ModifiedStat(spDefense, spDefenseStage);
        public int Speed => ModifiedStat(speed, speedStage);

        public void TakeDamage(int damage) => currentHp = Mathf.Max(0, currentHp - damage);

        public void Heal(int amount) => currentHp = Mathf.Min(maxHp, currentHp + amount);

        /// <summary>
        /// Reinicia los cambios de estadística de combate. Se aplica al salir del campo (cambio manual
        /// o desmayo), como en Gen 3: los stages no acompañan al Pokémon cuando vuelve más tarde.
        /// </summary>
        public void ResetStages()
        {
            attackStage = 0;
            defenseStage = 0;
            spAttackStage = 0;
            spDefenseStage = 0;
            speedStage = 0;
            accuracyStage = 0;
            evasionStage = 0;
        }
    }
}