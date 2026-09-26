using Sirenix.OdinInspector;
using OpenGSCore;
using UnityEngine;
using System;



namespace OpenGS
{

    [DisallowMultipleComponent]
    public class PlayerGrenadeComponent : MonoBehaviour
    {
        [SerializeField] public AllGrenadeListMasterData grenadeListMasterData;
        [SerializeField] private EGrenadeType grenadeType = EGrenadeType.Normal;

        [Header("Grenade Throw Settings")]
        [SerializeField] private float maxChargeTime = 2.0f; // パワー1.0になるまでの時間（秒）
        [SerializeField] private float minPower = 0.1f;
        [SerializeField] private float maxPower = 1.0f;
        [SerializeField] private float baseThrowForce = 20f; // 基準となる投擲の強さ

        [Header("References")]
        [SerializeField] private Transform throwPoint; // 投げる位置の起点

        private bool isCharging = false;
        private float currentChargeTime = 0f;
        private float lastThrowTime = -999f;
        private PlayerAgent playerAgent;
        private AbstractPlayer abstractPlayer;
        private IInputService inputService = new UnityInputService();
        
        // UI 用に現在のパワー (0.0 ~ 1.0) を公開する場合に使う
        public float CurrentChargeRatio => isCharging ? Mathf.Clamp01(currentChargeTime / maxChargeTime) : 0f;
        public EGrenadeType CurrentGrenadeType => grenadeType;

        [Zenject.Inject]
        private void Construct(IInputService resolvedInputService)
        {
            if (resolvedInputService != null)
            {
                inputService = resolvedInputService;
            }
        }

        private void Awake()
        {
            maxChargeTime = float.IsFinite(maxChargeTime) ? Mathf.Max(0.01f, maxChargeTime) : 2f;
            minPower = float.IsFinite(minPower) ? Mathf.Clamp01(minPower) : 0.1f;
            maxPower = float.IsFinite(maxPower) ? Mathf.Clamp01(maxPower) : 1f;
            if (maxPower < minPower)
            {
                maxPower = minPower;
            }

            baseThrowForce = float.IsFinite(baseThrowForce) ? Mathf.Max(0f, baseThrowForce) : 20f;
        }

        private void OnDisable()
        {
            isCharging = false;
            currentChargeTime = 0f;
        }

        void Start()
        {
            playerAgent = GetComponent<PlayerAgent>();
            abstractPlayer = GetComponent<AbstractPlayer>();
            if (throwPoint == null)
            {
                throwPoint = transform; // 設定されてなければ自身の位置から
            }

            playerAgent?.RefillNormalGrenadeIfEmpty();
        }

        void Update()
        {
            // ジャンプ（Space）と投擲を同じキーにしない。
            if (inputService.IsGrenadeJustPressed())
            {
                isCharging = true;
                currentChargeTime = 0f;
                abstractPlayer?.TryPlayGeneralSound(EPlayerGeneralSound.OpenGrenade);
            }

            // グレネードキー長押し中（パワーを溜める）
            if (isCharging && inputService.IsGrenadePressed())
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    return;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                currentChargeTime = Mathf.Min(maxChargeTime, currentChargeTime + deltaTime);
            }

            // グレネードキーを離した瞬間（投げる）
            if (isCharging && inputService.IsGrenadeJustReleased())
            {
                isCharging = false;
                
                // 比率 (0.0 ~ 1.0) を元にパワー (minPower ~ maxPower) を決定
                float ratio = currentChargeTime / maxChargeTime;
                float powerMultiplier = Mathf.Lerp(minPower, maxPower, ratio);
                
                abstractPlayer?.TryPlayGeneralSound(EPlayerGeneralSound.ThrowGrenade);
                ThrowGrenade(powerMultiplier);
            }
        }

        [Button("オートセット")]
        private void AutoSet()
        {
            if (throwPoint == null)
            {
                throwPoint = transform;
            }

            if (grenadeListMasterData == null)
            {
                Debug.LogWarning("[PlayerGrenadeComponent] Grenade master data is not assigned.");
            }
        }

        [Button("グレネード投擲(テスト用)")]
        private void TestThrow()
        {
            ThrowGrenade(1.0f);
        }

        public void SetGrenadeType(EGrenadeType type)
        {
            grenadeType = type;
        }

        private GrenadeEntry ResolveGrenadeEntry()
        {
            var prefab = GrenadeVisualResolver.GetProjectilePrefab(grenadeType, grenadeListMasterData);
            if (prefab == null)
            {
                return null;
            }

            return new GrenadeEntry
            {
                Name = GrenadeVisualResolver.GetInternalName(grenadeType),
                GrenadePrefab = prefab
            };
        }

        private GrenadeEntry LoadSmokeGrenadeEntry()
        {
            var smokePrefab = GrenadeVisualResolver.GetProjectilePrefab(EGrenadeType.Smoke, grenadeListMasterData);
            if (smokePrefab == null)
            {
                Debug.LogWarning("[PlayerGrenadeComponent] Smoke grenade prefab was not found.");
                return null;
            }

            return new GrenadeEntry
            {
                Name = GrenadeVisualResolver.GetInternalName(EGrenadeType.Smoke),
                GrenadePrefab = smokePrefab
            };
        }

        public void ThrowGrenade(float powerMultiplier)
        {
            powerMultiplier = float.IsFinite(powerMultiplier)
                ? Mathf.Clamp(powerMultiplier, minPower, maxPower)
                : minPower;
            var now = Time.time;
            if (!float.IsFinite(now) || now < 0f)
            {
                return;
            }

            if (now - lastThrowTime < 0.5f)
            {
                Debug.LogWarning("[PlayerGrenadeComponent] Duplicate grenade throw ignored.");
                return;
            }
            lastThrowTime = now;
            var owner = GetComponent<AbstractPlayer>();
            var playableAgent = GetComponent<PlayerAgent>();
            var consumedPlayableGrenade = false;
            if (playableAgent != null)
            {
                grenadeType = playableAgent.GetGrenadeSlotType(0);
            }
            if (owner != null)
            {
                if (owner.Status == null)
                {
                    // PlayerAgent-only playable prefabs do not use the legacy
                    // AbstractPlayer status component. Allow local preview
                    // throws while keeping normal ammo consumption intact when
                    // an AbstractPlayer is present.
                    Debug.LogWarning("プレイヤーステータスが見つからないため、プレビュー投擲として実行します。");
                }
                else if (!owner.Status.UseGrenade(grenadeType))
                {
                    Debug.Log($"選択中のグレネード({grenadeType})の残弾がありません。");
                    return;
                }
            }
            else if (playableAgent != null)
            {
                if (!playableAgent.TryConsumeGrenadeSlot(out grenadeType))
                {
                    Debug.Log("[PlayerGrenadeComponent] No grenade slots remaining.");
                    return;
                }

                consumedPlayableGrenade = true;
            }

            var grenadeData = ResolveGrenadeEntry();
            if (grenadeData == null || grenadeData.GrenadePrefab == null)
            {
                if (consumedPlayableGrenade)
                {
                    playableAgent.RefillGrenade(EGrenadeType.Normal, playableAgent.NormalGrenadeCount + 1);
                }
                return;
            }

            var facingDir = transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            var throwDir = (facingDir + Vector2.up * 0.5f).normalized;
            var throwSpeed = baseThrowForce * powerMultiplier;
            if (TrySendServerGrenadeThrow(throwDir, powerMultiplier))
            {
                Debug.Log($"[PlayerGrenadeComponent] Server-authoritative grenade throw sent: {grenadeData.Name}");
                return;
            }

            var spawnPosition = throwPoint != null ? throwPoint.position : transform.position;
            var grenadeObj = Instantiate(grenadeData.GrenadePrefab, spawnPosition, Quaternion.identity);
            var grenadeProjectile = grenadeObj.GetComponent<GrenadeProjectileController>();

            if (grenadeProjectile != null)
            {
                grenadeProjectile.Launch(
                    throwDir,
                    throwSpeed,
                    owner != null ? owner.UniqueID().ToString() : string.Empty,
                    grenadeData.Name,
                    owner != null ? owner.Team() : ETeam.NoTeam,
                    owner != null ? owner.transform : transform,
                    grenadeType);
            }
            else
            {
                // 従来の Rigidbody2D ベース挙動へのフォールバック
                var rb = grenadeObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(throwDir * throwSpeed, ForceMode2D.Impulse);
                    rb.AddTorque(-5f * powerMultiplier, ForceMode2D.Impulse);
                }
                else
                {
                    Destroy(grenadeObj);
                    Debug.LogWarning("[PlayerGrenadeComponent] Grenade prefab has no projectile or Rigidbody2D component.");
                    return;
                }
            }

            Debug.Log($"グレネードを投げました! パワー倍率: {powerMultiplier:F2}");
            
            // 必要に応じて GameEventBroker に投擲イベントを Publish してネットワークに同期させる
            // var evt = new GrenadeThrowEvent(GetComponent<AbstractPlayer>()?.UniqueID(), throwPoint.position, throwDir, grenadeData.name);
            // GameEventBroker.Publish(evt);
        }

        private bool TrySendServerGrenadeThrow(Vector2 direction, float powerMultiplier)
        {
            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager == null || !networkManager.IsConnected())
                {
                    return false;
                }

                var owner = GetComponent<AbstractPlayer>();
                var playerId = owner != null ? owner.UniqueID().ToString() : string.Empty;
                var position = throwPoint != null ? (Vector2)throwPoint.position : (Vector2)transform.position;

                NetworkEventSerializer.SerializeAndSend(new GrenadeThrowEvent(
                    playerId,
                    position,
                    direction,
                    grenadeType.ToString(),
                    powerMultiplier));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerGrenadeComponent] Failed to send server grenade throw: {ex.Message}");
                return false;
            }
        }
    }
}
