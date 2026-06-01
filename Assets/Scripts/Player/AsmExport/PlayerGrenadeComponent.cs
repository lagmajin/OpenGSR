using Sirenix.OdinInspector;
using OpenGSCore;
using UnityEngine;



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
        
        // UI 用に現在のパワー (0.0 ~ 1.0) を公開する場合に使う
        public float CurrentChargeRatio => isCharging ? Mathf.Clamp01(currentChargeTime / maxChargeTime) : 0f;
        public EGrenadeType CurrentGrenadeType => grenadeType;

        void Start()
        {
            if (throwPoint == null)
            {
                throwPoint = transform; // 設定されてなければ自身の位置から
            }
        }

        void Update()
        {
            // スペースキーを押した瞬間
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isCharging = true;
                currentChargeTime = 0f;
                GetComponent<AbstractPlayer>()?.TryPlayGeneralSound(EPlayerGeneralSound.OpenGrenade);
            }

            // スペースキー長押し中（パワーを溜める）
            if (isCharging && Input.GetKey(KeyCode.Space))
            {
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
            }

            // スペースキーを離した瞬間（投げる）
            if (isCharging && Input.GetKeyUp(KeyCode.Space))
            {
                isCharging = false;
                
                // 比率 (0.0 ~ 1.0) を元にパワー (minPower ~ maxPower) を決定
                float ratio = currentChargeTime / maxChargeTime;
                float powerMultiplier = Mathf.Lerp(minPower, maxPower, ratio);
                
                GetComponent<AbstractPlayer>()?.TryPlayGeneralSound(EPlayerGeneralSound.ThrowGrenade);
                ThrowGrenade(powerMultiplier);
            }
        }

        [Button("オートセット")]
        private void AutoSet()
        {
            // エディタ設定用
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
            var owner = GetComponent<AbstractPlayer>();
            if (owner != null)
            {
                if (owner.Status == null)
                {
                    Debug.LogWarning("プレイヤーステータスが見つかりません");
                    return;
                }

                if (!owner.Status.UseGrenade(grenadeType))
                {
                    Debug.Log($"選択中のグレネード({grenadeType})の残弾がありません。");
                    return;
                }
            }

            var grenadeData = ResolveGrenadeEntry();
            if (grenadeData == null || grenadeData.GrenadePrefab == null) return;

            var facingDir = transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            var throwDir = (facingDir + Vector2.up * 0.5f).normalized;
            var throwSpeed = baseThrowForce * powerMultiplier;
            var grenadeObj = Instantiate(grenadeData.GrenadePrefab, throwPoint.position, Quaternion.identity);
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
            }

            Debug.Log($"グレネードを投げました! パワー倍率: {powerMultiplier:F2}");
            
            // 必要に応じて GameEventBroker に投擲イベントを Publish してネットワークに同期させる
            // var evt = new GrenadeThrowEvent(GetComponent<AbstractPlayer>()?.UniqueID(), throwPoint.position, throwDir, grenadeData.name);
            // GameEventBroker.Publish(evt);
        }
    }
}
