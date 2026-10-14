using System;
using System.IO;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Progreso del jugador. Se irá ampliando con el equipo, la mochila, los sellos, etc.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string playerName = "Pedro";
        public string location = "Plazoleta";
        public int credits;
        public int seals;
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
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"No se pudo leer la partida guardada ({SavePath}): {e.Message}");
            }
            return data != null;
        }

        public static void Save(SaveData data)
        {
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
