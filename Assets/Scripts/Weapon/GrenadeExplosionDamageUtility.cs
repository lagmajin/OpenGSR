using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// グレネード系の爆風ダメージを共通適用するヘルパー。
    /// マスターデータの具体型に依存せず、武器側から呼べるようにする。
    /// </summary>
    public static class GrenadeExplosionDamageUtility
    {
        public static void ApplyCircularDamage(
            Vector2 origin,
            Object _,
            string ownerPlayerId,
            string weaponName,
            ETeam team,
            float damageMultiplier = 1f)
        {
            if (PlayerRegistry.Instance == null)
            {
                return;
            }

            const float defaultBaseDamage = 100f;
            const float radius = 2.5f;
            const float minDamageMultiplier = 0.35f;

            var hits = Physics2D.OverlapCircleAll(origin, radius);
            var processed = new HashSet<string>();

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

                if (!string.IsNullOrWhiteSpace(ownerPlayerId) && playerId == ownerPlayerId)
                {
                    continue;
                }

                if (team != ETeam.NoTeam && player.Team() != ETeam.NoTeam && player.Team() == team)
                {
                    continue;
                }

                float distance = Vector2.Distance(origin, player.transform.position);
                float normalized = Mathf.Clamp01(distance / radius);
                float curveMultiplier = Mathf.Lerp(1f, minDamageMultiplier, normalized);
                float finalDamage = Mathf.Max(1f, defaultBaseDamage * curveMultiplier * damageMultiplier);

                PlayerRegistry.Instance.ApplyDamage(
                    player.UniqueID(),
                    player.transform.position - (Vector3)origin,
                    finalDamage,
                    eDamageType.Explosion,
                    ownerPlayerId,
                    weaponName,
                    false
                );
            }
        }
    }
}
