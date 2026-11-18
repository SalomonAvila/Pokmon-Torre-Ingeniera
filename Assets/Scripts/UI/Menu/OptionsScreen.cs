using System;
using System.Collections;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Opciones (maqueta 11), compartidas por el menú de pausa y el título (<see cref="GameSettings"/>):
    /// volumen en 10 pasos, pantalla completa, velocidad del texto y, solo en el juego, SALIR AL TÍTULO.
    /// ▲ ▼ eligen la fila, ◀ ▶ cambian el valor y B vuelve (los cambios se aplican al momento).
    /// </summary>
    public class OptionsScreen : MonoBehaviour
    {
        public enum Result { Closed, QuitToTitle }

        enum Row { Volume, Fullscreen, TextSpeed, QuitToTitle, Back }

        static readonly string[] Help =
        {
            "Volumen de la música y los efectos.",
            "Muestra el juego en pantalla completa o en ventana.",
            "Velocidad con la que aparecen los textos.",
            "Vuelve a la pantalla de título. Lo no guardado se pierde.",
            "Vuelve al menú.",
        };

        [SerializeField] InputActionAsset controls;
        [SerializeField, Tooltip("Una fila por opción, en el orden de Row.")] GameObject[] rows;
        [SerializeField] GameObject[] cursors;
        [SerializeField, Tooltip("Valor de cada fila (vacío para las filas sin valor).")] TMP_Text[] values;
        [SerializeField, Tooltip("Los 10 segmentos de la barra de volumen.")] Image[] volumeSegments;
        [SerializeField] Color segmentOn = Color.white;
        [SerializeField] Color segmentOff = Color.gray;

        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Muestra la pantalla hasta que el jugador vuelve. <paramref name="inGame"/> muestra SALIR AL
        /// TÍTULO (desde el título no tiene sentido).
        /// </summary>
        public IEnumerator Run(GameMenu menu, bool inGame, Action<Result> onClosed)
        {
            var input = new MenuInput(controls);
            rows[(int)Row.QuitToTitle].SetActive(inGame);
            gameObject.SetActive(true);

            var row = Row.Volume;
            Refresh(row, menu);
            yield return null; // la pulsación que abrió la pantalla no cuenta

            while (true)
            {
                if (input.Cancelled)
                    break;

                if (input.Submitted)
                {
                    if (row == Row.Back)
                        break;
                    if (row == Row.QuitToTitle)
                    {
                        bool yes = false;
                        yield return menu.Confirm("¿Salir al título? Lo que no hayas guardado se perderá.",
                            value => yes = value);
                        if (yes)
                        {
                            GameSettings.Save();
                            Hide();
                            onClosed(Result.QuitToTitle);
                            yield break;
                        }
                        Refresh(row, menu);
                    }
                    else
                    {
                        Change(row, 1);
                        Refresh(row, menu);
                    }
                }

                Vector2Int step = input.ReadStep();
                if (step.y != 0)
                {
                    row = Move(row, -step.y, inGame);
                    Refresh(row, menu);
                }
                else if (step.x != 0)
                {
                    Change(row, step.x);
                    Refresh(row, menu);
                }

                yield return null;
            }

            GameSettings.Save();
            yield return null; // B no debe cerrar también el menú de pausa
            Hide();
            onClosed(Result.Closed);
        }

        static Row Move(Row row, int delta, bool inGame)
        {
            int count = Enum.GetValues(typeof(Row)).Length;
            int next = (int)row;
            do
                next = (next + delta + count) % count;
            while (!inGame && next == (int)Row.QuitToTitle);
            return (Row)next;
        }

        static void Change(Row row, int delta)
        {
            switch (row)
            {
                case Row.Volume:
                    GameSettings.Volume += delta;
                    break;
                case Row.Fullscreen:
                    GameSettings.Fullscreen = !GameSettings.Fullscreen;
                    break;
                case Row.TextSpeed:
                    int speeds = GameSettings.TextSpeedNames.Length;
                    GameSettings.Speed = (GameSettings.TextSpeed)(((int)GameSettings.Speed + delta + speeds) % speeds);
                    break;
            }
        }

        void Refresh(Row selected, GameMenu menu)
        {
            for (int i = 0; i < cursors.Length; i++)
                cursors[i].SetActive(i == (int)selected);

            int volume = GameSettings.Volume;
            for (int i = 0; i < volumeSegments.Length; i++)
                volumeSegments[i].color = i < volume ? segmentOn : segmentOff;

            SetValue(Row.Volume, volume.ToString());
            SetValue(Row.Fullscreen, GameSettings.Fullscreen ? "< SÍ >" : "< NO >");
            SetValue(Row.TextSpeed, $"< {GameSettings.TextSpeedNames[(int)GameSettings.Speed]} >");
            menu.Prompt(Help[(int)selected]);
        }

        void SetValue(Row row, string text)
        {
            int index = (int)row;
            if (values != null && index < values.Length && values[index] != null)
                values[index].text = text;
        }
    }
}
