using UnityEngine;
using OpenGSCore;
using System;

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

            // CompareTag throws when a project does not define the requested tag.
            // Use the tag value comparison so optional tags do not spam the console.
            if (target.tag == "StageObject" || target.tag == "BurstArea")
            {
                return true;
            }

            var tags = target.GetComponentInParent<IMultipleTags>();
            if (tags != null)
            {
                return tags.HasStageObjectTag() || tags.HasBurstAreaTag();
            }

            var root = target.transform.root;
            return target.layer == LayerMask.NameToLayer("Platforms")
                || (root != null && root.gameObject.layer == LayerMask.NameToLayer("Platforms"));
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

            if (!string.IsNullOrWhiteSpace(ownerPlayerId) &&
                string.Equals(target.UniqueID().ToString(), ownerPlayerId, StringComparison.OrdinalIgnoreCase))
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

            if (!IsFinite(impactOrigin) || !float.IsFinite(damage) || damage <= 0f)
            {
                return false;
            }

            var registry = PlayerRegistry.Instance;
            if (registry == null)
            {
                return false;
            }

            var source = (Vector2)(target.transform.position - (Vector3)impactOrigin);
            return registry.ApplyDamage(
                target.UniqueID(),
                source,
                damage,
                damageType,
                ownerPlayerId,
                weaponName,
                false,
                knockback);
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }
    }
}
