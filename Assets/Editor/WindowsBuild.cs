using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Build de Windows del prototipo (Etapa 9): las escenas activas de Build Profiles, en
    /// <c>Builds/Windows/TorreIngenieria.exe</c> (la carpeta Builds/ está en .gitignore). Requiere el módulo
    /// Windows Build Support instalado en el Hub.
    /// Menú: Torre Ingeniería > Build de Windows.
    /// </summary>
    public static class WindowsBuild
    {
        const string OutputPath = "Builds/Windows/TorreIngenieria.exe";

        [MenuItem("Torre Ingeniería/Build de Windows")]
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path))
                .Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build de Windows", "No hay escenas activas en Build Profiles.", "OK");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"Build de Windows lista: {OutputPath} ({summary.totalSize / (1024f * 1024f):0.0} MB, " +
                          $"{scenes.Length} escenas, {summary.totalTime.TotalSeconds:0} s).");
            else
                Debug.LogError($"La build de Windows falló ({summary.result}, {summary.totalErrors} errores). Revisa la consola.");
        }

        [MenuItem("Torre Ingeniería/Build de Windows", true)]
        static bool CanBuild() => !EditorApplication.isPlaying;
    }
}
