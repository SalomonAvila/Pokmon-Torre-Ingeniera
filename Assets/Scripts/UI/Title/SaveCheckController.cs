using System.Collections;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Después de PRESS START: revisa si hay una partida guardada.
    /// Si la hay, ofrece continuarla o empezar de nuevo; si no, crea una partida nueva.
    /// </summary>
    public class SaveCheckController : MonoBehaviour
    {
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ChoiceBox choices;

        public IEnumerator Run()
        {
            yield return dialogue.Say("Revisando si hay progreso guardado...");

            if (SaveSystem.TryLoad(out SaveData save))
            {
                yield return dialogue.Say($"Se encontró la partida de {save.playerName} en {save.location}.");
                yield return dialogue.Type("¿Qué quieres hacer?");

                int choice = 0;
                yield return choices.Choose(new[] { "CONTINUAR", "NUEVA PARTIDA" }, i => choice = i);

                if (choice == 0)
                {
                    yield return dialogue.Say("Cargando partida...");
                    // TODO: cargar la escena guardada cuando exista.
                    Debug.Log($"Continuar partida: {SaveSystem.SavePath}");
                    yield break;
                }

                yield return dialogue.Type("La partida anterior se borrará. ¿Seguro?");
                int confirm = 1;
                yield return choices.Choose(new[] { "SÍ", "NO" }, i => confirm = i);
                if (confirm == 1)
                {
                    // Vuelve a preguntar desde el principio.
                    yield return Run();
                    yield break;
                }
            }
            else
            {
                yield return dialogue.Say("No se encontró ningún progreso guardado.");
            }

            yield return dialogue.Say("Creando una nueva partida...");
            SaveData data = SaveSystem.CreateNew();
            yield return dialogue.Say($"¡Listo, {data.playerName}! Tu aventura en la Torre de Ingeniería está por comenzar.");

            if (Application.CanStreamedLevelBeLoaded(SceneNames.Intro))
                SceneManager.LoadScene(SceneNames.Intro);
            else
                Debug.Log($"Partida nueva creada en {SaveSystem.SavePath}. Falta la escena '{SceneNames.Intro}'.");
        }
    }
}
