using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Mapa del edificio (maqueta 09; opción MAPA y tecla M): los pisos de arriba abajo con el % del
    /// Pokédex de cada uno y, a la derecha, el detalle del resaltado (área, estado, sello y Pokédex). Abajo,
    /// el objetivo actual de la guía.
    ///
    /// El selector de ascensor usa la misma pantalla (comparten los datos de los pisos): solo deja elegir
    /// pisos ya visitados, distintos del actual, y nunca la plazoleta (los ascensores están dentro).
    /// </summary>
    public class BuildingMapScreen : MonoBehaviour
    {
        [SerializeField] MenuList list;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text floorText;
        [SerializeField] TMP_Text detailText;
        [SerializeField, Tooltip("Arte de los sellos por piso, en el orden de sealFloors (opcional).")]
        Sprite[] sealSprites;
        [SerializeField] string[] sealFloors;

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Consulta del mapa: B cierra.</summary>
        public IEnumerator Browse(GameMenu menu)
        {
            titleText.text = "MAPA DEL EDIFICIO";
            yield return Run(menu, false, null);
        }

        /// <summary>
        /// Selector de ascensor. Llama a <paramref name="onChosen"/> con el piso elegido, o null si el
        /// jugador cancela.
        /// </summary>
        public IEnumerator ChooseElevatorFloor(GameMenu menu, Action<string> onChosen)
        {
            titleText.text = "ASCENSOR";
            string destination = null;
            yield return Run(menu, true, floor => destination = floor);
            onChosen(destination);
        }

        IEnumerator Run(GameMenu menu, bool elevator, Action<string> onChosen)
        {
            List<string> floors = FloorsTopDown();
            var labels = new string[floors.Count];
            var details = new string[floors.Count];
            for (int i = 0; i < floors.Count; i++)
            {
                string floor = floors[i];
                labels[i] = floor == "PT" ? "PISO PT" : Building.DisplayName(floor);
                details[i] = elevator && floor == GameState.CurrentFloor ? "AQUÍ" : PokedexPercent(floor);
            }

            gameObject.SetActive(true);
            list.IconFor = index => SealSprite(floors[index]);
            string help = elevator ? "¿A qué piso quieres ir?" : Guidance.MessageText();

            int start = Mathf.Max(0, floors.IndexOf(GameState.CurrentFloor));
            while (true)
            {
                menu.Prompt(help);
                int chosen = MenuList.Cancelled;
                yield return list.Choose(labels, details, value => chosen = value,
                    index => Describe(floors[index]), start);
                if (chosen == MenuList.Cancelled)
                    break;

                start = chosen;
                if (!elevator)
                    continue;

                string floor = floors[chosen];
                string problem = ElevatorProblem(floor);
                if (problem != null)
                {
                    yield return menu.Say(problem);
                    continue;
                }

                list.IconFor = null;
                Hide();
                onChosen(floor);
                yield break;
            }

            list.IconFor = null;
            Hide();
        }

        /// <summary>Por qué el ascensor no puede ir a ese piso; null si puede.</summary>
        public static string ElevatorProblem(string floor)
        {
            if (floor == Building.Floors[0])
                return "Los ascensores no llegan a la plazoleta: baja por la escalera del lobby.";
            if (floor == GameState.CurrentFloor)
                return "Ya estás en este piso.";
            if (!GameState.IsFloorVisited(floor))
                return "El ascensor solo va a pisos que ya conoces.";
            return null;
        }

        void Describe(string floor)
        {
            bool visited = GameState.IsFloorVisited(floor);
            floorText.text = visited ? Building.HudTitle(floor) : $"{Building.DisplayName(floor)} – ???";

            string state = !visited ? "SIN VISITAR"
                : !Building.HasChallenge(floor) ? "SIN RETO"
                : GameState.IsFloorCompleted(floor) ? "COMPLETADO"
                : "EN CURSO";
            string seal = !Building.HasChallenge(floor) ? "-"
                : GameState.HasSeal(floor) ? "OBTENIDO" : "PENDIENTE";
            (int caught, int total) = GameState.FloorPokedexProgress(floor);
            string dex = total == 0 ? "-" : visited ? $"{caught}/{total} ({GameState.FloorPokedexPercent(floor)} %)" : "?";
            string here = floor == GameState.CurrentFloor ? "\nESTÁS AQUÍ" : string.Empty;

            detailText.text = $"ESTADO  {state}\nSELLO   {seal}\nPOKÉDEX {dex}{here}";
        }

        static string PokedexPercent(string floor)
        {
            (_, int total) = GameState.FloorPokedexProgress(floor);
            return total > 0 && GameState.IsFloorVisited(floor) ? $"{GameState.FloorPokedexPercent(floor)}%" : string.Empty;
        }

        Sprite SealSprite(string floor)
        {
            if (!GameState.HasSeal(floor) || sealFloors == null || sealSprites == null)
                return null;
            int index = Array.IndexOf(sealFloors, floor);
            return index >= 0 && index < sealSprites.Length ? sealSprites[index] : null;
        }

        /// <summary>Pisos de arriba (14) abajo (1), como un directorio de edificio.</summary>
        static List<string> FloorsTopDown()
        {
            var floors = new List<string>(Building.Floors);
            floors.Reverse();
            return floors;
        }
    }
}
