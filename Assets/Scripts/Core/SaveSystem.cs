using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Decisión del jugador cuando Gabriel le ofrece ayuda (Doc §8).</summary>
    public enum GabrielChoice { Undecided, Yes, No }

    /// <summary>Nota que deja un jefe al ser vencido; su promedio se muestra en el carné.</summary>
    [Serializable]
    public class FloorNote
    {
        public string floor;
        public float note;
    }

    /// <summary>Movimiento aprendido con sus PP (v4).</summary>
    [Serializable]
    public class MoveSlot
    {
        public string moveId;
        public int pp;

        /// <summary>
        /// Caché de los PP máximos. En la Etapa 1, cuando existan los datos de movimientos, se recalculará
        /// desde MoveData al cargar y al subir de nivel (o se retirará del JSON con [NonSerialized]).
        /// </summary>
        public int maxPp;
    }

    /// <summary>
    /// Pokémon del equipo. La v4 añadió PS, EXP y movimientos; cuando exista la ficha de la especie
    /// (BattleSystem/PokemonData) se calcularán los PS máximos y los movimientos iniciales.
    /// </summary>
    [Serializable]
    public class TeamMember
    {
        /// <summary>
        /// PS actuales sin calcular: significa "PS completos". Las partidas anteriores a la v4 no guardaban
        /// daño, así que al migrar no se pueden dejar en 0 (eso sería un Pokémon debilitado).
        /// </summary>
        public const int FullHp = -1;

        public string speciesId;
        public int level;

        /// <summary>PS actuales; <see cref="FullHp"/> = sin daño (pendiente de calcular el máximo).</summary>
        public int hp = FullHp;

        /// <summary>
        /// Caché de los PS máximos. En la Etapa 1, cuando exista PokemonData, se recalculará desde
        /// la especie y el nivel al cargar y al subir de nivel (o se retirará del JSON con [NonSerialized]).
        /// </summary>
        public int maxHp;

        /// <summary>Experiencia acumulada.</summary>
        public int exp;

        /// <summary>Hasta cuatro movimientos con sus PP; vacío hasta que exista el learnset.</summary>
        public List<MoveSlot> moves = new List<MoveSlot>();
    }

    /// <summary>Objeto de la mochila con su cantidad.</summary>
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public int amount;
    }

    /// <summary>
    /// Progreso del jugador: equipo, inventario, sellos, notas de jefes, Pokédex, pisos visitados y banderas.
    /// JsonUtility no soporta Dictionary ni HashSet, así que el inventario y los conjuntos son listas.
    /// Los floats se guardan con punto decimal y se muestran con GameRules.FormatGrade (coma es-CO).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Versión actual del archivo. Al añadir campos se sube; los ausentes arrancan con su valor inicial.</summary>
        public const int CurrentVersion = 4;

        /// <summary>
        /// El 1 es el valor antiguo: si el JSON no trae 'version' (formato anterior al campo) se trata como v1
        /// y pasa por la migración. Los guardados nuevos escriben CurrentVersion (lo impone SaveSystem.Save).
        /// </summary>
        public int version = 1;
        public string playerName = "Pedro";
        /// <summary>
        /// Piso actual por id de Building.Floors ("1" = atrio, "2" = ascensores, "3"...). No guardar
        /// nombres de escena: la escena se resuelve con Building.SceneFor(location).
        /// </summary>
        public string location = "1";
        public int credits;

        /// <summary>Sellos obtenidos: un id por jefe (el id del piso). La cantidad se deriva con sealIds.Count.</summary>
        public List<string> sealIds = new List<string>();

        /// <summary>Notas de jefes, para el promedio.</summary>
        public List<FloorNote> bossNotes = new List<FloorNote>();

        /// <summary>Equipo actual (máximo GameRules.MaxTeamSize).</summary>
        public List<TeamMember> team = new List<TeamMember>();

        public List<InventoryItem> inventory = new List<InventoryItem>();

        /// <summary>Especies capturadas, sin repetir (Pokédex total). El reparto por piso se deriva de SpeciesCatalog.</summary>
        public List<string> pokedex = new List<string>();

        /// <summary>Pisos a los que el jugador ya llegó (distinto de completedFloors, que son los vencidos).</summary>
        public List<string> visitedFloors = new List<string>();

        /// <summary>Carné de acceso que da la Ing. Cantera y activa los ascensores del sur.</summary>
        public bool accessCard;

        /// <summary>Especie del inicial elegido; vacío mientras no elija.</summary>
        public string starterId = "";

        public GabrielChoice gabrielChoice = GabrielChoice.Undecided;

        /// <summary>Tutorial con Gabriel superado (lobby); no abre la puerta del edificio.</summary>
        public bool tutorialDone;

        public List<string> defeatedTrainers = new List<string>();
        public List<string> collectedItems = new List<string>();

        /// <summary>Jefe de la racha de derrotas actual.</summary>
        public string lastBossFloor = "";

        /// <summary>Derrotas seguidas del jugador contra ese jefe.</summary>
        public int consecutiveBossLosses;

        /// <summary>Pisos completados (ids de Building.Floors); habilitan la escalera hacia el piso siguiente.</summary>
        public List<string> completedFloors = new List<string>();

        public string createdAt;
        public string savedAt;
    }

    /// <summary>
    /// Guardado en un archivo JSON local (Application.persistentDataPath/partida.json).
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "partida.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(SavePath);

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            if (!HasSave())
                return false;

            try
            {
                data = ParseJson(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"No se pudo leer la partida guardada ({SavePath}): {e.Message}");
            }
            return data != null;
        }

        /// <summary>
        /// Lee un JSON de partida y lo migra a la versión actual. Es tolerante con los campos que faltan
        /// (toman el valor inicial del campo) y con las claves viejas (se ignoran). Lanza si el texto no es
        /// un JSON legible; TryLoad es quien convierte ese fallo en "no hay partida".
        /// </summary>
        public static SaveData ParseJson(string json)
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
                Migrate(data);
            return data;
        }

        /// <summary>
        /// Adapta una partida guardada a la versión actual del formato.
        /// v1 → v2: se añadieron equipo, inventario, sellos por id, notas de jefes, Pokédex, pisos visitados
        /// y banderas. v2 → v3: se retiró pokedexByFloor (el Pokédex por piso se deriva de SpeciesCatalog).
        /// v3 → v4: el equipo guarda PS, EXP y movimientos con PP.
        /// JsonUtility deja los campos nuevos con su valor inicial e ignora las claves viejas que ya no existen;
        /// la v4 necesita además el centinela de PS para no dejar debilitados a los Pokémon de partidas viejas.
        /// </summary>
        static void Migrate(SaveData data)
        {
            // Convención de 'location': id de piso. Las partidas antiguas guardaban el nombre de la escena.
            // La conversión es idempotente y no depende de la versión del formato.
            MigrateLocation(data);

            if (data.version > SaveData.CurrentVersion)
            {
                Debug.LogWarning($"La partida guardada es de la versión {data.version} y esta build soporta la " +
                                 $"{SaveData.CurrentVersion}: se carga igual, pero al guardar se perderá lo que no conozca.");
                return;
            }

            if (data.version < 1)
                data.version = 1; // Formato anterior a que existiera el campo 'version'.

            NormalizeLists(data);

            if (data.version < 4)
                MigrateTeamToV4(data);
            else
                NormalizeTeam(data);

            data.version = SaveData.CurrentVersion;
        }

        /// <summary>
        /// Convierte el 'location' viejo (nombre de escena) al id de piso actual. Es idempotente: si ya es
        /// un id de piso lo deja igual. Se aplica a cualquier versión para que un guardado intermedio no
        /// se quede con el formato anterior.
        /// </summary>
        static void MigrateLocation(SaveData data)
        {
            if (data.location == "Atrio")
                data.location = "1";
            else if (data.location == "Ascensores")
                data.location = "2";
        }

        /// <summary>Las claves ausentes no deben dejar listas en null: el resto del juego las recorre sin comprobar.</summary>
        static void NormalizeLists(SaveData data)
        {
            if (data.sealIds == null) data.sealIds = new List<string>();
            if (data.bossNotes == null) data.bossNotes = new List<FloorNote>();
            if (data.team == null) data.team = new List<TeamMember>();
            if (data.inventory == null) data.inventory = new List<InventoryItem>();
            if (data.pokedex == null) data.pokedex = new List<string>();
            if (data.visitedFloors == null) data.visitedFloors = new List<string>();
            if (data.defeatedTrainers == null) data.defeatedTrainers = new List<string>();
            if (data.collectedItems == null) data.collectedItems = new List<string>();
            if (data.completedFloors == null) data.completedFloors = new List<string>();
        }

        /// <summary>
        /// Equipo anterior a la v4: no había PS, EXP ni movimientos. Los PS quedan en el centinela
        /// <see cref="TeamMember.FullHp"/> (sin daño) y el máximo en 0 a la espera de la ficha de la especie.
        /// </summary>
        static void MigrateTeamToV4(SaveData data)
        {
            if (data.team == null)
            {
                data.team = new List<TeamMember>();
                return;
            }

            foreach (TeamMember member in data.team)
            {
                if (member == null)
                    continue;

                member.hp = TeamMember.FullHp;
                member.maxHp = 0;
                member.exp = 0;
                member.moves = new List<MoveSlot>();
            }
        }

        /// <summary>Partidas v4 o posteriores: JsonUtility puede dejar listas en null si la clave no existe.</summary>
        static void NormalizeTeam(SaveData data)
        {
            if (data.team == null)
            {
                data.team = new List<TeamMember>();
                return;
            }

            foreach (TeamMember member in data.team)
                if (member != null && member.moves == null)
                    member.moves = new List<MoveSlot>();
        }

        public static void Save(SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        public static SaveData CreateNew()
        {
            var data = new SaveData { createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
            Save(data);
            return data;
        }
    }
}
