using UnityEngine;
using OpenGSCore;
using Sirenix.OdinInspector;
using DG.Tweening;
using Zenject;

namespace OpenGS
{
    public enum EFireMode
    {
        Semi,
        Auto,
        Burst
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

        [Inject]
        public void Construct(ISoundService soundService, IInputService inputService)
        {
            this.soundService = soundService;
            this.inputService = inputService;
        }

        private void Awake()
        {
            originalHeadPos = gameObject.transform.localPosition;
        }

        protected virtual void OnUpdate()
        {
            UpdateRotation();
            
            if (shotTimer > 0) shotTimer -= Time.deltaTime;

            if (reloadingNow) UpdateReloading();

            // 射撃入力処理 (Autoモード用)
            if (isFiring && fireMode == EFireMode.Auto && CanShot())
            {
                Shot();
            }

            if (heat > 0f) heat -= heatDecayPerSecond * Time.deltaTime;
        }

        private void UpdateRotation()
        {
            if (inputService == null) return;

            var aimPos = inputService.GetAimWorldPosition();
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
            reloadDelay += Time.deltaTime;
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
            if (shotEffectPrefab && muzzle)
                Instantiate(shotEffectPrefab, muzzle.position, muzzle.rotation);
        }

        protected virtual void CreateBullet(EBulletType type = EBulletType.Normal) { }

        protected virtual void CreateEmptyShellCasing()
        {
            if (!shellCasingPrefab) return;

            float angleOffset = Random.Range(-19f, 19f);
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(0, 0, angleOffset);
            var shell = Instantiate(shellCasingPrefab, transform.position, spawnRot);

            var rb = shell.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                bool isWildShell = Random.value < 0.05f;
                float fx = isWildShell ? Random.Range(3f, 4f) : Random.Range(1.4f, 1.4f);
                float fy = isWildShell ? Random.Range(6f, 8f) : Random.Range(3.0f, 4.0f);

                Vector2 force = transform.right * fx + transform.up * fy;
                rb.AddForce(force, ForceMode2D.Impulse);

                float torque = isWildShell ? Random.Range(-40f, 40f) : Random.Range(-10f, 10f);
                rb.AddTorque(torque, ForceMode2D.Impulse);
            }
        }

        public virtual void ReloadStart()
        {
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
            remains = magazine;
            PublishAmmoUpdate();
        }

        protected void PublishAmmoUpdate()
        {
            var pid = GetPlayerID();
            if (!string.IsNullOrEmpty(pid))
            {
                GameEventBroker.Publish(new AmmoUpdateEvent(pid, Name, remains, MagazineMaxCount()));
            }
        }

        protected string GetPlayerID()
        {
            var player = GetComponentInParent<AbstractPlayer>();
            return player != null ? player.UniqueID().ToString() : "";
        }

        protected Vector2 GetShotDirection()
        {
            if (inputService == null) return muzzle.right;

            Vector2 baseDir = inputService.GetAimDirection(muzzle.position);
            float currentSpread = baseSpread + (heat * 2.5f); 
            float spreadAngle = UnityEngine.Random.Range(-currentSpread, currentSpread);
            
            return Quaternion.Euler(0, 0, spreadAngle) * baseDir;
        }

        public virtual float ReloadTime() => data != null ? data.reloadTime : 2.0f;

        public void StartFire()
        {
            isFiring = true;
            if (fireMode == EFireMode.Semi || fireMode == EFireMode.Burst)
            {
                Shot();
            }
        }

        public void StopFire() => isFiring = false;

        public void Shot()
        {
            if (CanShot())
            {
                remains--;
                shotTimer = shotDelay;
                
                CreateBullet();
                CreateMuzzulleFlash();
                CreateEmptyShellCasing();
                PlayShotSound();
                
                heat = Mathf.Min(heat + heatPerShot, heatMax);
                PublishAmmoUpdate();
            }
        }

        public virtual bool CanShot() => shotTimer <= 0 && remains > 0 && !reloadingNow;

        public bool CanReload() => magazine > remains;

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
        public int MagazineMaxCount() => data != null ? data.maxBullet : magazine;

        public void PlayShotSound()
        {
            if (data != null && soundService != null)
            {
                // TODO: SoundService にピッチ変更オプションを追加するか、
                // あるいは SoundService 内部で熱によるピッチ変更を実装する
                soundService.PlayWeaponShot(data.weaponType);
            }
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

        protected Vector2 CalculateSpreadDirection(Transform muzzle, float spreadAngle)
        {
            if (inputService == null) return muzzle.right;
            
            Vector2 dir = inputService.GetAimDirection(muzzle.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += spreadAngle;

            return Quaternion.Euler(0, 0, angle) * Vector2.right;
        }
    }
}
