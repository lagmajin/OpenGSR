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
private IBattlePlayer cachedPlayer;
        private float lastHealthValue;
        [SerializeField] private float healthCheckInterval = 0.1f; // Check health every 0.1 seconds
        private float healthCheckTimer = 0f;

        private void OnValidate()
        {
            if (!float.IsFinite(healthCheckInterval)) healthCheckInterval = 0.1f;
            healthCheckInterval = Mathf.Max(0.02f, healthCheckInterval);
        }

        private void Start()
        {
            // Whichever tree carries the player. This asked for one specific
            // component, and a prefab that did not have it disabled itself with an
            // error: on every player prefab in the project, which meant this
            // monitor reported nothing anywhere.
            cachedPlayer = GetComponent<IBattlePlayer>();
            if (cachedPlayer == null)
            {
                Debug.LogError("PlayerHealthMonitor: no player component found on this GameObject");
                enabled = false;
                return;
            }

            lastHealthValue = GetSafeHealth(cachedPlayer.GetHP());
        }

        private void Update()
        {
            // A destroyed Unity component keeps comparing non-null as an interface, so
            // the monitor has to ask the object rather than the reference.
            if (cachedPlayer == null || cachedPlayer is not Object live || live == null) return;

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            healthCheckTimer += deltaTime;
            if (!float.IsFinite(healthCheckTimer))
            {
                healthCheckTimer = 0f;
                return;
            }
            if (healthCheckTimer < healthCheckInterval)
                return;

            healthCheckTimer = 0f;

            float currentHealth = GetSafeHealth(cachedPlayer.GetHP());
            if (!Mathf.Approximately(lastHealthValue, currentHealth))
            {
                // Health changed - publish to PlayerRegistry
                if (PlayerRegistry.Instance != null)
                {
                    PlayerRegistry.Instance.NotifyPlayerHealthChanged(cachedPlayer, currentHealth);
                }
                lastHealthValue = currentHealth;
            }
        }

        private static float GetSafeHealth(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }
    }
}

