using System;
using System.Collections;
using System.Collections.Generic;
using TorreIngenieria.Core;
using TorreIngenieria.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Interfaz del combate: fichas de los dos combatientes, sus sprites, el cuadro de mensajes y los menús
    /// (principal, de movimientos y de objetos). Es solo presentación: recibe el estado del
    /// <see cref="BattleEngine"/> y devuelve las acciones elegidas. El menú principal sigue el orden del
    /// Doc §7: Luchar, Mochila, Pokémon, Huir; Mochila abre los objetos usables y Pokémon es un aviso
    /// provisional hasta que exista el cambio de Pokémon en combate.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] BattleHUD enemyHud;
        [SerializeField] BattleHUD playerHud;
        [SerializeField] Image enemySprite;
        [SerializeField] Image playerSprite;
        [SerializeField] DialogueBox message;
        [SerializeField] BattleMenu menu;
        [SerializeField] BattleMenu moveMenu;

        TeamMember playerMember;

        public void Bind(BattlePokemon player, BattlePokemon enemy, TeamMember member = null)
        {
            playerMember = member;
            SetSprite(enemySprite, enemy.speciesId, false);
            SetSprite(playerSprite, player.speciesId, true);
            Refresh(player, enemy);
        }

        public void Refresh(BattlePokemon player, BattlePokemon enemy)
        {
            playerHud.Show(player, playerMember);
            enemyHud.Show(enemy);
        }

        /// <summary>
        /// Presenta la EXP ganada y los niveles/movimientos nuevos. La barra avanza desde la EXP
        /// anterior, se reinicia al cruzar cada nivel y termina en el estado real del miembro.
        /// Las líneas de nivel y movimiento esperan tecla, como el resto de mensajes del combate.
        /// </summary>
        public IEnumerator ShowExpGain(TeamMember member, ExpGainResult result)
        {
            if (member == null || result == null)
                yield break;
            if (result.ExpGained <= 0 && !result.LeveledUp)
                yield break;

            string name = PokemonDatabase.Get(member.speciesId)?.displayName ?? member.speciesId;
            message.SetPanel(DialogueBox.PanelSide.Full);
            if (result.ExpGained > 0)
                yield return message.Say($"¡{name} ganó {result.ExpGained} de EXP!");

            int level = result.OldLevel;
            int experience = result.OldExp;

            foreach (LevelUpEvent levelUp in result.LevelUps)
            {
                int threshold = PokemonStats.ExperienceForLevel(levelUp.Level);
                yield return AnimateExp(level, experience, threshold);

                level = levelUp.Level;
                experience = threshold;
                playerHud.ShowExp(experience, level);
                yield return message.Say($"¡{name} subió al nivel {level}!");

                foreach (string moveId in levelUp.LearnedMoves)
                    yield return message.Say($"¡{name} aprendió {MoveName(moveId)}!");

                foreach (string moveId in levelUp.MissedMoves)
                    yield return message.Say(
                        $"{name} quiere aprender {MoveName(moveId)}, pero ya tiene cuatro movimientos.");
            }

            yield return AnimateExp(level, experience, result.NewExp);
            playerHud.ShowExp(result.NewExp, result.NewLevel);
        }

        /// <summary>Relleno animado de la barra de EXP entre dos valores del mismo nivel.</summary>
        IEnumerator AnimateExp(int level, int fromExp, int toExp)
        {
            const float duration = 0.4f;
            if (fromExp == toExp)
            {
                playerHud.ShowExp(toExp, level);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(fromExp, toExp, Mathf.Clamp01(elapsed / duration)));
                playerHud.ShowExp(current, level);
                yield return null;
            }

            playerHud.ShowExp(toExp, level);
        }

        static string MoveName(string moveId) =>
            MoveDatabase.Get(moveId)?.displayName ?? moveId;

        /// <summary>Pausa de las líneas que no esperan una tecla (sacudidas de la Pokébola).</summary>
        const float AutoMessageDelay = 0.5f;

        /// <summary>
        /// Muestra los mensajes uno tras otro. Los normales esperan a que el jugador pulse; los marcados
        /// como <see cref="BattleMessage.autoAdvance"/> se escriben y pasan solos tras medio segundo.
        /// </summary>
        public IEnumerator Messages(IReadOnlyList<BattleMessage> lines)
        {
            message.SetPanel(DialogueBox.PanelSide.Full);
            foreach (BattleMessage line in lines)
            {
                if (line.autoAdvance)
                {
                    yield return message.Type(line.text);
                    yield return new WaitForSecondsRealtime(AutoMessageDelay);
                }
                else
                {
                    yield return message.Say(line.text);
                }
            }
        }

        /// <summary>Atajo para los textos fijos de la escena (introducción, avisos, captura).</summary>
        public IEnumerator Messages(IReadOnlyList<string> lines)
        {
            var messages = new List<BattleMessage>(lines.Count);
            foreach (string line in lines)
                messages.Add(line);
            yield return Messages(messages);
        }

        /// <summary>
        /// Menú principal. Devuelve Luchar (con su movimiento), Huir, usar un objeto o <c>null</c> en el
        /// aviso de Pokémon. La Mochila vuelve al menú si no hay objetos usables.
        /// </summary>
        public IEnumerator ChooseAction(BattlePokemon player, bool canFlee, Action<BattleAction> onChosen)
        {
            string[] options = canFlee
                ? new[] { "LUCHAR", "MOCHILA", "POKÉMON", "HUIR" }
                : new[] { "LUCHAR", "MOCHILA", "POKÉMON" };

            while (true)
            {
                // El menú principal ocupa la mitad derecha; el mensaje queda a la izquierda.
                message.SetPanel(DialogueBox.PanelSide.Left);

                int choice = -1;
                yield return menu.Choose(options, null, 2, value => choice = value);

                // Cancelar en el menú principal no elige acción: se vuelve a abrir.
                if (choice < 0)
                    continue;

                if (choice == 0)
                {
                    int moveIndex = -1;
                    yield return ChooseMove(player, value => moveIndex = value);
                    if (moveIndex < 0)
                        continue;

                    onChosen(BattleAction.Move(moveIndex));
                    yield break;
                }

                if (choice == 1)
                {
                    string itemId = null;
                    yield return ChooseItem(value => itemId = value);
                    if (itemId == null)
                        continue;

                    onChosen(BattleAction.Item(itemId));
                    yield break;
                }

                if (canFlee && choice == 3)
                {
                    onChosen(BattleAction.Run());
                    yield break;
                }

                message.SetPanel(DialogueBox.PanelSide.Full);
                yield return message.Say("Cambiar de Pokémon todavía está en construcción.");
            }
        }

        /// <summary>
        /// Lista los objetos usables en combate: curativos siempre y Pokébolas solo contra salvajes.
        /// Si no hay ninguno, avisa y devuelve <c>null</c> sin gastar el turno.
        /// </summary>
        IEnumerator ChooseItem(Action<string> onChosen)
        {
            bool ballAllowed = BattleRequest.Kind == BattleKind.Wild;
            var labels = new List<string>();
            var details = new List<string>();
            var itemIds = new List<string>();

            foreach (InventoryItem entry in GameState.Save.inventory)
            {
                if (entry == null || entry.amount <= 0)
                    continue;

                ItemData item = ItemDatabase.Get(entry.id);
                if (item == null)
                    continue;

                bool usable = item.category == ItemCategory.Heal
                    || (ballAllowed && item.category == ItemCategory.Ball);
                if (!usable)
                    continue;

                itemIds.Add(entry.id);
                labels.Add(item.displayName);
                details.Add($"x{entry.amount}");
            }

            if (itemIds.Count == 0)
            {
                message.SetPanel(DialogueBox.PanelSide.Full);
                yield return message.Say(ballAllowed
                    ? "No tienes objetos para usar en combate."
                    : "No tienes objetos de cura para usar en combate.");
                onChosen(null);
                yield break;
            }

            // La lista ocupa la mitad izquierda; el mensaje (descripción) queda a la derecha.
            message.SetPanel(DialogueBox.PanelSide.Right);
            string previous = message.CurrentText;
            int chosen = -1;
            yield return moveMenu.Choose(labels.ToArray(), details.ToArray(), 1, value => chosen = value, 84f,
                index => ShowDescription(itemIds, index, previous));

            if (chosen < 0)
                message.ShowText(previous, false); // al cancelar se recupera el mensaje anterior
            onChosen(chosen >= 0 && chosen < itemIds.Count ? itemIds[chosen] : null);
        }

        /// <summary>Descripción del objeto resaltado en el panel derecho.</summary>
        void ShowDescription(List<string> itemIds, int index, string fallback)
        {
            if (index < 0 || index >= itemIds.Count)
            {
                message.ShowText(fallback, false);
                return;
            }

            string description = ComposeItemDescription(ItemDatabase.Get(itemIds[index]));
            message.ShowText(string.IsNullOrEmpty(description)
                ? fallback
                : description.ToUpperInvariant());
        }

        /// <summary>
        /// Texto del objeto con la curación calculada desde sus datos (no escrita a mano): el Tinto y el
        /// Agua de panela muestran los PS que realmente restaura <see cref="ItemData.healAmount"/>.
        /// </summary>
        static string ComposeItemDescription(ItemData item)
        {
            if (item == null)
                return null;

            string description = item.description;
            if (item.category != ItemCategory.Heal)
                return description;

            int heal = item.healAmount > 0
                ? item.healAmount
                : item.itemId switch
                {
                    "tinto" => GameRules.TintoHeal,
                    "agua_panela" => GameRules.PanelaWaterHeal,
                    _ => 0,
                };

            if (heal <= 0)
                return description;

            string effect = $"RESTAURA {heal} PS.";
            if (string.IsNullOrWhiteSpace(description))
                return effect;

            // Compatibilidad con datos antiguos que ya traían la cantidad escrita en el texto.
            if (description.IndexOf(effect, StringComparison.OrdinalIgnoreCase) >= 0)
                return description;

            return $"{description.TrimEnd('.', ' ')}. {effect}";
        }

        IEnumerator ChooseMove(BattlePokemon player, Action<int> onChosen)
        {
            var labels = new List<string>();
            var details = new List<string>();
            for (int i = 0; i < player.moves.Count; i++)
            {
                BattleMoveSlot slot = player.moves[i];
                MoveData move = slot.Data;
                labels.Add(move != null ? move.displayName : slot.moveId);
                details.Add(move != null ? $"{slot.currentPp}/{slot.maxPp}" : "—");
            }

            if (labels.Count == 0)
            {
                message.SetPanel(DialogueBox.PanelSide.Full);
                yield return message.Say("Este Pokémon no tiene movimientos.");
                onChosen(-1);
                yield break;
            }

            // La lista ocupa la mitad izquierda; el mensaje (descripción) queda a la derecha.
            message.SetPanel(DialogueBox.PanelSide.Right);
            string previous = message.CurrentText;
            int chosen = -1;
            yield return moveMenu.Choose(labels.ToArray(), details.ToArray(), 1, value => chosen = value, 112f,
                index => ShowMoveDescription(player, index, previous));

            if (chosen < 0)
                message.ShowText(previous, false); // al cancelar se recupera el mensaje anterior
            onChosen(chosen);
        }

        /// <summary>Descripción del movimiento resaltado en el panel derecho.</summary>
        void ShowMoveDescription(BattlePokemon player, int index, string fallback)
        {
            if (index < 0 || index >= player.moves.Count)
            {
                message.ShowText(fallback, false);
                return;
            }

            message.ShowMarkup(ComposeMoveDescription(player.moves[index].Data, fallback));
        }

        /// <summary>Ancho aproximado del panel de descripción a 22 px (la fuente es monoespaciada).</summary>
        const int DescriptionCharsPerLine = 16;

        /// <summary>Máximo de renglones para la descripción; el resto se recorta con puntos suspensivos.</summary>
        const int DescriptionMaxLines = 2;

        /// <summary>
        /// Panel derecho de la lista de movimientos: etiqueta con el color del tipo, categoría y poder,
        /// y la descripción solo si queda espacio. La etiqueta usa &lt;mark&gt; con opacidad completa
        /// para que el color del tipo se vea, y texto oscuro o blanco según el contraste. Se deja una
        /// línea mínima entre la etiqueta y la categoría para que el resaltado no toque la de abajo.
        /// </summary>
        static string ComposeMoveDescription(MoveData move, string fallback)
        {
            if (move == null)
                return fallback;

            // La etiqueta va en ASCII: la fuente pixel dibuja las vocales mayúsculas acentuadas
            // con un glifo pequeño (FÍSICO se veía como "FíSICO").
            string typeName = PokemonTypeNames.BadgeName(move.type);
            string header =
                $"<mark={PokemonTypeColors.Hex(move.type)}FF>" +
                $"<color={PokemonTypeColors.BadgeText(move.type)}> TIPO/{typeName} </color></mark>";

            string category = move.category switch
            {
                MoveCategory.Physical => "FISICO",
                MoveCategory.Special => "ESPECIAL",
                _ => "ESTADO",
            };

            // A 18 px la línea de categoría y poder cabe en el ancho del panel sin robar más renglones.
            string stats = move.power > 0
                ? $"<size=18>{category}  PODER {move.power}</size>"
                : $"<size=18>{category}</size>";

            // La línea de 6 px separa el borde inferior del resaltado de la categoría sin gastar
            // un renglón completo.
            string text = $"{header}\n<size=6>\n</size>{stats}";
            if (!string.IsNullOrWhiteSpace(move.description))
                text += $"\n{LimitDescription(move.description)}";
            return text;
        }

        /// <summary>
        /// Recorta la descripción a dos renglones como máximo. Si cabe, se deja que TMP haga el
        /// ajuste normal; si no, se llena el segundo renglón y se cierra con "...".
        /// </summary>
        static string LimitDescription(string description)
        {
            string text = description.Trim().ToUpperInvariant();
            List<string> lines = Wrap(text, DescriptionCharsPerLine);
            if (lines.Count <= DescriptionMaxLines)
                return text;

            string first = lines[0];
            string second = lines[1];
            int available = DescriptionCharsPerLine - 3; // espacio para "..."
            if (second.Length > available)
                second = second.Substring(0, available);

            return $"{first}\n{second}...";
        }

        /// <summary>Reparte el texto en líneas de ancho fijo sin partir palabras.</summary>
        static List<string> Wrap(string text, int width)
        {
            var lines = new List<string>();
            var current = new System.Text.StringBuilder();
            foreach (string word in text.Split(' '))
            {
                if (string.IsNullOrEmpty(word))
                    continue;

                if (current.Length == 0)
                    current.Append(word);
                else if (current.Length + 1 + word.Length <= width)
                    current.Append(' ').Append(word);
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString());
            return lines;
        }

        void SetSprite(Image image, string speciesId, bool back)
        {
            if (image == null)
                return;

            PokemonData species = PokemonDatabase.Get(speciesId);
            Sprite sprite = species != null ? (back ? species.backSprite : species.frontSprite) : null;
            if (sprite == null && species != null)
                sprite = back ? species.frontSprite : species.backSprite;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}