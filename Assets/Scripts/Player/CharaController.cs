using OpenGSCore;
using OpenGS;
using UnityEngine;
//using KanKikuchi.AudioManager;
using System;
using System.Collections;

using System.Collections.Generic;
//using MoreMountains.CorgiEngine;
using Sirenix.OdinInspector;



#pragma warning disable 0414

namespace OpenGS
{




    public partial class CharaController : AbstractPlayer
    {


        [SerializeField]
        public BoxCollider2D defaultCollider;
        [SerializeField]
        public BoxCollider2D triggerCollider;





        private int currentWeapon = 0;

        private bool canOpenGranade = false;
        public float blinkInterval = 0.1f;

        private bool spaceKeyDown = false;

        private bool onDamage = false;

        bool isBlink = false;


        [SerializeField]
        private float time = 10.0f;
        private bool rightKey = false;
        private bool leftKey = false;

        private List<KeyCommand> dashCommand = new List<KeyCommand>();
        private readonly InstantItemSlots instantItemSlots = new InstantItemSlots();
        private MatchEventProvider matchEventProvider;
        private PlayerGrenadeComponent grenadeComponent;


        private PlayerStatus status = GamePlayerManager.Instance.Status;


        [SerializeField] [Required] private Animator animetor;

        //private matchnet

        private PlayerStatus PlayerStatus()
        {
            return GamePlayerManager.Instance.Status;
        }


        private IEnumerator OnBlink()
        {

            yield return new WaitForSeconds(3.0f);

            // 通常状態に戻す
            isBlink = false;
            onDamage = false;

        }

        public override void OnSpawn()
        {
            ResetInstantItems();
            grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            canOpenGranade = true;
            onDamage = false;
            isBlink = false;
        }

        public override void OnReSpawn()
        {
            ResetInstantItems();
            grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            canOpenGranade = true;
            onDamage = false;
            isBlink = false;
        }

        private void StartBlink()
        {
            if (isBlink)
            {
                return;
            }

            isBlink = true;
            onDamage = true;
            StartCoroutine(OnBlink());
        }

        public void Start()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rigidbody2D = GetComponent<Rigidbody2D>();
            grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            ResetInstantItems();
        }

        public void Update()
        {
            var screenPos = Camera.main.WorldToScreenPoint(transform.position);



            var direction = Input.mousePosition - screenPos;
            var trans = transform.localScale;



            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var direc = Input.mousePosition - screenPos;

            // var gun = CurrentWeapon().GetComponent<AbstractGunController>();



            //if (mousePos.x<=transform.position.x)

            if (direc.x >= 0)
            {
                var ls = transform.localScale;
                ls.x = -Mathf.Abs(ls.x);
                transform.localScale = ls;

                //gun?.transform.SetLocalScaleX(-1);
            }
            else
            {
                var ls = transform.localScale;
                ls.x = Mathf.Abs(ls.x);
                transform.localScale = ls;

                //gun?.transform.SetLocalScaleX(1);
            }


            if (Input.GetMouseButton(0))
            {
                //Debug.LogError("Shot");

                Shot();
            }






        }



        private void FixedUpdate()
        {
            if (onDamage && !isBlink)
            {
                StartBlink();
            }
        }
        private void LateUpdate()
        {
            var gun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
            if (gun != null)
            {
                gun.SetGunDirection(transform.localScale.x < 0f);
            }
        }
        public void OpenGrenade()
        {
            canOpenGranade = true;
            Debug.Log("[CharaController] OpenGrenade");
        }

        [Button("グレネードテスト")]
        public void ThrowGrenade()
        {
            if (!canOpenGranade)
            {
                Debug.Log("[CharaController] ThrowGrenade ignored because grenade is locked.");
                return;
            }

            if (grenadeComponent != null)
            {
                grenadeComponent.ThrowGrenade(1.0f);
                canOpenGranade = false;
                return;
            }

            if (Status != null && Status.UseGrenade())
            {
                Debug.Log("[CharaController] Threw grenade using status fallback.");
                canOpenGranade = false;
                return;
            }

            Debug.LogWarning("[CharaController] No grenade component available.");
        }
        [Button("Shot")]
        void Shot()
        {
            var weapon = weaponSlots.mainWeaponSlot;

            var cont = weapon.transform.GetComponentInChildren<AbstractGunController>();

            if (cont.CanShot())
            {

                cont.Shot();

                // 射撃通知をサーバーに送信
                SendShotNotificationToServer();
            }

        }

        /// <summary>
        /// 射撃通知をサーバーに送信
        /// </summary>
        private void SendShotNotificationToServer()
        {
            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager != null && networkManager.IsConnected())
                {
                    // 武器タイプを取得
                    string weaponType = weaponSlots?.mainWeaponSlot?.name ?? "Unknown";

                    // 射撃メッセージを作成して送信
                    var shotMsg = RUDPMessageBuilder.CreatePlayerShot(
                        UniqueID().ToString(),
                        transform.position,
                        new Vector2(transform.localScale.x, 0), // 方向
                        weaponType
                    );
                    networkManager.SendToServer(shotMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Network] Failed to send shot notification: {ex.Message}");
            }
        }

        [Button("しゃがみテスト")]
        public override void Sit()
        {
            Debug.Log("Sit");

            if (null != animator)
            {
                //animator.SetBool("Sit", true);
            }

        }
        [Button("ローリングテスト")]
        public void Rolling()
        {
            Debug.Log("[CharaController] Rolling");
            if (animator != null)
            {
                animator.SetTrigger("Roll");
            }
        }

        public new void StandUp()
        {
            if (animator)
            {
                animator.SetBool("Sit", false);
            }
        }







        void Scope()
        {
            var gun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
            if (gun != null && gun.canZooming)
            {
                Debug.Log($"[CharaController] Scope on: {gun.Name}");
                return;
            }

            Debug.Log("[CharaController] Scope");
        }
        [Button("死亡テスト")]
        void Die()
        {
            if (animator)
            {
                animator.SetBool("Die", true);
            }


            DropWeapon();


            //Instantiate(DeathAnimationPrefab, this.transform.position, Quaternion.identity);

            gameObject.SetActive(false);



        }

        public override void Burst()
        {



            //script.PostEvent();

        }

        public override void UseItem(int num = 0)
        {
            var slotIndex = num > 0 ? num - 1 : 0;
            if (slotIndex < 0 || slotIndex >= instantItemSlots.Count())
            {
                Debug.LogWarning($"[CharaController] Invalid instant item slot: {num}");
                return;
            }

            EnsureInstantItemsLoaded();

            var slotType = instantItemSlots.GetSlotType(slotIndex);
            if (slotType == EInstantItemType.None)
            {
                Debug.LogWarning($"[CharaController] Instant item slot {slotIndex + 1} is empty.");
                return;
            }

            if (instantItemSlots.IsUsed(slotIndex))
            {
                Debug.Log($"[CharaController] Instant item slot {slotIndex + 1} already used.");
                return;
            }

            if (!instantItemSlots.TryUse(slotIndex, out var itemType))
            {
                Debug.LogWarning($"[CharaController] No usable instant item in slot {slotIndex + 1}.");
                return;
            }

            ApplyInstantItem(itemType);

            if (matchEventProvider == null)
            {
                matchEventProvider = FindFirstObjectByType<MatchEventProvider>();
            }

            matchEventProvider?.UseInstantItem(this, itemType);
        }

        GameObject CurrentWeapon()
        {
            return weaponSlots != null ? weaponSlots.currentWeapon : null;

        }

        private void ResetInstantItems()
        {
            instantItemSlots.SetFromEquippedItems(GetEquippedInstantItemTypes());
        }

        private void EnsureInstantItemsLoaded()
        {
            if (instantItemSlots.Count() == 0)
            {
                instantItemSlots.SetFromEquippedItems(GetEquippedInstantItemTypes());
            }
        }

        private void ApplyInstantItem(EInstantItemType itemType)
        {
            switch (itemType)
            {
                case EInstantItemType.HealthKit:
                    Status?.AddHp(100f);
                    Debug.Log("[CharaController] Used HealthKit.");
                    break;
                case EInstantItemType.FireBullet:
                    FireBullet(30f);
                    Debug.Log("[CharaController] Used FireBullet.");
                    break;
                case EInstantItemType.PoisonBullet:
                    PoisonBullet(30f);
                    Debug.Log("[CharaController] Used PoisonBullet.");
                    break;
                case EInstantItemType.PowerGrenadePack:
                case EInstantItemType.ClusterGrenadePack:
                case EInstantItemType.MagnetGrenadePack:
                case EInstantItemType.MineGrenadePack:
                    if (Status != null)
                    {
                        Status.GrenadeCount = Mathf.Min(Status.GrenadeCount + 1, 3);
                    }
                    Debug.Log($"[CharaController] Used grenade pack: {itemType}.");
                    break;
                default:
                    Debug.LogWarning($"[CharaController] Unsupported instant item: {itemType}");
                    break;
            }
        }

        private static IEnumerable<EInstantItemType> GetEquippedInstantItemTypes()
        {
            var equippedItems = UserSaveManager.GetEquippedInstantItems();
            if (equippedItems == null || equippedItems.Length == 0)
            {
                return Array.Empty<EInstantItemType>();
            }

            var result = new List<EInstantItemType>(equippedItems.Length);
            foreach (var itemId in equippedItems)
            {
                if (string.IsNullOrWhiteSpace(itemId) || !Enum.TryParse(itemId, true, out EInstantItemType itemType))
                {
                    result.Add(EInstantItemType.None);
                    continue;
                }

                result.Add(itemType);
            }

            return result;
        }

        public void FlipWeapon()
        {
            if (weaponSlots == null)
            {
                Debug.LogWarning("[CharaController] weaponSlots is null.");
                return;
            }

            weaponSlots.FlipWeapon();
        }
        void TakeNewWeapon()
        {
            TakeWeapon();
        }

        void TakeWeapon()
        {
            if (weaponSlots == null)
            {
                Debug.LogWarning("[CharaController] weaponSlots is null.");
                return;
            }

            var weapon = CurrentWeapon();
            if (weapon != null)
            {
                Debug.Log($"[CharaController] TakeWeapon: {weapon.name}");
            }
            else
            {
                Debug.Log("[CharaController] TakeWeapon requested but no weapon is equipped.");
            }
        }

        protected void DropWeapon()
        {
            weaponSlots?.DropCurrentWeapon();
            OnDropWeapon();
        }

        public override void ReloadStart()
        {
            var gun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;
            if (gun == null)
            {
                Debug.LogWarning("[CharaController] ReloadStart requested but no gun is equipped.");
                return;
            }

            if (!gun.CanReload())
            {
                Debug.Log("[CharaController] ReloadStart ignored because ammo is full.");
                return;
            }

            gun.ReloadStart();
        }

        private void ReloadCancel()
        {
            var gun = weaponSlots?.GetCurrentGun();
            if (gun == null)
            {
                Debug.LogWarning("[CharaController] ReloadCancel requested but no gun is equipped.");
                return;
            }

            gun.ReloadCancel();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            Debug.Log("OnTriggerEnter2D: " + other.tag);

            if ("StageObject" == other.tag)
            {
                StartBlink();
            }

            if ("GameOverArea" == other.tag)
            {
                Burst();
            }

            if ("FieldWeapon" == other.tag)
            {
                TakeNewWeapon();
            }

        }


        public override void IncreaseAttack(float sec)
        {
            base.IncreaseAttack(sec);
        }

        public override void IncreaseDefense(float sec)
        {
            base.IncreaseDefense(sec);
        }



        public override void SpeedUp(float sec)
        {
            base.SpeedUp(sec);
        }
        public override void Invisible(float sec)
        {
            base.Invisible(sec);
        }

        /*
        public override void EquipWeapon(GameObject weaponPrefab)
        {


            //weaponSlots.transform.Find("Weapi")


            weaponSlots.EquipWeapon(weaponPrefab);


        }
*/


    }
}
