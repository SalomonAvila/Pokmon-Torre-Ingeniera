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
                    save = SaveSystem.TryLoad(out SaveData loaded)
                        ? loaded
                        : new SaveData { playerId = SaveSystem.NewPlayerId() };
                return save;
            }
            set => save = value;
        }

        /// <summary>
        /// ¿Ya hay una partida en memoria? A diferencia de <see cref="Save"/>, no la carga ni la crea:
        /// lo usa el reloj de juego para no tocar el disco desde la pantalla de título.
        /// </summary>
        public static bool IsLoaded => save != null;

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

        /// <summary>
        /// Guarda la partida a petición del jugador (menú de pausa o mostrador de Don Chucho). Es el
        /// mismo archivo que el guardado automático; existe para que las pantallas no llamen a SaveSystem.
        /// </summary>
        public static void SaveNow() => Commit();

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

        /// <summary>
        /// Borra el progreso del edificio para probar desde cero: pisos completados, sellos, notas de jefes,
        /// Carné de acceso y racha de derrotas contra jefes (para que Gabriel no ofrezca ayuda por un jefe
        /// que ya no existe). No toca equipo, Pokédex, inventario, inicial elegido ni banderas de historia:
        /// es una herramienta de pruebas, no un "empezar partida nueva".
        /// </summary>
        public static void ResetProgress()
        {
            Save.completedFloors.Clear();
            Save.sealIds.Clear();
            Save.bossNotes.Clear();
            Save.accessCard = false;
            ResetBossLossStreak();
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

        /// <summary>
        /// Guarda el piso actual en <see cref="SaveData.location"/> para que «Continuar» cargue la
        /// escena correcta. Lo llama FloorInfo al cargar cada escena de piso.
        /// </summary>
        public static void SetLocation(string floor)
        {
            if (!Building.IsValid(floor) || Save.location == floor)
                return;
            Save.location = floor;
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

        /// <summary>
        /// Guarda y avisa cuando el combate ha modificado PS o PP del equipo. El combate muta los
        /// <see cref="TeamMember"/> de <see cref="Save"/> y llama aquí al terminar; así el daño y los PP
        /// gastados no se pierden al volver al mundo.
        /// </summary>
        public static void NotifyTeamChanged() => Commit();

        /// <summary>
        /// Cura por completo al equipo (PS y PP) y revive a los debilitados. Es el punto de curación de la
        /// plazoleta: Gabriel lo ofrece al hablar con él, antes de que exista Don Chucho tras la puerta.
        /// Devuelve true si cambió algo.
        /// </summary>
        public static bool HealTeam()
        {
            bool healed = HealTeamSilently();
            if (healed)
                Commit();
            return healed;
        }

        /// <summary>Cura PS y PP del equipo sin guardar; devuelve si cambió algo.</summary>
        static bool HealTeamSilently()
        {
            bool healed = false;
            foreach (TeamMember member in Save.team)
            {
                if (member == null)
                    continue;

                // El centinela negativo significa "sin daño"; 0 es debilitado y sí se cura.
                if (member.hp >= 0 && member.hp < member.maxHp)
                {
                    member.hp = member.maxHp;
                    healed = true;
                }

                if (member.moves == null)
                    continue;

                foreach (MoveSlot move in member.moves)
                {
                    if (move != null && move.pp < move.maxPp)
                    {
                        move.pp = move.maxPp;
                        healed = true;
                    }
                }
            }
            return healed;
        }

        /// <summary>
        /// Crea el Pokémon capturado con sus PS, EXP, movimientos y PP, y lo suma al equipo si hay sitio.
        /// La captura siempre cuenta para el Pokédex, aunque el equipo esté lleno (el prototipo no tiene PC).
        /// Devuelve el miembro creado para que el combate pueda mostrar su ficha.
        ///
        /// (propuesta) El capturado conserva los PS actuales del salvaje. Se crea con
        /// <see cref="TeamMembers.CreateCaptured"/> (que parte de la EXP del nivel y copia los PS y los PP
        /// actuales). Los PS nunca se guardan en 0 ni por encima del máximo; si no se pasan, el Pokémon
        /// queda con los PS llenos. La captura no da EXP adicional.
        /// </summary>
        public static TeamMember AddCapturedPokemon(string speciesId, int level, int currentHp = -1,
            IReadOnlyList<int> currentPp = null)
        {
            TeamMember member = TeamMembers.CreateCaptured(speciesId, level, currentHp, currentPp);

            if (!Save.pokedex.Contains(speciesId))
                Save.pokedex.Add(speciesId);
            if (!Save.seen.Contains(speciesId))
                Save.seen.Add(speciesId);

            if (CanAddToTeam)
                Save.team.Add(member);

            Commit();
            return member;
        }

        /// <summary>Intercambia dos Pokémon del equipo (CAMBIAR en el menú Equipo). El primero sale a combatir.</summary>
        public static void SwapTeamMembers(int first, int second)
        {
            if (first == second || first < 0 || second < 0 ||
                first >= Save.team.Count || second >= Save.team.Count)
                return;

            (Save.team[first], Save.team[second]) = (Save.team[second], Save.team[first]);
            Commit();
        }

        /// <summary>
        /// Usa un objeto curativo de la mochila en un Pokémon del equipo fuera de combate. Solo gasta el
        /// objeto si cura algo (ver <see cref="TeamMembers.ApplyHeal"/>).
        /// </summary>
        public static HealResult UseHealItem(string itemId, int teamIndex, out int healed)
        {
            healed = 0;
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.category != ItemCategory.Heal || CountItem(itemId) <= 0 ||
                teamIndex < 0 || teamIndex >= Save.team.Count)
                return HealResult.NoEffect;

            HealResult result = TeamMembers.ApplyHeal(Save.team[teamIndex], item.healAmount, out healed);
            if (result == HealResult.Healed)
                AddItem(itemId, -1); // AddItem ya guarda y avisa
            return result;
        }

        /// <summary>
        /// Cafeína: sube un nivel al Pokémon y se gasta. No hace nada (ni gasta) en el nivel máximo ni
        /// con un debilitado. (propuesta) Los movimientos del nivel nuevo que no caben se pierden, igual
        /// que al subir en combate.
        /// </summary>
        public static ExpGainResult UseLevelUpItem(string itemId, int teamIndex)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.category != ItemCategory.LevelUp || CountItem(itemId) <= 0 ||
                teamIndex < 0 || teamIndex >= Save.team.Count)
                return null;

            TeamMember member = Save.team[teamIndex];
            if (member == null || TeamMembers.IsFainted(member) || member.level >= PokemonStats.MaxLevel)
                return null;

            ExpGainResult result = TeamMembers.LevelUp(member, 1);
            if (result.LeveledUp)
                AddItem(itemId, -1); // AddItem guarda y avisa
            return result;
        }

        /// <summary>
        /// Arepa: revive al debilitado con la mitad de sus PS y se gasta. (propuesta) Solo fuera de
        /// combate, desde la Mochila: el combate solo usa objetos sobre el Pokémon activo, que nunca
        /// está debilitado.
        /// </summary>
        public static bool UseReviveItem(string itemId, int teamIndex)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.category != ItemCategory.Revive || CountItem(itemId) <= 0 ||
                teamIndex < 0 || teamIndex >= Save.team.Count)
                return false;

            if (!TeamMembers.Revive(Save.team[teamIndex]))
                return false;
            AddItem(itemId, -1); // AddItem guarda y avisa
            return true;
        }

        /// <summary>
        /// Apuntes: enseñan su movimiento y se gastan si lo aprende. Con cuatro movimientos hay que
        /// pasar <paramref name="replaceIndex"/> (lo elige la Mochila).
        /// </summary>
        public static TeachResult UseTeachItem(string itemId, int teamIndex, int replaceIndex = -1)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.category != ItemCategory.Teach || CountItem(itemId) <= 0 ||
                teamIndex < 0 || teamIndex >= Save.team.Count)
                return TeachResult.Invalid;

            TeachResult result = TeamMembers.TeachMove(Save.team[teamIndex], item.moveId, replaceIndex);
            if (result == TeachResult.Learned)
                AddItem(itemId, -1);
            return result;
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

        /// <summary>Suma (o resta, con cantidad negativa) créditos y guarda. Los da el combate de entrenador.</summary>
        public static void AddCredits(int amount)
        {
            if (amount == 0)
                return;

            Save.credits = Math.Max(0, Save.credits + amount);
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
            if (!Save.seen.Contains(speciesId))
                Save.seen.Add(speciesId);
            Commit();
        }

        public static int SeenSpeciesCount => Save.seen.Count;

        /// <summary>¿La especie apareció alguna vez en combate (o se capturó o se eligió)?</summary>
        public static bool HasSeen(string speciesId) => Save.seen.Contains(speciesId) || HasCaught(speciesId);

        /// <summary>
        /// Registra una especie vista (v7). La llama el combate cada vez que entra un Pokémon rival; solo
        /// guarda si la especie es nueva.
        /// </summary>
        public static void RegisterSeen(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId) || Save.seen.Contains(speciesId))
                return;
            Save.seen.Add(speciesId);
            Commit();
        }

        // ---------- Tiempo de juego ----------

        /// <summary>
        /// Suma tiempo de juego sin guardar (sería escribir el archivo cada fotograma): el valor viaja en
        /// el siguiente guardado de cualquier cambio. Lo llama <c>PlayTimeClock</c>.
        /// </summary>
        public static void AddPlayTime(float seconds)
        {
            if (save == null || seconds <= 0f)
                return;
            save.playTimeSeconds += seconds;
        }

        /// <summary>Tiempo de juego con formato H:MM (o HHH:MM), como en el Carné de FireRed.</summary>
        public static string FormattedPlayTime() => GameRules.FormatPlayTime(Save.playTimeSeconds);

        // ---------- Servicios del lobby (Don Chucho) ----------

        /// <summary>
        /// Compra <paramref name="amount"/> unidades de un objeto con su <see cref="ItemData.price"/>.
        /// Devuelve false sin cambiar nada si el objeto no se vende o no alcanzan los créditos.
        /// </summary>
        public static bool TryBuy(string itemId, int amount)
        {
            ItemData item = ItemDatabase.Get(itemId);
            if (item == null || item.price <= 0 || amount <= 0)
                return false;

            int cost = item.price * amount;
            if (cost > Save.credits)
                return false;

            Save.credits -= cost;
            AddItem(itemId, amount); // AddItem guarda y avisa
            return true;
        }

        /// <summary>
        /// Desmayo (Doc §8): el equipo se queda sin PS. Pierde el 25 % de los créditos
        /// (<see cref="GameRules.CreditsLost"/>), el equipo se cura por completo y la partida queda en el
        /// lugar de reaparición (<see cref="FaintRespawnFloor"/>). Devuelve los créditos perdidos.
        ///
        /// (propuesta) Se cura al reaparecer, como en el Centro Pokémon de FireRed: sin eso el jugador
        /// llegaría al lobby con todo el equipo debilitado.
        /// </summary>
        public static int Faint()
        {
            int lost = GameRules.CreditsLost(Save.credits);
            Save.credits = Math.Max(0, Save.credits - lost);
            HealTeamSilently();
            Save.location = FaintRespawnFloor;
            Commit();
            return lost;
        }

        /// <summary>
        /// Piso donde se reaparece tras un desmayo: el lobby (junto a Don Chucho). (propuesta) Antes de
        /// pasar la puerta del edificio el jugador aún no puede entrar, así que reaparece en la plazoleta,
        /// junto a Gabriel.
        /// </summary>
        public static string FaintRespawnFloor => PassesBuildingDoor ? Building.Floors[1] : Building.Floors[0];

        // ---------- Banderas de historia ----------

        /// <summary>
        /// Fija el inicial elegido y lo registra como especie conocida del Pokédex (los iniciales se eligen,
        /// no se capturan, pero cuentan para el Pokédex total). La puerta del edificio no lo cuenta como captura.
        /// También lo entrega al equipo al nivel <see cref="GameRules.StarterLevel"/> si todavía no está,
        /// para que los encuentros salvajes y el combate funcionen desde el primer momento.
        /// </summary>
        public static void SetStarter(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId))
                return;

            Save.starterId = speciesId;
            if (!Save.pokedex.Contains(speciesId))
                Save.pokedex.Add(speciesId);
            if (!Save.seen.Contains(speciesId))
                Save.seen.Add(speciesId);

            // El inicial se entrega una sola vez: si ya está en el equipo (por ejemplo, al inferirlo en
            // EnsureStarterFromTeam) no se duplica.
            if (CanAddToTeam && !HasSpeciesInTeam(speciesId))
                Save.team.Add(TeamMembers.Create(speciesId, GameRules.StarterLevel));

            Commit();
        }

        /// <summary>¿El equipo ya trae un Pokémon de esa especie? (el inicial no se duplica al fijarlo).</summary>
        public static bool HasSpeciesInTeam(string speciesId)
        {
            if (Save.team == null || string.IsNullOrEmpty(speciesId))
                return false;

            foreach (TeamMember member in Save.team)
                if (member != null && member.speciesId == speciesId)
                    return true;
            return false;
        }

        public static void SetGabrielChoice(GabrielChoice choice)
        {
            Save.gabrielChoice = choice;
            Commit();
        }

        /// <summary>Marca el combate tutorial con Gabriel como superado (abre la escalera del lobby al piso 3).</summary>
        public static void CompleteTutorial()
        {
            if (Save.tutorialDone)
                return;
            Save.tutorialDone = true;
            Commit();
        }

        /// <summary>Tutorial superado (combate con Gabriel en el lobby); lo consultan la escalera, el Vigilante y Gabriel.</summary>
        public static bool TutorialDone => Save.tutorialDone;

        /// <summary>
        /// Inicial de la partida: el fijado por <see cref="SetStarter"/> o, en partidas de prueba, el
        /// primero que traiga el equipo (ver <see cref="EnsureStarterFromTeam"/>).
        /// </summary>
        public static string StarterId =>
            !string.IsNullOrEmpty(Save.starterId) ? Save.starterId : StarterInTeam();

        /// <summary>¿La partida ya tiene inicial? Lo consultan el Vigilante y el tutorial de Gabriel.</summary>
        public static bool HasStarter => !string.IsNullOrEmpty(StarterId);

        /// <summary>
        /// El Vigilante Gómez deja entrar al edificio con el inicial elegido y al menos un Pokémon capturado
        /// (Doc §8). La puerta Atrio ↔ Ascensores consulta esta condición. El inicial consta en el Pokédex
        /// desde que se elige, pero no cuenta como captura: la puerta exige una especie distinta de él.
        ///
        /// (propuesta) Mientras no exista la pantalla de elección de inicial, si la partida no lo tiene
        /// fijado pero el equipo trae un Pokémon inicial (por ejemplo, el que añaden las herramientas de
        /// prueba), se usa ese como inicial. Así una partida de prueba no queda bloqueada para siempre.
        /// </summary>
        public static bool PassesBuildingDoor
        {
            get
            {
                string starterId = StarterId;
                if (string.IsNullOrEmpty(starterId))
                    return false;

                foreach (string species in Save.pokedex)
                    if (species != starterId)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// ¿Salen salvajes en el piso actual? En la plazoleta los encuentros son la zona de tutorial: en
        /// cuanto el jugador logra la captura que abre la puerta (<see cref="PassesBuildingDoor"/>), las
        /// jardineras se calman y no vuelven a salir. En el resto de pisos siempre hay encuentros, porque
        /// alimentan el Pokédex del piso (Doc §8).
        /// </summary>
        public static bool AllowWildEncounters =>
            CurrentFloor != Building.Floors[0] || !PassesBuildingDoor;

        /// <summary>
        /// (propuesta) Fija como inicial el primer Pokémon inicial del equipo si la partida aún no tiene
        /// uno. Lo usan la carga de partida y las herramientas de prueba; no hace nada si ya hay inicial
        /// o si el equipo no trae ninguno. No se llama desde <see cref="PassesBuildingDoor"/> porque esa
        /// consulta no debe mutar la partida.
        /// </summary>
        public static void EnsureStarterFromTeam()
        {
            if (!string.IsNullOrEmpty(Save.starterId))
                return;

            string starter = StarterInTeam();
            if (!string.IsNullOrEmpty(starter))
                SetStarter(starter);
        }

        /// <summary>Primer Pokémon inicial del equipo, o <c>null</c> si no hay ninguno.</summary>
        static string StarterInTeam()
        {
            if (Save.team == null)
                return null;

            foreach (TeamMember member in Save.team)
                if (member != null && SpeciesCatalog.IsStarter(member.speciesId))
                    return member.speciesId;

            return null;
        }

        /// <summary>La Ing. Cantera entrega el Carné de acceso que activa los ascensores del sur.</summary>
        public static void GrantAccessCard()
        {
            if (Save.accessCard)
                return;
            Save.accessCard = true;
            Commit();
        }

        // ---------- Conversaciones vistas ----------

        /// <summary>¿El jugador ya vio esta versión del diálogo de ese NPC?</summary>
        public static bool HasSeenDialogue(string npcId, string key)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(key) || Save.seenDialogues == null)
                return false;

            foreach (SeenDialogue entry in Save.seenDialogues)
                if (entry != null && entry.id == npcId && entry.key == key)
                    return true;
            return false;
        }

        /// <summary>
        /// Marca como vista la conversación actual del NPC. Solo guarda una clave por NPC: al cambiar
        /// el diálogo se reemplaza la anterior, así el indicador vuelve a aparecer con la nueva.
        /// </summary>
        public static void MarkDialogueSeen(string npcId, string key)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(key))
                return;

            if (Save.seenDialogues == null)
                Save.seenDialogues = new List<SeenDialogue>();
            if (HasSeenDialogue(npcId, key))
                return;

            Save.seenDialogues.RemoveAll(entry => entry != null && entry.id == npcId);
            Save.seenDialogues.Add(new SeenDialogue { id = npcId, key = key });
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

        /// <summary>Jefe de la racha de derrotas actual, o cadena vacía si no hay racha.</summary>
        public static string BossStreakFloor => Save.lastBossFloor;

        /// <summary>
        /// ¿Gabriel debe ofrecer la pista del jefe? Solo con la racha completa (3 derrotas seguidas
        /// contra el mismo jefe), sin jefe vencido y sin haber entregado ya la ayuda de esta racha.
        /// </summary>
        public static bool BossHintAvailable =>
            !string.IsNullOrEmpty(Save.lastBossFloor)
            && Save.consecutiveBossLosses >= GameRules.DefeatsForHint
            && !Save.bossHintGiven;

        /// <summary>Marca la ayuda de la racha como entregada (el Agua de panela de Gabriel).</summary>
        public static void MarkBossHintGiven()
        {
            if (Save.bossHintGiven)
                return;
            Save.bossHintGiven = true;
            Commit();
        }

        /// <summary>Suma una derrota del jugador contra el jefe; cambiar de jefe reinicia la racha.</summary>
        public static void RegisterLossToBoss(string floor)
        {
            if (string.IsNullOrEmpty(floor))
                return;

            if (Save.lastBossFloor != floor)
            {
                Save.consecutiveBossLosses = 0;
                Save.bossHintGiven = false;
            }

            Save.consecutiveBossLosses++;
            Save.lastBossFloor = floor;
            Commit();
        }

        /// <summary>Vencer al jefe corta la racha: Gabriel deja de ofrecer la pista.</summary>
        static void ResetBossLossStreak()
        {
            if (string.IsNullOrEmpty(Save.lastBossFloor) && Save.consecutiveBossLosses == 0 && !Save.bossHintGiven)
                return;
            Save.lastBossFloor = "";
            Save.consecutiveBossLosses = 0;
            Save.bossHintGiven = false;
        }
    }
}
