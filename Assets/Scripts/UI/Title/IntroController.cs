using System.Collections;
using TorreIngenieria.Core;
using TorreIngenieria.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Introducción narrada (Doc §7, pantalla 3): cuenta la tormenta y la llegada de Pedro antes de
    /// la plazoleta. Se muestra en la escena <c>Intro</c>, entre el título y el <c>Atrio</c>.
    /// El texto avanza con Z/E/Enter, como el resto de los diálogos.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] DialogueBox dialogue;

        static readonly string[] Lines =
        {
            "Era lunes de semana de parciales y el edificio llevaba toda la noche zumbando.",
            "En el piso técnico, el ingeniero Aurelio Montoya encendió el Proyecto Cóspide.",
            "Un relámpago cayó sobre el piso 14 y, por una grieta de luz, empezaron a salir criaturas.",
            "Esa mañana, Pedro Pérez llegó tarde, como siempre, con un tinto en la mano.",
            "A las 4:00 p. m. debía sustentar su proyecto integrador en el piso 14.",
            "Pero en la plazoleta lo esperaba el caos: la puerta cerrada y los ascensores fuera de servicio.",
            "Gabriel Gutiérrez, monitor de Programación, le puso una Pokébola en las manos.",
            "\"Si quieres llegar al 14, vas a necesitar un equipo, Pedro.\"",
        };

        void Awake()
        {
            // La UI se habilita para avanzar el texto con Z/E/Enter.
            controls.FindActionMap("UI", true).Enable();
        }

        void Start()
        {
            // Al llegar desde el título el fundido ya está en curso; al abrir la escena directamente
            // se hace un fundido de entrada para no aparecer de golpe.
            FadeManager fade = FadeManager.Ensure();
            if (!FadeManager.IsFading)
            {
                fade.SetBlack(true);
                fade.StartCoroutine(fade.FadeFromBlack(0.5f));
            }

            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return null;
            foreach (string line in Lines)
                yield return dialogue.Say(line);

            if (!SceneTransition.Go(SceneNames.Atrio, null))
                Debug.LogWarning($"Falta la escena '{SceneNames.Atrio}': ejecuta Mapas > Construir atrio provisional.");
        }
    }
}
