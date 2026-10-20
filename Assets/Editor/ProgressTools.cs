using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Atajos para probar el progreso mientras no existen los jefes de piso:
    /// completar pisos (desbloquea la escalera hacia el siguiente), abrir la puerta del edificio con un inicial
    /// y una captura de prueba (mientras no exista el Vigilante Gómez) o reiniciar el progreso.
    /// Los pisos 1 y 2 no tienen reto: su escalera de subida siempre está abierta.
    /// Funcionan en Play (efecto inmediato) y fuera de Play (modifican la partida guardada).
    /// </summary>
    public static class ProgressTools
    {
        [MenuItem("Torre Ingeniería/Progreso/Completar piso actual")]
        static void CompleteCurrent()
        {
            PrepareState();
            GameState.CompleteFloor(GameState.CurrentFloor);
            Report($"{Building.DisplayName(GameState.CurrentFloor)} completado.");
        }

        [MenuItem("Torre Ingeniería/Progreso/Completar piso actual", true)]
        static bool CanCompleteCurrent() => EditorApplication.isPlaying;

        [MenuItem("Torre Ingeniería/Progreso/Completar piso 3")]
        static void CompleteThird()
        {
            PrepareState();
            GameState.CompleteFloor("3");
            Report("PISO 3 completado: la escalera al piso 4 queda habilitada.");
        }

        [MenuItem("Torre Ingeniería/Progreso/Completar todos los pisos")]
        static void CompleteAll()
        {
            PrepareState();
            foreach (string floor in Building.Floors)
                GameState.CompleteFloor(floor);
            Report("Todos los pisos completados.");
        }

        [MenuItem("Torre Ingeniería/Progreso/Abrir puerta del edificio (prueba)")]
        static void OpenBuildingDoor()
        {
            PrepareState();
            if (string.IsNullOrEmpty(GameState.Save.starterId))
                GameState.SetStarter("bulbasaur");
            GameState.RegisterCapture("pidgey");
            Report("Puerta del edificio abierta: inicial elegido y captura de prueba (pidgey).");
        }

        [MenuItem("Torre Ingeniería/Progreso/Reiniciar progreso")]
        static void ResetProgress()
        {
            PrepareState();
            GameState.ResetProgress();
            Report("Progreso reiniciado (pisos, sellos, notas y Carné): desde el piso 3, las escaleras de subida quedan bloqueadas.");
        }

        /// <summary>Fuera de Play se relee la partida desde el archivo para no usar datos viejos.</summary>
        static void PrepareState()
        {
            if (!EditorApplication.isPlaying)
                GameState.Save = null;
        }

        static void Report(string message) =>
            Debug.Log($"{message} Pisos completados: {string.Join(", ", GameState.Save.completedFloors)}");
    }
}
