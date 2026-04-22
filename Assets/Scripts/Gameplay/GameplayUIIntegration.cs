using UnityEngine;
using OpenGSCore;
using System;

namespace OpenGS
{
    /// <summary>
    /// Example usage of the UI Management System in a game scene.
    /// This demonstrates how to use PlayerRegistry events in your game logic.
    /// </summary>
    public class GameplayUIIntegration : MonoBehaviour
    {
        [SerializeField] private Canvas gameplayCanvas;
        [SerializeField] private float damageAmountPerShot = 10f;
        [SerializeField] private float boosterRegenPerSecond = 5f;

        private void Start()
        {
            // Ensure PlayerRegistry exists
            if (PlayerRegistry.Instance == null)
            {
                Debug.LogError("PlayerRegistry not found in scene!");
                return;
            }

            // Subscribe to death event to show/hide HUD elements
            PlayerRegistry.Instance.OnPlayerDied += OnPlayerDied;
            PlayerRegistry.Instance.OnPlayerRespawned += OnPlayerRespawned;

            Debug.Log("GameplayUIIntegration initialized");
        }

        private void OnDestroy()
        {
            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerDied -= OnPlayerDied;
                PlayerRegistry.Instance.OnPlayerRespawned -= OnPlayerRespawned;
            }
        }

        private void Update()
        {
            // Example: Handle input for simulating damage
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SimulatePlayerTakingDamage();
            }

            // Example: Regenerate booster over time
            RegenerateBooster();
        }

        /// <summary>
        /// Simulate the player taking damage
        /// The UI will automatically update through the event system
        /// </summary>
        private void SimulatePlayerTakingDamage()
        {
            var players = PlayerRegistry.Instance.GetAllPlayers();
            var myPlayer = System.Linq.Enumerable.FirstOrDefault(
                players,
                p => p != null && p.PlayerType() == EPlayerType.MyPlayer
            );

            if (myPlayer == null)
            {
                Debug.LogWarning("Could not find MyPlayer");
                return;
            }

            // Apply damage - UI will update automatically
            bool success = PlayerRegistry.Instance.ApplyDamage(
                id: myPlayer.UniqueID(),
                source: Vector2.zero,
                damage: damageAmountPerShot,
                type: eDamageType.None
            );

            if (success)
            {
                Debug.Log($"Damage applied: {damageAmountPerShot}");
            }
        }

        /// <summary>
        /// Regenerate player booster over time
        /// </summary>
        private void RegenerateBooster()
        {
            var players = PlayerRegistry.Instance.GetAllPlayers();
            var myPlayer = System.Linq.Enumerable.FirstOrDefault(
                players,
                p => p != null && p.PlayerType() == EPlayerType.MyPlayer
            );

            if (myPlayer == null) return;

            float currentBooster = myPlayer.GetBooster();
            float maxBooster = myPlayer.GetMaxBooster();

            // Simple regeneration logic
            if (currentBooster < maxBooster)
            {
                float newBooster = Mathf.Min(
                    currentBooster + (boosterRegenPerSecond * Time.deltaTime),
                    maxBooster
                );

                // Publish booster change event
                PlayerRegistry.Instance.PublishPlayerBooster(myPlayer, newBooster);
            }
        }

        /// <summary>
        /// Called when player dies - hide gameplay UI
        /// </summary>
        private void OnPlayerDied(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer)
                return;

            Debug.Log("Player died - hiding gameplay UI");
            // Optionally disable gameplay-related UI
            // gameplayCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when player respawns - show gameplay UI
        /// </summary>
        private void OnPlayerRespawned(AbstractPlayer player)
        {
            if (player == null || player.PlayerType() != EPlayerType.MyPlayer)
                return;

            Debug.Log("Player respawned - showing gameplay UI");
            // Optionally enable gameplay-related UI
            // gameplayCanvas.gameObject.SetActive(true);
        }

        /// <summary>
        /// Example: Handle damage from external source (e.g., enemy)
        /// Call this from your damage logic
        /// </summary>
        public void PlayerTakesDamageFromEnemy(Guid victimId, Vector2 damageSource, float damageAmount)
        {
            bool success = PlayerRegistry.Instance.ApplyDamage(
                id: victimId,
                source: damageSource,
                damage: damageAmount,
                type: eDamageType.None
            );

            if (success)
            {
                Debug.Log($"Enemy dealt {damageAmount} damage");
            }
        }

        /// <summary>
        /// Example: Update player status (kills, deaths, etc.)
        /// </summary>
        public void UpdatePlayerStats(AbstractPlayer player, int kills, int deaths)
        {
            if (player == null) return;

            player.Status.KillCount = kills;
            //player.Status.

            // Publish status change event to update UI
            PlayerRegistry.Instance.PublishPlayerStatus(player);
        }

        /// <summary>
        /// Example: Handle special booster consumption
        /// </summary>
        public void ConsumeBooster(AbstractPlayer player, float amount)
        {
            if (player == null) return;

            float newBooster = Mathf.Max(player.GetBooster() - amount, 0f);
            PlayerRegistry.Instance.PublishPlayerBooster(player, newBooster);
        }
    }
}

