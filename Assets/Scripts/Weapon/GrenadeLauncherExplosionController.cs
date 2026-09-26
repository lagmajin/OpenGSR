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

        private void OnValidate()
        {
            if (!float.IsFinite(damage)) damage = 0f;
            if (!float.IsFinite(activeTime)) activeTime = 2f;
            damage = Mathf.Max(0f, damage);
            activeTime = Mathf.Max(0.01f, activeTime);
        }

        private void Start()
        {
            Destroy(this.gameObject, activeTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (exploded || collision == null)
            {
                return;
            }

            var tags = collision.GetComponentInParent<IMultipleTags>();
            if (tags == null || !tags.HasPlayerTag())
            {
                return;
            }

            var owner = GetComponentInParent<AbstractPlayer>();
            var resolvedOwnerId = !string.IsNullOrWhiteSpace(ownerPlayerId)
                ? ownerPlayerId
                : owner != null ? owner.UniqueID().ToString() : string.Empty;
            var resolvedTeam = owner != null ? owner.Team() : ETeam.NoTeam;
            var attackMultiplier = owner != null ? owner.AttackMultiplier() : 1f;
            var effectiveDamage = damage * (float.IsFinite(attackMultiplier) ? Mathf.Max(0f, attackMultiplier) : 0f);
            if (!float.IsFinite(effectiveDamage) || effectiveDamage <= 0f)
            {
                return;
            }
            exploded = true;
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, resolvedOwnerId, weaponName, resolvedTeam, effectiveDamage / 100f);

            Destroy(gameObject, 0.1f);
        }
    }
}
