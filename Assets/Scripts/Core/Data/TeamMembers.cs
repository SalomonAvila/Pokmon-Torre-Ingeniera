using System;
using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Resultado de un nivel ganado: PS ganados y movimientos aprendidos u omitidos.</summary>
    public sealed class LevelUpEvent
    {
        public int Level;
        public int HpGained;
        public readonly List<string> LearnedMoves = new List<string>();
        public readonly List<string> MissedMoves = new List<string>();
    }

    /// <summary>
    /// Resultado de entregar EXP o de subir niveles con un objeto como la Cafeína. Incluye el tramo
    /// completo para que la UI pueda animar la barra y mostrar un mensaje por nivel.
    /// </summary>
    public sealed class ExpGainResult
    {
        public int OldLevel;
        public int OldExp;
        public int NewLevel;
        public int NewExp;

        /// <summary>EXP que realmente se sumó (0 si ya estaba en el nivel 100).</summary>
        public int ExpGained;

        public readonly List<LevelUpEvent> LevelUps = new List<LevelUpEvent>();

        public bool LeveledUp => LevelUps.Count > 0;
    }

    /// <summary>
    /// Única factoría del equipo: convierte especie + nivel en un <see cref="TeamMember"/> con PS,
    /// EXP, movimientos y PP, y repara esos campos derivados al cargar una partida.
    ///
    /// Vive aparte de <see cref="TeamMember"/> (que es un dato puro de guardado) para no acoplar el
    /// JSON a <see cref="PokemonDatabase"/> ni a <see cref="MoveDatabase"/>.
    /// </summary>
    public static class TeamMembers
    {
        /// <summary>Movimientos que puede llevar un Pokémon (Doc §7).</summary>
        public const int MaxMoves = 4;

        /// <summary>
        /// Crea un Pokémon listo para jugar. Los PS actuales arrancan al máximo (no el centinela de
        /// migración), la EXP es la del nivel (nivel³) y los movimientos salen del learnset: los cuatro
        /// de mayor nivel ≤ <paramref name="level"/>, con desempate por el orden de la tabla.
        /// </summary>
        public static TeamMember Create(string speciesId, int level) =>
            Create(speciesId, level, null);

        /// <summary>
        /// Crea el Pokémon de un entrenador o jefe. Si <see cref="TrainerPokemon.moveIds"/> trae
        /// movimientos fijos, se respetan; si está vacío, se usa el learnset.
        /// </summary>
        public static TeamMember Create(TrainerPokemon pokemon)
        {
            if (pokemon == null)
                throw new ArgumentNullException(nameof(pokemon));

            return Create(pokemon.speciesId, pokemon.level, pokemon.moveIds);
        }

        /// <summary>
        /// Crea el Pokémon capturado. Parte de <see cref="Create(string, int)"/> (EXP del nivel,
        /// movimientos y PP máximos) y después copia los PS y los PP actuales del salvaje; los PP van
        /// en el mismo orden que los movimientos del learnset. (propuesta) Los PS nunca se guardan en 0
        /// ni por encima del máximo; si no se pasan, quedan llenos. La captura no da EXP adicional:
        /// solo la del nivel.
        /// </summary>
        public static TeamMember CreateCaptured(string speciesId, int level, int currentHp = -1,
            IReadOnlyList<int> currentPp = null)
        {
            TeamMember member = Create(speciesId, level);

            if (currentHp > 0)
                member.hp = Math.Min(currentHp, member.maxHp);

            if (currentPp != null)
            {
                int count = Math.Min(currentPp.Count, member.moves.Count);
                for (int i = 0; i < count; i++)
                    member.moves[i].pp = Math.Clamp(currentPp[i], 0, member.moves[i].maxPp);
            }

            return member;
        }

        /// <summary>
        /// Suma EXP y sube todos los niveles que correspondan, en bucle y con tope en 100 (Doc §7).
        /// Los PS actuales suben la diferencia entre el máximo nuevo y el anterior; un Pokémon debilitado
        /// (0 PS) no revive. Los movimientos nuevos del learnset se aprenden solos si hay hueco; con
        /// cuatro se anotan en <see cref="LevelUpEvent.MissedMoves"/> y no se reemplaza nada.
        /// Solo se guarda <c>maxHp</c>; las demás estadísticas se derivan de <see cref="PokemonStats"/>.
        /// La función es la misma para el combate y para objetos que suben niveles (Cafeína).
        /// </summary>
        public static ExpGainResult GainExp(TeamMember member, int amount)
        {
            var result = new ExpGainResult();
            if (member == null || string.IsNullOrEmpty(member.speciesId))
                return result;

            PokemonData species = PokemonDatabase.Get(member.speciesId);
            if (species == null)
                return result;

            member.level = Math.Clamp(member.level, PokemonStats.MinLevel, PokemonStats.MaxLevel);
            result.OldLevel = member.level;

            int levelFloor = PokemonStats.ExperienceForLevel(member.level);
            result.OldExp = Math.Max(member.exp, levelFloor);
            member.exp = result.OldExp;

            int maxExp = PokemonStats.ExperienceForLevel(PokemonStats.MaxLevel);
            int beforeExp = member.exp;
            member.exp = Math.Min(maxExp, member.exp + Math.Max(0, amount));
            result.ExpGained = member.exp - beforeExp;

            // maxHp es el único campo derivado que se guarda: se recalcula antes de repartir los
            // aumentos de nivel. El centinela de PS completos pasa al máximo; el 0 se respeta.
            int oldMaxHp = PokemonStats.MaxHp(species.baseHp, member.level);
            member.maxHp = oldMaxHp;
            if (member.hp < 0 || member.hp > oldMaxHp)
                member.hp = oldMaxHp;

            if (member.moves == null)
                member.moves = new List<MoveSlot>();

            while (member.level < PokemonStats.MaxLevel &&
                   member.exp >= PokemonStats.ExperienceForLevel(member.level + 1))
            {
                int previousMaxHp = member.maxHp;
                member.level++;
                member.maxHp = PokemonStats.MaxHp(species.baseHp, member.level);

                int hpGained = member.maxHp - previousMaxHp;
                if (member.hp > 0)
                    member.hp = Math.Min(member.maxHp, member.hp + hpGained);

                var levelUp = new LevelUpEvent
                {
                    Level = member.level,
                    HpGained = hpGained,
                };
                LearnMovesAtLevel(member, species, levelUp);
                result.LevelUps.Add(levelUp);
            }

            result.NewLevel = member.level;
            result.NewExp = member.exp;
            return result;
        }

        /// <summary>
        /// Sube niveles sin EXP de combate (Cafeína). Para cada nivel fija la EXP mínima del siguiente
        /// y reutiliza <see cref="GainExp"/>, así los PS y los movimientos se tratan igual que en el
        /// combate. (propuesta) La Cafeína aún no existe como objeto en los datos del prototipo.
        /// </summary>
        public static ExpGainResult LevelUp(TeamMember member, int levels = 1)
        {
            var result = new ExpGainResult();
            if (member == null || levels <= 0)
                return result;

            result.OldLevel = member.level;
            result.OldExp = member.exp;

            for (int i = 0; i < levels && member.level < PokemonStats.MaxLevel; i++)
            {
                int threshold = PokemonStats.ExperienceForLevel(member.level + 1);
                if (member.exp < threshold)
                    member.exp = threshold;

                ExpGainResult step = GainExp(member, 0);
                result.LevelUps.AddRange(step.LevelUps);
                result.ExpGained += step.ExpGained;
            }

            result.NewLevel = member.level;
            result.NewExp = member.exp;
            return result;
        }

        /// <summary>Materializa el equipo completo de un entrenador o jefe.</summary>
        public static List<TeamMember> CreateTeam(TrainerData trainer)
        {
            var team = new List<TeamMember>();
            if (trainer == null || trainer.team == null)
                return team;

            foreach (TrainerPokemon pokemon in trainer.team)
                team.Add(Create(pokemon));

            return team;
        }

        /// <summary>
        /// Repara los campos derivados de un Pokémon cargado de una partida:
        /// <list type="bullet">
        /// <item>PS máximos, EXP mínima del nivel y movimientos vacíos u obsoletos.</item>
        /// <item>PP máximos desde <see cref="MoveData"/> y PP actuales recortados.</item>
        /// <item>El centinela <see cref="TeamMember.FullHp"/> y los PS por encima del máximo pasan al máximo.</item>
        /// <item>Los PS 0 (debilitado) se conservan.</item>
        /// </list>
        /// Es idempotente: aplicarlo dos veces deja el mismo resultado.
        /// </summary>
        public static void RecalculateOnLoad(TeamMember member)
        {
            if (member == null || string.IsNullOrEmpty(member.speciesId))
                return;

            PokemonData species = PokemonDatabase.Get(member.speciesId);
            if (species == null)
            {
                Debug.LogWarning($"No se pudo recalcular '{member.speciesId}': falta su asset de especie.");
                return;
            }

            member.level = Math.Clamp(member.level, PokemonStats.MinLevel, PokemonStats.MaxLevel);
            member.maxHp = PokemonStats.MaxHp(species.baseHp, member.level);
            member.exp = Math.Max(member.exp, PokemonStats.ExperienceForLevel(member.level));

            NormalizeMoves(member, species);

            // El centinela (cualquier negativo) significa "sin daño"; 0 es debilitado y se respeta.
            if (member.hp < 0 || member.hp > member.maxHp)
                member.hp = member.maxHp;
        }

        /// <summary>
        /// Learnset Gen 3: los cuatro movimientos de mayor nivel que no superen el nivel actual.
        /// A igual nivel manda el orden de la tabla. Sin duplicados.
        /// Público para que el combate materialice los movimientos de un rival salvaje.
        /// </summary>
        public static List<string> LearnsetMoves(PokemonData species, int level)
        {
            var candidates = new List<(int level, int order, string moveId)>();
            if (species.learnset != null)
            {
                for (int i = 0; i < species.learnset.Count; i++)
                {
                    LearnsetEntry entry = species.learnset[i];
                    if (entry != null && entry.level <= level && !string.IsNullOrEmpty(entry.moveId))
                        candidates.Add((entry.level, i, entry.moveId));
                }
            }

            candidates.Sort((a, b) =>
            {
                int byLevel = b.level.CompareTo(a.level);
                return byLevel != 0 ? byLevel : a.order.CompareTo(b.order);
            });

            var moves = new List<string>(MaxMoves);
            var seen = new HashSet<string>();
            foreach ((int _, int _, string moveId) in candidates)
            {
                if (!seen.Add(moveId))
                    continue;

                moves.Add(moveId);
                if (moves.Count == MaxMoves)
                    break;
            }

            return moves;
        }

        /// <summary>Valida los movimientos fijos de un entrenador (sin vacíos, repetidos ni inexistentes).</summary>
        static List<string> ValidateFixedMoves(IReadOnlyList<string> fixedMoves, string speciesId)
        {
            var moves = new List<string>(fixedMoves.Count);
            var seen = new HashSet<string>();
            foreach (string moveId in fixedMoves)
            {
                if (string.IsNullOrEmpty(moveId))
                    throw new InvalidOperationException($"'{speciesId}' tiene un movimiento vacío en su equipo fijo.");

                if (!seen.Add(moveId))
                    throw new InvalidOperationException($"'{speciesId}' repite el movimiento '{moveId}'.");

                if (!MoveDatabase.Exists(moveId))
                    throw new InvalidOperationException($"'{speciesId}' usa el movimiento '{moveId}', que no existe.");

                moves.Add(moveId);
            }

            if (moves.Count > MaxMoves)
                throw new InvalidOperationException(
                    $"'{speciesId}' tiene {moves.Count} movimientos fijos; el máximo es {MaxMoves}.");

            return moves;
        }

        static TeamMember Create(string speciesId, int level, IReadOnlyList<string> fixedMoves)
        {
            if (string.IsNullOrEmpty(speciesId))
                throw new ArgumentException("La especie no puede estar vacía.", nameof(speciesId));

            if (level < PokemonStats.MinLevel || level > PokemonStats.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level),
                    $"El nivel {level} está fuera del rango {PokemonStats.MinLevel}–{PokemonStats.MaxLevel}.");

            PokemonData species = PokemonDatabase.Get(speciesId);
            if (species == null)
                throw new ArgumentException($"No existe la especie '{speciesId}'.", nameof(speciesId));

            // El llamador pasa la especie final: una pre-evolución no puede llegar al nivel de evolución.
            if (!string.IsNullOrEmpty(species.evolvesTo) && species.evolveLevel > 0 && level >= species.evolveLevel)
                throw new InvalidOperationException(
                    $"'{speciesId}' no puede crearse al nivel {level}: evoluciona a '{species.evolvesTo}' " +
                    $"al nivel {species.evolveLevel}.");

            List<string> moveIds = fixedMoves != null && fixedMoves.Count > 0
                ? ValidateFixedMoves(fixedMoves, speciesId)
                : LearnsetMoves(species, level);

            if (moveIds.Count == 0)
                throw new InvalidOperationException(
                    $"'{speciesId}' no tiene movimientos al nivel {level}: revisa su learnset.");

            var member = new TeamMember
            {
                speciesId = speciesId,
                level = level,
                exp = PokemonStats.ExperienceForLevel(level),
                maxHp = PokemonStats.MaxHp(species.baseHp, level),
                moves = new List<MoveSlot>(moveIds.Count),
            };
            member.hp = member.maxHp;

            foreach (string moveId in moveIds)
            {
                MoveData move = MoveDatabase.Get(moveId);
                if (move == null)
                    throw new InvalidOperationException(
                        $"'{speciesId}' usa el movimiento '{moveId}', que no existe en MoveDatabase.");

                member.moves.Add(new MoveSlot
                {
                    moveId = moveId,
                    pp = move.maxPp,
                    maxPp = move.maxPp,
                });
            }

            return member;
        }

        /// <summary>
        /// Aprende los movimientos del learnset que corresponden exactamente al nivel alcanzado. Si ya
        /// hay cuatro, el movimiento queda en <see cref="LevelUpEvent.MissedMoves"/>: el menú para
        /// olvidar uno se aplaza hasta que lo necesiten los Apuntes.
        /// </summary>
        static void LearnMovesAtLevel(TeamMember member, PokemonData species, LevelUpEvent levelUp)
        {
            if (species.learnset == null)
                return;

            var known = new HashSet<string>();
            foreach (MoveSlot slot in member.moves)
                if (slot != null && !string.IsNullOrEmpty(slot.moveId))
                    known.Add(slot.moveId);

            foreach (LearnsetEntry entry in species.learnset)
            {
                if (entry == null || entry.level != levelUp.Level || string.IsNullOrEmpty(entry.moveId))
                    continue;

                if (!known.Add(entry.moveId))
                    continue;

                MoveData move = MoveDatabase.Get(entry.moveId);
                if (move == null)
                    continue;

                if (member.moves.Count >= MaxMoves)
                {
                    levelUp.MissedMoves.Add(entry.moveId);
                    continue;
                }

                member.moves.Add(new MoveSlot
                {
                    moveId = entry.moveId,
                    pp = move.maxPp,
                    maxPp = move.maxPp,
                });
                levelUp.LearnedMoves.Add(entry.moveId);
            }
        }

        /// <summary>
        /// Limpia movimientos nulos/inexistentes o repetidos y recorta la lista a cuatro. Si queda vacía,
        /// la completa con el learnset. Después recalcula los PP máximos y recorta los actuales.
        /// </summary>
        static void NormalizeMoves(TeamMember member, PokemonData species)
        {
            if (member.moves == null)
                member.moves = new List<MoveSlot>();

            var cleaned = new List<MoveSlot>(MaxMoves);
            var seen = new HashSet<string>();
            foreach (MoveSlot slot in member.moves)
            {
                if (slot == null || string.IsNullOrEmpty(slot.moveId) || !MoveDatabase.Exists(slot.moveId))
                    continue;

                if (!seen.Add(slot.moveId))
                    continue;

                cleaned.Add(slot);
                if (cleaned.Count == MaxMoves)
                    break;
            }

            member.moves = cleaned;

            if (member.moves.Count == 0)
            {
                foreach (string moveId in LearnsetMoves(species, member.level))
                {
                    MoveData move = MoveDatabase.Get(moveId);
                    if (move == null)
                        continue;

                    member.moves.Add(new MoveSlot
                    {
                        moveId = moveId,
                        pp = move.maxPp,
                        maxPp = move.maxPp,
                    });
                }

                if (member.moves.Count == 0)
                    Debug.LogWarning($"No se pudo completar el learnset de '{member.speciesId}' nivel {member.level}.");
                return;
            }

            foreach (MoveSlot slot in member.moves)
            {
                MoveData move = MoveDatabase.Get(slot.moveId);
                slot.maxPp = move.maxPp;

                // 0 PP es un movimiento agotado y se respeta; solo se corrige lo fuera de rango.
                if (slot.pp < 0 || slot.pp > slot.maxPp)
                    slot.pp = slot.maxPp;
            }
        }
    }
}
