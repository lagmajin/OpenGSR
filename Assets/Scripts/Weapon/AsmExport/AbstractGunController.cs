//using KanKikuchi.AudioManager;

using UnityEngine;
using OpenGSCore;
using Sirenix.OdinInspector;
using DG.Tweening;

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
        [SerializeField, Range(0.05f, 5f)] public float shotDelay = 0.1f; // 間隔 (FireRate)

        [Header("Accuracy & Recoil")]
        [SerializeField, Range(0f, 10f)] public float baseSpread = 0.0f;
        [SerializeField, Range(0f, 1f)] public float recoilPerShot = 0.1f;
        [SerializeField, Range(0f, 5f)] public float recoilRecovery = 1.0f;

        protected bool isShottable = true;
        [ShowInInspector] protected int remains = 0;
        protected bool isFiring = false; // 入力が押されているか
        protected float shotTimer = 0f;

        [SerializeField, Range(0f, 1f)]
        public float rand = 0.0f;


        public bool bulletGravity = false;
        [SerializeField, Range(0, 20f)]
        public float gravity = 0.0f;

        protected bool reloadingNow = false;
        protected bool reloadCancelFlag = false;

        protected float reloadDelay = 0.0f;

        //public AudioClip shotSound;
        //public AudioClip reloadStartSound;
        Vector3 originalHeadPos;
        public GameObject bulletPrefab;

        [SerializeField]protected AudioSource audioSource;


        [SerializeField]public GameObject shotEffectPrefab;
        [SerializeField]public GameObject shellCasingPrefab;
        [SerializeField]public Transform muzzle;


        [SerializeField] public GameObject fieldWeaponPrefab;
 


        [SerializeField] public Sprite gunBigIcon;
        [SerializeField] public Sprite gunSilhouette;

        public WeaponMasterData data;

        protected float spreadAngle = 0f;

        protected float heat = 0;//演出用の銃のオーバーヒート
        protected float heatMax = 5f;

        float heatPerShot = 0.5f;
        float heatDecayPerSecond = 1f;


        private void Awake()
        {

            originalHeadPos = gameObject.transform.localPosition;
        }
        private void Start()
        {

        }


        protected virtual void OnUpdate()
        {
            UpdateRotation();
            
            // 射撃間隔タイマー
            if (shotTimer > 0)
            {
                shotTimer -= Time.deltaTime;
            }

            // リロード進捗
            if (reloadingNow)
            {
                UpdateReloading();
            }

            // 射撃入力処理 (Autoモード用)
            if (isFiring && fireMode == EFireMode.Auto && CanShot())
            {
                Shot();
            }

            // 熱減衰
            if (heat > 0f)
            {
                heat -= heatDecayPerSecond * Time.deltaTime;
            }
        }

        private void UpdateRotation()
        {
            if (Camera.main == null) return;

            var screenPos = Camera.main.WorldToScreenPoint(transform.position);
            var dir = Input.mousePosition - screenPos;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (Input.mousePosition.x < screenPos.x) // マウスが左側
            {
                transform.rotation = Quaternion.Euler(0f, 0f, -(180 - angle));
            }
            else // マウスが右側
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

    

        void LastUpdate()
        {

        }

        void RemoveGameObject()
        {
            Destroy(this.gameObject);
        }

        protected void CreateShell()
        {
            //Instantiate(shellCasingPrefab, gameObject.transform.position, Quaternion.identity);

        }

        protected void CreateMuzzulleFlash()
        {
            Instantiate(shotEffectPrefab, muzzle.position, muzzle.rotation);
        }
        protected virtual void CreateBullet(EBulletType type=EBulletType.Normal)
        {

        }

        protected virtual void CreateEmptyShellCasing()
        {
            float angleOffset = Random.Range(-19f, 19f);
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(0, 0, angleOffset);

            var shell = Instantiate(shellCasingPrefab, transform.position, spawnRot);

            var rb = shell.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                bool isWildShell = Random.value < 0.05f; // 10%の確率で「暴れ薬莢」

                float fx = isWildShell ? Random.Range(3f,4f) : Random.Range(1.4f, 1.4f);
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

            //Functions.WaitAfterAction(ReloadComplete, reloadTime);

            //PlaySound.PlaySE(reloadStartSound);
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
            if (Camera.main == null) return muzzle.right;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 baseDir = ((Vector2)mouseWorldPos - (Vector2)muzzle.transform.position).normalized;

            // スプレッド（基本拡散 + 熱/連射によるブレ）
            // heat は射撃ごとに 1.0 (デフォルト) 溜まり、徐々に減衰する
            float currentSpread = baseSpread + (heat * 2.5f); 
            float spreadAngle = UnityEngine.Random.Range(-currentSpread, currentSpread);
            
            return Quaternion.Euler(0, 0, spreadAngle) * baseDir;
        }

        public virtual float ReloadTime()
        {
            return data.reloadTime;

            //return 1.0f;
        }

        public void StartFire()
        {
            isFiring = true;
            if (fireMode == EFireMode.Semi || fireMode == EFireMode.Burst)
            {
                Shot();
            }
        }

        public void StopFire()
        {
            isFiring = false;
        }

        public void Shot()
        {
            if (CanShot())
            {
                remains--;
                shotTimer = shotDelay;
                
                // 演出
                CreateBullet();
                CreateMuzzulleFlash();
                CreateEmptyShellCasing();
                PlayShotSound();
                
                // 熱と反動
                heat = Mathf.Min(heat + heatPerShot, heatMax);
                
                PublishAmmoUpdate();
                
                var pid = GetPlayerID();
                if (!string.IsNullOrEmpty(pid))
                {
                    // 必要に応じてグローバルイベントを送信
                    // GameEventBroker.Publish(new PlayerShotEvent(pid, muzzle.position, muzzle.right, Name));
                }
            }
        }

        public virtual bool CanShot()
        {
            return shotTimer <= 0 && remains > 0 && !reloadingNow;
        }

        public bool CanReload()
        {
            if (magazine <= remains)
            {
                return false;
            }

            return true;
        }

        public int gunDirection()
        {
            var scale = gameObject.transform.localScale.x;

            if (scale > 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }

        }
        public void SetGunDirection(bool left = true)
        {
            Debug.Log("ChangeGunDirection");

            var scale = gameObject.transform.localScale;
            if (left)
            {


                scale.x = 1;


            }
            else
            {
                scale.x = -1;



            }

            gameObject.transform.localScale = scale;

        }

        public virtual void SetGunAngle()
        {

        }

        public void EnableRigidBody()
        {

        }

        public void DisableRigidBody()
        {

        }

        public Sprite GunBigIcon()
        {
            return gunBigIcon;
        }

        public Sprite GunSilhouette()
        {
            return gunSilhouette;
        }

        public int MagazineCount()
        {
            return remains;
        }

        string IGunInfo.Name()
        {
            return Name;
        }

        public int MagazineMaxCount()
        {
            return data.maxBullet;

        }

        public void PlayShotSound()
        {
            if (data)
            {
                float heatFactor = Mathf.Clamp01(heat / heatMax);
                audioSource.pitch = 1.0f + heatFactor * 0.2f;
                audioSource.PlayOneShot(data.shotSound);
            }
        }
        public void Sit()
        {
            if (gameObject != null)
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.4f); // 0.1秒待つ
                seq.Append(gameObject.transform.DOLocalMove(originalHeadPos + new Vector3(-0.02f, -0.01f, 0f), 0.2f)
                    .SetEase(Ease.OutSine));
            }

        }
        public void StandUp()
        {
   
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(0.1f); // 0.1秒ディレイ
                seq.Append(gameObject.transform.DOLocalMove(originalHeadPos, 0.2f).SetEase(Ease.OutSine));
            
        }

        public void RemoveThis()
        {
            Destroy(gameObject);
        }

        public GameObject FieldPrefab()
        {
            return fieldWeaponPrefab;
        }

        protected Vector2 CalculateSpreadDirection(Transform muzzle, float spreadAngle)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorldPos - muzzle.position).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += spreadAngle;

            Quaternion spreadRotation = Quaternion.Euler(0, 0, angle);

            return spreadRotation * Vector2.right;
        }
    }


}
