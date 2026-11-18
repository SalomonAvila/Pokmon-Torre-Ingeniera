using TorreIngenieria.Audio;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Mochila fuera de combate (maqueta 07), con los bolsillos de FireRed: OBJETOS, POKÉBOLAS y OBJETOS
    /// CLAVE, que se cambian con ◀ ▶. A la izquierda la mochila de Pedro, a la derecha la lista con
    /// cantidades y CERRAR MOCHILA, y abajo el icono y la descripción del objeto resaltado.
    ///
    /// USAR (curativos) abre el Equipo para elegir a quién; TIRAR quita uno tras confirmar. Las Pokébolas
    /// solo se usan en combate y los objetos clave no se tiran.
    /// </summary>
    public class BagScreen : MonoBehaviour
    {
        enum Pocket { Items, Balls, Key }

        static readonly string[] PocketNames = { "OBJETOS", "POKÉBOLAS", "OBJETOS CLAVE" };

        [SerializeField] MenuList itemList;
        [SerializeField] TMP_Text pocketText;
        [SerializeField] Image bagImage;
        [SerializeField, Tooltip("Una imagen de la mochila por bolsillo (opcional).")] Sprite[] bagSprites;
        [SerializeField] Image itemIcon;
        [SerializeField] TMP_Text descriptionText;

        public void Hide() => gameObject.SetActive(false);

        public IEnumerator Browse(GameMenu menu)
        {
            var pocket = Pocket.Items;
            int selected = 0;

            while (true)
            {
                Show(menu, pocket);
                List<string> ids = ItemsIn(pocket);
                var labels = new List<string>();
                var amounts = new List<string>();
                foreach (string id in ids)
                {
                    labels.Add(DisplayName(id));
                    amounts.Add($"x{GameState.CountItem(id)}"); // ASCII, como en el combate
                }
                labels.Add("CERRAR MOCHILA");
                amounts.Add(string.Empty);

                int chosen = MenuList.Cancelled;
                yield return itemList.Choose(labels.ToArray(), amounts.ToArray(), value => chosen = value,
                    index => Describe(index < ids.Count ? ids[index] : null), selected, horizontal: true);

                if (chosen == MenuList.PressedLeft || chosen == MenuList.PressedRight)
                {
                    int step = chosen == MenuList.PressedRight ? 1 : -1;
                    pocket = (Pocket)(((int)pocket + step + PocketNames.Length) % PocketNames.Length);
                    selected = 0;
                    continue;
                }
                if (chosen == MenuList.Cancelled || chosen >= ids.Count)
                    break;

                selected = chosen;
                yield return ItemActions(menu, ids[chosen]);
            }

            Hide();
        }

        void Show(GameMenu menu, Pocket pocket)
        {
            gameObject.SetActive(true);
            menu.HideMessage();
            pocketText.text = $"< {PocketNames[(int)pocket]} >"; // la fuente pixel no tiene ◀ ▶
            Sprite bagSprite = bagSprites != null && (int)pocket < bagSprites.Length ? bagSprites[(int)pocket] : null;
            bagImage.sprite = bagSprite;
            bagImage.enabled = bagSprite != null;
        }

        IEnumerator ItemActions(GameMenu menu, string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            string name = DisplayName(itemId);

            if (item != null && item.category == ItemCategory.Key)
            {
                yield return menu.Say($"{name} es un objeto clave. No se puede usar ni tirar aquí.");
                yield break;
            }

            bool canUse = item != null && (item.category == ItemCategory.Heal ||
                                           item.category == ItemCategory.Teach ||
                                           item.category == ItemCategory.LevelUp ||
                                           item.category == ItemCategory.Revive);
            string[] options = canUse ? new[] { "USAR", "TIRAR", "CANCELAR" } : new[] { "TIRAR", "CANCELAR" };
            int action = MenuList.Cancelled;
            yield return menu.ChooseAction($"¿Qué hacer con {name}?", options, value => action = value);
            if (action < 0)
                yield break;

            string chosen = options[action];
            if (chosen == "USAR" && item.category == ItemCategory.Teach)
                yield return TeachOnTeam(menu, item, name);
            else if (chosen == "USAR" && item.category == ItemCategory.Revive)
                yield return ReviveOnTeam(menu, itemId, name);
            else if (chosen == "USAR" && item.category == ItemCategory.LevelUp)
                yield return LevelUpOnTeam(menu, itemId, name);
            else if (chosen == "USAR")
                yield return UseOnTeam(menu, itemId, name);
            else if (chosen == "TIRAR")
                yield return Toss(menu, itemId, name);
        }

        IEnumerator UseOnTeam(GameMenu menu, string itemId, string itemName)
        {
            if (GameState.Save.team.Count == 0)
            {
                yield return menu.Say("No tienes POKÉMON en tu equipo.");
                yield break;
            }

            Hide();
            int target = MenuList.Cancelled;
            yield return menu.Team.ChooseTarget(menu, $"¿A quién le das {itemName}?", value => target = value);
            if (target >= 0)
            {
                string pokemon = PokemonName(target);
                HealResult result = GameState.UseHealItem(itemId, target, out int healed);
                GameAudio.Sfx(result == HealResult.Healed ? GameAudio.Heal : GameAudio.Error);
                menu.Team.Refresh();
                yield return menu.Say(result switch
                {
                    HealResult.Healed => $"{pokemon} recuperó {healed} PS.",
                    HealResult.Fainted => $"{pokemon} está debilitado. {itemName} no le hace efecto.",
                    _ => "No tendría ningún efecto.",
                });
            }
            menu.Team.Hide();
        }

        /// <summary>Arepa: revive al elegido con la mitad de sus PS.</summary>
        IEnumerator ReviveOnTeam(GameMenu menu, string itemId, string itemName)
        {
            if (GameState.Save.team.Count == 0)
            {
                yield return menu.Say("No tienes POKÉMON en tu equipo.");
                yield break;
            }

            Hide();
            int target = MenuList.Cancelled;
            yield return menu.Team.ChooseTarget(menu, $"¿A quién le das {itemName}?", value => target = value);
            if (target >= 0)
            {
                string pokemon = PokemonName(target);
                bool revived = GameState.UseReviveItem(itemId, target);
                GameAudio.Sfx(revived ? GameAudio.Heal : GameAudio.Error);
                menu.Team.Refresh();
                yield return menu.Say(revived
                    ? $"¡{pokemon} se recuperó y puede volver a luchar!"
                    : $"{pokemon} no está debilitado. No tendría ningún efecto.");
            }
            menu.Team.Hide();
        }

        /// <summary>Cafeína: sube un nivel al elegido y cuenta lo que aprendió (u omitió por tener cuatro).</summary>
        IEnumerator LevelUpOnTeam(GameMenu menu, string itemId, string itemName)
        {
            if (GameState.Save.team.Count == 0)
            {
                yield return menu.Say("No tienes POKÉMON en tu equipo.");
                yield break;
            }

            Hide();
            int target = MenuList.Cancelled;
            yield return menu.Team.ChooseTarget(menu, $"¿A quién le das {itemName}?", value => target = value);
            if (target >= 0)
            {
                string pokemon = PokemonName(target);
                ExpGainResult result = GameState.UseLevelUpItem(itemId, target);
                menu.Team.Refresh();
                if (result == null || !result.LeveledUp)
                {
                    yield return menu.Say("No tendría ningún efecto.");
                }
                else
                {
                    GameAudio.Sfx(GameAudio.LevelUp);
                    yield return menu.Say($"¡{pokemon} subió al nivel {result.NewLevel}!");
                    foreach (LevelUpEvent level in result.LevelUps)
                    {
                        foreach (string move in level.LearnedMoves)
                            yield return menu.Say($"¡{pokemon} aprendió {MoveName(move)}!");
                        foreach (string move in level.MissedMoves)
                            yield return menu.Say($"{pokemon} quería aprender {MoveName(move)}, pero ya sabe cuatro movimientos.");
                    }
                }
            }
            menu.Team.Hide();
        }

        /// <summary>
        /// Apuntes: enseñan su movimiento. Si el elegido ya sabe cuatro, pregunta cuál olvidar (B cancela
        /// y los Apuntes no se gastan).
        /// </summary>
        IEnumerator TeachOnTeam(GameMenu menu, ItemData item, string itemName)
        {
            if (GameState.Save.team.Count == 0)
            {
                yield return menu.Say("No tienes POKÉMON en tu equipo.");
                yield break;
            }

            string moveName = MoveName(item.moveId);
            Hide();
            int target = MenuList.Cancelled;
            yield return menu.Team.ChooseTarget(menu, $"¿Quién estudia {itemName}?", value => target = value);
            if (target < 0)
            {
                menu.Team.Hide();
                yield break;
            }

            string pokemon = PokemonName(target);
            TeachResult result = GameState.UseTeachItem(item.itemId, target);
            if (result == TeachResult.NeedsReplace)
            {
                yield return menu.Say($"{pokemon} quiere aprender {moveName}, pero ya sabe cuatro movimientos.");
                List<MoveSlot> moves = GameState.Save.team[target].moves;
                var names = new string[moves.Count];
                for (int i = 0; i < moves.Count; i++)
                    names[i] = MoveName(moves[i]?.moveId);

                int forget = MenuList.Cancelled;
                yield return menu.ChooseAction("¿Qué movimiento olvida? (B para cancelar)", names, value => forget = value);
                if (forget < 0)
                {
                    yield return menu.Say($"{pokemon} no aprendió {moveName}.");
                    menu.Team.Hide();
                    yield break;
                }

                string forgotten = names[forget];
                result = GameState.UseTeachItem(item.itemId, target, forget);
                if (result == TeachResult.Learned)
                    yield return menu.Say($"{pokemon} olvidó {forgotten}...");
            }

            menu.Team.Refresh();
            yield return menu.Say(result switch
            {
                TeachResult.Learned => $"¡{pokemon} aprendió {moveName}!",
                TeachResult.AlreadyKnows => $"{pokemon} ya sabe {moveName}.",
                _ => "No tendría ningún efecto.",
            });
            menu.Team.Hide();
        }

        static string MoveName(string moveId)
        {
            MoveData move = MoveDatabase.Get(moveId);
            return (move != null ? move.displayName : moveId ?? "").ToUpperInvariant();
        }

        IEnumerator Toss(GameMenu menu, string itemId, string itemName)
        {
            bool yes = false;
            yield return menu.Confirm($"¿Tirar un {itemName}?", value => yes = value);
            if (!yes)
                yield break;

            GameState.AddItem(itemId, -1);
            yield return menu.Say($"Tiraste un {itemName}.");
        }

        void Describe(string itemId)
        {
            ItemData item = itemId != null ? ItemDatabase.Get(itemId) : null;
            if (item == null)
            {
                descriptionText.text = itemId == null ? "Cierra la mochila." : string.Empty;
                itemIcon.enabled = false;
                return;
            }

            string text = item.description;
            if (item.category == ItemCategory.Heal && item.healAmount > 0)
                text = $"{text.TrimEnd('.', ' ')}. Restaura {item.healAmount} PS.";
            descriptionText.text = text;
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
        }

        static List<string> ItemsIn(Pocket pocket)
        {
            var ids = new List<string>();
            foreach (InventoryItem entry in GameState.Save.inventory)
            {
                if (entry == null || entry.amount <= 0)
                    continue;
                ItemData item = ItemDatabase.Get(entry.id);
                if (item != null && PocketOf(item.category) == pocket)
                    ids.Add(entry.id);
            }
            return ids;
        }

        static Pocket PocketOf(ItemCategory category) => category switch
        {
            ItemCategory.Ball => Pocket.Balls,
            ItemCategory.Key => Pocket.Key,
            _ => Pocket.Items,
        };

        static string DisplayName(string itemId)
        {
            ItemData item = ItemDatabase.Get(itemId);
            return (item != null ? item.displayName : itemId).ToUpperInvariant();
        }

        static string PokemonName(int index)
        {
            TeamMember member = GameState.Save.team[index];
            PokemonData species = PokemonDatabase.Get(member.speciesId);
            return (species != null ? species.displayName : member.speciesId).ToUpperInvariant();
        }
    }
}
