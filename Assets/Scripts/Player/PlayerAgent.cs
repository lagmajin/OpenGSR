using System.Collections;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using OpenGSCore;
using UnityEngine.Audio;
using UnityEngine.Serialization;


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
        private bool invisibleBuffActive = false;

        public int NormalGrenadeCount => normalGrenadeCount;

        void Start()
        {
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
            ResetPowerupState();
            StartInvincibility(2.0f);

            AutoSetMediateObject();
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
            GetInput();
            CheckJumping();
            CheckFlip();
            CheckDashing();
            jetBooster?.SetBoostHeld(Input.GetMouseButton(1));

            if (!isDashing && isGrounded) // 地上限定
            {
                if (Input.GetKeyDown(KeyCode.D))
                {
                    if (Time.time - lastRightTapTime < doubleTapTime)
                        StartDash(Vector2.right);
                    lastRightTapTime = Time.time;
                }
                if (Input.GetKeyDown(KeyCode.A))
                {
                    if (Time.time - lastLeftTapTime < doubleTapTime)
                        StartDash(Vector2.left);
                    lastLeftTapTime = Time.time;
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Sit();
            }

            if(Input.GetKeyUp(KeyCode.S))
            {
                StandUp();
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
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

            if (Input.GetMouseButtonDown(0))
            {
                currentGun.StartFire();
                // 特殊武器の弾数減衰を通知
                weaponSlots.OnFireSpecialWeapon();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                currentGun.StopFire();
            }
        }

        public void FixedUpdate()
        {
            CheckGround();
            coyoteTimeLeft = Mathf.Max(0f, coyoteTimeLeft - Time.fixedDeltaTime);
            jumpBufferLeft = Mathf.Max(0f, jumpBufferLeft - Time.fixedDeltaTime);
            ApplyDashing();
            ApplyMovement();
            ResolvePenetration();
            SnapToGround();
            CheckGround();
            jetBooster?.RecoverFuel(Time.fixedDeltaTime);
        }
        void StartDash(Vector2 direction)
        {
            dashDir = (dashAngle.normalized.x * direction.x) * Vector2.right + dashAngle.normalized.y * Vector2.up;
            isDashing = true;
            dashTimer = dashDuration;
        }
        void GetInput()
        {
            if (Input.GetKey(KeyCode.LeftShift))
                extraSpeed = 2f;
            else
                extraSpeed = 1;

            horizontalSpeed = Input.GetAxis("Horizontal") * extraSpeed;
        }

        public void Sit()
        {
            animator.SetBool("IsSit", true);
            if (standingCollider != null) standingCollider.enabled = false;
            if (sittingCollider != null) sittingCollider.enabled = true;
            
            headController?.Sit();

            primaryGunController?.Sit();

            armController?.Sit();
        }

        private void StandUp()
        {
            animator.SetBool("IsSit", false);
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
        private void OnValidate()
        {
            RecalculateJumpVelocity();
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
            if (Input.GetKeyDown(KeyCode.W))
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
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
            if (Input.GetKeyDown(KeyCode.F) && !isDashing)
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
                    dashingSpeed = Mathf.Lerp(dashingSpeed, 2 * Mathf.Sign(transform.localScale.x), 10 * Time.deltaTime);
                else
                {
                    isDashing = false;
                    dashingSpeed = 0;
                }
            }
            else
            {
                dashingSpeed = Mathf.Lerp(dashingSpeed, 0, 10 * Time.deltaTime);
            }
        }
        void ApplyMovement()
        {
            float dt = Time.fixedDeltaTime;

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
        private void Die(EDeadReason reason=EDeadReason.Unknown)
        {
            if(playerMasterData)
            {
                var sound=playerMasterData.damageVoices[0];

                audioSource?.PlayOneShot(sound);

            }

            DropWeapon();

            // ネットワーク死亡通知を送信
            SendDeathNotificationToServer(reason);

            this.battleSceneMediateObject.mainscript.OnMyPlayerDead();


            Destroy(this.gameObject);
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

        public void TakeDamage()
        {
            Debug.Log("TakeDamage Func");

            if (invincible || IsIncreaseDefenseNow())
            {
                Debug.Log("TakeDamage ignored by invincibility or defense buff");
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
            StartCoroutine(SpeedUpCounter(sec));
        }

        public void IncreaseAttack(float sec = 30.0f)
        {
            StartCoroutine(IncreaseAttackCounter(sec));
        }

        public void IncreaseDefense(float sec = 30.0f)
        {
            StartCoroutine(IncreaseDefenseCounter(sec));
        }

        public void Invisible(float sec = 30.0f)
        {
            StartCoroutine(InvisibleCounter(sec));
        }

        public void RefillGrenade()
        {
            normalGrenadeCount = 3;
            Debug.Log($"[PlayerAgent] Normal grenade refilled: {NormalGrenadeCount}");
        }

        public void Berserk()
        {
            IncreaseAttack(10.0f);
            SpeedUp(10.0f);
            Debug.Log("[PlayerAgent] Berserk activated");
        }

        public void AddDamage(Vector2 source, float damage, eDamageType type)
        {
            if (damage <= 0f)
            {
                return;
            }

            if (invincible || IsIncreaseDefenseNow())
            {
                Debug.Log("[PlayerAgent] Damage ignored by invincibility or defense buff.");
                return;
            }

            TakeDamage();
        }

        public void AddDamageAndForce(float damage, Vector3 vec, float force = 1.0f)
        {
            if (damage <= 0f)
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

            TakeDamage();
        }

        public void AddDamageAndForce2(float damage, Vector2 point)
        {
            AddDamage(point, damage, eDamageType.None);
        }

        public void Heal(float heal = 0)
        {
            Debug.Log($"[PlayerAgent] Heal requested: {heal}");
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
            if (time <= 0f) time = 30f;

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
            if (time <= 0f) time = 30f;

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
            if (time <= 0f) time = 30f;

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
            if (time <= 0f) time = 30f;

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

    }


}
