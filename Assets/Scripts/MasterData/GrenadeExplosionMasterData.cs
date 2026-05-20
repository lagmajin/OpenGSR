using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 爆風ダメージの共通設定。
    /// グレネードランチャー、クラスター、その他の爆発系で再利用する。
    /// </summary>
    [CreateAssetMenu(menuName = "MasterData/Weapon/GrenadeExplosionMasterData")]
    public class GrenadeExplosionMasterData : ScriptableObject
    {
        [Header("Damage")]
        [SerializeField] private float baseDamage = 100f;
        [SerializeField] private float radius = 2.5f;
        [SerializeField, Range(0f, 1f)] private float minDamageMultiplier = 0.35f;

        [Header("Falloff")]
        [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f);

        private static GrenadeExplosionMasterData instance;

        public static GrenadeExplosionMasterData Instance()
        {
            if (instance == null)
            {
                instance = Resources.Load<GrenadeExplosionMasterData>("MasterData/Grenade/GrenadeExplosionMasterData");
            }

            return instance;
        }

        public float BaseDamage() => baseDamage;
        public float Radius() => Mathf.Max(0.01f, radius);
        public float MinDamageMultiplier() => Mathf.Clamp01(minDamageMultiplier);
        public AnimationCurve DamageFalloff() => damageFalloff;
    }

    /// <summary>
    /// 爆風ダメージを PlayerRegistry 経由で適用する共通ヘルパー。
    /// </summary>
    public static class GrenadeExplosionDamageUtility
    {
        public static void ApplyCircularDamage(
            Vector2 origin,
            GrenadeExplosionMasterData masterData,
            string ownerPlayerId,
            string weaponName,
            ETeam team,
            float damageMultiplier = 1f)
        {
            if (masterData == null)
            {
                masterData = GrenadeExplosionMasterData.Instance();
            }
            if (masterData == null || PlayerRegistry.Instance == null)
            {
                return;
            }

            float baseDamage = Mathf.Max(0f, masterData.BaseDamage() * damageMultiplier);
            float radius = masterData.Radius();
            float minMultiplier = masterData.MinDamageMultiplier();
            var falloff = masterData.DamageFalloff();

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
                float curveMultiplier = falloff != null ? Mathf.Clamp01(falloff.Evaluate(normalized)) : Mathf.Lerp(1f, minMultiplier, normalized);
                curveMultiplier = Mathf.Max(minMultiplier, curveMultiplier);

                float finalDamage = Mathf.Max(1f, baseDamage * curveMultiplier);

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
