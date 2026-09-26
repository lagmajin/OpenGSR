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
        private bool wasBoosting;
        private bool wasGrounded;
        private float boosterLoopTimer;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!float.IsFinite(airResistance)) airResistance = 0.95f;
            if (!float.IsFinite(groundAcceleration)) groundAcceleration = 50f;
            if (!float.IsFinite(maxSpeed)) maxSpeed = 8f;
            if (!float.IsFinite(boosterAcceleration)) boosterAcceleration = 25f;
            if (!float.IsFinite(maxBoosterSpeed)) maxBoosterSpeed = 12f;
            if (!float.IsFinite(boosterConsumption)) boosterConsumption = 20f;
            if (!float.IsFinite(boosterRecovery)) boosterRecovery = 15f;

            airResistance = Mathf.Clamp01(airResistance);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            maxSpeed = Mathf.Max(0f, maxSpeed);
            boosterAcceleration = Mathf.Max(0f, boosterAcceleration);
            maxBoosterSpeed = Mathf.Max(0f, maxBoosterSpeed);
            boosterConsumption = Mathf.Max(0f, boosterConsumption);
            boosterRecovery = Mathf.Max(0f, boosterRecovery);
        }

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
            wasGrounded = IsGround();
            
            Status.FullRecovery();
        }

        protected virtual void Update()
        {
            if (CheckFallDeath() || isDead || inputService == null) return;

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            UpdateFacing();
            HandleActions();

            // ブースター燃料の回復
            if (IsGround() && !inputService.IsBoosterPressed())
            {
                Status.RefillBooster(boosterRecovery * deltaTime);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (CheckFallDeath() || isDead || inputService == null || rigidbody2D == null) return;

            if (!float.IsFinite(Time.fixedDeltaTime) || Time.fixedDeltaTime < 0f) return;

            HandleMovement();
            HandleBooster();
        }

        private void HandleMovement()
        {
            float horizontal = inputService.GetHorizontalAxis();
            if (float.IsNaN(horizontal) || float.IsInfinity(horizontal))
            {
                horizontal = 0f;
            }
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            bool isGround = IsGround();
            var fixedDeltaTime = Time.fixedDeltaTime;
            if (!float.IsFinite(fixedDeltaTime) || fixedDeltaTime < 0f)
            {
                return;
            }

            Vector2 velocity = rigidbody2D.linearVelocity;
            if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
            {
                velocity = Vector2.zero;
            }

            if (Mathf.Abs(horizontal) > 0.1f)
            {
                float accel = isGround ? groundAcceleration : groundAcceleration * 0.5f;
                accel = float.IsFinite(accel) ? Mathf.Max(0f, accel) : 0f;
                var safeMaxSpeed = float.IsFinite(maxSpeed) ? Mathf.Max(0f, maxSpeed) : 0f;
                velocity.x += horizontal * accel * fixedDeltaTime;
                velocity.x = Mathf.Clamp(velocity.x, -safeMaxSpeed, safeMaxSpeed);
            }
            else
            {
                float friction = isGround ? 0.8f : airResistance;
                friction = float.IsFinite(friction) ? Mathf.Clamp01(friction) : 0f;
                velocity.x *= friction;
            }

            if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
            {
                velocity = Vector2.zero;
            }

            rigidbody2D.linearVelocity = velocity;
        }

        private void HandleBooster()
        {
            bool boostingNow = inputService.IsBoosterPressed() && Status.Booster > 0;
            var fixedDeltaTime = Time.fixedDeltaTime;
            if (!float.IsFinite(fixedDeltaTime) || fixedDeltaTime < 0f)
            {
                return;
            }

            if (boostingNow)
            {
                if (!wasBoosting)
                {
                    PlayGeneralSound(EPlayerGeneralSound.BoosterStart);
                    boosterLoopTimer = 0f;
                }

                boosterLoopTimer = (float.IsFinite(boosterLoopTimer) ? boosterLoopTimer : 0f) - fixedDeltaTime;
                if (boosterLoopTimer <= 0f)
                {
                    PlayGeneralSound(EPlayerGeneralSound.BoosterLoop);
                    boosterLoopTimer = 0.35f;
                }

                Vector2 velocity = rigidbody2D.linearVelocity;
                if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                {
                    velocity = Vector2.zero;
                }

                var safeAcceleration = float.IsFinite(boosterAcceleration) ? Mathf.Max(0f, boosterAcceleration) : 0f;
                var safeMaxBoosterSpeed = float.IsFinite(maxBoosterSpeed) ? Mathf.Max(0f, maxBoosterSpeed) : 0f;
                velocity.y += safeAcceleration * fixedDeltaTime;
                velocity.y = Mathf.Min(velocity.y, safeMaxBoosterSpeed);
                if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                {
                    velocity = Vector2.zero;
                }

                rigidbody2D.linearVelocity = velocity;

                var safeConsumption = float.IsFinite(boosterConsumption) ? Mathf.Max(0f, boosterConsumption) : 0f;
                Status.ConsumeBooster(safeConsumption * fixedDeltaTime);
            }
            else if (wasBoosting)
            {
                PlayGeneralSound(EPlayerGeneralSound.BoosterEnd);
            }

            wasBoosting = boostingNow;
        }

        protected virtual void LateUpdate()
        {
            if (CheckFallDeath() || isDead)
            {
                wasGrounded = IsGround();
                return;
            }

            bool groundedNow = IsGround();
            if (groundedNow && !wasGrounded)
            {
                PlayGeneralSound(EPlayerGeneralSound.JumpEnd);
            }

            wasGrounded = groundedNow;
        }

        private void HandleActions()
        {
            var currentGun = weaponSlots != null ? weaponSlots.GetCurrentGun() : null;

            // 射撃入力
            if (inputService.IsFirePressed())
            {
                currentGun?.StartFire();
            }
            else
            {
                currentGun?.StopFire();
            }

            // リロード入力
            if (inputService.IsReloadJustPressed())
            {
                currentGun?.ReloadStart();
            }

            if (inputService.IsJumpJustPressed())
            {
                Jump();
            }

            if (inputService.IsSitJustPressed())
            {
                if (Sitting())
                {
                    StandUp();
                }
                else
                {
                    Sit();
                }
            }

            if (inputService.IsLieDownJustPressed())
            {
                LieDown();
            }

            if (weaponSlots != null && inputService.IsSwapWeaponJustPressed())
            {
                weaponSlots.FlipWeapon();
            }

            if (weaponSlots != null && inputService.IsDropWeaponJustPressed())
            {
                weaponSlots.DropCurrentWeapon();
            }

            int instantItemSlot = inputService.GetInstantItemSlotJustPressed();
            if (instantItemSlot > 0)
            {
                UseItem(instantItemSlot);
            }
        }

        public new void Jump()
        {
            if (rigidbody2D != null && IsGround())
            {
                rigidbody2D.AddForce(Vector2.up * jumpCurve.Evaluate(1.0f) * 10f, ForceMode2D.Impulse);
                PlayGeneralSound(EPlayerGeneralSound.JumpStart);
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
            if (wasBoosting)
            {
                PlayGeneralSound(EPlayerGeneralSound.BoosterEnd);
                wasBoosting = false;
            }
            rigidbody2D.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
            rigidbody2D.angularVelocity = Random.Range(-360f, 360f);
        }
    }
}
