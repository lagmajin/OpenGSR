using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class NetworkCombatReplay : MonoBehaviour
    {
        [SerializeField] private GameObject shotFlashPrefab;
        [SerializeField] private GameObject grenadeThrowFlashPrefab;
        [SerializeField] private float shotFlashLifetime = 0.12f;
        [SerializeField] private float grenadeFlashLifetime = 0.18f;
        [Tooltip("Enable detailed replay logs for spawned and destroyed network objects. Warnings are always shown.")]
        [SerializeField] private bool verboseReplayLogs = false;

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

        private void Start()
        {
            GameEventBroker.Subscribe<PlayerShotEvent>(HandlePlayerShot).AddTo(disposables);
            GameEventBroker.Subscribe<GrenadeThrowEvent>(HandleGrenadeThrow).AddTo(disposables);
            GameEventBroker.Subscribe<ObjectSpawnedEvent>(HandleObjectSpawned).AddTo(disposables);
            GameEventBroker.Subscribe<ObjectDestroyedEvent>(HandleObjectDestroyed).AddTo(disposables);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        private void HandlePlayerShot(PlayerShotEvent e)
        {
            if (e == null || IsLocalPlayer(e.PlayerID()))
            {
                return;
            }

            var position = new Vector3(e.Position().x, e.Position().y, 0f);
            var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(e.Direction().y, e.Direction().x) * Mathf.Rad2Deg);

            if (shotFlashPrefab != null)
            {
                Destroy(Instantiate(shotFlashPrefab, position, rotation), shotFlashLifetime);
            }
            else
            {
                SpawnSimpleFlash(position, Color.yellow, 0.12f);
            }
        }

        private void HandleGrenadeThrow(GrenadeThrowEvent e)
        {
            if (e == null || IsLocalPlayer(e.PlayerID()))
            {
                return;
            }

            var position = new Vector3(e.Position().x, e.Position().y, 0f);
            var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(e.Direction().y, e.Direction().x) * Mathf.Rad2Deg);

            if (grenadeThrowFlashPrefab != null)
            {
                Destroy(Instantiate(grenadeThrowFlashPrefab, position, rotation), grenadeFlashLifetime);
            }
            else
            {
                SpawnSimpleFlash(position, Color.cyan, 0.18f);
            }
        }

        private void HandleObjectSpawned(ObjectSpawnedEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (spawnedObjects.TryGetValue(e.ObjectID(), out var existing) && existing != null)
            {
                LogReplayEvent("Replacing existing", e.ObjectType(), e.ObjectID());
                Destroy(existing);
                spawnedObjects.Remove(e.ObjectID());
            }

            var prefab = ResolveSpawnPrefab(e.ObjectType());
            if (prefab == null)
            {
                LogReplayWarning("Missing prefab", e.ObjectType(), e.ObjectID());
                return;
            }

            var position = new Vector3(e.Position().x, e.Position().y, 0f);
            var rotation = Quaternion.Euler(0f, 0f, e.Rotation());
            var spawned = Instantiate(prefab, position, rotation);
            spawnedObjects[e.ObjectID()] = spawned;
            LogReplayEvent("Spawned", e.ObjectType(), e.ObjectID(), position);
            Destroy(spawned, ResolveLifetime(e.ObjectType()));
        }

        private void HandleObjectDestroyed(ObjectDestroyedEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (spawnedObjects.TryGetValue(e.ObjectID(), out var go) && go != null)
            {
                Destroy(go);
                LogReplayEvent("Removed", e.ObjectType(), e.ObjectID());
            }
            else
            {
                LogReplayWarning("Missing object for destroy", e.ObjectType(), e.ObjectID());
            }

            spawnedObjects.Remove(e.ObjectID());
        }

        private static void SpawnSimpleFlash(Vector3 position, Color color, float lifetime)
        {
            var go = new GameObject("NetworkCombatFlash");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/Bullet/Circle");
            if (sr.sprite == null)
            {
                Destroy(go);
                return;
            }

            sr.color = color;
            go.transform.localScale = Vector3.one * 0.08f;
            Destroy(go, lifetime);
        }

        private static GameObject ResolveSpawnPrefab(string objectType)
        {
            switch (objectType)
            {
                case "Bullet":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/Bullet");
                case "NormalGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/NormalGrenade");
                case "PowerGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/PowerGrenade");
                case "MagneticGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/MagneticGrenade");
                case "MineGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/MineGrenade");
                case "ClusterGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/ClusterGrenade");
                case "ChildClusterGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/ChildClusterGrenade");
                case "FireGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/FireGrenade");
                case "SmokeGrenade":
                    return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/SmokeGrenade");
                default:
                    return null;
            }
        }

        private static float ResolveLifetime(string objectType)
        {
            return objectType switch
            {
                "SmokeGrenade" => 3.5f,
                "ClusterGrenade" => 3.0f,
                "ChildClusterGrenade" => 1.25f,
                "PowerGrenade" => 3.0f,
                "MagneticGrenade" => 3.0f,
                "MineGrenade" => 3.0f,
                "FireGrenade" => 3.0f,
                _ => 0.5f
            };
        }

        private static bool IsLocalPlayer(string playerId)
        {
            var localId = AccountManager.Instance?.CurrentProfile?.GlobalUserId;
            return !string.IsNullOrWhiteSpace(localId) &&
                   string.Equals(localId, playerId, StringComparison.OrdinalIgnoreCase);
        }

        private void LogReplayEvent(string action, string objectType, string objectId, Vector3? position = null)
        {
            if (!verboseReplayLogs)
            {
                return;
            }

            var roomId = MatchRoomManager.Instance?.OnlineMatchRoom?.Id;
            var roomTag = string.IsNullOrWhiteSpace(roomId) ? "no-room" : roomId;

            if (position.HasValue)
            {
                Debug.Log($"[NetReplay] [{roomTag}] {action} '{objectType}' ({objectId}) at {position.Value}");
                return;
            }

            Debug.Log($"[NetReplay] [{roomTag}] {action} '{objectType}' ({objectId})");
        }

        private static void LogReplayWarning(string action, string objectType, string objectId)
        {
            var roomId = MatchRoomManager.Instance?.OnlineMatchRoom?.Id;
            var roomTag = string.IsNullOrWhiteSpace(roomId) ? "no-room" : roomId;
            Debug.LogWarning($"[NetReplay] [{roomTag}] {action} '{objectType}' ({objectId})");
        }
    }
}
