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

    /// <summary>Pokémon del equipo. Por ahora lo mínimo para persistir; se amplía con BattleSystem/PokemonData.</summary>
    [Serializable]
    public class TeamMember
    {
        public string speciesId;
        public int level;
    }

    /// <summary>Objeto de la mochila con su cantidad.</summary>
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public int amount;
    }

    /// <summary>Especies capturadas en un piso: cada especie cuenta solo en el piso de su primera captura.</summary>
    [Serializable]
    public class FloorSpecies
    {
        public string floor;
        public List<string> species = new List<string>();
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
        public const int CurrentVersion = 2;

        /// <summary>
        /// El 1 es el valor antiguo: si el JSON no trae 'version' (formato anterior al campo) se trata como v1
        /// y pasa por la migración. Los guardados nuevos escriben CurrentVersion (lo impone SaveSystem.Save).
        /// </summary>
        public int version = 1;
        public string playerName = "Pedro";
        public string location = "Atrio";
        public int credits;

        /// <summary>Sellos obtenidos: un id por jefe (el id del piso). La cantidad se deriva con sealIds.Count.</summary>
        public List<string> sealIds = new List<string>();

        /// <summary>Notas de jefes, para el promedio.</summary>
        public List<FloorNote> bossNotes = new List<FloorNote>();

        /// <summary>Equipo actual (máximo GameRules.MaxTeamSize).</summary>
        public List<TeamMember> team = new List<TeamMember>();

        public List<InventoryItem> inventory = new List<InventoryItem>();

        /// <summary>Especies capturadas, sin repetir (Pokédex total).</summary>
        public List<string> pokedex = new List<string>();

        /// <summary>Pokédex por piso: la especie queda en el piso de su primera captura.</summary>
        public List<FloorSpecies> pokedexByFloor = new List<FloorSpecies>();

        /// <summary>Pisos a los que el jugador ya llegó (distinto de completedFloors, que son los vencidos).</summary>
        public List<string> visitedFloors = new List<string>();

        /// <summary>Carné de acceso que da la Ing. Cantera y activa los ascensores del sur.</summary>
        public bool accessCard;

        /// <summary>Especie del inicial elegido; vacío mientras no elija.</summary>
        public string starterId = "";

        public GabrielChoice gabrielChoice = GabrielChoice.Undecided;

        /// <summary>Tutorial superado: habilita la puerta Atrio ↔ Ascensores.</summary>
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
        /// y banderas. No hay nada que recalcular: JsonUtility deja los campos nuevos con su valor inicial.
        /// </summary>
        static void Migrate(SaveData data)
        {
            if (data.version > SaveData.CurrentVersion)
            {
                Debug.LogWarning($"La partida guardada es de la versión {data.version} y esta build soporta la " +
                                 $"{SaveData.CurrentVersion}: se carga igual, pero al guardar se perderá lo que no conozca.");
                return;
            }

            if (data.version < 1)
                data.version = 1; // Formato anterior a que existiera el campo 'version'.
            // Las migraciones futuras se encadenan aquí (por ejemplo: if (data.version < 3) ...).

            data.version = SaveData.CurrentVersion;
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
