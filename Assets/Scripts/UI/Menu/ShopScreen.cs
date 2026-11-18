using TorreIngenieria.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Tienda de Don Chucho en el lobby: lista de objetos con su precio (<see cref="ItemData.price"/>),
    /// los créditos del jugador arriba, y abajo el icono y la descripción del resaltado. Al elegir uno se
    /// pide la cantidad (▲ ▼ de uno en uno, ◀ ▶ de diez en diez), se confirma y se compra con
    /// <see cref="GameState.TryBuy"/>. Lo comprado va a la misma mochila. La venta queda como (propuesta).
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        static readonly CultureInfo Colombian = CultureInfo.GetCultureInfo("es-CO");

        /// <summary>Lo que vende Don Chucho, en este orden. Solo aparecen los que tienen precio.</summary>
        public static readonly string[] Stock = { "pokeball", "tinto", "agua_panela", "arepa" };

        const int MaxQuantity = 99;

        [SerializeField] InputActionAsset controls;
        [SerializeField] MenuList itemList;
        [SerializeField] TMP_Text creditsText;
        [SerializeField] Image itemIcon;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] GameObject quantityBox;
        [SerializeField] TMP_Text quantityText;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text ownedText;

        public void Hide() => gameObject.SetActive(false);

        public IEnumerator Browse(GameMenu menu)
        {
            List<ItemData> items = ItemsForSale();
            var labels = new List<string>();
            var prices = new List<string>();
            foreach (ItemData item in items)
            {
                labels.Add(item.displayName.ToUpperInvariant());
                prices.Add(item.price.ToString("N0", Colombian));
            }
            labels.Add("SALIR");
            prices.Add(string.Empty);

            gameObject.SetActive(true);
            quantityBox.SetActive(false);
            menu.HideMessage();
            int selected = 0;

            while (true)
            {
                RefreshCredits();
                int chosen = MenuList.Cancelled;
                yield return itemList.Choose(labels.ToArray(), prices.ToArray(), value => chosen = value,
                    index => Describe(index < items.Count ? items[index] : null), selected);
                if (chosen == MenuList.Cancelled || chosen >= items.Count)
                    break;

                selected = chosen;
                yield return Buy(menu, items[chosen]);
                menu.HideMessage();
            }

            Hide();
        }

        IEnumerator Buy(GameMenu menu, ItemData item)
        {
            string name = item.displayName.ToUpperInvariant();
            int affordable = Mathf.Min(MaxQuantity, GameState.Save.credits / item.price);
            if (affordable <= 0)
            {
                GameAudio.Sfx(GameAudio.Error);
                yield return menu.Say("No te alcanzan los créditos.");
                yield break;
            }

            int amount = 0;
            yield return ChooseQuantity(menu, item, affordable, value => amount = value);
            if (amount <= 0)
                yield break;

            int cost = item.price * amount;
            bool yes = false;
            yield return menu.Confirm($"{amount} {name} cuestan {cost.ToString("N0", Colombian)} créditos. ¿Los compras?",
                value => yes = value);
            if (!yes)
                yield break;

            if (!GameState.TryBuy(item.itemId, amount))
            {
                yield return menu.Say("No te alcanzan los créditos.");
                yield break;
            }

            RefreshCredits();
            GameAudio.Sfx(GameAudio.Buy);
            yield return menu.Say($"¡Aquí tienes! Guardaste {amount} {name} en la mochila.");
        }

        IEnumerator ChooseQuantity(GameMenu menu, ItemData item, int max, System.Action<int> onChosen)
        {
            menu.Prompt($"¿Cuántos {item.displayName.ToUpperInvariant()} quieres?");
            var input = new MenuInput(controls);
            int amount = 1;
            quantityBox.SetActive(true);
            RefreshQuantity(item, amount);
            yield return null;

            while (true)
            {
                if (input.Submitted)
                    break;
                if (input.Cancelled)
                {
                    amount = 0;
                    break;
                }

                Vector2Int step = input.ReadStep();
                int delta = step.y != 0 ? step.y : step.x * 10;
                if (delta != 0)
                {
                    // Como en FireRed: de 1 hacia abajo da la vuelta al máximo, y del máximo hacia arriba a 1.
                    int next = amount + delta;
                    amount = next > max ? (amount == max ? 1 : max) : next < 1 ? (amount == 1 ? max : 1) : next;
                    RefreshQuantity(item, amount);
                }
                yield return null;
            }

            yield return null;
            quantityBox.SetActive(false);
            onChosen(amount);
        }

        void RefreshQuantity(ItemData item, int amount)
        {
            quantityText.text = $"x{amount:00}";
            totalText.text = (item.price * amount).ToString("N0", Colombian);
        }

        void RefreshCredits() =>
            creditsText.text = $"CRÉDITOS\n{GameState.Save.credits.ToString("N0", Colombian)}";

        void Describe(ItemData item)
        {
            if (item == null)
            {
                descriptionText.text = "¡Vuelve pronto!";
                itemIcon.enabled = false;
                ownedText.text = string.Empty;
                return;
            }

            string text = item.description;
            if (item.category == ItemCategory.Heal && item.healAmount > 0)
                text = $"{text.TrimEnd('.', ' ')}. Restaura {item.healAmount} PS.";
            descriptionText.text = text;
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
            ownedText.text = $"EN LA MOCHILA: {GameState.CountItem(item.itemId)}";
        }

        static List<ItemData> ItemsForSale()
        {
            var items = new List<ItemData>();
            foreach (string id in Stock)
            {
                ItemData item = ItemDatabase.Get(id);
                if (item != null && item.price > 0)
                    items.Add(item);
            }
            return items;
        }
    }
}
