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
    /// (principal y de movimientos). Es solo presentación: recibe el estado del <see cref="BattleEngine"/> y
    /// devuelve las acciones elegidas. El menú principal sigue el orden del Doc §7: Luchar, Mochila,
    /// Pokémon, Huir; Mochila y Pokémon son un aviso provisional hasta la segunda mitad de la etapa.
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

        public void Bind(BattlePokemon player, BattlePokemon enemy)
        {
            SetSprite(enemySprite, enemy.speciesId, false);
            SetSprite(playerSprite, player.speciesId, true);
            Refresh(player, enemy);
        }

        public void Refresh(BattlePokemon player, BattlePokemon enemy)
        {
            playerHud.Show(player);
            enemyHud.Show(enemy);
        }

        /// <summary>Muestra los mensajes uno tras otro (el jugador avanza con Z/E/Enter).</summary>
        public IEnumerator Messages(IReadOnlyList<string> lines)
        {
            foreach (string line in lines)
                yield return message.Say(line);
        }

        /// <summary>Menú principal. Devuelve Luchar (con su movimiento), Huir, o <c>null</c> en los avisos.</summary>
        public IEnumerator ChooseAction(BattlePokemon player, bool canFlee, Action<BattleAction> onChosen)
        {
            string[] options = canFlee
                ? new[] { "LUCHAR", "MOCHILA", "POKÉMON", "HUIR" }
                : new[] { "LUCHAR", "MOCHILA", "POKÉMON" };

            while (true)
            {
                int choice = -1;
                yield return menu.Choose(options, null, 2, value => choice = value);

                if (choice == 0)
                {
                    int moveIndex = -1;
                    yield return ChooseMove(player, value => moveIndex = value);
                    if (moveIndex < 0)
                        continue;

                    onChosen(BattleAction.Move(moveIndex));
                    yield break;
                }

                if (canFlee && choice == 3)
                {
                    onChosen(BattleAction.Run());
                    yield break;
                }

                string notice = choice == 1
                    ? "La mochila todavía está en construcción."
                    : "Cambiar de Pokémon todavía está en construcción.";
                yield return message.Say(notice);
            }
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
                yield return message.Say("Este Pokémon no tiene movimientos.");
                onChosen(-1);
                yield break;
            }

            int chosen = -1;
            yield return moveMenu.Choose(labels.ToArray(), details.ToArray(), 1, value => chosen = value);
            onChosen(chosen);
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