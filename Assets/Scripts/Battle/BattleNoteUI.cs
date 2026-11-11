using System.Collections;
using TMPro;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TorreIngenieria.Battle
{
    /// <summary>
    /// Pantalla de la nota del jefe (Doc §8, pantalla 11): sello del piso, nota con coma decimal, Carné
    /// de acceso (solo el piso 3) y el dato de ingeniería del jefe. La construye
    /// <c>BattleSceneBuilder</c> dentro de la escena de combate y queda oculta hasta que
    /// <see cref="BattleSystem"/> vence a un jefe.
    ///
    /// Es solo presentación: recibe el <see cref="TrainerData"/> del jefe vencido y la nota ya calculada.
    /// </summary>
    public class BattleNoteUI : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text gradeText;
        [SerializeField] TMP_Text sealText;
        [SerializeField] TMP_Text cardText;
        [SerializeField] TMP_Text factText;
        [SerializeField] TMP_Text footerText;

        InputAction submitAction;

        void Awake()
        {
            submitAction = controls.FindAction("UI/Submit", true);
            SetVisible(false);
        }

        /// <summary>Muestra la evaluación del jefe y espera a que el jugador pulse Z/E/Enter.</summary>
        public IEnumerator Show(TrainerData trainer, float grade, bool accessCard)
        {
            string floor = trainer != null ? trainer.floorId : null;
            string floorName = string.IsNullOrEmpty(floor) ? "PISO" : Building.DisplayName(floor);

            titleText.text = $"{floorName} COMPLETADO";
            gradeText.text = $"NOTA {GameRules.FormatGrade(grade)}";
            sealText.text = trainer != null && !string.IsNullOrEmpty(trainer.sealId)
                ? $"SELLO DEL {floorName} OBTENIDO"
                : $"SELLO DEL {floorName}";

            // El Carné de acceso solo lo entrega el jefe del piso 3 (Doc §8); en los demás pisos la
            // línea se oculta para no prometer algo que no se da.
            cardText.gameObject.SetActive(accessCard);
            if (accessCard)
                cardText.text = "CARNÉ DE ACCESO OBTENIDO: LOS ASCENSORES DEL SUR VUELVEN A FUNCIONAR.";

            factText.text = trainer != null && !string.IsNullOrWhiteSpace(trainer.engineeringFact)
                ? $"DATO DE INGENIERÍA: {trainer.engineeringFact}"
                : string.Empty;
            footerText.text = "Z PARA CONTINUAR";

            SetVisible(true);
            yield return null; // evita que la misma pulsación que cerró el combate pase de largo
            while (!submitAction.WasPressedThisFrame())
                yield return null;

            SetVisible(false);
            yield return null;
        }

        void SetVisible(bool visible)
        {
            if (group == null)
                return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
