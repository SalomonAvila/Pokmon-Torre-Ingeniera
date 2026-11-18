using TorreIngenieria.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Lectura de los controles de menú para las pantallas que no son una lista (Opciones, Carné,
    /// cantidad de la tienda): aceptar, cancelar y una dirección por pulsación. Igual que en
    /// <see cref="MenuList"/>, una flecha mantenida no repite: hay que soltarla para volver a moverse.
    /// </summary>
    public class MenuInput
    {
        readonly InputAction submit;
        readonly InputAction cancel;
        readonly InputAction navigate;
        bool held = true; // si se llega con una flecha pulsada, no cuenta hasta soltarla

        public MenuInput(InputActionAsset controls)
        {
            submit = controls.FindAction("UI/Submit", true);
            cancel = controls.FindAction("UI/Cancel", true);
            navigate = controls.FindAction("UI/Navigate", true);
        }

        /// <summary>Aceptar pulsado este fotograma (suena al leerlo).</summary>
        public bool Submitted => Pressed(submit, GameAudio.Accept);

        /// <summary>Cancelar pulsado este fotograma (suena al leerlo).</summary>
        public bool Cancelled => Pressed(cancel, GameAudio.Cancel);

        static bool Pressed(InputAction action, string sound)
        {
            bool pressed = action.WasPressedThisFrame();
            if (pressed)
                GameAudio.Sfx(sound);
            return pressed;
        }

        /// <summary>Dirección pulsada este fotograma (una sola vez por pulsación); cero si no hay.</summary>
        public Vector2Int ReadStep()
        {
            Vector2 nav = navigate.ReadValue<Vector2>();
            if (nav.sqrMagnitude <= 0.25f)
            {
                held = false;
                return Vector2Int.zero;
            }
            if (held)
                return Vector2Int.zero;

            held = true;
            GameAudio.Sfx(GameAudio.Cursor);
            return Mathf.Abs(nav.x) > Mathf.Abs(nav.y)
                ? new Vector2Int(nav.x < 0 ? -1 : 1, 0)
                : new Vector2Int(0, nav.y < 0 ? -1 : 1);
        }
    }
}
