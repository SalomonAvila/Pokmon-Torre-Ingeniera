using System;
using System.Collections;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TorreIngenieria.UI
{
    /// <summary>
    /// Pantalla de Equipo (maquetas 02 y 03), con la distribución de FireRed: el líder en una ficha grande a
    /// la izquierda, los otros cinco a la derecha y SALIR abajo. Con ▲ ▼ se recorre en orden; ◀ salta al
    /// líder y ▶ vuelve a la columna derecha.
    ///
    /// Dos modos: <see cref="Browse"/> (DATOS / CAMBIAR / SALIR) y <see cref="ChooseTarget"/>, que usa la
    /// Mochila para elegir a quién darle un objeto.
    /// </summary>
    public class TeamScreen : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] TeamSlotView[] slots;
        [SerializeField] Image exitBackground;
        [SerializeField] Color exitNormalColor = new Color32(0xF4, 0xF1, 0xE8, 0xFF);
        [SerializeField] Color exitSelectedColor = new Color32(0xF2, 0xC2, 0x30, 0xFF);

        static readonly string[] MemberActions = { "DATOS", "CAMBIAR", "SALIR" };

        InputAction submitAction;
        InputAction cancelAction;
        InputAction navigateAction;

        int TeamCount => GameState.Save.team.Count;

        /// <summary>Índice de SALIR en el recorrido del cursor (después del último Pokémon).</summary>
        int ExitIndex => TeamCount;

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Recorre el equipo: DATOS abre el Resumen y CAMBIAR intercambia dos Pokémon.</summary>
        public IEnumerator Browse(GameMenu menu)
        {
            Show();
            if (TeamCount == 0)
            {
                yield return menu.Say("No tienes POKÉMON en tu equipo.");
                Hide();
                yield break;
            }

            int index = 0;
            while (true)
            {
                int chosen = MenuList.Cancelled;
                yield return Pick(menu, "Elige un POKÉMON.", index, -1, value => chosen = value);
                if (chosen == MenuList.Cancelled || chosen == ExitIndex)
                    break;

                index = chosen;
                int action = MenuList.Cancelled;
                yield return menu.ChooseAction($"¿Qué hacer con {DisplayName(index)}?", MemberActions,
                    value => action = value);

                if (action == 0)
                {
                    Hide();
                    menu.HideMessage();
                    yield return menu.Summary.Run(index, value => index = value);
                    Show();
                }
                else if (action == 1)
                {
                    int other = MenuList.Cancelled;
                    yield return Pick(menu, "¿Con cuál lo cambias?", index, index, value => other = value);
                    if (other >= 0 && other != ExitIndex && other != index)
                    {
                        GameState.SwapTeamMembers(index, other);
                        Refresh(-1, -1);
                        index = other;
                    }
                }
            }

            Hide();
        }

        /// <summary>
        /// Muestra el equipo para elegir a un Pokémon (por ejemplo, a quién darle un Tinto). Devuelve su
        /// índice o <see cref="MenuList.Cancelled"/>. La pantalla queda abierta para mostrar el resultado.
        /// </summary>
        public IEnumerator ChooseTarget(GameMenu menu, string prompt, Action<int> onChosen)
        {
            Show();
            int chosen = MenuList.Cancelled;
            yield return Pick(menu, prompt, 0, -1, value => chosen = value);
            onChosen(chosen == ExitIndex ? MenuList.Cancelled : chosen);
        }

        /// <summary>Vuelve a pintar las fichas (tras curar o cambiar el orden).</summary>
        public void Refresh() => Refresh(-1, -1);

        void Show()
        {
            gameObject.SetActive(true);
            Refresh(-1, -1);
        }

        /// <summary>Bucle del cursor. <paramref name="marked"/> es el Pokémon elegido para CAMBIAR (o −1).</summary>
        IEnumerator Pick(GameMenu menu, string prompt, int start, int marked, Action<int> onChosen)
        {
            submitAction ??= controls.FindAction("UI/Submit", true);
            cancelAction ??= controls.FindAction("UI/Cancel", true);
            navigateAction ??= controls.FindAction("UI/Navigate", true);

            menu.Prompt(prompt);
            int index = Mathf.Clamp(start, 0, ExitIndex);
            int lastRight = index > 0 && index < ExitIndex ? index : Mathf.Min(1, ExitIndex);
            Refresh(index, marked);
            yield return null;

            bool held = true;
            while (true)
            {
                if (submitAction.WasPressedThisFrame())
                {
                    yield return null;
                    onChosen(index);
                    yield break;
                }

                if (cancelAction.WasPressedThisFrame())
                {
                    yield return null;
                    onChosen(MenuList.Cancelled);
                    yield break;
                }

                Vector2 nav = navigateAction.ReadValue<Vector2>();
                if (nav.sqrMagnitude > 0.25f)
                {
                    if (!held)
                    {
                        int next = index;
                        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
                            next = nav.x < 0 ? 0 : (index == 0 ? lastRight : index);
                        else
                            next = (index + (nav.y < 0 ? 1 : -1) + ExitIndex + 1) % (ExitIndex + 1);

                        if (next != index)
                        {
                            index = next;
                            if (index > 0 && index < ExitIndex)
                                lastRight = index;
                            Refresh(index, marked);
                        }
                    }
                    held = true;
                }
                else
                {
                    held = false;
                }

                yield return null;
            }
        }

        void Refresh(int cursor, int marked)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                TeamMember member = i < TeamCount ? GameState.Save.team[i] : null;
                slots[i].Show(member);
                slots[i].SetLook(i == cursor, i == marked);
            }

            if (exitBackground != null)
                exitBackground.color = cursor == ExitIndex ? exitSelectedColor : exitNormalColor;
        }

        static string DisplayName(int index)
        {
            TeamMember member = GameState.Save.team[index];
            PokemonData species = PokemonDatabase.Get(member.speciesId);
            return (species != null ? species.displayName : member.speciesId).ToUpperInvariant();
        }
    }
}
