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
            // isDamage = false;
            //sp.color = new Color(1f, 1f, 1f, 1f);

        }

        public override void OnSpawn()
        {
           // var uiManager = 0;


        }

        public override void OnReSpawn()
        {

        }

        private void StartBlink()
        {

        }

        public new void Start()
        {


            animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rigidbody2D = GetComponent<Rigidbody2D>();





            //Invisible(30.0f);
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
                transform.SetLocalScaleX(-1);

                //gun?.transform.SetLocalScaleX(-1);
            }
            else
            {
                transform.SetLocalScaleX(1);

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


        }
        private void LateUpdate()
        {

        }
        public void OpenGrenade()
        {


        }

        [Button("グレネードテスト")]
        public void ThrowGrenade()
        {




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
                    string weaponType = weapon?.name ?? "Unknown";

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
        public void Sit()
        {
            Debug.Log("Sit");

            if (null != animator)
            {
                //animator.SetBool("Sit", true);
            }

        }
        [Button("ローリングテスト")]
        public new void Rolling()
        {

        }

        public void StandUp()
        {
            if (animator)
            {
                animator.SetBool("Sit", false);
            }
        }







        void Scope()
        {

        }
        [Button("死亡テスト")]
        void Die()
        {
            if (animator)
            {
                animator.SetBool("Die", true);
            }


            weaponSlots.DropCurrentWeapon();


            //Instantiate(DeathAnimationPrefab, this.transform.position, Quaternion.identity);

            gameObject.SetActive(false);



        }

        public override void Burst()
        {



            //script.PostEvent();

        }

        public override void UseItem(int num = 0)
        {

        }

        GameObject CurrentWeapon()
        {
            //var weaponSlot1 = weaponSlots.gameObject.transform.Find("WeaponSlot1");
            //var weaponSlot2 = weaponSlots.gameObject.transform.Find("WeaponSlot2");


            return null;

        }

        public void FlipWeapon()
        {


        }
        void TakeNewWeapon()
        {


        }

        void TakeWeapon()
        {




        }

        protected void DropWeapon()
        {




        }

        public override void ReloadStart()
        {


        }

        private void ReloadCancel()
        {

        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("OnTriggerEnter2D: " + other.tag);

            if ("StageObject" == other.tag)
            {

            }

            if ("GameOverArea" == other.tag)
            {


                Burst();
            }

            if ("FieldWeapon" == other.tag)
            {


            }

        }


        public override void IncreaseAttack(float sec)
        {
            StartCoroutine(IncreaseAttackCounter());


            //var effectStorage=PlayerEffectStorage.GetComponent<IPlayerEffectManager>().TakePowerUpItemEffectPrefab();

            //Instantiate(effectStorage, gameObject.transform.position, Quaternion.identity);


            Debug.Log("dfdetr");
        }

        public override void IncreaseDefense(float sec)
        {
            //var effectStorage = PlayerEffectStorage.GetComponent<IPlayerEffectManager>().TakePowerUpItemEffectPrefab();

            //Instantiate(effectStorage, gameObject.transform.position, Quaternion.identity);
        }



        public override void SpeedUp(float sec)
        {

            StartCoroutine(WaitCallback(sec, () => { }));

        }
        public override void Invisible(float sec)
        {
            _spriteRenderer.color = new Color(1f, 1f, 1f, .3f);


        }
        IEnumerator WaitCallback(float sec, Action callback)
        {
            if (sec < 0)
            {
                sec = 0.0f;
            }

            yield return new WaitForSeconds(sec);

            callback();

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
