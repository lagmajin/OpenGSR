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
        private AbstractPlayer myPlayer;

        private void OnValidate()
        {
            if (!float.IsFinite(damageAmountPerShot) || damageAmountPerShot < 0f)
                damageAmountPerShot = 0f;
            if (!float.IsFinite(boosterRegenPerSecond) || boosterRegenPerSecond < 0f)
                boosterRegenPerSecond = 0f;
        }

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
            PlayerRegistry.Instance.OnPlayerRegistered += OnPlayerRegistered;
            PlayerRegistry.Instance.OnPlayerUnregistered += OnPlayerUnregistered;
            ResolveMyPlayer();

            Debug.Log("GameplayUIIntegration initialized");
        }

        private void OnDestroy()
        {
            if (PlayerRegistry.Instance != null)
            {
                PlayerRegistry.Instance.OnPlayerDied -= OnPlayerDied;
                PlayerRegistry.Instance.OnPlayerRespawned -= OnPlayerRespawned;
                PlayerRegistry.Instance.OnPlayerRegistered -= OnPlayerRegistered;
                PlayerRegistry.Instance.OnPlayerUnregistered -= OnPlayerUnregistered;
            }
            myPlayer = null;
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
            ResolveMyPlayer();

            var registry = PlayerRegistry.Instance;
            if (myPlayer == null || registry == null || damageAmountPerShot <= 0f)
            {
                return;
            }

            // Apply damage - UI will update automatically
            bool success = registry.ApplyDamage(
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
            var registry = PlayerRegistry.Instance;
            if (registry == null) return;

            if (myPlayer == null)
            {
                ResolveMyPlayer();
            }

            if (myPlayer == null) return;

            float currentBooster = myPlayer.GetBooster();
            float maxBooster = myPlayer.GetMaxBooster();

            if (!float.IsFinite(currentBooster) || !float.IsFinite(maxBooster) || maxBooster <= 0f)
                return;

            // Simple regeneration logic
            if (currentBooster < maxBooster)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
                deltaTime = Mathf.Min(deltaTime, 0.1f);
                float newBooster = Mathf.Min(
                    currentBooster + (boosterRegenPerSecond * deltaTime),
                    maxBooster
                );

                // Publish booster change event
                registry.PublishPlayerBooster(myPlayer, newBooster);
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

        private void OnPlayerRegistered(AbstractPlayer player)
        {
            if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
            {
                myPlayer = player;
            }
        }

        private void OnPlayerUnregistered(AbstractPlayer player)
        {
            if (player == myPlayer)
            {
                myPlayer = null;
            }
        }

        private void ResolveMyPlayer()
        {
            if (myPlayer != null || PlayerRegistry.Instance == null)
            {
                return;
            }

            foreach (var player in PlayerRegistry.Instance.GetAllPlayers())
            {
                if (player != null && player.PlayerType() == EPlayerType.MyPlayer)
                {
                    myPlayer = player;
                    break;
                }
            }
        }

        /// <summary>
        /// Example: Handle damage from external source (e.g., enemy)
        /// Call this from your damage logic
        /// </summary>
        public void PlayerTakesDamageFromEnemy(Guid victimId, Vector2 damageSource, float damageAmount)
        {
            var registry = PlayerRegistry.Instance;
            if (registry == null || !float.IsFinite(damageAmount) || damageAmount <= 0f ||
                !float.IsFinite(damageSource.x) || !float.IsFinite(damageSource.y))
                return;

            bool success = registry.ApplyDamage(
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
            var registry = PlayerRegistry.Instance;
            if (player == null || registry == null || kills < 0 || deaths < 0) return;

            player.Status.KillCount = kills;
            //player.Status.

            // Publish status change event to update UI
            registry.PublishPlayerStatus(player);
        }

        /// <summary>
        /// Example: Handle special booster consumption
        /// </summary>
        public void ConsumeBooster(AbstractPlayer player, float amount)
        {
            var registry = PlayerRegistry.Instance;
            if (player == null || registry == null || !float.IsFinite(amount) || amount <= 0f) return;

            float newBooster = Mathf.Max(player.GetBooster() - amount, 0f);
            if (!float.IsFinite(newBooster)) return;
            registry.PublishPlayerBooster(player, newBooster);
        }
    }
}

