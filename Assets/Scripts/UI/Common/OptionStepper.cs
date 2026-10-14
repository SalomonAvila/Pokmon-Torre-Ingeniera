using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Fila de opción con valor "◄ valor ►" que se cambia con izquierda/derecha.
    /// Aceptar o hacer clic avanza al siguiente valor.
    /// </summary>
    public class OptionStepper : Selectable, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] TMP_Text valueLabel;

        int minValue;
        int maxValue = 1;
        int value;
        bool wrap;
        Func<int, string> format = v => v.ToString();

        public event Action<int> ValueChanged;

        public int Value => value;

        public void Configure(int min, int max, int current, bool wrapAround, Func<int, string> formatter)
        {
            minValue = min;
            maxValue = max;
            value = Mathf.Clamp(current, min, max);
            wrap = wrapAround;
            format = formatter ?? (v => v.ToString());
            UpdateLabel();
        }

        public override void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    Step(-1);
                    break;
                case MoveDirection.Right:
                    Step(1);
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }

        public void OnSubmit(BaseEventData eventData) => Step(1);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Step(1);
        }

        void Step(int delta)
        {
            if (!IsInteractable())
                return;

            int next = value + delta;
            if (wrap)
            {
                int range = maxValue - minValue + 1;
                next = minValue + ((next - minValue) % range + range) % range;
            }
            else
            {
                next = Mathf.Clamp(next, minValue, maxValue);
            }

            if (next == value)
                return;

            value = next;
            UpdateLabel();
            ValueChanged?.Invoke(value);
        }

        void UpdateLabel()
        {
            if (valueLabel != null)
                valueLabel.text = $"◄ {format(value)} ►";
        }
    }
}
