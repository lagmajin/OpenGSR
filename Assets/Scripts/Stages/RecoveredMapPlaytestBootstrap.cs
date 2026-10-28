using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Spawns the game's real player prefab into a recovered map preview for
    /// local movement and footing checks without the broken legacy match prefabs.
    /// </summary>
    public sealed class RecoveredMapPlaytestBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector2 spawnPosition;
        [SerializeField] private Camera targetCamera;

        public void Configure(GameObject prefab, Vector2 spawn, Camera camera)
        {
            playerPrefab = prefab;
            spawnPosition = spawn;
            targetCamera = camera;
        }

        private void Awake()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[RecoveredMapPlaytest] Player prefab is not assigned.", this);
                return;
            }

            var player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            player.name = "Recovered Map Playtest Player";

            var playerAgent = player.GetComponent<PlayerAgent>();
            if (playerAgent == null)
            {
                Debug.LogError("[RecoveredMapPlaytest] Player prefab has no PlayerAgent.", player);
                Destroy(player);
                return;
            }

            playerAgent.SetPlayerType(EPlayerType.MyPlayer);
            playerAgent.groundLayer = LayerMask.GetMask("Platforms");

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                var follow = targetCamera.GetComponent<RecoveredMapCameraFollow>();
                if (follow == null)
                {
                    follow = targetCamera.gameObject.AddComponent<RecoveredMapCameraFollow>();
                }
                follow.SetTarget(player.transform);
            }
            else
            {
                Debug.LogError("[RecoveredMapPlaytest] No MainCamera is available.", this);
            }
        }
    }
}
