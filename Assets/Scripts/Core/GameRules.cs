using System;
using System.Globalization;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Única fuente de los números del documento de diseño (Doc §7 y §8): equipo, sellos, encuentros,
    /// entrenadores, jefes, combate, nota final y objetos. Sin estado mutable: solo constantes y
    /// funciones puras, así que no hace falta reiniciarlo al entrar en Play.
    ///
    /// Las funciones de dificultad trabajan sobre k (posición en la secuencia de retos; ver
    /// <see cref="Building.SequenceIndex"/>), y Building expone la consulta ya resuelta por piso.
    /// Ninguna otra clase debe volver a escribir estos números.
    /// </summary>
    public static class GameRules
    {
        // ---------- Equipo y progreso (Doc §7, §8) ----------

        /// <summary>Pokémon máximos en el equipo.</summary>
        public const int MaxTeamSize = 6;

        /// <summary>Sellos del juego: uno por jefe; el último lo da Montoya.</summary>
        public const int TotalSeals = 13;

        /// <summary>Derrotas seguidas contra el mismo jefe para que Gabriel dé una pista y un Agua de panela.</summary>
        public const int DefeatsForHint = 3;

        // ---------- Encuentros (Doc §7) ----------

        /// <summary>Probabilidad de encuentro por casilla en jardineras y zonas de encuentro de las salas-laboratorio.</summary>
        public const float EncounterChance = 0.10f;

        /// <summary>Niveles salvajes de la plazoleta (pisos sin rango propio).</summary>
        public const int PlazaWildMinLevel = 2;
        public const int PlazaWildMaxLevel = 5;

        /// <summary>IA salvaje: probabilidad de atacar en vez de usar otro movimiento.</summary>
        public const float WildAttackChance = 0.70f;

        /// <summary>Un salvaje intenta huir si le queda menos de este porcentaje de PS.</summary>
        public const float WildFleeHpThreshold = 0.25f;
        public const float WildFleeChance = 0.10f;

        // ---------- Entrenadores y jefes (Doc §7, §8) ----------

        /// <summary>Casillas de visión de un entrenador, en línea recta.</summary>
        public const int TrainerSightRange = 3;

        /// <summary>Probabilidad de que un entrenador elija su mejor movimiento.</summary>
        public const float BestMoveChance = 0.60f;

        /// <summary>Umbral de PS por debajo del cual el entrenador o el jefe curan a su Pokémon.</summary>
        public const float TrainerHealThreshold = 0.25f;
        public const float BossHealThreshold = 0.30f;

        /// <summary>El jefe va 4 niveles por encima del salvaje más alto de su piso.</summary>
        public const int BossLevelBonus = 4;

        /// <summary>Equipo final de Montoya: el documento dice «4 Pokémon, niveles 46 a 50» y se fijan 46, 48, 49 y 50.</summary>
        public static readonly int[] MontoyaLevels = { 46, 48, 49, 50 };

        // ---------- Combate (Doc §7, §8) ----------

        /// <summary>Aleatorio que multiplica el daño: entre 0,85 y 1,0.</summary>
        public const float DamageRollMin = 0.85f;
        public const float DamageRollMax = 1.0f;

        /// <summary>Mismo tipo que el atacante: ×1,5 (STAB).</summary>
        public const float StabMultiplier = 1.5f;

        /// <summary>
        /// (propuesta) Golpe crítico: ×2 y 1 de cada 16 ataques (Gen 3). El Doc §7 no lo menciona;
        /// se deja apagado por defecto y el combate decide si lo tira (ver <see cref="Damage"/>).
        /// </summary>
        public const float CriticalMultiplier = 2f;
        public const int CriticalDenominator = 16;

        /// <summary>La probabilidad de captura nunca sale de este rango.</summary>
        public const float CaptureChanceMin = 0.05f;
        public const float CaptureChanceMax = 0.95f;

        /// <summary>Obediencia: el equipo obedece hasta 10 + 5 × sellos; por encima hay 30 % de desobedecer.</summary>
        public const int ObedienceBase = 10;
        public const int ObediencePerSeal = 5;
        public const float DisobedienceChance = 0.30f;

        /// <summary>Experiencia: base × nivel rival / 7.</summary>
        public const int ExpDivisor = 7;

        /// <summary>Créditos por vencer a un rival: 10 × su nivel; al desmayarse se pierde el 25 %.</summary>
        public const int CreditsPerLevel = 10;
        public const float FaintedCreditsLoss = 0.25f;

        // ---------- Nota del jefe (Doc §8) ----------

        /// <summary>La nota parte de 5,0, baja 0,1 por turno sobre 6 y 0,4 por Pokémon debilitado, con mínimo 3,0.</summary>
        public const float GradeBase = 5.0f;
        public const int GradeFreeTurns = 6;
        public const float GradePerTurnOver = -0.1f;
        public const float GradePerFaintedPokemon = -0.4f;
        public const float GradeMin = 3.0f;

        // ---------- Objetos (Doc §8) ----------

        public const int TintoHeal = 20;
        public const int PanelaWaterHeal = 50;

        // ---------- Dificultad según k (Doc §7, §8) ----------

        /// <summary>Nivel máximo que obedece el equipo con tantos sellos: 10 + 5 × sellos.</summary>
        public static int ObedienceLevelCap(int seals) => ObedienceBase + ObediencePerSeal * seals;

        /// <summary>Nivel salvaje mínimo del piso con posición k: 3 + 3k.</summary>
        public static int WildMinLevel(int k) => 3 + 3 * k;

        /// <summary>Nivel salvaje máximo del piso con posición k: 6 + 3k.</summary>
        public static int WildMaxLevel(int k) => 6 + 3 * k;

        /// <summary>Nivel del jefe: salvaje máximo de su piso + 4.</summary>
        public static int BossLevel(int k) => WildMaxLevel(k) + BossLevelBonus;

        /// <summary>
        /// Pokémon del jefe según k: 2 en el piso 3 (k = 0), 3 en los pisos 4 a 7 y PT (k = 1 a 5)
        /// y 4 desde el piso 8 (k ≥ 6).
        /// </summary>
        public static int BossTeamSize(int k) => k <= 0 ? 2 : k <= 5 ? 3 : 4;

        // ---------- Fórmulas auxiliares ----------

        /// <summary>Nota del combate con las penalizaciones del documento, sin bajar nunca de 3,0.</summary>
        public static float Grade(int turns, int faintedPokemon)
        {
            float grade = GradeBase
                + GradePerTurnOver * Math.Max(0, turns - GradeFreeTurns)
                + GradePerFaintedPokemon * faintedPokemon;
            return Math.Max(GradeMin, grade);
        }

        /// <summary>Nota con coma decimal, como se muestra al jugador (4,6 / 5,0).</summary>
        public static string FormatGrade(float grade)
        {
            try
            {
                return grade.ToString("0.0", CultureInfo.GetCultureInfo("es-CO"));
            }
            catch (CultureNotFoundException)
            {
                // Si el runtime no trae los datos de cultura (builds recortados), se formatea a mano.
                return grade.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            }
        }

        /// <summary>Experiencia que da un rival: base del Pokémon vencido × su nivel / 7.</summary>
        public static int ExpReward(int baseExp, int defeatedLevel) => baseExp * defeatedLevel / ExpDivisor;

        /// <summary>Créditos que da un rival.</summary>
        public static int CreditsReward(int defeatedLevel) => CreditsPerLevel * defeatedLevel;

        /// <summary>Créditos que se pierden al desmayarse.</summary>
        public static int CreditsLost(int credits) => (int)(credits * FaintedCreditsLoss);

        // ---------- Estados y huida (propuesta: el Doc §7 no los detalla) ----------

        /// <summary>Multiplicador de precisión por nivel de estadística (Gen 3): +n → (3+n)/3.</summary>
        public static float AccuracyStageMultiplier(int stage)
        {
            int clamped = Math.Clamp(stage, -6, 6);
            return clamped >= 0 ? (3f + clamped) / 3f : 3f / (3f - clamped);
        }

        /// <summary>Daño por veneno al final de cada turno: PS máximos / 8.</summary>
        public const int PoisonDamageDivisor = 8;

        /// <summary>Daño por quemadura al final de cada turno: PS máximos / 16.</summary>
        public const int BurnDamageDivisor = 16;

        /// <summary>Un Pokémon paralizado no se mueve con esta probabilidad.</summary>
        public const float ParalysisSkipChance = 0.25f;

        /// <summary>Turnos que dura el sueño (ambos inclusive).</summary>
        public const int SleepMinTurns = 1;
        public const int SleepMaxTurns = 3;

        /// <summary>Un congelado se descongela con esta probabilidad por turno.</summary>
        public const float FreezeThawChance = 0.20f;

        /// <summary>
        /// Probabilidad base de huir de un combate salvaje con la misma Velocidad. Escala con la ventaja
        /// de Velocidad y se recorta al rango [0,2, 0,95] **(propuesta)**: el Doc §7 solo dice que se
        /// puede huir de un salvaje y que de un entrenador o jefe no.
        /// </summary>
        public const float FleeBaseChance = 0.5f;
        public const float FleeMinChance = 0.2f;
        public const float FleeMaxChance = 0.95f;

        /// <summary>Probabilidad de huir según la Velocidad de ambos; ver <see cref="FleeBaseChance"/>.</summary>
        public static float FleeChance(int playerSpeed, int enemySpeed)
        {
            if (enemySpeed <= 0)
                return FleeMaxChance;

            float ratio = (float)playerSpeed / enemySpeed;
            return Math.Clamp(FleeBaseChance * ratio, FleeMinChance, FleeMaxChance);
        }

        /// <summary>Probabilidad de captura recortada al rango del documento (5 %–95 %).</summary>
        public static float ClampCaptureChance(float chance) =>
            Math.Clamp(chance, CaptureChanceMin, CaptureChanceMax);

        /// <summary>
        /// Daño de un golpe (fórmula Gen 3). El poder 0 devuelve 0. El daño nunca baja de 1.
        /// La efectividad, el STAB, el crítico y la variación aleatoria se aplican después de la base.
        /// El crítico es (propuesta) y se inyecta desde fuera: por defecto está apagado, así las pruebas
        /// pueden fijar valores sin depender del azar.
        /// </summary>
        public static int Damage(int level, int power, int attack, int defense, float effectiveness,
            bool stab, float roll, bool critical = false)
        {
            if (power <= 0 || defense <= 0)
                return 0;

            int baseDamage = (2 * level / 5 + 2) * power * attack / defense / 50 + 2;
            float damage = baseDamage
                * effectiveness
                * (stab ? StabMultiplier : 1f)
                * (critical ? CriticalMultiplier : 1f)
                * roll;

            return Math.Max(1, (int)damage);
        }

        /// <summary>
        /// Probabilidad de captura con la fórmula de la franquicia: ratio, PS restantes y bonus de bola,
        /// recortada al rango del documento. <paramref name="catchRate"/> viene en la escala oficial
        /// 1–255 de <see cref="PokemonData.catchRate"/>; la división entre 255 es <b>(propuesta)</b>,
        /// porque el Doc §7 no detalla la normalización. El bonus por estado también es (propuesta)
        /// —el Doc §7 no lo menciona— y viene en 1 por defecto (sin estado); el combate lo sube a 1,5
        /// con parálisis/quemadura/envenenamiento o a 2 con sueño/congelación si se decide usarlo.
        /// </summary>
        public static float CaptureChance(int maxHp, int hp, int catchRate, float ballBonus = 1f, float statusBonus = 1f)
        {
            if (maxHp <= 0)
                return CaptureChanceMin;

            float hpFactor = (3f * maxHp - 2f * hp) / (3f * maxHp);
            float chance = hpFactor * catchRate * ballBonus * statusBonus / 255f;
            return ClampCaptureChance(chance);
        }
    }
}
