using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// グレネードランチャーの爆発エフェクト・ダメージ判定コントローラー
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class GrenadeLauncherExplosion : MonoBehaviour, IGrenadeLauncherExplosion
    {
        [SerializeField] public float damage = 100.0f;
        [SerializeField] public float activeTime = 2.0f;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Start()
        {
            Destroy(this.gameObject, activeTime);
        }

        private float GetEffectiveDamage()
        {
            var player = GetComponentInParent<AbstractPlayer>();
            return player != null ? damage * player.AttackMultiplier() : damage;
        }

        // ─── 衝突処理 ────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var tags = collision.gameObject.GetComponent<IMultipleTags>();
            if (tags == null) return;

            if (tags.HasPlayerTag())
            {
                var damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.AddDamage(transform.position, GetEffectiveDamage(), eDamageType.Explosion);
                }
            }
        }
    }
}
