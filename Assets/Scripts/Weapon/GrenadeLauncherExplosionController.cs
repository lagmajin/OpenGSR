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
        [SerializeField] private GrenadeExplosionMasterData explosionMasterData;
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
            var baseDamage = explosionMasterData != null ? explosionMasterData.BaseDamage() : Mathf.Max(1f, damage);
            var multiplier = effectiveDamage / Mathf.Max(1f, baseDamage);

            exploded = true;
            GrenadeExplosionDamageUtility.ApplyCircularDamage(
                (Vector2)transform.position,
                explosionMasterData,
                resolvedOwnerId,
                weaponName,
                resolvedTeam,
                multiplier
            );

            Destroy(gameObject, 0.1f);
        }
    }
}
