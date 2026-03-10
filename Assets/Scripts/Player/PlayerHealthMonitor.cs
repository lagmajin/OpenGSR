using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Helper component that monitors a player's health and publishes changes to PlayerRegistry.
    /// Attach this to player prefabs to enable automatic health change events.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealthMonitor : MonoBehaviour
    {
        private AbstractPlayer cachedPlayer;
        private float lastHealthValue;
        private float healthCheckInterval = 0.1f; // Check health every 0.1 seconds
        private float healthCheckTimer = 0f;

        private void Start()
        {
            cachedPlayer = GetComponent<AbstractPlayer>();
            if (cachedPlayer == null)
            {
                Debug.LogError("PlayerHealthMonitor: AbstractPlayer component not found on this GameObject");
                enabled = false;
                return;
            }

            lastHealthValue = cachedPlayer.GetHP();
        }

        private void Update()
        {
            if (cachedPlayer == null) return;

            healthCheckTimer += Time.deltaTime;
            if (healthCheckTimer < healthCheckInterval)
                return;

            healthCheckTimer = 0f;

            float currentHealth = cachedPlayer.GetHP();
            if (!Mathf.Approximately(lastHealthValue, currentHealth))
            {
                // Health changed - publish to PlayerRegistry
                if (PlayerRegistry.Instance != null)
                {
                    //PlayerRegistry.Instance.OnPlayerHealthChanged?.Invoke(cachedPlayer, currentHealth);
                }
                lastHealthValue = currentHealth;
            }
        }
    }
}

