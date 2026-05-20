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

}
