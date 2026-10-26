using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Un solo clic para dejar el proyecto al día después de cambiar datos o añadir arte: importa los
    /// archivos nuevos, corta las hojas de caminata, regenera los datos del prototipo (con sprites e iconos), regenera el menú del juego
    /// y ejecuta los tres validadores. El resultado de cada paso queda en la consola.
    ///
    /// No regenera escenas (título, combate ni mapas): cada regeneración renumera los ids internos y ensucia
    /// el diff aunque nada cambie. Para eso siguen sus menús propios.
    ///
    /// Menú: Torre Ingeniería > Construir todo (Ctrl+Shift+B).
    /// </summary>
    public static class BuildEverything
    {
        [MenuItem("Torre Ingeniería/Construir todo %#b", priority = -100)]
        public static void Build()
        {
            AssetDatabase.Refresh(); // importa el arte recién copiado antes de enlazarlo

            Debug.Log("--- Construir todo: hojas de caminata ---");
            CharacterSpriteSlicer.SliceCharacters();

            Debug.Log("--- Construir todo: datos del prototipo ---");
            PrototypeDataBuilder.BuildAll();

            Debug.Log("--- Construir todo: menú del juego ---");
            GameMenuBuilder.Build();

            ValidateRules.Validate();
            ValidateData.Validate();
            VerifySaveLoading.Verify();

            Debug.Log("--- Construir todo: listo. Revisa arriba si algún paso salió en rojo. ---");
        }

        /// <summary>Fuera de Play: en Play los validadores tocarían la partida en curso.</summary>
        [MenuItem("Torre Ingeniería/Construir todo %#b", true)]
        static bool CanBuild() => !EditorApplication.isPlaying;
    }
}
