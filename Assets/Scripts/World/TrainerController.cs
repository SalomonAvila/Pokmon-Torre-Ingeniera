using System.Collections;
using TorreIngenieria.Battle;
using TorreIngenieria.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TorreIngenieria.World
{
    /// <summary>
    /// Entrenador del mapa (Doc §7 y §8): ve al jugador en línea recta hasta
    /// <see cref="GameRules.TrainerSightRange"/> casillas, camina hasta él, lo reta y arranca el combate.
    /// Se enfrenta una sola vez: <see cref="GameState.HasDefeatedTrainer"/> apaga la visión y, si se le
    /// vuelve a hablar, da la pista del piso. Tampoco deja huir del combate (lo impone el motor).
    ///
    /// Hereda de <see cref="Interactable"/> para ser la misma casilla que bloquea el paso y muestra la
    /// burbuja de diálogo. El builder lo crea con <c>ProvisionalMap.CreateTrainer</c>.
    /// </summary>
    public class TrainerController : Interactable
    {
        [SerializeField, Min(1), Tooltip("Casillas de visión en línea recta.")]
        int sightRange = GameRules.TrainerSightRange;

        [SerializeField, Tooltip("Dirección en la que mira; define la línea de visión y la de acercamiento.")]
        PlayerController.Direction facing = PlayerController.Direction.Down;

        [SerializeField, Tooltip("Segundos por casilla al acercarse al jugador.")]
        float stepDuration = 0.12f;

        Tilemap collisionLayer;
        PlayerController player;
        bool approaching;
        bool wasInSight;
        bool hasBaseline;
        float nextSightCheck;

        /// <summary>Antes de vencerlo cuenta como desafío; después, como pista del piso.</summary>
        public override string DialogueKey => IsTrainerDefeated ? "defeated" : "challenge";

        protected override void Awake()
        {
            base.Awake();
            collisionLayer = FindCollisionLayer();
        }

        void Update()
        {
            if (approaching || IsTrainerDefeated)
                return;
            if (WorldMessage.IsOpen || SceneTransition.IsTransitioning)
                return;

            // La primera lectura con el mundo en marcha es la de referencia. Se toma aquí y no en Start
            // porque el jugador se recoloca en PlayerController.Start: al volver de un combate perdido
            // reaparece dentro de la vista y solo se le reta cuando sale de la línea y vuelve a entrar,
            // así no queda atrapado en un bucle de derrotas sin poder ir a curarse.
            if (!hasBaseline)
            {
                hasBaseline = true;
                wasInSight = TrySpotPlayer(out _);
                nextSightCheck = Time.unscaledTime + 0.15f;
                return;
            }

            if (Time.unscaledTime < nextSightCheck)
                return;

            bool inSight = TrySpotPlayer(out Vector3Int targetCell);
            bool entered = inSight && !wasInSight;
            wasInSight = inSight;
            if (!entered)
            {
                nextSightCheck = Time.unscaledTime + 0.2f;
                return;
            }

            StartCoroutine(Approach(targetCell));
        }

        /// <summary>Hablarle: reta (si sigue en pie) o recuerda la pista del piso (si ya fue vencido).</summary>
        protected override void InteractTrainer()
        {
            if (IsTrainerDefeated)
            {
                ShowTrainerHint();
                return;
            }

            Challenge();
        }

        /// <summary>Muestra el desafío y, al terminar las líneas, arranca el combate. Lo usa la visión.</summary>
        void Challenge()
        {
            if (WorldMessage.IsOpen || SceneTransition.IsTransitioning)
                return;

            MarkDialogueSeen();
            WorldMessage.Show(Lines, null, _ => StartBattle());
        }

        IEnumerator Approach(Vector3Int targetCell)
        {
            approaching = true;

            // La casilla de destino es la que queda pegada al jugador por el lado del entrenador: la
            // visión es recta, así que se avanza en la dirección en la que mira.
            Vector2Int step = Step(facing);
            while (CellOf(transform.position) != targetCell)
            {
                Vector3 from = transform.position;
                Vector3 to = from + new Vector3(step.x, step.y, 0f);
                for (float elapsed = 0f; elapsed < stepDuration; elapsed += Time.deltaTime)
                {
                    transform.position = Vector3.Lerp(from, to, elapsed / stepDuration);
                    yield return null;
                }

                transform.position = to;
                yield return null;
            }

            approaching = false;
            Challenge();
        }

        /// <summary>
        /// ¿El jugador está en la línea de visión? Devuelve la casilla a la que hay que acercarse y corta
        /// la mirada en cuanto encuentra una pared, un objeto o un personaje en medio.
        /// </summary>
        bool TrySpotPlayer(out Vector3Int targetCell)
        {
            targetCell = default;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return false;

            Vector3Int origin = CellOf(transform.position);
            Vector3Int playerCell = CellOf(player.transform.position);
            Vector2Int step = Step(facing);

            for (int distance = 1; distance <= sightRange; distance++)
            {
                var cell = new Vector3Int(origin.x + step.x * distance, origin.y + step.y * distance, 0);
                if (cell == playerCell)
                {
                    targetCell = new Vector3Int(cell.x - step.x, cell.y - step.y, 0);
                    return true;
                }

                if (Blocked(cell))
                    return false;
            }

            return false;
        }

        bool Blocked(Vector3Int cell) =>
            (collisionLayer != null && collisionLayer.HasTile(cell))
            || Interactable.At(CellCenter(cell)) != null;

        static Vector3Int CellOf(Vector3 position) =>
            new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), 0);

        static Vector3 CellCenter(Vector3Int cell) => new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        static Vector2Int Step(PlayerController.Direction direction) => direction switch
        {
            PlayerController.Direction.Up => Vector2Int.up,
            PlayerController.Direction.Left => Vector2Int.left,
            PlayerController.Direction.Right => Vector2Int.right,
            _ => Vector2Int.down,
        };

        /// <summary>Capa de colisiones del mapa (el PlayerController usa la misma para bloquear el paso).</summary>
        static Tilemap FindCollisionLayer()
        {
            foreach (Tilemap tilemap in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
                if (tilemap.gameObject.name == "Colisiones")
                    return tilemap;
            return null;
        }

        void StartBattle()
        {
            if (string.IsNullOrEmpty(TrainerId) || !TrainerDatabase.Exists(TrainerId))
            {
                Debug.LogError($"La casilla '{name}' apunta a un entrenador que no existe: '{TrainerId}'.");
                WorldMessage.Show("Este entrenador todavía no tiene combate configurado.");
                return;
            }

            Vector3 position = player != null ? player.transform.position : transform.position;
            int facingValue = player != null ? (int)player.Facing : (int)PlayerController.Direction.Down;

            BattleRequest.StartTrainer(TrainerId, SceneManager.GetActiveScene().name, position, facingValue);

            if (!SceneTransition.Go(SceneNames.Battle, null))
            {
                BattleRequest.Clear();
                WorldMessage.Show("El combate no está disponible: revisa que la escena Battle esté en Build Profiles.");
            }
        }
    }
}
