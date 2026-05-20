using UnityEngine;
using OpenGSCore;

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
        [SerializeField] private string ownerPlayerId = "";
        [SerializeField] private string weaponName = "GrenadeLauncher";
        private bool exploded;

        private void Start()
        {
            Destroy(this.gameObject, activeTime);
        }

        private float GetEffectiveDamage()
        {
            var player = GetComponentInParent<AbstractPlayer>();
            return player != null ? damage * player.AttackMultiplier() : damage;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (exploded)
            {
                return;
            }

            var tags = collision.gameObject.GetComponent<IMultipleTags>();
            if (tags == null || !tags.HasPlayerTag())
            {
                return;
            }

            var owner = GetComponentInParent<AbstractPlayer>();
            var resolvedOwnerId = !string.IsNullOrWhiteSpace(ownerPlayerId)
                ? ownerPlayerId
                : owner != null ? owner.UniqueID().ToString() : string.Empty;
            var resolvedTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            var effectiveDamage = GetEffectiveDamage();

            exploded = true;
            const float radius = 2.5f;
            const float minDamageMultiplier = 0.35f;
            const float baseDamage = 100f;
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            var processed = new System.Collections.Generic.HashSet<string>();
            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                var player = hit.GetComponentInParent<AbstractPlayer>();
                if (player == null)
                {
                    continue;
                }

                var playerId = player.UniqueID().ToString();
                if (!processed.Add(playerId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(resolvedOwnerId) && playerId == resolvedOwnerId)
                {
                    continue;
                }

                if (resolvedTeam != ETeam.NoTeam && player.Team() != ETeam.NoTeam && player.Team() == resolvedTeam)
                {
                    continue;
                }

                float distance = Vector2.Distance(transform.position, player.transform.position);
                float normalized = Mathf.Clamp01(distance / radius);
                float multiplier = Mathf.Lerp(1f, minDamageMultiplier, normalized);
                float finalDamage = Mathf.Max(1f, baseDamage * multiplier * (effectiveDamage / 100f));

                PlayerRegistry.Instance?.ApplyDamage(
                    player.UniqueID(),
                    player.transform.position - transform.position,
                    finalDamage,
                    eDamageType.Explosion,
                    resolvedOwnerId,
                    weaponName,
                    false
                );
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
