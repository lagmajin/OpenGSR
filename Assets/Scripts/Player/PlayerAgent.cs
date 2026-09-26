using System.Collections;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using OpenGSCore;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Zenject;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerAgent : AbstractPlayerAgent, IDamagableObject, IPowerupable, IDamageable
    {
        [SerializeField] private BoxCollider2D standingCollider;
        [FormerlySerializedAs("sitingCollider")]
        [SerializeField] private BoxCollider2D sittingCollider;

        [SerializeField]private GameObject head;
        [SerializeField]private HeadController headController;
        [SerializeField] private GameObject weaponArm;
        [SerializeField] private AbstractGunController primaryGunController;
        [SerializeField]private WeaponArmController armController;
        [SerializeField] private WeaponSlots weaponSlots;

        public float movementSpeed;
        public float jumpHeight;

        public LayerMask groundLayer;

        public float horizontalSpeed, verticalSpeed;
        public bool isGrounded, isWallAhead;
        float extraSpeed;
        Vector2 rightScale, leftScale;
        float dashingSpeed = 0;
        float dashTimer = 0.15f;
        bool isDashing;



        [SerializeField] float doubleTapTime = 0.3f;
        [SerializeField] float dashDuration = 0.2f;
        [SerializeField] float dashSpeed = 6f;
        [SerializeField] Vector2 dashAngle = new Vector2(1f, 0.3f); // 右上方向など
        [SerializeField] JetBooster jetBooster;
        float lastLeftTapTime = -1f;
        float lastRightTapTime = -1f;

        Vector2 dashDir = Vector2.zero;
        private Vector2 currentGroundNormal = Vector2.up; // 地面の法線を保持



        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField]private Animator animator;
        private bool invincible = false;
        Tween fadeTween;

        [SerializeField]protected BattleSceneMediateObject battleSceneMediateObject;

        [SerializeField]private PlayerMasterData playerMasterData;
        [SerializeField] private AudioSource audioSource;
        private IInputService inputService = new UnityInputService();

        public Vector2 GetAimWorldPosition() => inputService.GetAimWorldPosition();

        [Inject]
        private void Construct(IInputService resolvedInputService)
        {
            if (resolvedInputService != null)
            {
                inputService = resolvedInputService;
            }
        }

        private SpriteRenderer[] spriteRendereres;

        [SerializeField] private new Rigidbody2D rigidbody2D;
        [SerializeField] private float gravity = 28f;
        [SerializeField] private float groundProbeDistance = 0.12f;
        [SerializeField] private float groundSnapDistance = 0.18f;
        [SerializeField] private float wallProbeDistance = 0.08f;
        [SerializeField] private float maxGroundAngle = 70f;
        [SerializeField] private float groundAcceleration = 45f;
        [SerializeField] private float airAcceleration = 25f;
        [SerializeField] private float groundFriction = 35f;
        [SerializeField] private float collisionSkinWidth = 0.02f;
        [SerializeField] private float maxFallSpeed = 12f;
        [SerializeField] private float maxBoostRiseSpeed = 10f;
        [SerializeField] private float fallDeathY = -80f;

        private float currentHorizontalVelocity;
        private Vector2 scriptedPosition;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        private float coyoteTimeLeft;
        private float jumpBufferLeft;
        private float jumpVelocity;

        private const float BuffedMultiplier = 2f;
        private const float InvisibleAlpha = 0.3f;
        private float baseMovementSpeed;
        private float baseDashSpeed;
        private float attackMultiplier = 1f;
        private float defenseMultiplier = 1f;
        private float moveSpeedMultiplier = 1f;
        private int attackBuffVersion;
        private int defenseBuffVersion;
        private int speedBuffVersion;
        private int invisibleBuffVersion;
        private int normalGrenadeCount = 3;
        private int powerGrenadeCount;
        private int clusterGrenadeCount;
        private int magneticGrenadeCount;
        private int mineGrenadeCount;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        private readonly InstantItemSlots instantItemSlots = new InstantItemSlots();
        private readonly List<EGrenadeType> grenadeSlotTypes = new List<EGrenadeType>(3);
        private bool instantItemsLoaded;
        private MatchEventProvider matchEventProvider;
        private bool invisibleBuffActive = false;
        private float elementalBulletTime;
        private eDamageType elementalBulletType = eDamageType.Bullet;
        private bool deathTriggered = false;
        [Header("Standing Sway")]
        [SerializeField] private float standBobAmplitude = 0.02f;
        [SerializeField] private float standBobFrequency = 7.5f;
        [SerializeField] private Vector3 standBobDirection = new Vector3(0f, 1f, 0f);

        public int NormalGrenadeCount => normalGrenadeCount;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public eDamageType CurrentBulletDamageType => elementalBulletTime > 0f ? elementalBulletType : eDamageType.Bullet;

        public int GetGrenadeCount(EGrenadeType type)
        {
            return type switch
            {
                EGrenadeType.Normal => normalGrenadeCount,
                EGrenadeType.Power => powerGrenadeCount,
                EGrenadeType.Cluster => clusterGrenadeCount,
                EGrenadeType.Magnetic => magneticGrenadeCount,
                EGrenadeType.Mine => mineGrenadeCount,
                _ => 0
            };
        }
        public Vector3 StandingBobOffset => GetStandingBobOffset();

        public bool TryConsumeNormalGrenade()
        {
            if (normalGrenadeCount <= 0)
            {
                return false;
            }

            normalGrenadeCount--;
            return true;
        }

        public void RefillNormalGrenadeIfEmpty()
        {
            if (normalGrenadeCount <= 0)
            {
                normalGrenadeCount = 3;
                Debug.Log("[PlayerAgent] Normal grenades refilled on spawn/respawn.");
            }
        }

        void Start()
        {
            AutoBindComponents();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            AutoBindColliders();
            rigidbody2D = GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                rigidbody2D.gravityScale = 0f;
                rigidbody2D.freezeRotation = true;
                rigidbody2D.useFullKinematicContacts = true;
                rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
                rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            PhysicsMaterial2D material = new PhysicsMaterial2D();
            material.bounciness = 0; // 反発なし
            material.friction = 0.4f; // 適度な摩擦

            if (rigidbody2D != null)
            {
                rigidbody2D.sharedMaterial = material;
            }
            scriptedPosition = rigidbody2D != null ? rigidbody2D.position : (Vector2)transform.position;
            RecalculateJumpVelocity();



            Debug.Log("Init scale: " + transform.localScale);
            rightScale = transform.localScale;
            leftScale = transform.localScale;
            leftScale.x *= -1;

            var list = new List<SpriteRenderer>();
            if (spriteRenderer != null) list.Add(spriteRenderer);

            if (head != null)
            {
                var headRenderer = head.GetComponent<SpriteRenderer>();
                if (headRenderer != null) list.Add(headRenderer);
            }

            if (weaponArm != null)
            {
                var armRenderer=weaponArm.GetComponent<SpriteRenderer>();
                if (armRenderer != null) list.Add(armRenderer);
            }

            spriteRendereres = list.ToArray();

            baseMovementSpeed = movementSpeed;
            baseDashSpeed = dashSpeed;
            currentHealth = Mathf.Max(1f, maxHealth);
            ResetPowerupState();
            StartInvincibility(2.0f);

            AutoSetMediateObject();
            LoadInstantItems();
            LoadGrenadeSlots();
            EnsureDefaultSecondaryWeapon();
            AutoBindComponents();

            if (PlayerType() == EPlayerType.MyPlayer && GetComponent<PlayerInputSender>() == null)
            {
                gameObject.AddComponent<PlayerInputSender>();
            }
        }

        private void AutoBindComponents()
        {
            playerTransform ??= transform;
            rigidbody2D ??= GetComponent<Rigidbody2D>();
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            headController ??= GetComponent<HeadController>();
            armController ??= GetComponentInChildren<WeaponArmController>(true);
            jetBooster ??= GetComponentInChildren<JetBooster>(true);
            primaryGunController ??= GetComponentInChildren<AbstractGunController>(true);

            head ??= transform.Find("Head");
            weaponArm ??= transform.Find("Arm");
        }

        private void OnDisable()
        {
            CancelInvoke();
            StopAllCoroutines();
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }

            attackBuffVersion++;
            defenseBuffVersion++;
            speedBuffVersion++;
            invisibleBuffVersion++;
            attackMultiplier = 1f;
            defenseMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            invisibleBuffActive = false;
            invincible = false;
            movementSpeed = baseMovementSpeed;
            dashSpeed = baseDashSpeed;
            isDashing = false;
            dashTimer = 0f;
            SetSpriteAlpha(1f);
            base.OnDisable();
        }

        private void EnsureDefaultSecondaryWeapon()
        {
            if (weaponSlots == null)
            {
                weaponSlots = GetComponent<WeaponSlots>();
            }

            if (weaponSlots == null)
            {
                weaponSlots = gameObject.AddComponent<WeaponSlots>();
            }

            weaponSlots.mainWeaponSlot = EnsureWeaponSlot("MainWeaponSlot");
            weaponSlots.secondaryWeaponSlot = EnsureWeaponSlot("SecondaryWeaponSlot");
            weaponSlots.specialWeaponSlot = EnsureWeaponSlot("SpecialWeaponSlot");

            if (weaponSlots.secondaryWeaponSlot.transform.childCount == 0)
            {
                var pistol = Resources.Load<GameObject>("Prefabs/Weapon/Guns/Pistol/Glock");
                if (pistol != null)
                {
                    weaponSlots.currentEquipType = EPlayerEquipWeapon.SecondaryWeapon;
                    weaponSlots.EquipWeapon(pistol);
                }
                else
                {
                    Debug.LogWarning("[PlayerAgent] Default secondary pistol was not found.");
                }
            }
        }

        public bool CanEquipWeapon()
        {
            return weaponSlots != null && weaponSlots.currentEquipType != EPlayerEquipWeapon.SpecialWeapon;
        }

        public void EquipWeapon(GameObject prefab)
        {
            if (prefab == null || weaponSlots == null) return;
            weaponSlots.EquipWeapon(prefab);
        }

        public bool EquipWeaponType(EWeaponType weaponType)
        {
            var prefab = WeaponPrefabResolver.Load(weaponType);
            if (prefab == null)
            {
                Debug.LogWarning($"[PlayerAgent] Weapon prefab not found for {weaponType}.");
                return false;
            }

            if (weaponSlots == null)
            {
                EnsureDefaultSecondaryWeapon();
            }

            if (weaponSlots.currentEquipType == EPlayerEquipWeapon.SpecialWeapon)
            {
                weaponSlots.ClearSpecialWeapon();
            }

            weaponSlots.EquipWeapon(prefab);
            return weaponSlots.currentWeaponObject != null;
        }

        public void SetCurrentWeaponMagazine(int magazine)
        {
            weaponSlots?.GetCurrentGun()?.SetMagazineCount(magazine);
        }

        private GameObject EnsureWeaponSlot(string slotName)
        {
            var existing = transform.Find(slotName);
            if (existing != null) return existing.gameObject;
            var slot = new GameObject(slotName);
            slot.transform.SetParent(transform, false);
            return slot;
        }

        private void LoadInstantItems()
        {
            instantItemSlots.SetFromEquippedItems(GetEquippedInstantItemTypes());
            instantItemsLoaded = true;
        }

        private void LoadGrenadeSlots()
        {
            grenadeSlotTypes.Clear();
            var equipped = UserSaveManager.GetEquippedGrenadeSlots();
            for (var i = 0; i < 3; i++)
            {
                var type = EGrenadeType.Normal;
                if (equipped != null && i < equipped.Length && !string.IsNullOrWhiteSpace(equipped[i]))
                    Enum.TryParse(equipped[i], true, out type);
                grenadeSlotTypes.Add(type);
            }
        }

        private static IEnumerable<EInstantItemType> GetEquippedInstantItemTypes()
        {
            var equipped = UserSaveManager.GetEquippedInstantItems();
            if (equipped == null) yield break;
            foreach (var id in equipped)
            {
                if (string.IsNullOrWhiteSpace(id) || !Enum.TryParse(id, true, out EInstantItemType type))
                    yield return EInstantItemType.None;
                else
                    yield return type;
            }
        }

        public bool TryUseInstantItem(int slotNumber)
        {
            if (!instantItemsLoaded) LoadInstantItems();
            var equippedItemType = instantItemSlots.GetSlotType(slotNumber - 1);
            if (IsGrenadePack(equippedItemType) && grenadeSlotTypes.Count >= 3)
            {
                Debug.Log("[PlayerAgent] Grenade slots are full; grenade pack was not used.");
                return false;
            }
            if (!instantItemSlots.TryUse(slotNumber - 1, out var type)) return false;

            switch (type)
            {
                case EInstantItemType.HealthKit:
                    Heal(100f);
                    break;
                case EInstantItemType.FireBullet:
                    elementalBulletType = eDamageType.Fire;
                    elementalBulletTime = 30f;
                    break;
                case EInstantItemType.PoisonBullet:
                    elementalBulletType = eDamageType.Poison;
                    elementalBulletTime = 30f;
                    break;
                case EInstantItemType.PowerGrenadePack:
                    TryAddGrenadeSlot(EGrenadeType.Power);
                    RefillGrenade(EGrenadeType.Power);
                    break;
                case EInstantItemType.ClusterGrenadePack:
                    TryAddGrenadeSlot(EGrenadeType.Cluster);
                    RefillGrenade(EGrenadeType.Cluster);
                    break;
                case EInstantItemType.MagnetGrenadePack:
                    TryAddGrenadeSlot(EGrenadeType.Magnetic);
                    RefillGrenade(EGrenadeType.Magnetic);
                    break;
                case EInstantItemType.MineGrenadePack:
                    TryAddGrenadeSlot(EGrenadeType.Mine);
                    RefillGrenade(EGrenadeType.Mine);
                    break;
            }

            matchEventProvider ??= GetComponent<MatchEventProvider>();
            matchEventProvider?.UseInstantItem(this, type);
            Debug.Log($"[PlayerAgent] Instant item used: {type}, slot={slotNumber}");
            return true;
        }

        private static bool IsGrenadePack(EInstantItemType type)
        {
            return type == EInstantItemType.PowerGrenadePack
                || type == EInstantItemType.ClusterGrenadePack
                || type == EInstantItemType.MagnetGrenadePack
                || type == EInstantItemType.MineGrenadePack;
        }

        private bool TryAddGrenadeSlot(EGrenadeType type)
        {
            if (grenadeSlotTypes.Count >= 3) return false;
            grenadeSlotTypes.Add(type);
            return true;
        }

        public int GetInstantItemSlotCount() => instantItemSlots.Count();
        public EInstantItemType GetInstantItemType(int slotIndex) => instantItemSlots.GetSlotType(slotIndex);

        public EGrenadeType GetGrenadeSlotType(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < grenadeSlotTypes.Count
                ? grenadeSlotTypes[slotIndex]
                : EGrenadeType.Empty;
        }

        public bool TryConsumeGrenadeSlot(out EGrenadeType type)
        {
            type = EGrenadeType.Empty;
            if (grenadeSlotTypes.Count == 0) return false;
            type = grenadeSlotTypes[0];
            grenadeSlotTypes.RemoveAt(0);
            return true;
        }

        protected void AutoSetMediateObject()
        {
            battleSceneMediateObject = FindFirstObjectByType<BattleSceneMediateObject>();
        }
        void SetMatchManager(MatchManager matchManager)
        {
            if (matchManager == null)
            {
                Debug.LogWarning("[PlayerAgent] SetMatchManager called with null");
                return;
            }

            Debug.Log("[PlayerAgent] MatchManager assigned");
        }

        void StartInvincibility(float duration)
        {
            invincible = true;
            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                // 全てのSpriteRendererに対して点滅開始
                if (spriteRendereres == null) return;
                foreach (var sr in spriteRendereres)
                {
                    sr?.DOFade(0f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetId("invincible");
                }
            });

            seq.AppendInterval(duration);

            seq.AppendCallback(() =>
            {
                invincible = false;

                // DOTweenのIDを使って全部止める
                DOTween.Kill("invincible");

                if (spriteRendereres == null) return;
                foreach (var sr in spriteRendereres)
                {
                    sr?.DOFade(invisibleBuffActive ? InvisibleAlpha : 1f, 0f); // 完全表示に戻す
                }
            });
        }
        // Start is called before the first frame update

        // Update is called once per frame

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) return;
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            if (elementalBulletTime > 0f) elementalBulletTime = Mathf.Max(0f, elementalBulletTime - deltaTime);

            if (IsRemoteControlled())
            {
                return;
            }

            HandleDefaultWeaponInput();
            var instantItemSlot = inputService.GetInstantItemSlotJustPressed();
            if (instantItemSlot > 0) TryUseInstantItem(instantItemSlot);
            if (CheckFallDeath())
            {
                return;
            }

            GetInput();
            CheckJumping();
            CheckFlip();
            CheckDashing();
            jetBooster?.SetBoostHeld(inputService.IsBoosterPressed());

            if (!isDashing && isGrounded) // 地上限定
            {
                if (inputService.IsMoveRightJustPressed())
                {
                    var now = Time.time;
                    if (!float.IsFinite(now) || now < 0f)
                    {
                        return;
                    }

                    if (now - lastRightTapTime < doubleTapTime)
                        StartDash(Vector2.right);
                    lastRightTapTime = now;
                }
                if (inputService.IsMoveLeftJustPressed())
                {
                    var now = Time.time;
                    if (!float.IsFinite(now) || now < 0f)
                    {
                        return;
                    }

                    if (now - lastLeftTapTime < doubleTapTime)
                        StartDash(Vector2.left);
                    lastLeftTapTime = now;
                }
            }

            if (inputService.IsSitJustPressed())
            {
                Sit();
            }

            if(inputService.IsCrouchJustReleased())
            {
                StandUp();
            }

            if (inputService.IsLieDownJustPressed())
            {
                LieDown();
            }

            if (inputService.IsLieDownJustReleased())
            {
                StandUp();
            }

            if (isDashing)
            {
                dashTimer = Mathf.Max(0f, dashTimer - deltaTime);
                if (dashTimer <= 0f)
                    isDashing = false;
            }

            // 射撃入力の処理
            HandleFireInput();
        }

        private void HandleFireInput()
        {
            if (weaponSlots == null) return;
            
            var currentGun = weaponSlots.currentWeapon != null
                ? weaponSlots.currentWeapon.GetComponentInChildren<AbstractGunController>()
                : null;
            if (currentGun == null) return;

            if (inputService.IsFireJustPressed())
            {
                currentGun.StartFire();
                // 特殊武器の弾数減衰を通知
                weaponSlots.OnFireSpecialWeapon();
            }
            else if (!inputService.IsFirePressed())
            {
                currentGun.StopFire();
            }
        }

        public void FixedUpdate()
        {
            var fixedDeltaTime = Time.fixedDeltaTime;
            if (!float.IsFinite(fixedDeltaTime) || fixedDeltaTime < 0f) return;

            if (CheckFallDeath())
            {
                return;
            }

            if (IsRemoteControlled())
            {
                return;
            }

            CheckGround();
            coyoteTimeLeft = Mathf.Max(0f, coyoteTimeLeft - fixedDeltaTime);
            jumpBufferLeft = Mathf.Max(0f, jumpBufferLeft - fixedDeltaTime);
            ApplyDashing();
            ApplyMovement();
            ResolvePenetration();
            SnapToGround();
            CheckGround();
            jetBooster?.RecoverFuel(fixedDeltaTime);
        }

        private bool IsRemoteControlled()
        {
            return PlayerType() != EPlayerType.Unknown && PlayerType() != EPlayerType.MyPlayer;
        }
        void StartDash(Vector2 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            var safeDashAngle = dashAngle;
            if (!float.IsFinite(safeDashAngle.x) || !float.IsFinite(safeDashAngle.y) ||
                safeDashAngle.sqrMagnitude <= Mathf.Epsilon)
            {
                safeDashAngle = Vector2.right;
            }

            safeDashAngle.Normalize();
            dashDir = new Vector2(safeDashAngle.x * direction.x, safeDashAngle.y).normalized;
            if (!float.IsFinite(dashDir.x) || !float.IsFinite(dashDir.y) || dashDir.sqrMagnitude <= Mathf.Epsilon)
            {
                dashDir = direction;
            }

            isDashing = true;
            dashTimer = float.IsFinite(dashDuration) ? Mathf.Max(0f, dashDuration) : 0f;
        }
        void GetInput()
        {
            if (inputService.IsSprintPressed())
                extraSpeed = 2f;
            else
                extraSpeed = 1;

            horizontalSpeed = inputService.GetHorizontalAxis() * extraSpeed;
        }

        public void Sit()
        {
            SetPoseState(isSit: true, isLieDown: false);
            if (standingCollider != null) standingCollider.enabled = false;
            if (sittingCollider != null) sittingCollider.enabled = true;
            
            headController?.Sit();

            primaryGunController?.Sit();

            armController?.Sit();
        }

        private void StandUp()
        {
            SetPoseState(isSit: false, isLieDown: false);
            if (standingCollider != null) standingCollider.enabled = true;
            if (sittingCollider != null) sittingCollider.enabled = false;

            headController?.StandUp();
            primaryGunController?.StandUp();
            armController?.StandUp();
        }
        private void AutoBindColliders()
        {
            var boxColliders = GetComponents<BoxCollider2D>();
            if (standingCollider == null && boxColliders.Length > 0)
            {
                standingCollider = boxColliders[0];
            }

            if (sittingCollider == null && boxColliders.Length > 1)
            {
                sittingCollider = boxColliders[1];
            }

            if (standingCollider != null)
            {
                standingCollider.enabled = true;
            }

            var capsuleCollider = GetComponent<CapsuleCollider2D>();
            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }
        }

        public void LieDown()
        {
            SetPoseState(isSit: false, isLieDown: true);
            if (standingCollider != null) standingCollider.enabled = false;
            if (sittingCollider != null) sittingCollider.enabled = true;

            headController?.Sit();
            primaryGunController?.Sit();
            armController?.Sit();
        }

        private void SetPoseState(bool isSit, bool isLieDown)
        {
            if (animator == null)
            {
                return;
            }

            if (animator != null)
            {
                animator.SetBool("IsSit", isSit);
                animator.SetBool("IsLieDown", isLieDown);
            }
        }
        private void OnValidate()
        {
            doubleTapTime = SafeMin(doubleTapTime, 0.01f, 0.3f);
            dashDuration = SafeMin(dashDuration, 0.01f, 0.2f);
            dashSpeed = SafeMin(dashSpeed, 0f, 6f);
            gravity = SafeMin(gravity, 0f, 28f);
            groundProbeDistance = SafeMin(groundProbeDistance, 0f, 0.12f);
            groundSnapDistance = SafeMin(groundSnapDistance, 0f, 0.18f);
            wallProbeDistance = SafeMin(wallProbeDistance, 0f, 0.08f);
            maxGroundAngle = SafeMin(maxGroundAngle, 0f, 70f, 180f);
            groundAcceleration = SafeMin(groundAcceleration, 0f, 45f);
            airAcceleration = SafeMin(airAcceleration, 0f, 25f);
            groundFriction = SafeMin(groundFriction, 0f, 35f);
            collisionSkinWidth = SafeMin(collisionSkinWidth, 0f, 0.02f);
            maxFallSpeed = SafeMin(maxFallSpeed, 0f, 12f);
            maxBoostRiseSpeed = SafeMin(maxBoostRiseSpeed, 0f, 10f);
            fallDeathY = SafeFinite(fallDeathY, -80f);
            coyoteTime = SafeMin(coyoteTime, 0f, 0.12f);
            jumpBufferTime = SafeMin(jumpBufferTime, 0f, 0.12f);
            maxHealth = SafeMin(maxHealth, 1f, 100f);
            currentHealth = Mathf.Clamp(SafeMin(currentHealth, 0f, maxHealth), 0f, maxHealth);
            standBobAmplitude = SafeMin(standBobAmplitude, 0f, 0.02f);
            standBobFrequency = SafeMin(standBobFrequency, 0f, 7.5f);
            if (!float.IsFinite(dashAngle.x) || !float.IsFinite(dashAngle.y) || dashAngle.sqrMagnitude <= Mathf.Epsilon)
            {
                dashAngle = new Vector2(1f, 0.3f);
            }
            RecalculateJumpVelocity();
        }

        private static float SafeMin(float value, float minimum, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(minimum, value) : fallback;
        }

        private static float SafeMin(float value, float minimum, float fallback, float maximum)
        {
            return float.IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : fallback;
        }

        private static float SafeFinite(float value, float fallback)
        {
            return float.IsFinite(value) ? value : fallback;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var collider = GetMovementCollider();
            if (collider != null)
            {
                var bounds = collider.bounds;
                Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z), new Vector3(bounds.size.x, 0.04f, 0f));
            }
        }
        void CheckGround()
        {
            bool wasGrounded = isGrounded;
            if (!TryGetGroundHit(GetCurrentPosition(), groundProbeDistance, out var hit))
            {
                isGrounded = false;
                currentGroundNormal = Vector2.up;
                return;
            }

            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle <= maxGroundAngle && verticalSpeed <= 0.5f)
            {
                isGrounded = true;
                currentGroundNormal = hit.normal;

                if (!wasGrounded)
                {
                    headController?.OnGround();
                    jetBooster?.OnLanding();
                    animator?.SetBool("IsJump", false);
                }

                if (verticalSpeed < 0f)
                {
                    verticalSpeed = 0f;
                }
                coyoteTimeLeft = coyoteTime;
                return;
            }

            isGrounded = false;
            currentGroundNormal = Vector2.up;
        }
        void CheckJumping()
        {
            if (inputService.IsJumpJustPressed())
            {
                jumpBufferLeft = jumpBufferTime;
            }
        }

        void CheckHorizontalCollision()
        {
            float facing = Mathf.Sign(transform.localScale.x);
            if (Mathf.Approximately(facing, 0f))
            {
                facing = 1f;
            }

            Vector2 direction = Vector2.right * facing;
            isWallAhead = TryGetHit(GetCurrentPosition(), direction, wallProbeDistance, out var hit)
                && !IsWalkable(hit.normal);
        }

        void CheckFlip()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 mouseWorldPos = inputService.GetAimWorldPosition();
            float deltaX = mouseWorldPos.x - transform.position.x;

            if (deltaX > 0)
            {
                transform.localScale = leftScale;
            }
            else if (deltaX < 0)
            {
                transform.localScale = rightScale;
            }
        }

        void CheckDashing()
        {
            if (inputService.IsDashJustPressed() && !isDashing)
            {
                isDashing = true;
                Invoke("EndDash", dashTimer);
            }
        }

        void EndDash()
        {
            isDashing = false;
        }

        void ApplyDashing()
        {
            if (isDashing)
            {
                if (!isWallAhead)
                    dashingSpeed = Mathf.Lerp(dashingSpeed, 2 * Mathf.Sign(transform.localScale.x), 10 * Time.fixedDeltaTime);
                else
                {
                    isDashing = false;
                    dashingSpeed = 0;
                }
            }
            else
            {
                dashingSpeed = Mathf.Lerp(dashingSpeed, 0, 10 * Time.fixedDeltaTime);
            }
        }
        void ApplyMovement()
        {
            float dt = Time.fixedDeltaTime;
            if (!float.IsFinite(dt) || dt < 0f) return;

            CheckHorizontalCollision();

            float horizontalInput = isWallAhead ? 0f : horizontalSpeed;
            float targetHorizontalVelocity = (horizontalInput + dashingSpeed) * movementSpeed;
            float accel = Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(dashingSpeed) > 0.01f
                ? (isGrounded ? groundAcceleration : airAcceleration)
                : groundFriction;

            currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, accel * dt);

            if (jumpBufferLeft > 0f && coyoteTimeLeft > 0f)
            {
                jumpBufferLeft = 0f;
                coyoteTimeLeft = 0f;
                isGrounded = false;
                verticalSpeed = jumpVelocity;
                headController?.Jump();
                animator?.SetBool("IsJump", true);
            }

            if (!isGrounded || verticalSpeed > 0f)
            {
                verticalSpeed = Mathf.MoveTowards(verticalSpeed, -maxFallSpeed, gravity * dt);
            }
            else if (verticalSpeed < 0f)
            {
                verticalSpeed = 0f;
            }

            if (jetBooster != null)
            {
                float boostAcceleration = jetBooster.StepBoost(dt);
                if (boostAcceleration > 0f)
                {
                    verticalSpeed = Mathf.Min(verticalSpeed + boostAcceleration * dt, maxBoostRiseSpeed);
                    isGrounded = false;
                }
            }

            Vector2 position = GetCurrentPosition();
            Vector2 delta = BuildMovementDelta(currentHorizontalVelocity * dt, verticalSpeed * dt);
            position = MoveWithCast(position, delta);
            ApplyPosition(position);
        }

        private BoxCollider2D GetMovementCollider()
        {
            if (standingCollider != null && standingCollider.enabled)
            {
                return standingCollider;
            }

            if (sittingCollider != null && sittingCollider.enabled)
            {
                return sittingCollider;
            }

            return GetComponent<BoxCollider2D>();
        }

        private void SnapToGround()
        {
            if (verticalSpeed > 0.1f)
            {
                return;
            }

            Vector2 position = GetCurrentPosition();
            if (!TryGetGroundHit(position, groundSnapDistance, out var hit))
            {
                return;
            }

            if (!IsWalkable(hit.normal))
            {
                return;
            }

            float snapDistance = Mathf.Max(0f, hit.distance - collisionSkinWidth);
            if (snapDistance <= 0f)
            {
                return;
            }

            ApplyPosition(position + Vector2.down * snapDistance);
            isGrounded = true;
            currentGroundNormal = hit.normal;
            verticalSpeed = 0f;
        }

        private void ResolvePenetration()
        {
            var collider = GetMovementCollider();
            if (collider == null)
            {
                return;
            }

            const int maxIterations = 8;
            const float step = 0.02f;

            for (int i = 0; i < maxIterations; i++)
            {
                Bounds bounds = collider.bounds;
                Collider2D[] overlaps = Physics2D.OverlapBoxAll(bounds.center, bounds.size - new Vector3(collisionSkinWidth, collisionSkinWidth, 0f), 0f, groundLayer);
                bool hasForeignOverlap = false;
                for (int j = 0; j < overlaps.Length; j++)
                {
                    if (overlaps[j] != null && overlaps[j] != collider)
                    {
                        hasForeignOverlap = true;
                        break;
                    }
                }

                if (!hasForeignOverlap)
                {
                    return;
                }

                ApplyPosition(GetCurrentPosition() + Vector2.up * step);
            }
        }

        private Vector2 BuildMovementDelta(float horizontalDelta, float verticalDelta)
        {
            if (!isGrounded)
            {
                return new Vector2(horizontalDelta, verticalDelta);
            }

            Vector2 tangent = new Vector2(currentGroundNormal.y, -currentGroundNormal.x).normalized;
            Vector2 slopeMove = tangent * horizontalDelta;
            Vector2 verticalMove = Vector2.up * verticalDelta;
            return slopeMove + verticalMove;
        }

        private Vector2 MoveWithCast(Vector2 position, Vector2 delta)
        {
            if (delta.sqrMagnitude <= 0.0000001f)
            {
                return position;
            }

            Vector2 direction = delta.normalized;
            float castDistance = delta.magnitude;

            if (!TryGetHit(position, direction, castDistance + collisionSkinWidth, out var hit))
            {
                return position + delta;
            }

            float moveDistance = Mathf.Max(0f, hit.distance - collisionSkinWidth);
            position += direction * moveDistance;

            if (delta.y < 0f && IsWalkable(hit.normal))
            {
                isGrounded = true;
                currentGroundNormal = hit.normal;
                verticalSpeed = 0f;
            }
            else if (delta.y > 0f)
            {
                verticalSpeed = 0f;
            }

            if (Mathf.Abs(delta.x) > 0f && !IsWalkable(hit.normal))
            {
                isWallAhead = true;
                currentHorizontalVelocity = 0f;
            }

            return position;
        }

        private bool TryGetGroundHit(Vector2 position, float distance, out RaycastHit2D hit)
        {
            hit = default;
            return TryGetHit(position, Vector2.down, distance, out hit) && IsWalkable(hit.normal);
        }

        private bool TryGetHit(Vector2 position, Vector2 direction, float distance, out RaycastHit2D bestHit)
        {
            bestHit = default;
            var collider = GetMovementCollider();
            if (collider == null)
            {
                return false;
            }

            Bounds bounds = collider.bounds;
            Vector2 delta = position - GetCurrentPosition();
            Vector2 center = (Vector2)bounds.center + delta;
            Vector2 size = bounds.size - new Vector3(collisionSkinWidth * 2f, collisionSkinWidth * 2f, 0f);
            size.x = Mathf.Max(size.x, 0.05f);
            size.y = Mathf.Max(size.y, 0.05f);

            bestHit = Physics2D.BoxCast(center, size, 0f, direction.normalized, distance, groundLayer);

            Debug.DrawRay(center, direction.normalized * distance, bestHit.collider ? Color.green : Color.red, Time.fixedDeltaTime);
            return bestHit.collider != null;
        }

        private bool IsWalkable(Vector2 normal)
        {
            return Vector2.Angle(normal, Vector2.up) <= maxGroundAngle;
        }

        private void RecalculateJumpVelocity()
        {
            jumpVelocity = jumpHeight > 0f
                ? Mathf.Sqrt(2f * gravity * jumpHeight)
                : 0f;
        }

        private Vector2 GetCurrentPosition()
        {
            return scriptedPosition;
        }

        private void ApplyPosition(Vector2 position)
        {
            scriptedPosition = position;

            if (rigidbody2D != null)
            {
                rigidbody2D.position = position;
            }

            transform.position = position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            var collider = GetMovementCollider();
            if (collider == null)
            {
                return;
            }

            Bounds bounds = collider.bounds;
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);
            Gizmos.DrawWireCube(center, new Vector3(bounds.size.x, 0.04f, 0f));
        }

        private void OnSpawn()
        {
            ResetPowerupState();
            isDashing = false;
            dashDir = Vector2.zero;
            invincible = false;
            SetSpriteAlpha(1f);
        }

        private void DropWeapon()
        {
            weaponSlots?.DropCurrentWeapon();
            OnDropWeapon();
        }

        [Button("死亡")]
        private void Die(EDeadReason reason=EDeadReason.Unknown, bool notifyServer = true)
        {
            if (deathTriggered)
            {
                return;
            }

            deathTriggered = true;

            if (TryGetDeathVoiceClip(out var sound))
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(sound);
                }
                else
                {
                    SoundManager.Instance?.PlayOneShotSafe(sound, context: nameof(PlayerAgent));
                }
            }

            DropWeapon();

            // Online matches receive the authoritative death event back from the
            // match server. Local hazards still use the request path.
            if (notifyServer)
            {
                SendDeathNotificationToServer(reason);
            }

            if (battleSceneMediateObject?.mainscript != null)
            {
                battleSceneMediateObject.mainscript.OnMyPlayerDead();
            }
            else
            {
                Debug.LogWarning("[PlayerAgent] Battle scene mediator/main script is not available during death cleanup.");
            }


            Destroy(this.gameObject);
        }

        /// <summary>
        /// Applies a death already confirmed by the match server without sending
        /// a second client-originated death notification.
        /// </summary>
        public void ApplyServerDeath(EDeadReason reason = EDeadReason.Unknown)
        {
            Die(reason, false);
        }

        private void HandleDefaultWeaponInput()
        {
            if (weaponSlots == null) return;

            var gun = weaponSlots.GetCurrentGun();
            if (gun != null)
            {
                if (inputService.IsFirePressed()) gun.StartFire();
                else gun.StopFire();

                if (inputService.IsReloadJustPressed()) gun.ReloadStart();
            }

            if (inputService.IsSwapWeaponJustPressed()) weaponSlots.FlipWeapon();
            if (inputService.IsDropWeaponJustPressed()) weaponSlots.DropCurrentWeapon();
        }

        private bool TryGetDeathVoiceClip(out AudioClip clip)
        {
            clip = null;

            if (playerMasterData == null || playerMasterData.damageVoices == null || playerMasterData.damageVoices.Length == 0)
            {
                return false;
            }

            var index = UnityEngine.Random.Range(0, playerMasterData.damageVoices.Length);
            clip = playerMasterData.damageVoices[index];
            return clip != null;
        }

        /// <summary>
        /// 死亡通知をサーバーに送信
        /// </summary>
        private void SendDeathNotificationToServer(EDeadReason reason)
        {
            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                if (networkManager != null && networkManager.IsConnected())
                {
                    // プレイヤーIDを取得
                    string playerId = gameObject.name;
                    string killerId = ""; // キルした場合はサーバー側で設定

                    // 死亡メッセージを作成して送信
                    var deathMsg = RUDPMessageBuilder.CreatePlayerDeath(playerId, killerId);
                    networkManager.SendToServer(deathMsg);

                    Debug.Log($"[Network] Death notification sent: {playerId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Network] Failed to send death notification: {ex.Message}");
            }
        }

        private bool CheckFallDeath()
        {
            if (deathTriggered || transform.position.y >= fallDeathY)
            {
                return false;
            }

            Die(EDeadReason.Suicide);
            return true;
        }

        public void TakeDamage()
        {
            if (invincible || IsIncreaseDefenseNow())
            {
                return;
            }

            Die();
        }

        public bool IsSpeedUpNow()
        {
            return moveSpeedMultiplier > 1f;
        }

        public bool IsIncreaseAttackNow()
        {
            return attackMultiplier > 1f;
        }

        public bool IsIncreaseDefenseNow()
        {
            return defenseMultiplier > 1f;
        }

        public void SpeedUp(float sec = 30.0f)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            StartCoroutine(SpeedUpCounter(sec));
        }

        public void IncreaseAttack(float sec = 30.0f)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            StartCoroutine(IncreaseAttackCounter(sec));
        }

        public void IncreaseDefense(float sec = 30.0f)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            StartCoroutine(IncreaseDefenseCounter(sec));
        }

        public void Invisible(float sec = 30.0f)
        {
            if (!float.IsFinite(sec) || sec <= 0f) return;
            StartCoroutine(InvisibleCounter(sec));
        }

        public void RefillGrenade()
        {
            RefillGrenade(EGrenadeType.Normal, 3);
        }

        public void RefillGrenade(EGrenadeType type, int amount = 3)
        {
            var value = Mathf.Clamp(amount, 0, 3);
            switch (type)
            {
                case EGrenadeType.Normal: normalGrenadeCount = value; break;
                case EGrenadeType.Power: powerGrenadeCount = value; break;
                case EGrenadeType.Cluster: clusterGrenadeCount = value; break;
                case EGrenadeType.Magnetic: magneticGrenadeCount = value; break;
                case EGrenadeType.Mine: mineGrenadeCount = value; break;
            }
            Debug.Log($"[PlayerAgent] Grenade refilled: {type}={value}");
        }

        public void Berserk()
        {
            IncreaseAttack(10.0f);
            SpeedUp(10.0f);
            Debug.Log("[PlayerAgent] Berserk activated");
        }

        public void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            if (!float.IsFinite(damage) || damage <= 0f)
            {
                return;
            }

            if (invincible || IsIncreaseDefenseNow())
            {
                Debug.Log("[PlayerAgent] Damage ignored by invincibility or defense buff.");
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void AddDamageAndForce(float damage, Vector3 vec, float force = 1.0f)
        {
            if (!float.IsFinite(damage) || damage <= 0f
                || !float.IsFinite(force)
                || !float.IsFinite(vec.x) || !float.IsFinite(vec.y) || !float.IsFinite(vec.z))
            {
                return;
            }

            if (invincible || IsIncreaseDefenseNow())
            {
                Debug.Log("[PlayerAgent] Damage ignored by invincibility or defense buff.");
                return;
            }

            if (rigidbody2D != null)
            {
                rigidbody2D.AddForce(new Vector2(vec.x, vec.y).normalized * force, ForceMode2D.Impulse);
            }

            // Apply the requested amount instead of routing impact damage to
            // TakeDamage(), which would kill the player regardless of damage.
            AddDamage(Vector2.zero, damage, eDamageType.None);
        }

        public void AddDamageAndForce2(float damage, Vector2 point)
        {
            AddDamage(point, damage, eDamageType.None);
        }

        public void Heal(float heal = 0)
        {
            if (!float.IsFinite(heal) || heal <= 0f || deathTriggered) return;
            currentHealth = Mathf.Min(Mathf.Max(1f, maxHealth), currentHealth + heal);
            Debug.Log($"[PlayerAgent] Healed: +{heal}, HP={currentHealth}/{maxHealth}");
        }

        public void TakeLavaDamage()
        {
            TakeDamage();
        }

        public void AddSlipDamage(float v, string id)
        {
            if (v > 0f)
            {
                TakeDamage();
            }
        }

        private void ResetPowerupState()
        {
            attackBuffVersion++;
            defenseBuffVersion++;
            speedBuffVersion++;
            invisibleBuffVersion++;

            attackMultiplier = 1f;
            defenseMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            invisibleBuffActive = false;
            invincible = false;
            normalGrenadeCount = 3;
            movementSpeed = baseMovementSpeed;
            dashSpeed = baseDashSpeed;
            SetSpriteAlpha(1f);
        }

        private IEnumerator IncreaseAttackCounter(float time)
        {
            if (!float.IsFinite(time) || time <= 0f) time = 30f;

            var version = ++attackBuffVersion;
            attackMultiplier = BuffedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == attackBuffVersion)
            {
                attackMultiplier = 1f;
            }
        }

        private IEnumerator IncreaseDefenseCounter(float time)
        {
            if (!float.IsFinite(time) || time <= 0f) time = 30f;

            var version = ++defenseBuffVersion;
            defenseMultiplier = BuffedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == defenseBuffVersion)
            {
                defenseMultiplier = 1f;
            }
        }

        private IEnumerator SpeedUpCounter(float time)
        {
            if (!float.IsFinite(time) || time <= 0f) time = 30f;

            var version = ++speedBuffVersion;
            moveSpeedMultiplier = BuffedMultiplier;
            movementSpeed = baseMovementSpeed * moveSpeedMultiplier;
            dashSpeed = baseDashSpeed * moveSpeedMultiplier;
            yield return new WaitForSecondsRealtime(time);
            if (version == speedBuffVersion)
            {
                moveSpeedMultiplier = 1f;
                movementSpeed = baseMovementSpeed;
                dashSpeed = baseDashSpeed;
            }
        }

        private IEnumerator InvisibleCounter(float time)
        {
            if (!float.IsFinite(time) || time <= 0f) time = 30f;

            var version = ++invisibleBuffVersion;
            invisibleBuffActive = true;
            SetSpriteAlpha(InvisibleAlpha);
            yield return new WaitForSecondsRealtime(time);
            if (version == invisibleBuffVersion)
            {
                invisibleBuffActive = false;
                SetSpriteAlpha(1f);
            }
        }

        private void SetSpriteAlpha(float alpha)
        {
            if (spriteRendereres == null)
            {
                return;
            }

            foreach (var sr in spriteRendereres)
            {
                if (sr == null) continue;

                var color = sr.color;
                color.a = Mathf.Clamp01(alpha);
                sr.color = color;
            }
        }

        public Vector3 GetStandingBobOffset()
        {
            if (standBobAmplitude <= 0f || standBobFrequency <= 0f)
            {
                return Vector3.zero;
            }

            var now = Time.time;
            if (!float.IsFinite(now))
            {
                return Vector3.zero;
            }

            var phase = now * standBobFrequency;
            var bob = Mathf.Sin(phase) * standBobAmplitude;
            if (!float.IsFinite(standBobDirection.x) ||
                !float.IsFinite(standBobDirection.y) ||
                !float.IsFinite(standBobDirection.z))
            {
                return Vector3.zero;
            }

            var direction = standBobDirection.sqrMagnitude > 0.0001f ? standBobDirection.normalized : Vector3.up;
            return direction * bob;
        }

    }


}
