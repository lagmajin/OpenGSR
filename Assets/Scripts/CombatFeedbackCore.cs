using System;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ダメージ表示とキルログ向けの純データ。
    /// UI やシーンへは接続せず、あとから表示層で使える形に整える。
    /// </summary>
    public struct DamageFeedbackInfo
    {
        public string TargetId;
        public string AttackerId;
        public int PreviousHp;
        public int CurrentHp;
        public int MaxHp;
        public int Damage;
        public bool IsCritical;

        public bool IsHealing => Damage < 0;
        public bool ShouldShow => Damage > 0;
        public float DamageRatio => MaxHp <= 0 ? 0f : Mathf.Clamp01(Damage / (float)MaxHp);
    }

    public static class DamageFeedbackCalculator
    {
        public static DamageFeedbackInfo FromHealthSnapshot(
            string targetId,
            string attackerId,
            float previousHp,
            float currentHp,
            float maxHp,
            bool isCritical = false)
        {
            int prev = Mathf.Max(0, Mathf.RoundToInt(previousHp));
            int current = Mathf.Max(0, Mathf.RoundToInt(currentHp));
            int max = Mathf.Max(0, Mathf.RoundToInt(maxHp));

            return new DamageFeedbackInfo
            {
                TargetId = targetId ?? string.Empty,
                AttackerId = attackerId ?? string.Empty,
                PreviousHp = prev,
                CurrentHp = current,
                MaxHp = max,
                Damage = ResolveDamageAmount(prev, current),
                IsCritical = isCritical
            };
        }

        public static int ResolveDamageAmount(int previousHp, int currentHp)
        {
            return Mathf.Max(0, previousHp - currentHp);
        }

        public static int ResolveDamageAmount(float previousHp, float currentHp)
        {
            return ResolveDamageAmount(Mathf.RoundToInt(previousHp), Mathf.RoundToInt(currentHp));
        }

        public static int ResolveHealAmount(int previousHp, int currentHp)
        {
            return Mathf.Max(0, currentHp - previousHp);
        }

        public static int ResolveHealAmount(float previousHp, float currentHp)
        {
            return ResolveHealAmount(Mathf.RoundToInt(previousHp), Mathf.RoundToInt(currentHp));
        }
    }

    /// <summary>
    /// キルログ向けの純データ。
    /// 表示層ではこのデータから色やテキストを組み立てる。
    /// </summary>
    public struct KillLogEntryData
    {
        public string KillerName;
        public string VictimName;
        public string WeaponName;
        public bool IsKillerMe;
        public bool IsVictimMe;
        public bool IsHeadshot;

        public bool IsSuicide => string.Equals(KillerName, VictimName, StringComparison.OrdinalIgnoreCase);
        public bool HasWeapon => !string.IsNullOrWhiteSpace(WeaponName);
    }

    public static class KillLogFormatter
    {
        public static KillLogEntryData Create(
            string killerName,
            string victimName,
            string weaponName = null,
            bool isKillerMe = false,
            bool isVictimMe = false,
            bool isHeadshot = false)
        {
            return new KillLogEntryData
            {
                KillerName = string.IsNullOrWhiteSpace(killerName) ? "Unknown" : killerName,
                VictimName = string.IsNullOrWhiteSpace(victimName) ? "Unknown" : victimName,
                WeaponName = string.IsNullOrWhiteSpace(weaponName) ? string.Empty : weaponName,
                IsKillerMe = isKillerMe,
                IsVictimMe = isVictimMe,
                IsHeadshot = isHeadshot
            };
        }

        public static string FormatSummary(KillLogEntryData entry)
        {
            string weapon = entry.HasWeapon ? $" [{entry.WeaponName}]" : string.Empty;
            string headshot = entry.IsHeadshot ? " HS" : string.Empty;
            return $"{entry.KillerName} -> {entry.VictimName}{weapon}{headshot}";
        }

        public static Color ResolveKillerColor(KillLogEntryData entry)
        {
            if (entry.IsSuicide) return new Color(1f, 0.6f, 0.2f);
            if (entry.IsKillerMe) return Color.cyan;
            if (entry.IsVictimMe) return Color.white;
            return Color.white;
        }

        public static Color ResolveVictimColor(KillLogEntryData entry)
        {
            if (entry.IsSuicide) return new Color(1f, 0.6f, 0.2f);
            if (entry.IsVictimMe) return Color.red;
            return Color.gray;
        }
    }
}
