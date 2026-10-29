using System.Collections;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEngine;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Después de PRESS START: revisa si hay una partida guardada.
    /// Si la hay, ofrece continuarla o empezar de nuevo; si no, crea una partida nueva
    /// y presenta al protagonista, Pedro Pérez (documento de diseño, sección 3).
    /// </summary>
    public class SaveCheckController : MonoBehaviour
    {
        [SerializeField] DialogueBox dialogue;
        [SerializeField] ChoiceBox choices;

        [Header("Retratos")]
        [SerializeField] CanvasGroup hostPortrait;
        [SerializeField] CanvasGroup playerPortrait;
        [SerializeField] float portraitFade = 0.5f;

        void Awake() => playerPortrait.alpha = 0f;

        public IEnumerator Run()
        {
            yield return dialogue.Say("Revisando si hay progreso guardado...");

            if (SaveSystem.TryLoad(out SaveData save))
            {
                GameState.Save = save;
                yield return dialogue.Say($"Se encontró la partida de {save.playerName} en {Building.DisplayName(save.location)}.");
                yield return dialogue.Type("¿Qué quieres hacer?");

                int choice = 0;
                yield return choices.Choose(new[] { "CONTINUAR", "NUEVA PARTIDA" }, i => choice = i);

                if (choice == 0)
                {
                    yield return dialogue.Say("Cargando partida...");
                    // (propuesta) Partidas de prueba antiguas sin inicial fijado: se toma el inicial del equipo.
                    GameState.EnsureStarterFromTeam();
                    LoadWorld(Building.SceneFor(GameState.Save.location), SceneNames.Atrio);
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
            GameState.Save = data;
            GameState.EnsureStarterFromTeam();

            yield return IntroducePlayer(data);
            yield return dialogue.Say($"¡Listo, {data.playerName}! Tu aventura en la Torre de Ingeniería está por comenzar.");

            // La partida nueva pasa por la introducción narrada antes de la plazoleta.
            LoadWorld(SceneNames.Intro, SceneNames.Atrio);
        }

        static void LoadWorld(string scene, string fallback)
        {
            if (SceneTransition.Go(scene, null))
                return;

            if (!string.IsNullOrEmpty(fallback))
                SceneTransition.Go(fallback, null);
            else
                Debug.Log($"Falta la escena '{scene}': ejecuta su builder (por ejemplo, Torre Ingeniería > Construir escena Intro).");
        }

        /// <summary>Presentación del protagonista según su biografía en el documento de diseño.</summary>
        IEnumerator IntroducePlayer(SaveData data)
        {
            dialogue.Clear();
            yield return Fade(hostPortrait, 1f, 0f);
            yield return Fade(playerPortrait, 0f, 1f);

            yield return dialogue.Say($"Este es {data.playerName} Pérez.");
            yield return dialogue.Say("Tiene 18 años y acaba de empezar primer semestre de Ingeniería.");
            yield return dialogue.Say("Es el primero de su familia en llegar a la universidad, y estudia con beca.");
            yield return dialogue.Say("Su papá es técnico electricista y su mamá vende tintos.");
            yield return dialogue.Say("Creció arreglando radios y ventiladores. Sabe que todo sistema tiene una falla que se puede encontrar.");
            yield return dialogue.Say("Es curioso y algo desorganizado. Siempre llega unos minutos tarde... pero siempre llega.");
            yield return dialogue.Say("Nunca dice \"no puedo\". Dice \"déjame intentar otra vez\".");
            yield return dialogue.Say("Hoy, a las 4:00 p. m., debe sustentar su proyecto integrador en el piso 14.");
            yield return dialogue.Say("Pero una tormenta dejó el edificio sin ascensores... y lleno de criaturas.");
        }

        IEnumerator Fade(CanvasGroup group, float from, float to)
        {
            for (float t = 0f; t < portraitFade; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / portraitFade);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
