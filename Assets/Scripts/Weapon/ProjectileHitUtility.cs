using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Projectile collision helper for the common hit rules used by bullets and grenades.
    /// Keeps team/self filtering and stage/player detection in one place.
    /// </summary>
    public static class ProjectileHitUtility
    {
        public static bool IsStageHit(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.CompareTag("StageObject") || target.CompareTag("BurstArea"))
            {
                return true;
            }

            if (target.TryGetComponent<IMultipleTags>(out var tags))
            {
                return tags.HasStageObjectTag() || tags.HasBurstAreaTag();
            }

            return target.layer == LayerMask.NameToLayer("Platforms");
        }

        public static bool TryGetTargetPlayer(Collider2D collision, out AbstractPlayer player)
        {
            player = collision != null ? collision.GetComponentInParent<AbstractPlayer>() : null;
            return player != null;
        }

        public static bool ShouldIgnorePlayerHit(AbstractPlayer target, string ownerPlayerId, ETeam team)
        {
            if (target == null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(ownerPlayerId) && target.UniqueID().ToString() == ownerPlayerId)
            {
                return true;
            }

            if (team != ETeam.NoTeam && target.Team() != ETeam.NoTeam && target.Team() == team)
            {
                return true;
            }

            return false;
        }

        public static bool ApplyPlayerDamage(
            AbstractPlayer target,
            Vector2 impactOrigin,
            float damage,
            eDamageType damageType,
            string ownerPlayerId,
            string weaponName,
            ETeam team,
            bool knockback = false)
        {
            if (ShouldIgnorePlayerHit(target, ownerPlayerId, team))
            {
                return false;
            }

            var registry = PlayerRegistry.Instance;
            if (registry == null)
            {
                return false;
            }

            var source = (Vector2)(target.transform.position - (Vector3)impactOrigin);
            registry.ApplyDamage(
                target.UniqueID(),
                source,
                damage,
                damageType,
                ownerPlayerId,
                weaponName,
                false);
            return true;
        }
    }
}
