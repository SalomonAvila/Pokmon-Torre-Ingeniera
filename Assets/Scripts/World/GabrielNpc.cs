using System.Collections.Generic;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Gabriel en la plazoleta (Doc §8 y §10). Es el NPC que entrega el inicial y, si el jugador
    /// acepta su ayuda, 5 Pokébolas antes de la puerta del Vigilante Gómez.
    ///
    /// El diálogo depende del estado de la partida:
    /// <list type="bullet">
    /// <item>Sin inicial: presenta la situación y abre la elección de Bulbasaur, Charmander o Squirtle.</item>
    /// <item>Con inicial y sin decisión de ayuda: la ofrece y, si el jugador dice "Puedo solo", insiste.</item>
    /// <item>Con ayuda aceptada: recuerda capturar en las jardineras de abajo y cura al equipo al hablar
    /// (es el punto de curación de la plazoleta; tras el tutorial, felicita).</item>
    /// </list>
    ///
    /// Hereda de <see cref="Interactable"/> para que <see cref="PlayerController"/> lo encuentre y bloquee
    /// su casilla como cualquier NPC; el builder lo crea con <c>ProvisionalMap.CreateGabriel</c>.
    /// </summary>
    public class GabrielNpc : Interactable
    {
        static readonly string[] IntroLines =
        {
            "¡Hola! Soy Gabriel, estudiante de ingeniería.",
            "Con la tormenta, el edificio se llenó de criaturas y los ascensores no funcionan.",
            "Si quieres, te doy un POKÉMON para que puedas defenderte.",
        };

        static readonly string[] StarterOptions = { "BULBASAUR", "CHARMANDER", "SQUIRTLE" };
        static readonly string[] ConfirmOptions = { "SÍ", "NO" };
        static readonly string[] HelpOptions = { "Sí, ayúdame", "Puedo solo" };

        public override void Interact()
        {
            if (WorldMessage.IsOpen)
                return;

            // Sin inicial: presentación → elección → confirmación → entrega.
            if (string.IsNullOrEmpty(GameState.Save.starterId))
            {
                WorldMessage.Show(IntroLines, null, _ => ChooseStarter());
                return;
            }

            // Con inicial pero sin decisión de ayuda (o con un "no" de una partida vieja): vuelve a ofrecerla.
            if (GameState.Save.gabrielChoice != GabrielChoice.Yes)
            {
                AskForHelp();
                return;
            }

            if (GameState.TutorialDone)
                ShowHealed(
                    "¡Bien hecho, Pedro! Ya puedes subir al piso 3.",
                    "Si tu POKÉMON está herido, habla conmigo: te lo curo.",
                    "Nos vemos arriba.");
            else if (GameState.PassesBuildingDoor)
                ShowHealed(
                    "Ya capturaste lo que el vigilante Gómez pide.",
                    "Puedes pasar al lobby cuando quieras.",
                    "Si tu POKÉMON está herido, habla conmigo: te lo curo.");
            else
                ShowHealed(
                    "Captura al menos un POKÉMON en las jardineras de abajo, junto al kiosco.",
                    "Con eso, el vigilante Gómez te dejará pasar al lobby.",
                    "Si tu POKÉMON está herido, habla conmigo: te lo curo.",
                    "Allí te espero para una última prueba.");
        }

        void ChooseStarter() =>
            WorldMessage.Show(
                new[] { "¿Cuál quieres que sea tu compañero?" },
                StarterOptions,
                ConfirmStarter);

        void ConfirmStarter(int chosen)
        {
            if (chosen < 0 || chosen >= SpeciesCatalog.Starters.Length)
                return;

            string speciesId = SpeciesCatalog.Starters[chosen];
            string name = DisplayName(speciesId);

            WorldMessage.Show(
                new[] { $"¿Quieres a {name} como tu compañero?" },
                ConfirmOptions,
                answer =>
                {
                    if (answer < 0)
                        return;
                    if (answer != 0)
                    {
                        ChooseStarter(); // vuelve a la lista de iniciales
                        return;
                    }

                    GameState.SetStarter(speciesId);
                    WorldMessage.Show(
                        new[] { $"¡{name} se unió a tu equipo!" },
                        null,
                        _ => AskForHelp());
                });
        }

        void AskForHelp()
        {
            WorldMessage.Show(
                new[] { "¿Quieres que te acompañe en tu aventura?" },
                HelpOptions,
                answer =>
                {
                    if (answer < 0)
                        return;
                    if (answer == 0)
                    {
                        GameState.SetGabrielChoice(GabrielChoice.Yes);
                        GivePokeballs();
                        return;
                    }

                    // "Puedo solo": Gabriel insiste y vuelve a preguntar (la partida quedaría bloqueada sin Pokébolas).
                    WorldMessage.Show(
                        new[] { "¿Con qué vas a capturar, Pedro? Mejor acepta mi ayuda." },
                        null,
                        _ => AskForHelp());
                });
        }

        void GivePokeballs()
        {
            GameState.AddItem("pokeball", 5);
            WorldMessage.Show(
                "Toma estas 5 POKÉBOLAS.",
                "Captura un POKÉMON en las jardineras de abajo y el vigilante te dejará pasar.",
                "Si tu POKÉMON está herido, habla conmigo: te lo curo.");
        }

        /// <summary>
        /// Muestra las líneas y, si el equipo tenía PS o PP gastados, añade el aviso de la curación.
        /// Gabriel es el punto de curación de la plazoleta mientras Don Chucho está tras la puerta.
        /// </summary>
        static void ShowHealed(params string[] lines)
        {
            if (!GameState.HealTeam())
            {
                WorldMessage.Show(lines);
                return;
            }

            var allLines = new List<string>(lines) { "Gabriel curó a tus POKÉMON." };
            WorldMessage.Show(allLines.ToArray());
        }

        static string DisplayName(string speciesId)
        {
            PokemonData data = PokemonDatabase.Get(speciesId);
            return data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : speciesId;
        }
    }
}
