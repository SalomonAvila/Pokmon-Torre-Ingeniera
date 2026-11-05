using UnityEngine.SceneManagement;

namespace TorreIngenieria.Core
{
    /// <summary>
    /// Objetivo actual de la guía: el id estable del destino en la escena y el texto que ve el jugador.
    /// La franja del HUD y el aviso de M/MAPA comparten este texto.
    /// </summary>
    public readonly struct GuidanceInfo
    {
        public GuidanceInfo(string targetId, string text)
        {
            TargetId = targetId;
            Text = text;
        }

        /// <summary>Id del <c>GuidanceTarget</c> que hay que señalar en la escena.</summary>
        public string TargetId { get; }

        /// <summary>Texto corto de una línea para la franja y el aviso.</summary>
        public string Text { get; }
    }

    /// <summary>
    /// Guía de objetivos del onboarding (propuesta de la Etapa 5): qué hacer en el Atrio y en el
    /// pasillo de los ascensores según el progreso. Es una tabla derivada del estado de la partida;
    /// no se guarda nada nuevo en <see cref="SaveData"/>.
    ///
    /// La clave es el nombre de la escena, no <see cref="GameState.CurrentFloor"/>: la escena
    /// <c>Escalera</c> no tiene <c>FloorInfo</c> y conserva el piso anterior. Solo el Atrio y los
    /// Ascensores tienen guía forzada; los pisos 3 en adelante quedan libres.
    /// </summary>
    public static class Guidance
    {
        // Ids estables de los destinos que colocan AtrioBuilder y AscensoresBuilder.
        public const string GabrielAtrio = "gabriel_atrio";
        public const string JardineraInferior = "jardinera_inferior";
        public const string PuertaEdificio = "puerta_edificio";
        public const string GabrielTutorial = "gabriel_tutorial";
        public const string EscaleraPiso3 = "escalera_piso_3";

        // Textos cortos: la franja añade "OBJETIVO: " y el aviso de M/MAPA, "OBJETIVO ACTUAL:".
        const string TalkToGabriel = "Habla con Gabriel.";
        const string CaptureInPlanters = "Captura un Pokémon en las jardineras.";
        const string EnterBuilding = "Entra al edificio.";
        const string TalkToGabrielLobby = "Habla con Gabriel en el lobby.";
        const string ClimbToFloor3 = "Sube al piso 3.";

        /// <summary>Aviso cuando todavía no hay pantalla de mapa o la guía ya terminó.</summary>
        public const string MapUnderConstruction = "El mapa del edificio todavía está en construcción.";

        /// <summary>Objetivo de la escena activa, o null si no hay guía forzada.</summary>
        public static GuidanceInfo? Current => Resolve(
            SceneManager.GetActiveScene().name,
            GameState.HasStarter,
            GameState.PassesBuildingDoor,
            GameState.Save.gabrielChoice == GabrielChoice.Yes,
            GameState.TutorialDone,
            GameState.IsFloorVisited("3"));

        /// <summary>
        /// El borde amarillo de la jardinera solo se muestra cuando la guía apunta allí y los
        /// encuentros salvajes están activos. Antes del inicial la prioridad es Gabriel.
        /// </summary>
        public static bool HighlightsPlanter =>
            Current?.TargetId == JardineraInferior && GameState.AllowWildEncounters;

        /// <summary>
        /// Tabla pura de objetivos. Recibe el estado como parámetros para poder validarla en el
        /// editor sin tocar la partida guardada.
        /// </summary>
        public static GuidanceInfo? Resolve(string scene, bool hasStarter, bool passesDoor,
            bool helpedByGabriel, bool tutorialDone, bool floor3Visited)
        {
            if (scene == SceneNames.Atrio)
            {
                // Primero el inicial: sin él no hay equipo ni Pokébolas.
                if (!hasStarter)
                    return new GuidanceInfo(GabrielAtrio, TalkToGabriel);

                // El recorrido de onboarding termina al llegar al piso 3.
                if (floor3Visited)
                    return null;

                // Tras el tutorial se vuelve al Atrio para entrar al lobby; antes del tutorial,
                // la puerta se abre con la captura que pide el Vigilante.
                if (tutorialDone || passesDoor)
                    return new GuidanceInfo(PuertaEdificio, EnterBuilding);

                // Sin la ayuda de Gabriel no hay Pokébolas: hay que volver a hablar con él.
                if (!helpedByGabriel)
                    return new GuidanceInfo(GabrielAtrio, TalkToGabriel);

                return new GuidanceInfo(JardineraInferior, CaptureInPlanters);
            }

            if (scene == SceneNames.Ascensores)
            {
                if (floor3Visited)
                    return null;
                if (!tutorialDone)
                    return new GuidanceInfo(GabrielTutorial, TalkToGabrielLobby);
                return new GuidanceInfo(EscaleraPiso3, ClimbToFloor3);
            }

            // Escalera, pisos 3+ y cualquier otra escena: sin guía forzada.
            return null;
        }

        /// <summary>Texto de la franja superior; vacío si no hay objetivo.</summary>
        public static string StripText(GuidanceInfo? info) =>
            info.HasValue ? $"OBJETIVO: {info.Value.Text}" : string.Empty;

        /// <summary>Texto del aviso de M y de la opción MAPA del menú.</summary>
        public static string MessageText()
        {
            GuidanceInfo? info = Current;
            return info.HasValue ? $"OBJETIVO ACTUAL:\n{info.Value.Text}" : MapUnderConstruction;
        }
    }
}
