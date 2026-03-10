using System.Collections;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using OpenGSCore;
using UnityEngine.Audio;


namespace OpenGS
{
    [DisallowMultipleComponent]
    public class PlayerAgent : AbstractPlayerAgent,IDamagableObject
    {
        [SerializeField] private BoxCollider2D standingCollider;
        [SerializeField] private BoxCollider2D sitingCollider;

        [SerializeField]private GameObject head;
        [SerializeField]private HeadController headController;
        [SerializeField] private GameObject weaponArm;
        [SerializeField] private EPlayerCharacter character;
        [SerializeField] private AbstractGunController primaryGunController;
        [SerializeField]private AbstractGunController secondaryGunController;
        [SerializeField]private WeaponArmController armController;

        public float movementSpeed;
        public float jumpHeight;
        public Transform groundCheckRayPoint;

        public LayerMask groundLayer;

        public float horizontalSpeed, verticalSpeed;
        public bool isGrounded, isWallAhead;
        Vector2 movement;
        float extraSpeed;
        Vector2 rightScale, leftScale;
        float dashingSpeed = 0;
        float dashTimer = 0.15f;
        bool isDashing;
        float fallMultiplier = 0.5f;



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

        [SerializeField] float jumpDuration = 0.3f; // 上昇にかける時間
        [SerializeField] float jumpPeakSpeed = 6f;
        bool isJumping = false;
        float jumpTimer = 0f;

        void Start()
        {

            rigidbody2D = GetComponent<Rigidbody2D>();

            PhysicsMaterial2D material = new PhysicsMaterial2D();
            material.bounciness = 0; // 反発なし
            material.friction = 0.4f; // 適度な摩擦

            rigidbody2D.sharedMaterial = material;



            Debug.Log("Init scale: " + transform.localScale);
            rightScale = transform.localScale;
            leftScale = transform.localScale;
            leftScale.x *= -1;


            StartInvincibility(2.0f);

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
                if(weaponArm!=null) list.Add (armRenderer);
            }

            spriteRendereres = list.ToArray();

            AutoSetMediateObject();
        }

        protected void AutoSetMediateObject()
        {
            battleSceneMediateObject = FindFirstObjectByType<BattleSceneMediateObject>();
        }
        void SetMatchManager(MatchManager matchManager)
        {

        }

        void StartInvincibility(float duration)
        {
            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                invincible = true;

                // 全てのSpriteRendererに対して点滅開始
                foreach (var sr in spriteRendereres)
                {
                    sr.DOFade(0f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetId("invincible");
                }
            });

            seq.AppendInterval(duration);

            seq.AppendCallback(() =>
            {
                invincible = false;

                // DOTweenのIDを使って全部止める
                DOTween.Kill("invincible");

                foreach (var sr in spriteRendereres)
                {
                    sr.DOFade(1f, 0f); // 完全表示に戻す
                }
            });
        }
        // Start is called before the first frame update

        // Update is called once per frame

        void Update()
        {
            CheckGround();
            CheckJumping();

            GetInput();
            CheckFlip();
            CheckHorizontalCollision();
            CheckWallClimb();
            CheckDashing();
            ApplyDashing();

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
                transform.Translate(dashDir * dashSpeed * Time.deltaTime);
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                    isDashing = false;
            }

            if (verticalSpeed < -12f) verticalSpeed = -12f;
            if (verticalSpeed > 10f) verticalSpeed = 10f;

            // 射撃入力の処理
            HandleFireInput();
        }

        private void HandleFireInput()
        {
            if (weaponSlots == null) return;
            
            var currentGun = weaponSlots.GetCurrentGun();
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
            ApplyMovement();
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
            
            headController?.Sit();

            primaryGunController?.Sit();

            armController?.Sit();
        }

        private void StandUp()
        {
            animator.SetBool("IsSit", false);

            headController?.StandUp();
            primaryGunController?.StandUp();
            armController?.StandUp();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheckRayPoint.position, new Vector3(0.3f, 0.03f, 0f));
        }
        void CheckGround()
        {
            float maxDistance = 0.3f;
            Vector2 size = new Vector2(0.4f, 0.04f); // 幅を広げて小さな隙間を跨ぐ
            Vector2[] offsets = {
            new Vector2(-0.15f, 0f), // 左
            Vector2.zero,            // 中央
            new Vector2(0.15f, 0f)   // 右
            };

            bool wasGrounded = isGrounded;
            isGrounded = false;
            RaycastHit2D closestHit = default;
            float closestDist = float.MaxValue;

            foreach (var offset in offsets)
            {
                Vector2 origin = (Vector2)groundCheckRayPoint.position + offset;
                RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, maxDistance, groundLayer);
                Debug.DrawRay(origin, Vector2.down * (hit.collider ? hit.distance : maxDistance), hit.collider ? Color.green : Color.red, 0.1f);

                if (hit.collider && hit.distance < closestDist)
                {
                    closestHit = hit;
                    closestDist = hit.distance;
                }
            }

            if (closestHit.collider)
            {
                currentGroundNormal = closestHit.normal; // 法線を保存
                float slopeAngle = Vector2.Angle(closestHit.normal, Vector2.up);

                if (slopeAngle < 85f)  // 坂道の角度がゆるい場合は接地
                {
                    isGrounded = true;
                    headController?.OnGround();

                    // めり込み・浮き補正（Snapping）
                    float desiredDistance = 0.05f; // 床から理想的な距離
                    float snapTolerance = 0.2f;    // この距離までなら吸着する（ジャンプ中は吸着しないように注意）
                    float distanceDiff = desiredDistance - closestDist; // 正: めり込み, 負: 浮き
                    
                    // ジャンプ中(上向きの速度がある場合)は下に吸着させない
                    bool isJumpingUp = verticalSpeed > 0.1f;

                    if (distanceDiff > 0.001f || (distanceDiff < -0.001f && closestDist < snapTolerance && !isJumpingUp))
                    {
                        // 浮いている、またはめり込んでいる場合は垂直方向に補正して斜面に沿わせる
                        transform.Translate(Vector2.up * distanceDiff);
                    }

                    // 坂道で着地した際に必要な処理
                    if (!wasGrounded)
                    {
                        jetBooster?.OnLanding();
                        animator?.SetBool("IsJump", false);
                    }

                    verticalSpeed = 0f;
                    fallMultiplier = 1f;
                }
            }
            else
            {
                currentGroundNormal = Vector2.up; // 空中ではデフォルトの法線
            }

            // 地面に接していない場合は、落下速度を設定
            if (!isGrounded)
            {
                float targetFallSpeed = -10f * fallMultiplier;
                verticalSpeed = Mathf.MoveTowards(verticalSpeed, targetFallSpeed, Time.deltaTime * 30f);
            }
        }
        /*
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                animator.SetBool("IsJump", false);
            }
        }

        */
        void CheckJumping()
        {
            if ((isGrounded || fallMultiplier < 1) && Input.GetKeyDown(KeyCode.W))
            {
                isGrounded = false;
                isJumping = true;
                jumpTimer = 0f;

                headController.Jump();
                animator.SetBool("IsJump", true); 
                

            }

            if (isJumping)
            {
                jumpTimer += Time.deltaTime;
                float t = jumpTimer / jumpDuration;
                verticalSpeed = Mathf.Lerp(jumpPeakSpeed, 0f, t); // 上昇がだんだん遅くなる

                if (t >= 1f)
                {
                    isJumping = false;
                }
            }
        }

        void CheckHorizontalCollision()
        {
            // 横方向の壁チェックを有効化してめり込みを防止
            float checkDistance = 0.25f;
            Vector2 direction = Vector2.right * Mathf.Sign(transform.localScale.x);
            Vector2 origin = (Vector2)transform.position;
            Vector2 size = new Vector2(0.2f, 0.5f);
            
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, checkDistance, groundLayer);
            
            if (hit.collider)
            {
                isWallAhead = true;
                // 壁にめり込んでいる場合は押し戻す
                float penetration = checkDistance - hit.distance;
                if (penetration > 0.01f)
                {
                    transform.Translate(-direction * penetration);
                }
            }
            else
            {
                isWallAhead = false;
            }
        }

        void CheckWallClimb()
        {
            /*
            RaycastHit2D _hit = Physics2D.Raycast(horizontalCheckRayPoint.position, Vector2.right * Mathf.Sign(transform.localScale.x), 0.5f, groundLayer);
            if (!isGrounded && verticalSpeed > 0 && _hit.collider && fallMultiplier >= 1)
            {
                verticalSpeed = 0;
                fallMultiplier = 0.01f;
            }
            else if (!_hit.collider)
            {
                fallMultiplier = 1;
            }

            */
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
            if (isWallAhead)
                horizontalSpeed = 0;

            float totalHorizontalSpeed = horizontalSpeed + dashingSpeed;
            Vector2 moveDir = Vector2.right;

            if (isGrounded)
            {
                // 地面の法線から、斜面に沿った方向ベクトルを計算
                // 例: 法線が(0, 1)なら、方向は(1, 0) (右)
                // 法線が左上がり(-0.7, 0.7)なら、方向は(0.7, 0.7) (斜め右上)
                moveDir = new Vector2(currentGroundNormal.y, -currentGroundNormal.x).normalized;
            }

            // 斜面に沿った移動ベクトル + 垂直方向の速度ベクトル
            Vector2 finalMovement = (moveDir * totalHorizontalSpeed + Vector2.up * verticalSpeed) * movementSpeed * Time.deltaTime;
            
            // 過度な移動を制限（めり込み防止）
            finalMovement.x = Mathf.Clamp(finalMovement.x, -0.5f, 0.5f);
            finalMovement.y = Mathf.Clamp(finalMovement.y, -0.5f, 0.5f);
            
            transform.Translate(finalMovement);
        }

        private void OnDrawGizmos()
        {

                if (groundCheckRayPoint == null) return;

                Gizmos.color = Color.green;
                Vector2 size = new Vector2(0.3f, 0.02f);
                float maxDistance = 0.25f;
                Vector3 origin = groundCheckRayPoint.position;
                Vector3 center = origin + Vector3.down * maxDistance * 0.5f;
                Gizmos.DrawWireCube(center, new Vector3(size.x, maxDistance, 0f));


        }

        private void OnSpawn()
        {

        }

        private void DropWeapon()
        {

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
                    string playerId = UniqueID().ToString();
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

            

            Die();
        }

        /*
        [SerializeField] private GameObject myObject;
        [SerializeField] private JetBooster booster;

        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float fallSpeed = 5f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);
        // Use this for initialization
        private bool isGrounded;
        private float groundedTime;
        //private JetBooster booster;
        void Start()
        {
            booster = GetComponent<JetBooster>();
        }
        void CorrectGroundPenetration()
        {
            if (!IsGrounded()) return;

            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance + 0.2f, groundLayer);

            if (hit.collider != null)
            {
                float penetration = groundCheckDistance - hit.distance;
                if (penetration > 0f)
                {
                    transform.Translate(Vector2.up * penetration);
                }
            }
        }
        // Update is called once per frame
        void Update()
        {
            isGrounded = IsGrounded();

            // Booster に渡す
            booster.SetGrounded(isGrounded);
            HandleBooster();
            HandleInput();
            HandleGravity();
            CorrectGroundPenetration();
        }

        private void FixedUpdate()
        {

        }
        bool IsGrounded()
        {
            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            float radius = 0.2f; // 衝突判定に使う半径

            RaycastHit2D hit = Physics2D.CircleCast(origin, radius, Vector2.down, groundCheckDistance, groundLayer);

//#if UNITY_EDITOR
            Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit.collider ? Color.green : Color.red);
//#endif

            return hit.collider != null;
        }
        void HandleGravity()
        {
            RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + groundCheckOffset, Vector2.down, groundCheckDistance + 0.2f, groundLayer);
            if (hit.collider != null)
            {
                float penetration = (groundCheckDistance - hit.distance);
                if (penetration > 0)
                {
                    // めり込んでたら少し上に戻す
                    transform.Translate(Vector2.up * penetration);
                }
            }
            else
            {
                transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
            }
        }
        void HandleInput()
        {
            float horizontalInput = Input.GetAxis("Horizontal");

            Vector2 origin = (Vector2)transform.position + groundCheckOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance + 0.2f, groundLayer);

            if (hit.collider != null)
            {
                Vector2 groundNormal = hit.normal;
                Vector2 moveDir = new Vector2(groundNormal.y, -groundNormal.x);
                moveDir *= horizontalInput;

                transform.Translate(moveDir * Time.deltaTime * moveSpeed);
            }
            else
            {
                transform.Translate(Vector2.right * horizontalInput * Time.deltaTime * moveSpeed);
            }
        }

        void HandleBooster()
        {
            if (booster != null)
            {
                bool isBoosterActive = Input.GetMouseButton(1); // 右クリック
                booster.Activate(isBoosterActive);
            }
        }

        void Die()
        {

        }

        */
    }


}