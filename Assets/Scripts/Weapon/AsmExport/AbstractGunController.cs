using UnityEngine;
using OpenGSCore;
using Sirenix.OdinInspector;
using DG.Tweening;
using Zenject;
using System;

namespace OpenGS
{
    public enum EFireMode
    {
        Semi,
        Auto
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public abstract class AbstractGunController : MonoBehaviour, IReloadable, IShotable, IGunInfo
    {
        [Header("General Settings")]
        public bool autoDelete = false;
        public string Name = "";
        public EFireMode fireMode = EFireMode.Semi;

        [Header("Combat Stats")]
        [SerializeField, Range(0, 300)] public float damage = 30;
        [SerializeField, Range(0, 100)] public int magazine = 60;
        [SerializeField] public bool canZooming = false;
        [SerializeField, Range(0f, 100f)] protected float reloadTime = 2.0f;
        [SerializeField, Range(1f, 100f)] public float bulletSpeed = 100.0f;
        [SerializeField, Range(0.05f, 5f)] public float shotDelay = 0.1f;

        [Header("Accuracy & Recoil")]
        [SerializeField, Range(0f, 10f)] public float baseSpread = 0.0f;
        [SerializeField, Range(0f, 1f)] public float recoilPerShot = 0.1f;
        [SerializeField, Range(0f, 5f)] public float recoilRecovery = 1.0f;
        [SerializeField] private float shakeIntensity = 0.05f;
        [SerializeField] private float visualRecoilAmount = 0.1f;

        protected bool isShottable = true;
        [ShowInInspector] protected int remains = 0;
        protected bool isFiring = false;
        protected float shotTimer = 0f;

        [SerializeField, Range(0f, 1f)] public float rand = 0.0f;

        public bool bulletGravity = false;
        [SerializeField, Range(0, 20f)] public float gravity = 0.0f;

        protected bool reloadingNow = false;
        protected bool reloadCancelFlag = false;
        protected float reloadDelay = 0.0f;

        Vector3 originalHeadPos;
        public GameObject bulletPrefab;

        [Header("Effects")]
        [SerializeField] public GameObject shotEffectPrefab;
        [SerializeField] public GameObject shellCasingPrefab;
        [SerializeField] public Transform muzzle;
        [SerializeField] public GameObject fieldWeaponPrefab;
        [SerializeField] public Sprite gunBigIcon;
        [SerializeField] public Sprite gunSilhouette;

        public WeaponMasterData data;

        protected float spreadAngle = 0f;
        protected float heat = 0;
        protected float heatMax = 5f;
        float heatPerShot = 0.5f;
        float heatDecayPerSecond = 1f;

        // Services
        protected ISoundService soundService;
        protected IInputService inputService;
        protected IEffectService effectService;

        [Inject]
        public void Construct(ISoundService soundService, IInputService inputService, IEffectService effectService)
        {
            this.soundService = soundService;
            this.inputService = inputService;
            this.effectService = effectService;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(damage) || damage < 0f) damage = 30f;
            magazine = Mathf.Max(0, magazine);
            if (!float.IsFinite(reloadTime) || reloadTime < 0f) reloadTime = 2f;
            if (!float.IsFinite(bulletSpeed) || bulletSpeed < 0f) bulletSpeed = 100f;
            if (!float.IsFinite(shotDelay) || shotDelay < 0.01f) shotDelay = 0.1f;
            if (!float.IsFinite(baseSpread) || baseSpread < 0f) baseSpread = 0f;
            if (!float.IsFinite(recoilPerShot) || recoilPerShot < 0f) recoilPerShot = 0.1f;
            if (!float.IsFinite(recoilRecovery) || recoilRecovery < 0f) recoilRecovery = 1f;
            if (!float.IsFinite(shakeIntensity) || shakeIntensity < 0f) shakeIntensity = 0.05f;
            if (!float.IsFinite(visualRecoilAmount) || visualRecoilAmount < 0f) visualRecoilAmount = 0.1f;
            if (!float.IsFinite(rand) || rand < 0f) rand = 0f;
            if (!float.IsFinite(gravity) || gravity < 0f) gravity = 0f;
            if (!float.IsFinite(heatMax) || heatMax < 0f) heatMax = 5f;
            if (!float.IsFinite(heatPerShot) || heatPerShot < 0f) heatPerShot = 0.5f;
            if (!float.IsFinite(heatDecayPerSecond) || heatDecayPerSecond < 0f) heatDecayPerSecond = 1f;
        }

        private void Awake()
        {
            damage = float.IsFinite(damage) ? Mathf.Max(0f, damage) : 30f;
            magazine = Mathf.Max(0, magazine);
            reloadTime = float.IsFinite(reloadTime) ? Mathf.Max(0f, reloadTime) : 2f;
            bulletSpeed = float.IsFinite(bulletSpeed) ? Mathf.Max(0f, bulletSpeed) : 100f;
            shotDelay = float.IsFinite(shotDelay) ? Mathf.Max(0.01f, shotDelay) : 0.1f;
            baseSpread = float.IsFinite(baseSpread) ? Mathf.Clamp(baseSpread, 0f, 180f) : 0f;
            recoilPerShot = float.IsFinite(recoilPerShot) ? Mathf.Max(0f, recoilPerShot) : 0.1f;
            recoilRecovery = float.IsFinite(recoilRecovery) ? Mathf.Max(0f, recoilRecovery) : 1f;
            gravity = float.IsFinite(gravity) ? Mathf.Max(0f, gravity) : 0f;
            heatMax = float.IsFinite(heatMax) ? Mathf.Max(0f, heatMax) : 5f;
            heatPerShot = float.IsFinite(heatPerShot) ? Mathf.Max(0f, heatPerShot) : 0.5f;
            heatDecayPerSecond = float.IsFinite(heatDecayPerSecond) ? Mathf.Max(0f, heatDecayPerSecond) : 1f;
            shotTimer = 0f;
            heat = 0f;
            originalHeadPos = gameObject.transform.localPosition;
        }

        protected virtual void OnUpdate()
        {
            UpdateRotation();

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            // Keep a frame hitch from skipping cooldown/reload/heat state in one tick.
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            shotTimer = float.IsFinite(shotTimer) ? Mathf.Max(0f, shotTimer - deltaTime) : 0f;
            reloadDelay = float.IsFinite(reloadDelay) ? Mathf.Max(0f, reloadDelay) : 0f;
            heat = float.IsFinite(heat) ? Mathf.Max(0f, heat) : 0f;
            remains = Mathf.Clamp(remains, 0, MagazineMaxCount());

            if (reloadingNow) UpdateReloading();

            // 射撃入力処理 (Autoモード用)
            if (isFiring && fireMode == EFireMode.Auto && CanShot())
            {
                Shot();
            }

            if (heat > 0f)
            {
                heat = Mathf.Max(0f, heat - heatDecayPerSecond * deltaTime);
            }
        }

        private void UpdateRotation()
        {
            if (inputService == null) return;

            var aimPos = inputService.GetAimWorldPosition();
            if (!float.IsFinite(aimPos.x) || !float.IsFinite(aimPos.y) || !float.IsFinite(transform.position.x) ||
                !float.IsFinite(transform.position.y))
            {
                return;
            }

            var dir = (Vector3)aimPos - transform.position;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (aimPos.x < transform.position.x) // エイムが左側
            {
                transform.rotation = Quaternion.Euler(0f, 0f, -(180 - angle));
            }
            else // エイムが右側
            {
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void UpdateReloading()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            reloadDelay = Mathf.Max(0f, reloadDelay + deltaTime);
            if (reloadDelay >= reloadTime)
            {
                ReloadComplete();
            }
        }

        private void Update()
        {
            OnUpdate();
        }

        protected void CreateMuzzulleFlash()
        {
            if (!shotEffectPrefab || !muzzle)
            {
                return;
            }

            if (effectService != null)
            {
                effectService.PlayOneShotEffect(shotEffectPrefab, muzzle.position, muzzle.rotation);
            }
            else
            {
                var spawnedEffect = Instantiate(shotEffectPrefab, muzzle.position, muzzle.rotation);
                Destroy(spawnedEffect, 3f);
            }
        }

        protected float GetEffectiveDamage(AbstractPlayer player)
        {
            var powerupable = GetComponentInParent<IPowerupable>();
            if (powerupable != null)
            {
                var boostedDamage = damage * (powerupable.IsIncreaseAttackNow() ? 2f : 1f);
                return float.IsFinite(boostedDamage) ? Mathf.Max(0f, boostedDamage) : 0f;
            }

            var multiplier = player != null ? player.AttackMultiplier() : 1f;
            var effectiveDamage = damage * multiplier;
            return float.IsFinite(effectiveDamage) ? Mathf.Max(0f, effectiveDamage) : 0f;
        }

        protected virtual void CreateBullet(EBulletType type = EBulletType.Normal) { }

        protected virtual void CreateEmptyShellCasing()
        {
            if (!shellCasingPrefab) return;

            var ejectionAngle = UnityEngine.Random.Range(8f, 24f);
            var angleJitter = UnityEngine.Random.Range(-10f, 10f);
            var shellRotation = transform.rotation * Quaternion.Euler(0, 0, ejectionAngle + angleJitter);
            var shell = Instantiate(shellCasingPrefab, transform.position, shellRotation);

            var rb = shell.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                bool isWildShell = UnityEngine.Random.value < 0.05f;
                float fx = isWildShell ? UnityEngine.Random.Range(3f, 4f) : 1.4f;
                float fy = isWildShell ? UnityEngine.Random.Range(7f, 9f) : UnityEngine.Random.Range(4.5f, 5.5f);
                float sideSign = transform.right.x >= 0f ? 1f : -1f;

                var outward = transform.right * sideSign;
                var ejectDirection = Quaternion.Euler(0f, 0f, ejectionAngle + angleJitter) * outward;
                Vector2 force = ejectDirection * fx + transform.up * fy;
                rb.AddForce(force, ForceMode2D.Impulse);

                float torque = isWildShell ? UnityEngine.Random.Range(-40f, 40f) : UnityEngine.Random.Range(-10f, 10f);
                rb.AddTorque(torque, ForceMode2D.Impulse);
            }
        }

        public virtual void ReloadStart()
        {
            var magazineLimit = MagazineMaxCount();
            if (reloadingNow || remains >= magazineLimit) return;

            reloadCancelFlag = false;
            reloadingNow = true;
            if (data != null && soundService != null)
            {
                soundService.PlayWeaponReload(data.weaponType);
            }
        }

        public virtual void ReloadCancel()
        {
            reloadingNow = false;
            reloadCancelFlag = true;
        }

        public virtual void ReloadComplete()
        {
            reloadingNow = false;
            reloadCancelFlag = false;
            reloadDelay = 0.0f;
            remains = MagazineMaxCount();
            PublishAmmoUpdate();
            GetOwnerPlayer()?.PlayReloadCompleteEffect();
        }

        public int CurrentMagazineCount() => remains;

        public virtual void SetMagazineCount(int value)
        {
            remains = Mathf.Clamp(value, 0, MagazineMaxCount());
            PublishAmmoUpdate();
        }

        protected void PublishAmmoUpdate()
        {
            var pid = GetPlayerID(GetOwnerPlayer());
            if (!string.IsNullOrEmpty(pid))
            {
                GameEventBroker.Publish(new AmmoUpdateEvent(pid, Name, remains, MagazineMaxCount()));
            }
        }

        protected string GetPlayerID(AbstractPlayer player)
        {
            return player != null ? player.UniqueID().ToString() : "";
        }

        protected AbstractPlayer GetOwnerPlayer()
        {
            return GetComponentInParent<AbstractPlayer>();
        }

        protected Vector2 GetShotDirection()
        {
            if (muzzle == null)
            {
                return transform.right;
            }

            if (inputService == null) return muzzle.right;

            Vector2 baseDir = inputService.GetAimDirection(muzzle.position);
            if (!float.IsFinite(baseDir.x) || !float.IsFinite(baseDir.y) || baseDir.sqrMagnitude <= Mathf.Epsilon)
            {
                baseDir = muzzle.right;
            }

            baseDir.Normalize();
            float currentSpread = baseSpread + (heat * 2.5f);
            if (!float.IsFinite(currentSpread))
            {
                currentSpread = 0f;
            }

            currentSpread = Mathf.Clamp(currentSpread, 0f, 180f);
            float spreadAngle = UnityEngine.Random.Range(-currentSpread, currentSpread);
            
            var direction = Quaternion.Euler(0, 0, spreadAngle) * baseDir;
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        }

        public virtual float ReloadTime() => data != null ? data.reloadTime : 2.0f;

        public void StartFire()
        {
            isFiring = true;
            if (fireMode == EFireMode.Semi)
            {
                Shot();
            }
        }

        public void StopFire() => isFiring = false;

        public void Shot()
        {
            if (CanShot() && muzzle != null)
            {
                remains--;
                shotTimer = shotDelay;
                
                if (!TrySendServerShot())
                {
                    CreateBullet();
                }
                CreateMuzzulleFlash();
                CreateEmptyShellCasing();
                PlayShotSound();
                PlayVisualRecoil();
                
                if (effectService != null)
                {
                    effectService.ShakeCamera(shakeIntensity, 0.1f);
                }
                
                heat = Mathf.Min(heat + heatPerShot, heatMax);
                PublishAmmoUpdate();
            }
        }

        public virtual bool CanShot() => float.IsFinite(shotTimer) && shotTimer <= 0f && remains > 0 && !reloadingNow;

        public bool CanReload() => magazine > remains;

        protected virtual bool TrySendServerShot()
        {
            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager == null || !networkManager.IsConnected())
                {
                    return false;
                }

                var owner = GetOwnerPlayer();
                var weaponType = data != null ? data.weaponType.ToString() : Name;
                var position = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
                var direction = GetShotDirection();

                NetworkEventSerializer.SerializeAndSend(new PlayerShotEvent(
                    GetPlayerID(owner),
                    position,
                    direction,
                    weaponType));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AbstractGunController] Failed to send server shot: {ex.Message}");
                return false;
            }
        }

        protected eDamageType ResolveBulletDamageType()
        {
            return GetComponentInParent<PlayerAgent>()?.CurrentBulletDamageType ?? eDamageType.Bullet;
        }

        public int gunDirection() => transform.localScale.x > 0 ? 1 : -1;

        public void SetGunDirection(bool left = true)
        {
            var scale = transform.localScale;
            scale.x = left ? 1 : -1;
            transform.localScale = scale;
        }

        public virtual void SetGunAngle() { }

        public Sprite GunBigIcon() => gunBigIcon;
        public Sprite GunSilhouette() => gunSilhouette;
        public int MagazineCount() => remains;
        string IGunInfo.Name() => Name;
        public int MagazineMaxCount()
        {
            var configuredMax = data != null ? data.maxBullet : magazine;
            return Mathf.Max(0, configuredMax);
        }

        public void PlayShotSound()
        {
            if (data != null && soundService != null)
            {
                float randomPitch = UnityEngine.Random.Range(0.95f, 1.05f);
                soundService.PlayWeaponShot(data.weaponType, randomPitch);
            }
        }

        private void PlayVisualRecoil()
        {
            transform.DOKill(true);
            Vector3 recoilPos = Vector3.left * visualRecoilAmount;
            transform.DOLocalMove(originalHeadPos + recoilPos, 0.05f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transform.DOLocalMove(originalHeadPos, 0.15f).SetEase(Ease.InSine);
            });
        }

        public void Sit()
        {
            if (gameObject != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.4f);
                seq.Append(transform.DOLocalMove(originalHeadPos + new Vector3(-0.02f, -0.01f, 0f), 0.2f).SetEase(Ease.OutSine));
            }
        }

        public void StandUp()
        {
            if (gameObject != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.1f);
                seq.Append(transform.DOLocalMove(originalHeadPos, 0.2f).SetEase(Ease.OutSine));
            }
        }

        public void RemoveThis() => Destroy(gameObject);

        public GameObject FieldPrefab() => fieldWeaponPrefab;

        public virtual void OnSwappedIn()
        {
            ReloadComplete();
        }

        protected Vector2 CalculateSpreadDirection(Transform muzzle, float spreadAngle)
        {
            if (muzzle == null)
            {
                return transform.right;
            }

            spreadAngle = float.IsFinite(spreadAngle) ? Mathf.Clamp(spreadAngle, -180f, 180f) : 0f;

            if (inputService == null) return muzzle.right;
            
            Vector2 dir = inputService.GetAimDirection(muzzle.position);
            if (!float.IsFinite(dir.x) || !float.IsFinite(dir.y) || dir.sqrMagnitude <= Mathf.Epsilon)
            {
                dir = muzzle.right;
            }

            dir.Normalize();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += spreadAngle;

            var direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        }
    }
}
