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

        private readonly ProjectileBallistics2D ballistics = new ProjectileBallistics2D();

        public void Init(Vector2 direction, float speed, float damage)
        {
            Init(direction, speed, damage, string.Empty, "Unknown", ETeam.NoTeam);
        }

        public void Init(Vector2 direction, float speed, float damage, string ownerPlayerId, string weaponName, ETeam team)
        {
            this.speed = speed;
            this.Damage = Mathf.RoundToInt(damage);
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            WeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName;
            Team = team;
            ballistics.Configure(direction, this.speed, enableGravity, gravityStrength, true, 0f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Start()
        {
            Destroy(this.gameObject, lifetime);
            body = gameObject.GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            var step = ballistics.Step(Time.deltaTime);
            gameObject.transform.position += (Vector3)step;
            if (enableGravity)
            {
                transform.rotation = ballistics.GetRotation();
            }
        }

        // ─── 衝突処理 ────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("StageObject") || collision.CompareTag("BurstArea"))
            {
                HitStageObject();
                return;
            }

            var targetPlayer = collision.GetComponentInParent<AbstractPlayer>();
            if (targetPlayer != null)
            {
                bool shouldDestroy = true;

                if (!string.IsNullOrWhiteSpace(OwnerPlayerId) && targetPlayer.UniqueID().ToString() == OwnerPlayerId)
                {
                    Destroy(gameObject);
                    return;
                }

                if (Team != ETeam.NoTeam && targetPlayer.Team() != ETeam.NoTeam && targetPlayer.Team() == Team)
                {
                    Destroy(gameObject);
                    return;
                }

                var registry = PlayerRegistry.Instance;
                if (registry != null)
                {
                    var source = (Vector2)(targetPlayer.transform.position - transform.position);
                    registry.ApplyDamage(
                        targetPlayer.UniqueID(),
                        source,
                        Damage,
                        eDamageType.Bullet,
                        OwnerPlayerId,
                        WeaponName,
                        false
                    );
                }

                if (shouldDestroy)
                {
                    Destroy(gameObject);
                }
            }
        }

        // ─── IBulletController の実装 ────────────────────────────────

        public void EnableGravity()
        {
            enableGravity = true;
            ballistics.SetGravityEnabled(true);
        }

        public void Speed(float f)
        {
            speed = f;
            ballistics.SetSpeed(speed);
        }

        // ─── プライベートユーティリティ ──────────────────────────────

        private void HitStageObject()
        {
            SoundManager.Instance.PlayOneShotSafe(hitSound, context: nameof(BulletController));
            Destroy(gameObject);
        }
    }
}
