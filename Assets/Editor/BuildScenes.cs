using System;
using System.IO;
using System.Linq;
using TorreIngenieria.Core;
using UnityEditor;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Registra escenas en Build Profiles en un orden fijo: título, atrio, combate, escalera, los pisos de
    /// <see cref="Building.Floors"/> y el pasillo de los ascensores. Reconstruir una escena ya registrada
    /// conserva su posición, de modo que <c>ProjectSettings/EditorBuildSettings.asset</c> no se reordena
    /// cada vez que se regeneran los mapas.
    /// </summary>
    public static class BuildScenes
    {
        /// <summary>Posición de las escenas que no son del juego (p. ej. SampleScene): al final.</summary>
        const int OtherRank = 200;

        /// <summary>
        /// Agrega o actualiza la escena en Build Profiles dejando la lista en orden canónico.
        /// Descarta las entradas cuyos archivos ya no existen.
        /// </summary>
        public static void Register(string path)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => File.Exists(s.path))
                .OrderBy(s => Rank(s.path))
                .ToList();

            int index = scenes.FindIndex(s => s.path == path);
            if (index >= 0)
            {
                scenes[index] = new EditorBuildSettingsScene(path, scenes[index].enabled);
            }
            else
            {
                int rank = Rank(path);
                int position = scenes.FindIndex(s => Rank(s.path) > rank);
                scenes.Insert(position < 0 ? scenes.Count : position, new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Orden de la escena en Build Profiles (menor va antes).</summary>
        static int Rank(string path)
        {
            string scene = Path.GetFileNameWithoutExtension(path);
            if (scene == SceneNames.MainMenu)
                return 0;
            if (scene == SceneNames.Intro)
                return 1;
            if (scene == SceneNames.Atrio)
                return 2;
            if (scene == SceneNames.Battle)
                return 3;
            if (scene == SceneNames.Escalera)
                return 4;

            // Los pisos siguen el orden de Building.Floors: Piso3 … Piso7, PisoPT, Piso8 … Piso13.
            if (scene.StartsWith("Piso", StringComparison.Ordinal) && Building.IsValid(scene.Substring(4)))
                return 5 + Array.IndexOf(Building.Floors, scene.Substring(4));

            return scene == SceneNames.Ascensores ? 100 : OtherRank;
        }
    }
}
