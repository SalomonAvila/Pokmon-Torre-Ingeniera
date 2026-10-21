using System.Collections.Generic;
using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Acceso a los datos del prototipo por id. Los assets generados por el Editor viven en
    /// <c>Assets/Resources/Data/...</c>, así que se cargan con <c>Resources.Load</c> (Unity los cachea).
    /// Se añade una caché propia para no repetir la búsqueda y se limpia al entrar en Play, como el
    /// resto de estáticos del proyecto.
    /// </summary>
    public static class PokemonDatabase
    {
        const string Root = "Data/Pokemon/";
        static readonly Dictionary<string, PokemonData> Cache = new Dictionary<string, PokemonData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        public static PokemonData Get(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId))
                return null;
            if (Cache.TryGetValue(speciesId, out PokemonData cached) && cached != null)
                return cached;

            PokemonData data = Resources.Load<PokemonData>(Root + speciesId);
            if (data != null)
                Cache[speciesId] = data;
            return data;
        }

        public static bool Exists(string speciesId) => Get(speciesId) != null;

        public static IReadOnlyList<PokemonData> All()
        {
            PokemonData[] all = Resources.LoadAll<PokemonData>("Data/Pokemon");
            foreach (PokemonData data in all)
                if (data != null && !string.IsNullOrEmpty(data.speciesId))
                    Cache[data.speciesId] = data;
            return all;
        }
    }

    /// <summary>Movimientos por id (ver <see cref="PokemonDatabase"/> para el criterio de carga).</summary>
    public static class MoveDatabase
    {
        const string Root = "Data/Moves/";
        static readonly Dictionary<string, MoveData> Cache = new Dictionary<string, MoveData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        public static MoveData Get(string moveId)
        {
            if (string.IsNullOrEmpty(moveId))
                return null;
            if (Cache.TryGetValue(moveId, out MoveData cached) && cached != null)
                return cached;

            MoveData data = Resources.Load<MoveData>(Root + moveId);
            if (data != null)
                Cache[moveId] = data;
            return data;
        }

        public static bool Exists(string moveId) => Get(moveId) != null;
    }

    /// <summary>Objetos por id (ver <see cref="PokemonDatabase"/> para el criterio de carga).</summary>
    public static class ItemDatabase
    {
        const string Root = "Data/Items/";
        static readonly Dictionary<string, ItemData> Cache = new Dictionary<string, ItemData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        public static ItemData Get(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            if (Cache.TryGetValue(itemId, out ItemData cached) && cached != null)
                return cached;

            ItemData data = Resources.Load<ItemData>(Root + itemId);
            if (data != null)
                Cache[itemId] = data;
            return data;
        }

        public static bool Exists(string itemId) => Get(itemId) != null;
    }

    /// <summary>Entrenadores y jefes por id (ver <see cref="PokemonDatabase"/> para el criterio de carga).</summary>
    public static class TrainerDatabase
    {
        const string Root = "Data/Trainers/";
        static readonly Dictionary<string, TrainerData> Cache = new Dictionary<string, TrainerData>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Cache.Clear();

        public static TrainerData Get(string trainerId)
        {
            if (string.IsNullOrEmpty(trainerId))
                return null;
            if (Cache.TryGetValue(trainerId, out TrainerData cached) && cached != null)
                return cached;

            TrainerData data = Resources.Load<TrainerData>(Root + trainerId);
            if (data != null)
                Cache[trainerId] = data;
            return data;
        }

        public static bool Exists(string trainerId) => Get(trainerId) != null;
    }
}
