using TorreIngenieria.Core;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Construye todos los mapas provisionales de una vez, para que las conexiones entre ellos
    /// (escaleras, puntos de llegada) queden siempre al día. Al final deja abierto el atrio.
    /// Menú: Torre Ingeniería > Mapas > Construir todos los mapas.
    /// </summary>
    public static class BuildAllMaps
    {
        [MenuItem("Torre Ingeniería/Mapas/Construir todos los mapas", priority = 0)]
        public static void Build()
        {
            StairwellBuilder.Build();
            LabFloorBuilder.BuildAll();
            AscensoresBuilder.Build();
            AtrioBuilder.Build();
            EditorSceneManager.OpenScene(MapSceneBuilder.MapPath(SceneNames.Atrio));
        }
    }
}
