using System;
using System.Collections.Generic;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class NetworkCombatReplay : MonoBehaviour
    {
        [SerializeField] private GameObject shotFlashPrefab;
        [SerializeField] private float remotePredictedBulletLifetime = 2.0f;
        [SerializeField] private Color remotePredictedBulletColor = new Color(1f, 0.86f, 0.25f, 0.9f);
        [SerializeField] private GameObject grenadeThrowFlashPrefab;
        [SerializeField] private float shotFlashLifetime = 0.12f;
        [SerializeField] private float grenadeFlashLifetime = 0.18f;
        [Tooltip("Enable detailed replay logs for spawned and destroyed network objects. Warnings are always shown.")]
        [SerializeField] private bool verboseReplayLogs = false;
        [SerializeField] private float spawnedObjectCleanupInterval = 0.25f;

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, ShotPrediction> recentShots = new Dictionary<string, ShotPrediction>();
        private readonly Dictionary<string, GrenadePrediction> recentGrenades = new Dictionary<string, GrenadePrediction>();
        private readonly List<string> cleanupKeys = new List<string>(16);
        private static Sprite fallbackReplaySprite;
        private float nextSpawnedObjectCleanupTime;

        private void Awake()
        {
            remotePredictedBulletLifetime = float.IsFinite(remotePredictedBulletLifetime)
                ? Mathf.Max(0f, remotePredictedBulletLifetime)
                : 2f;
            shotFlashLifetime = float.IsFinite(shotFlashLifetime) ? Mathf.Max(0f, shotFlashLifetime) : 0.12f;
            grenadeFlashLifetime = float.IsFinite(grenadeFlashLifetime) ? Mathf.Max(0f, grenadeFlashLifetime) : 0.18f;
            spawnedObjectCleanupInterval = float.IsFinite(spawnedObjectCleanupInterval)
                ? Mathf.Max(0.05f, spawnedObjectCleanupInterval)
                : 0.25f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(remotePredictedBulletLifetime) || remotePredictedBulletLifetime < 0f)
                remotePredictedBulletLifetime = 2f;
            if (!float.IsFinite(shotFlashLifetime) || shotFlashLifetime < 0f)
                shotFlashLifetime = 0.12f;
            if (!float.IsFinite(grenadeFlashLifetime) || grenadeFlashLifetime < 0f)
                grenadeFlashLifetime = 0.18f;
            if (!float.IsFinite(spawnedObjectCleanupInterval) || spawnedObjectCleanupInterval < 0.05f)
                spawnedObjectCleanupInterval = 0.25f;
        }

        private void Update()
        {
            if (spawnedObjects.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!float.IsFinite(now) || now < 0f || now < nextSpawnedObjectCleanupTime)
            {
                return;
            }

            nextSpawnedObjectCleanupTime = now + Mathf.Max(0.05f, spawnedObjectCleanupInterval);

            cleanupKeys.Clear();
            foreach (var pair in spawnedObjects)
            {
                if (pair.Value != null)
                {
                    continue;
                }

                cleanupKeys.Add(pair.Key);
            }

            if (cleanupKeys.Count == 0)
            {
                return;
            }

            foreach (var objectId in cleanupKeys)
            {
                spawnedObjects.Remove(objectId);
            }
        }

        private readonly struct ShotPrediction
        {
            public ShotPrediction(Vector2 position, Vector2 direction, string weaponType, float timestamp)
            {
                Position = position;
                Direction = direction;
                WeaponType = weaponType;
                Timestamp = timestamp;
            }

            public Vector2 Position { get; }
            public Vector2 Direction { get; }
            public string WeaponType { get; }
            public float Timestamp { get; }
        }

        private readonly struct GrenadePrediction
        {
            public GrenadePrediction(Vector2 position, Vector2 direction, EGrenadeType grenadeType, float power, float timestamp)
            {
                Position = position;
                Direction = direction;
                GrenadeType = grenadeType;
                Power = power;
                Timestamp = timestamp;
            }

            public Vector2 Position { get; }
            public Vector2 Direction { get; }
            public EGrenadeType GrenadeType { get; }
            public float Power { get; }
            public float Timestamp { get; }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEventBroker.Subscribe<PlayerShotEvent>(HandlePlayerShot).AddTo(disposables);
            GameEventBroker.Subscribe<GrenadeThrowEvent>(HandleGrenadeThrow).AddTo(disposables);
            GameEventBroker.Subscribe<ObjectSpawnedEvent>(HandleObjectSpawned).AddTo(disposables);
            GameEventBroker.Subscribe<ObjectDestroyedEvent>(HandleObjectDestroyed).AddTo(disposables);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            disposables.Clear();

            foreach (var pair in spawnedObjects)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            spawnedObjects.Clear();
            recentShots.Clear();
            recentGrenades.Clear();
            nextSpawnedObjectCleanupTime = 0f;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearSpawnedObjects();
            disposables.Dispose();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearSpawnedObjects();
            recentShots.Clear();
            recentGrenades.Clear();
        }

        private void ClearSpawnedObjects()
        {
            foreach (var pair in spawnedObjects)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            spawnedObjects.Clear();
        }

        private void HandlePlayerShot(PlayerShotEvent e)
        {
            if (e == null || IsLocalPlayer(e.PlayerID()))
            {
                return;
            }

            if (!IsFinite(e.Position()) || !IsFinite(e.Direction()) || e.Direction().sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (!TryGetSafeTime(out var now))
            {
                return;
            }

            recentShots[e.PlayerID()] = new ShotPrediction(e.Position(), e.Direction(), e.WeaponType(), now);
            CleanupExpiredShotCache();

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
            if (e == null)
            {
                return;
            }

            if (!IsFinite(e.Position()) || !IsFinite(e.Direction()) || e.Direction().sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            if (!TryGetSafeTime(out var now))
            {
                return;
            }

            recentGrenades[e.PlayerID()] = new GrenadePrediction(
                e.Position(),
                e.Direction(),
                ResolveGrenadeTypeFromEvent(e.GrenadeType()),
                Mathf.Max(0f, SafeFinite(e.Power(), 1f)),
                now);
            CleanupExpiredGrenadeCache();

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

        private static float SafeFinite(float value, float fallback)
        {
            return float.IsFinite(value) ? value : fallback;
        }

        private static bool TryGetSafeTime(out float now)
        {
            now = Time.time;
            return float.IsFinite(now) && now >= 0f;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private void HandleObjectSpawned(ObjectSpawnedEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(e.ObjectID()))
            {
                LogReplayWarning("Ignored empty object id", e.ObjectType(), string.Empty);
                return;
            }

            if (!IsFinite(e.Position()))
            {
                LogReplayWarning("Ignored non-finite position", e.ObjectType(), e.ObjectID());
                return;
            }

            // LiteNetLib can deliver the same reliable event more than once
            // across reconnect/rejoin boundaries. Do not respawn an object that
            // is already represented locally.
            if (spawnedObjects.TryGetValue(e.ObjectID(), out var alreadySpawned) && alreadySpawned != null)
            {
                LogReplayEvent("Ignored duplicate", e.ObjectType(), e.ObjectID());
                return;
            }

            if (string.Equals(e.ObjectType(), "Bullet", StringComparison.OrdinalIgnoreCase))
            {
                if (TrySpawnPredictedBullet(e))
                {
                    return;
                }
            }

            if (IsGrenadeObjectType(e.ObjectType()) && TrySpawnPredictedGrenade(e))
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
            var usingFallbackPrefab = false;
            if (prefab == null)
            {
                LogReplayWarning("Missing prefab; using fallback visual", e.ObjectType(), e.ObjectID());
                prefab = CreateFallbackProjectilePrefab(e.ObjectType());
                usingFallbackPrefab = prefab != null;
                if (prefab == null)
                {
                    return;
                }
            }

            var position = new Vector3(e.Position().x, e.Position().y, 0f);
            var rotation = Quaternion.Euler(0f, 0f, float.IsFinite(e.Rotation()) ? e.Rotation() : 0f);
            var spawned = Instantiate(prefab, position, rotation);
            if (usingFallbackPrefab)
            {
                Destroy(prefab);
            }
            spawnedObjects[e.ObjectID()] = spawned;
            LogReplayEvent("Spawned", e.ObjectType(), e.ObjectID(), position);
            Destroy(spawned, ResolveLifetime(e.ObjectType()));
        }

        private static GameObject CreateFallbackProjectilePrefab(string objectType)
        {
            var normalized = objectType ?? string.Empty;
            var isGrenade = IsGrenadeObjectType(normalized);
            var primitive = GameObject.CreatePrimitive(isGrenade ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            primitive.name = $"ReplayFallback_{normalized}";
            primitive.transform.localScale = isGrenade
                ? Vector3.one * 0.18f
                : new Vector3(0.06f, 0.28f, 0.06f);

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    var material = new Material(shader);
                    material.color = isGrenade
                        ? new Color(1f, 0.35f, 0.1f, 0.9f)
                        : new Color(1f, 0.9f, 0.25f, 0.95f);
                    renderer.sharedMaterial = material;
                }
            }

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return primitive;
        }

        private void HandleObjectDestroyed(ObjectDestroyedEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (spawnedObjects.TryGetValue(e.ObjectID(), out var go) && go != null)
            {
                TryInvokeRemoteGrenadeExplosion(go, e.Position());

                Destroy(go);
                LogReplayEvent("Removed", e.DestroyedBy(), e.ObjectID());
            }
            else
            {
                LogReplayWarning("Missing object for destroy", e.DestroyedBy(), e.ObjectID());
            }

            spawnedObjects.Remove(e.ObjectID());
        }

        private static void SpawnSimpleFlash(Vector3 position, Color color, float lifetime)
        {
            var go = new GameObject("NetworkCombatFlash");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveReplaySprite();
            if (sr.sprite == null)
            {
                Destroy(go);
                return;
            }

            sr.color = color;
            go.transform.localScale = Vector3.one * 0.08f;
            Destroy(go, lifetime);
        }

        private bool TrySpawnPredictedBullet(ObjectSpawnedEvent e)
        {
            if (e == null)
            {
                return false;
            }

            var ownerId = ExtractOwnerId(e.ObjectID());
            if (IsLocalPlayer(ownerId))
            {
                return true;
            }

            var position = e.Position();
            if (!IsFinite(position))
            {
                return false;
            }

            var direction = Quaternion.Euler(0f, 0f, float.IsFinite(e.Rotation()) ? e.Rotation() : 0f) * Vector2.right;
            var weaponType = ResolveWeaponTypeFromCache(ownerId);
            var speed = ResolvePredictedBulletSpeed(weaponType);
            var lifetime = remotePredictedBulletLifetime;

            if (TryGetSafeTime(out var now) && recentShots.TryGetValue(ownerId, out var shot))
            {
                if (now - shot.Timestamp <= 1.25f)
                {
                    if (IsFinite(shot.Position))
                    {
                        position = shot.Position;
                    }

                    if (IsFinite(shot.Direction) && shot.Direction.sqrMagnitude > Mathf.Epsilon)
                    {
                        direction = shot.Direction.normalized;
                    }
                }
            }

            SpawnPredictedBullet(position, direction, speed, lifetime);
            return true;
        }

        private void SpawnPredictedBullet(Vector2 position, Vector2 direction, float speed, float lifetime)
        {
            var bullet = new GameObject("RemotePredictedBullet");
            bullet.transform.position = position;

            var spriteRenderer = bullet.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ResolveReplaySprite();
            if (spriteRenderer.sprite == null)
            {
                Destroy(bullet);
                return;
            }

            spriteRenderer.color = remotePredictedBulletColor;
            bullet.transform.localScale = Vector3.one * 0.07f;

            var mover = bullet.AddComponent<RemoteShotVisual>();
            mover.Initialize(direction, speed, lifetime);
        }

        private static Sprite ResolveReplaySprite()
        {
            var sprite = Resources.Load<Sprite>("Sprites/Bullet/Circle");
            if (sprite != null)
            {
                return sprite;
            }

            if (fallbackReplaySprite == null && Texture2D.whiteTexture != null)
            {
                fallbackReplaySprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                fallbackReplaySprite.name = "ReplayFallbackSprite";
            }

            return fallbackReplaySprite;
        }

        private bool TrySpawnPredictedGrenade(ObjectSpawnedEvent e)
        {
            if (e == null)
            {
                return false;
            }

            var grenadeType = ResolveGrenadeTypeFromObjectType(e.ObjectType());
            if (grenadeType == EGrenadeType.Empty)
            {
                return false;
            }

            var ownerId = ExtractOwnerId(e.ObjectID());
            var position = e.Position();
            if (!IsFinite(position))
            {
                return false;
            }

            var direction = Quaternion.Euler(0f, 0f, float.IsFinite(e.Rotation()) ? e.Rotation() : 0f) * Vector2.right;
            var power = 1f;

            if (TryGetSafeTime(out var now) && recentGrenades.TryGetValue(ownerId, out var grenade))
            {
                if (now - grenade.Timestamp <= 3f)
                {
                    if (IsFinite(grenade.Position))
                    {
                        position = grenade.Position;
                    }

                    if (IsFinite(grenade.Position) && IsFinite(grenade.Direction) && grenade.Direction.sqrMagnitude > Mathf.Epsilon)
                    {
                        direction = grenade.Direction.normalized;
                    }

                    if (grenade.GrenadeType != EGrenadeType.Empty)
                    {
                        grenadeType = grenade.GrenadeType;
                    }

                    power = grenade.Power;
                }
            }

            var speed = ResolvePredictedGrenadeSpeed(power);
            var gravity = ResolvePredictedGrenadeGravity(grenadeType);
            var lifetime = ResolveLifetime(e.ObjectType());

            SpawnPredictedGrenade(e.ObjectID(), position, direction, grenadeType, speed, gravity, lifetime);
            return true;
        }

        private void SpawnPredictedGrenade(
            string objectId,
            Vector2 position,
            Vector2 direction,
            EGrenadeType grenadeType,
            float speed,
            float gravity,
            float lifetime)
        {
            if (spawnedObjects.TryGetValue(objectId, out var existing) && existing != null)
            {
                Destroy(existing);
                spawnedObjects.Remove(objectId);
            }

            var grenade = new GameObject("RemotePredictedGrenade");
            grenade.transform.position = position;

            var spriteRenderer = grenade.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ResolveGrenadeSprite(grenadeType) ?? ResolveReplaySprite();
            spriteRenderer.sortingOrder = 10;
            if (spriteRenderer.sprite == null)
            {
                Destroy(grenade);
                return;
            }

            grenade.transform.localScale = Vector3.one * 0.09f;

            var moverType = ResolveRemoteGrenadeVisualType();
            if (moverType != null)
            {
                var mover = grenade.AddComponent(moverType);
                var init = moverType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                init?.Invoke(mover, new object[] { direction, speed, gravity, lifetime, grenadeType, (Action)(() => spawnedObjects.Remove(objectId)) });
            }

            spawnedObjects[objectId] = grenade;
            LogReplayEvent("Spawned", grenadeType.ToString(), objectId, position);
            Destroy(grenade, lifetime + 0.5f);
        }

        private void CleanupExpiredShotCache()
        {
            if (recentShots.Count == 0)
            {
                return;
            }

            if (!TryGetSafeTime(out var now))
            {
                return;
            }
            cleanupKeys.Clear();
            foreach (var kv in recentShots)
            {
                if (now - kv.Value.Timestamp > 1.5f)
                {
                    cleanupKeys.Add(kv.Key);
                }
            }

            foreach (var key in cleanupKeys)
            {
                recentShots.Remove(key);
            }
        }

        private void CleanupExpiredGrenadeCache()
        {
            if (recentGrenades.Count == 0)
            {
                return;
            }

            if (!TryGetSafeTime(out var now))
            {
                return;
            }
            cleanupKeys.Clear();
            foreach (var kv in recentGrenades)
            {
                if (now - kv.Value.Timestamp > 4f)
                {
                    cleanupKeys.Add(kv.Key);
                }
            }

            foreach (var key in cleanupKeys)
            {
                recentGrenades.Remove(key);
            }
        }

        private static string ExtractOwnerId(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                return string.Empty;
            }

            const string bulletMarker = "_bullet_";
            var index = objectId.IndexOf(bulletMarker, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                return objectId.Substring(0, index);
            }

            const string grenadeMarker = "_grenade_";
            index = objectId.IndexOf(grenadeMarker, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                return objectId.Substring(0, index);
            }

            var underscore = objectId.IndexOf('_');
            return underscore > 0 ? objectId.Substring(0, underscore) : objectId;
        }

        private string ResolveWeaponTypeFromCache(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return "Unknown";
            }

            return recentShots.TryGetValue(ownerId, out var shot) ? shot.WeaponType : "Unknown";
        }

        private static EGrenadeType ResolveGrenadeTypeFromEvent(string grenadeType)
        {
            if (string.IsNullOrWhiteSpace(grenadeType))
            {
                return EGrenadeType.Normal;
            }

            return grenadeType.ToLowerInvariant() switch
            {
                "powergrenade" => EGrenadeType.Power,
                "power" => EGrenadeType.Power,
                "magneticgrenade" => EGrenadeType.Magnetic,
                "magnetic" => EGrenadeType.Magnetic,
                "minegrenade" => EGrenadeType.Mine,
                "mine" => EGrenadeType.Mine,
                "clustergrenade" => EGrenadeType.Cluster,
                "cluster" => EGrenadeType.Cluster,
                "childclustergrenade" => EGrenadeType.ClusterChild,
                "clusterchild" => EGrenadeType.ClusterChild,
                "firegrenade" => EGrenadeType.Fire,
                "fire" => EGrenadeType.Fire,
                "smokegrenade" => EGrenadeType.Smoke,
                "smoke" => EGrenadeType.Smoke,
                _ => EGrenadeType.Normal
            };
        }

        private static EGrenadeType ResolveGrenadeTypeFromObjectType(string objectType)
        {
            if (string.IsNullOrWhiteSpace(objectType))
            {
                return EGrenadeType.Empty;
            }

            return objectType.ToLowerInvariant() switch
            {
                "normalgrenade" => EGrenadeType.Normal,
                "powergrenade" => EGrenadeType.Power,
                "magneticgrenade" => EGrenadeType.Magnetic,
                "minegrenade" => EGrenadeType.Mine,
                "clustergrenade" => EGrenadeType.Cluster,
                "childclustergrenade" => EGrenadeType.ClusterChild,
                "firegrenade" => EGrenadeType.Fire,
                "smokegrenade" => EGrenadeType.Smoke,
                _ => EGrenadeType.Empty
            };
        }

        private static bool IsGrenadeObjectType(string objectType)
        {
            return ResolveGrenadeTypeFromObjectType(objectType) != EGrenadeType.Empty;
        }

        private static Sprite ResolveGrenadeSprite(EGrenadeType grenadeType)
        {
            return GrenadeVisualResolver.GetHudSprite(grenadeType);
        }

        private static Type ResolveRemoteGrenadeVisualType()
        {
            const string fullName = "OpenGS.RemoteGrenadeVisual";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void TryInvokeRemoteGrenadeExplosion(GameObject go, Vector2 position)
        {
            if (go == null)
            {
                return;
            }

            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component == null)
                {
                    continue;
                }

                var method = component.GetType().GetMethod("ForceExplosion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                {
                    continue;
                }

                method.Invoke(component, new object[] { position });
                return;
            }
        }

        private static float ResolvePredictedGrenadeSpeed(float power)
        {
            return Mathf.Max(0f, 20f * Mathf.Clamp(power, 0.1f, 1f));
        }

        private static float ResolvePredictedGrenadeGravity(EGrenadeType grenadeType)
        {
            return grenadeType == EGrenadeType.Smoke ? 18f : 18f;
        }

        private static float ResolvePredictedBulletSpeed(string weaponType)
        {
            if (string.IsNullOrWhiteSpace(weaponType))
            {
                return 100f;
            }

            var normalized = weaponType.ToLowerInvariant();
            if (normalized.Contains("sniper") || normalized.Contains("awp") || normalized.Contains("dragunov") || normalized.Contains("psg"))
            {
                return 180f;
            }

            if (normalized.Contains("smg") || normalized.Contains("uzi") || normalized.Contains("mp5") || normalized.Contains("scorpion") || normalized.Contains("p90"))
            {
                return 130f;
            }

            if (normalized.Contains("shotgun") || normalized.Contains("spas") || normalized.Contains("benelli") || normalized.Contains("m3") || normalized.Contains("usas"))
            {
                return 110f;
            }

            if (normalized.Contains("launcher") || normalized.Contains("m79") || normalized.Contains("hk69") || normalized.Contains("gm94"))
            {
                return 90f;
            }

            return 100f;
        }

        private static GameObject ResolveSpawnPrefab(string objectType)
        {
            var grenadeType = ResolveGrenadeTypeFromObjectType(objectType);
            if (grenadeType != EGrenadeType.Empty)
            {
                var grenadeMasterData = Resources.Load<AllGrenadeListMasterData>("MasterData/Grenade/AllGrenadeListMasterData");
                var grenadePrefab = GrenadeVisualResolver.GetProjectilePrefab(grenadeType, grenadeMasterData);
                if (grenadePrefab != null)
                {
                    return grenadePrefab;
                }
            }

            switch (objectType)
            {
                case "Bullet":
                    return ResolveBulletReplayPrefab();
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

        private static GameObject ResolveBulletReplayPrefab()
        {
            var resourcePrefab = Resources.Load<GameObject>("Prefabs/Weapon/Projectile/Bullet");
            if (resourcePrefab != null)
            {
                return resourcePrefab;
            }

            foreach (var gun in FindObjectsByType<AbstractGunController>(FindObjectsSortMode.None))
            {
                if (gun == null || gun.bulletPrefab == null)
                {
                    continue;
                }

                if (gun.bulletPrefab.GetComponent<BulletController>() != null)
                {
                    return gun.bulletPrefab;
                }
            }

            return null;
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

            var roomManager = ResolveMatchRoomManager();
            var roomId = roomManager?.OnlineMatchRoom?.Id;
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
            var roomManager = ResolveMatchRoomManager();
            var roomId = roomManager?.OnlineMatchRoom?.Id;
            var roomTag = string.IsNullOrWhiteSpace(roomId) ? "no-room" : roomId;
            Debug.LogWarning($"[NetReplay] [{roomTag}] {action} '{objectType}' ({objectId})");
        }

        private static MatchRoomManager ResolveMatchRoomManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch
            {
                return null;
            }
        }
    }
}
