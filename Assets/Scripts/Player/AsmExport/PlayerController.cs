using UnityEngine;
using Zenject;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// 実際のプレイヤー操作を担当する具象クラス。
    /// 移動・ジャンプ・ブースターに加え、武器の使用や切り替えも管理する。
    /// </summary>
    public class PlayerController : AbstractPlayer
    {
        [Header("Movement Overrides")]
        [SerializeField] private float airResistance = 0.95f;
        [SerializeField] private float groundAcceleration = 50f;
        [SerializeField] private float maxSpeed = 8f;

        [Header("Booster Settings")]
        [SerializeField] private float boosterAcceleration = 25f;
        [SerializeField] private float maxBoosterSpeed = 12f;
        [SerializeField] private float boosterConsumption = 20f;
        [SerializeField] private float boosterRecovery = 15f;

        private IInputService inputService;
        private ISoundService soundService;
        private IEffectService effectService;

        [Inject]
        public void Construct(IInputService inputService, ISoundService soundService, IEffectService effectService)
        {
            this.inputService = inputService;
            this.soundService = soundService;
            this.effectService = effectService;
        }

        protected virtual void Start()
        {
            if (rigidbody2D == null) rigidbody2D = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            Status.FullRecovery();
        }

        protected virtual void Update()
        {
            if (isDead || inputService == null) return;

            UpdateFacing();
            HandleActions();

            // ブースター燃料の回復
            if (IsGround() && !inputService.IsBoosterPressed())
            {
                Status.RefillBooster(boosterRecovery * Time.deltaTime);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (isDead || inputService == null) return;

            HandleMovement();
            HandleBooster();
        }

        private void HandleMovement()
        {
            float horizontal = inputService.GetHorizontalAxis();
            bool isGround = IsGround();

            Vector2 velocity = rigidbody2D.linearVelocity;

            if (Mathf.Abs(horizontal) > 0.1f)
            {
                float accel = isGround ? groundAcceleration : groundAcceleration * 0.5f;
                velocity.x += horizontal * accel * Time.fixedDeltaTime;
                velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            }
            else
            {
                float friction = isGround ? 0.8f : airResistance;
                velocity.x *= friction;
            }

            rigidbody2D.linearVelocity = velocity;
        }

        private void HandleBooster()
        {
            if (inputService.IsBoosterPressed() && Status.Booster > 0)
            {
                Vector2 velocity = rigidbody2D.linearVelocity;
                velocity.y += boosterAcceleration * Time.fixedDeltaTime;
                velocity.y = Mathf.Min(velocity.y, maxBoosterSpeed);
                rigidbody2D.linearVelocity = velocity;

                Status.ConsumeBooster(boosterConsumption * Time.fixedDeltaTime);
            }
        }

        private void HandleActions()
        {
            if (weaponSlots == null) return;

            var currentGun = weaponSlots.GetCurrentGun();

            // 射撃入力
            if (inputService.IsFirePressed())
            {
                if (currentGun != null) currentGun.StartFire();
            }
            else
            {
                if (currentGun != null) currentGun.StopFire();
            }

            // リロード入力 (TODO: IInputService に追加が必要)
            if (inputService.IsReloadJustPressed())
            {
                if (currentGun != null) currentGun.ReloadStart();
            }

            // 武器切り替え入力 (TODO: 共通化が必要。現在は仮でキーボード直参照を避ける)
            // if (inputService.IsSwapWeaponJustPressed()) { weaponSlots.FlipWeapon(); }
        }

        public new void Jump()
        {
            if (IsGround())
            {
                rigidbody2D.AddForce(Vector2.up * jumpCurve.Evaluate(1.0f) * 10f, ForceMode2D.Impulse);
            }
        }

        private void UpdateFacing()
        {
            var aimPos = inputService.GetAimWorldPosition();
            bool faceRight = aimPos.x > transform.position.x;

            Vector3 scale = transform.localScale;
            scale.x = faceRight ? 1 : -1;
            transform.localScale = scale;
        }

        public override void OnDead()
        {
            base.OnDead();
            rigidbody2D.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
            rigidbody2D.angularVelocity = Random.Range(-360f, 360f);
        }
    }
}
