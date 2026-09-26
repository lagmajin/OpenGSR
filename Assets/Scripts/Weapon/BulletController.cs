using UnityEngine;
using OpenGSCore;

#pragma warning disable 0414

namespace OpenGS
{
    /// <summary>
    /// 通常弾のコントローラー。
    /// 生成後、一定時間経過または障害物への衝突で破壊される。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class BulletController : MonoBehaviour, IBulletController
    {
        [SerializeField] private float lifetime = 3.0f;
        [SerializeField] public float speed = 10.0f;
        [SerializeField] public bool enableGravity = false;
        [SerializeField] private float gravityStrength = 18.0f;
        [SerializeField] public AudioClip hitSound;
        [SerializeField] public Rigidbody2D body;

        public ETeam Team   { get; set; } = ETeam.NoTeam;
        public int   Damage { get; set; } = 50;
        public string OwnerPlayerId { get; private set; } = string.Empty;
        public string WeaponName { get; private set; } = "Unknown";
        public eDamageType DamageType { get; private set; } = eDamageType.Bullet;

        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            lifetime = float.IsFinite(lifetime) ? Mathf.Max(0.01f, lifetime) : 3f;
            speed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 10f;
            gravityStrength = float.IsFinite(gravityStrength) ? Mathf.Max(0f, gravityStrength) : 18f;
        }

        private void OnValidate()
        {
            if (!float.IsFinite(lifetime)) lifetime = 3f;
            if (!float.IsFinite(speed)) speed = 10f;
            if (!float.IsFinite(gravityStrength)) gravityStrength = 18f;
            lifetime = Mathf.Max(0.01f, lifetime);
            speed = Mathf.Max(0f, speed);
            gravityStrength = Mathf.Max(0f, gravityStrength);
        }

        public void Init(Vector2 direction, float speed, float damage)
        {
            Init(direction, speed, damage, string.Empty, "Unknown", ETeam.NoTeam);
        }

        public void Init(Vector2 direction, float speed, float damage, string ownerPlayerId, string weaponName, ETeam team)
        {
            Init(direction, speed, damage, ownerPlayerId, weaponName, team, eDamageType.Bullet);
        }

        public void Init(Vector2 direction, float speed, float damage, string ownerPlayerId, string weaponName, ETeam team, eDamageType damageType)
        {
            this.speed = float.IsFinite(speed) ? Mathf.Max(0f, speed) : 0f;
            this.Damage = float.IsFinite(damage) ? Mathf.Max(0, Mathf.RoundToInt(damage)) : 0;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            WeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName;
            Team = team;
            DamageType = damageType;
            ApplyDamageVisual();
            ballistics.Configure(direction, this.speed, enableGravity, gravityStrength, true, 0f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Start()
        {
            Destroy(this.gameObject, lifetime);
            body = gameObject.GetComponent<Rigidbody2D>();
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            ApplyDamageVisual();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f || !float.IsFinite(transform.position.x) ||
                !float.IsFinite(transform.position.y) || !float.IsFinite(transform.position.z))
            {
                Destroy(gameObject);
                return;
            }

            // Bound one simulation step so a frame hitch does not tunnel the projectile.
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            var step = ballistics.Step(deltaTime);
            gameObject.transform.position += (Vector3)step;
            if (enableGravity)
            {
                transform.rotation = ballistics.GetRotation();
            }
        }

        // ─── 衝突処理 ────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            if (ProjectileHitUtility.IsStageHit(collision.gameObject))
            {
                HitStageObject();
                return;
            }

            if (ProjectileHitUtility.TryGetTargetPlayer(collision, out var targetPlayer))
            {
                if (ProjectileHitUtility.ApplyPlayerDamage(
                        targetPlayer,
                        transform.position,
                        Damage,
                        DamageType,
                        OwnerPlayerId,
                        WeaponName,
                        Team,
                        true))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Destroy(gameObject);
        }

        // ─── IBulletController の実装 ────────────────────────────────

        public void EnableGravity()
        {
            enableGravity = true;
            ballistics.SetGravityEnabled(true);
        }

        public void Speed(float f)
        {
            speed = float.IsFinite(f) ? Mathf.Max(0f, f) : 0f;
            ballistics.SetSpeed(speed);
        }

        // ─── プライベートユーティリティ ──────────────────────────────

        private void HitStageObject()
        {
            SoundManager.Instance?.PlayOneShotSafe(hitSound, context: nameof(BulletController));
            Destroy(gameObject);
        }

        private void ApplyDamageVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            string resourcePath = DamageType switch
            {
                eDamageType.Fire => "Sprites/Weapon/Projectile/FireBullet",
                eDamageType.Poison => "Sprites/Weapon/Projectile/PoisonBullet",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(resourcePath))
            {
                var variant = Resources.Load<Sprite>(resourcePath);
                if (variant != null)
                {
                    spriteRenderer.sprite = variant;
                }
            }
        }
    }
}
