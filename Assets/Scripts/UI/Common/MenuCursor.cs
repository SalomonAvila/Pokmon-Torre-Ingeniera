using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Muestra el cursor "►" estilo GBA junto a la opción seleccionada y
    /// selecciona la opción al pasar el mouse por encima.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class MenuCursor : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] GameObject cursor;
        [SerializeField] TMP_Text label;
        [SerializeField] Color normalColor = Palette.DarkGray;
        [SerializeField] Color selectedColor = Palette.DarkOutline;
        [SerializeField] Color disabledColor = Palette.ConcreteGray;

        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        void OnEnable() => Refresh();

        public void OnSelect(BaseEventData eventData) => Refresh(true);

        public void OnDeselect(BaseEventData eventData) => Refresh(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        /// <summary>Actualiza el aspecto según la selección actual del EventSystem.</summary>
        public void Refresh()
        {
            bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            Refresh(selected);
        }

        void Refresh(bool selected)
        {
            if (cursor != null)
                cursor.SetActive(selected);

            if (label != null)
            {
                if (!selectable.IsInteractable())
                    label.color = disabledColor;
                else
                    label.color = selected ? selectedColor : normalColor;
            }
        }
    }
}
