using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Botón único para dejar todo el proyecto regenerado: lo de <see cref="BuildEverything"/> (hojas de
    /// caminata, datos y menú del juego) más todas las escenas (título, Intro, combate, escalera, pisos de
    /// laboratorio, lobby y plazoleta) y, al final, los tres validadores. Vuelve a abrir la escena que
    /// estaba abierta.
    ///
    /// A diferencia de Construir todo, sí regenera escenas: el diff de git cambia aunque el contenido sea
    /// el mismo (los ids internos se renumeran). Úsalo cuando cambien los builders de mapas o escenas.
    ///
    /// Menú: Torre Ingeniería > Reconstruir todo (incluye escenas) (Ctrl+Shift+Alt+B).
    /// </summary>
    public static class RebuildEverything
    {
        const string MenuPath = "Torre Ingeniería/Reconstruir todo (incluye escenas) %#&b";

        [MenuItem(MenuPath, priority = -99)]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string previousScene = EditorSceneManager.GetActiveScene().path;
            double started = EditorApplication.timeSinceStartup;
            try
            {
                Step("hojas de caminata", 0.05f);
                AssetDatabase.Refresh();
                CharacterSpriteSlicer.SliceCharacters();

                Step("datos del prototipo", 0.15f);
                PrototypeDataBuilder.BuildAll();

                Step("menú del juego", 0.25f);
                GameMenuBuilder.Build();
                UiAtlasBuilder.Build();

                Step("título", 0.35f);
                MainMenuSceneBuilder.Build();

                Step("Intro", 0.42f);
                MainMenuSceneBuilder.BuildIntro();

                Step("combate", 0.5f);
                BattleSceneBuilder.Build();

                Step("mapas (escalera, pisos, lobby y plazoleta)", 0.6f);
                BuildAllMaps.Build();

                Step("validadores", 0.9f);
                ValidateRules.Validate();
                ValidateData.Validate();
                VerifySaveLoading.Verify();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene);

            Debug.Log($"--- Reconstruir todo: listo en {EditorApplication.timeSinceStartup - started:0} s. " +
                      "Revisa arriba si algún paso salió en rojo. ---");
        }

        [MenuItem(MenuPath, true)]
        static bool CanRebuild() => !EditorApplication.isPlaying;

        static void Step(string name, float progress)
        {
            Debug.Log($"--- Reconstruir todo: {name} ---");
            EditorUtility.DisplayProgressBar("Reconstruir todo", name, progress);
        }
    }
}
