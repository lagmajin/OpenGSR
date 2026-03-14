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
        [SerializeField] public AudioClip hitSound;
        [SerializeField] public Rigidbody2D body;

        public ETeam Team   { get; set; } = ETeam.NoTeam;
        public int   Damage { get; set; } = 50;

        public void Init(Vector2 direction, float speed, float damage)
        {
            this.speed = speed;
            this.Damage = Mathf.RoundToInt(damage);
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
            Vector3 velocity = gameObject.transform.rotation * new Vector3(speed, 0, 0);
            gameObject.transform.position += velocity * Time.deltaTime;
        }

        // ─── 衝突処理 ────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("StageObject") || collision.CompareTag("BurstArea"))
            {
                HitStageObject();
                return;
            }

            var tags = collision.GetComponent<IMultipleTags>();
            if (tags != null && tags.HasPlayerTag())
            {
                // TODO: プレイヤーへのダメージ処理
            }
        }

        // ─── IBulletController の実装 ────────────────────────────────

        public void EnableGravity()
        {
            enableGravity = true;
        }

        public void Speed(float f)
        {
            speed = f;
        }

        // ─── プライベートユーティリティ ──────────────────────────────

        private void HitStageObject()
        {
            PlaySound.PlaySE(hitSound);
            Destroy(gameObject);
        }
    }
}
