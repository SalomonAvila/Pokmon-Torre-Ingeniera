using System;
using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Estado de la partida en curso: los datos guardados y el piso donde está el jugador.
    /// Si se prueba una escena directamente (sin pasar por el menú), carga la partida guardada o crea una vacía.
    ///
    /// Todo lo persistente vive en <see cref="SaveData"/>: su valor inicial son los inicializadores de los campos
    /// y su reinicio es el <c>save = null</c> de ResetStatics, que vuelve a crear el objeto. Aquí solo se añaden
    /// las consultas y mutaciones que validan las reglas (límite del equipo, rachas de jefe, Pokédex...).
    /// </summary>
    public static class GameState
    {
        static SaveData save;

        public static SaveData Save
        {
            get
            {
                if (save == null)
                    save = SaveSystem.TryLoad(out SaveData loaded) ? loaded : new SaveData();
                return save;
            }
            set => save = value;
        }

        /// <summary>Piso donde está el jugador (lo actualiza FloorInfo al entrar a cada piso).</summary>
        public static string CurrentFloor { get; set; } = Building.Floors[0];

        /// <summary>Se invoca cuando cambia el progreso (por ejemplo, al completar un piso).</summary>
        public static event Action ProgressChanged;

        /// <summary>Se invoca cuando cambia cualquier dato de la partida: sellos, equipo, Pokédex, banderas...</summary>
        public static event Action StateChanged;

        // El proyecto entra a Play sin recargar el dominio: los valores estáticos se reinician a mano.
        // Los campos nuevos de SaveData no necesitan nada aquí: al quedar save = null se crean con sus valores iniciales.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            save = null;
            CurrentFloor = Building.Floors[0];
            ProgressChanged = null;
            StateChanged = null;
        }

        /// <summary>Guarda la partida y avisa a quien escuche tras un cambio persistente.</summary>
        static void Commit()
        {
            SaveSystem.Save(Save);
            StateChanged?.Invoke();
        }

        // ---------- Progreso de pisos ----------

        /// <summary>
        /// Un piso está completado cuando se vence a su jefe. Los pisos sin reto (1 y 2) cuentan como completados,
        /// así que la escalera del 2 al 3 siempre está abierta; desde el 3 hay que completar cada piso para subir.
        /// </summary>
        public static bool IsFloorCompleted(string floor) =>
            !Building.HasChallenge(floor) || Save.completedFloors.Contains(floor);

        public static void CompleteFloor(string floor)
        {
            if (!Save.completedFloors.Contains(floor))
                Save.completedFloors.Add(floor);
            ResetBossLossStreak();
            SaveSystem.Save(Save);
            ProgressChanged?.Invoke();
            StateChanged?.Invoke();
        }

        public static void ResetProgress()
        {
            Save.completedFloors.Clear();
            SaveSystem.Save(Save);
            ProgressChanged?.Invoke();
            StateChanged?.Invoke();
        }

        /// <summary>Piso visitado = al que el jugador ya llegó; habilita los ascensores, no la escalera.</summary>
        public static bool IsFloorVisited(string floor) => Save.visitedFloors.Contains(floor);

        /// <summary>Marca el piso como visitado y guarda; lo llama FloorInfo al cargar cada escena de piso.</summary>
        public static void MarkFloorVisited(string floor)
        {
            if (!Building.IsValid(floor) || Save.visitedFloors.Contains(floor))
                return;
            Save.visitedFloors.Add(floor);
            Commit();
        }

        // ---------- Sellos y notas de jefes ----------

        public static int SealCount => Save.sealIds.Count;

        public static bool HasSeal(string sealId) => Save.sealIds.Contains(sealId);

        /// <summary>Otorga el sello de un jefe (su id es el del piso).</summary>
        public static void GrantSeal(string sealId)
        {
            if (string.IsNullOrEmpty(sealId) || Save.sealIds.Contains(sealId))
                return;
            Save.sealIds.Add(sealId);
            Commit();
        }

        /// <summary>Registra la nota de un jefe; si ya había nota de ese piso, la reemplaza.</summary>
        public static void RegisterBossNote(string floor, float note)
        {
            if (string.IsNullOrEmpty(floor))
                return;

            FloorNote entry = Save.bossNotes.Find(n => n.floor == floor);
            if (entry == null)
                Save.bossNotes.Add(new FloorNote { floor = floor, note = note });
            else
                entry.note = note;
            Commit();
        }

        /// <summary>Promedio de las notas de jefes, o 0 si todavía no hay ninguna.</summary>
        public static float AverageBossNote()
        {
            if (Save.bossNotes.Count == 0)
                return 0f;

            float total = 0f;
            foreach (FloorNote note in Save.bossNotes)
                total += note.note;
            return total / Save.bossNotes.Count;
        }

        /// <summary>Promedio de notas con coma decimal, listo para mostrar.</summary>
        public static string FormattedAverageNote() => GameRules.FormatGrade(AverageBossNote());

        // ---------- Equipo e inventario ----------

        public static int TeamCount => Save.team.Count;

        public static bool CanAddToTeam => Save.team.Count < GameRules.MaxTeamSize;

        /// <summary>Añade un Pokémon al equipo respetando el máximo del documento; devuelve si cupo.</summary>
        public static bool TryAddToTeam(TeamMember member)
        {
            if (member == null || string.IsNullOrEmpty(member.speciesId) || !CanAddToTeam)
                return false;

            Save.team.Add(member);
            Commit();
            return true;
        }

        public static int CountItem(string id)
        {
            InventoryItem item = Save.inventory.Find(i => i.id == id);
            return item != null ? item.amount : 0;
        }

        /// <summary>Suma (o resta, con cantidad negativa) un objeto; si la cantidad llega a 0, se saca de la mochila.</summary>
        public static void AddItem(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount == 0)
                return;

            InventoryItem item = Save.inventory.Find(i => i.id == id);
            if (item == null)
                Save.inventory.Add(new InventoryItem { id = id, amount = amount });
            else
                item.amount += amount;

            Save.inventory.RemoveAll(i => i.amount <= 0);
            Commit();
        }

        // ---------- Pokédex ----------

        public static int CaughtSpeciesCount => Save.pokedex.Count;

        public static bool HasCaught(string speciesId) => Save.pokedex.Contains(speciesId);

        /// <summary>Especies que aparecen en el piso y el jugador ya capturó (las listas salen de SpeciesCatalog).</summary>
        public static IReadOnlyList<string> CaughtSpeciesOnFloor(string floor)
        {
            var caught = new List<string>();
            foreach (string species in SpeciesCatalog.OnFloor(floor))
                if (HasCaught(species))
                    caught.Add(species);
            return caught;
        }

        /// <summary>Capturadas y total de especies del piso; (0, 0) si el piso no tiene encuentros.</summary>
        public static (int caught, int total) FloorPokedexProgress(string floor) =>
            (CaughtSpeciesOnFloor(floor).Count, SpeciesCatalog.OnFloor(floor).Count);

        /// <summary>Porcentaje del Pokédex del piso (0 a 100); 0 si el piso no tiene especies.</summary>
        public static int FloorPokedexPercent(string floor)
        {
            (int caught, int total) = FloorPokedexProgress(floor);
            return total == 0 ? 0 : Mathf.RoundToInt(100f * caught / total);
        }

        /// <summary>Registra una captura en el Pokédex total; el reparto por piso se deriva de SpeciesCatalog.</summary>
        public static void RegisterCapture(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId) || HasCaught(speciesId))
                return;

            Save.pokedex.Add(speciesId);
            Commit();
        }

        // ---------- Banderas de historia ----------

        public static void SetStarter(string speciesId)
        {
            Save.starterId = speciesId;
            Commit();
        }

        public static void SetGabrielChoice(GabrielChoice choice)
        {
            Save.gabrielChoice = choice;
            Commit();
        }

        /// <summary>Marca el combate tutorial con Gabriel como superado (no abre la puerta del edificio).</summary>
        public static void CompleteTutorial()
        {
            if (Save.tutorialDone)
                return;
            Save.tutorialDone = true;
            Commit();
        }

        /// <summary>Tutorial superado (combate con Gabriel en el lobby); todavía no lo consume nadie.</summary>
        public static bool TutorialDone => Save.tutorialDone;

        /// <summary>
        /// El Vigilante Gómez deja entrar al edificio con el inicial elegido y al menos un Pokémon capturado
        /// (Doc §8). La puerta Atrio ↔ Ascensores consulta esta condición.
        /// </summary>
        public static bool PassesBuildingDoor =>
            !string.IsNullOrEmpty(Save.starterId) && Save.pokedex.Count > 0;

        /// <summary>La Ing. Cantera entrega el Carné de acceso que activa los ascensores del sur.</summary>
        public static void GrantAccessCard()
        {
            if (Save.accessCard)
                return;
            Save.accessCard = true;
            Commit();
        }

        // ---------- Entrenadores e ítems del mundo ----------

        public static bool HasDefeatedTrainer(string trainerId) => Save.defeatedTrainers.Contains(trainerId);

        public static void DefeatTrainer(string trainerId)
        {
            if (string.IsNullOrEmpty(trainerId) || Save.defeatedTrainers.Contains(trainerId))
                return;
            Save.defeatedTrainers.Add(trainerId);
            Commit();
        }

        public static bool HasCollectedItem(string itemId) => Save.collectedItems.Contains(itemId);

        public static void CollectItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Save.collectedItems.Contains(itemId))
                return;
            Save.collectedItems.Add(itemId);
            Commit();
        }

        // ---------- Rachas contra un jefe ----------

        /// <summary>Derrotas seguidas del jugador contra ese jefe (0 si la racha es de otro piso).</summary>
        public static int BossLossStreak(string floor) =>
            Save.lastBossFloor == floor ? Save.consecutiveBossLosses : 0;

        /// <summary>Suma una derrota del jugador contra el jefe; cambiar de jefe reinicia la racha.</summary>
        public static void RegisterLossToBoss(string floor)
        {
            if (string.IsNullOrEmpty(floor))
                return;

            Save.consecutiveBossLosses = Save.lastBossFloor == floor ? Save.consecutiveBossLosses + 1 : 1;
            Save.lastBossFloor = floor;
            Commit();
        }

        /// <summary>Vencer al jefe corta la racha: Gabriel deja de ofrecer la pista.</summary>
        static void ResetBossLossStreak()
        {
            if (string.IsNullOrEmpty(Save.lastBossFloor) && Save.consecutiveBossLosses == 0)
                return;
            Save.lastBossFloor = "";
            Save.consecutiveBossLosses = 0;
        }
    }
}
