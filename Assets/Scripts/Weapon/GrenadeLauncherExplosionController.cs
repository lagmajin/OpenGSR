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
            var effectiveDamage = owner != null ? damage * owner.AttackMultiplier() : damage;
            exploded = true;
            GrenadeExplosionDamageUtility.ApplyCircularDamage((Vector2)transform.position, resolvedOwnerId, weaponName, resolvedTeam, effectiveDamage / 100f);

            Destroy(gameObject, 0.1f);
        }
    }
}
