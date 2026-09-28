using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    /// <summary>
    /// フィールドアイテム: マップ上の特定の地点に直接出現し、プレイヤーが触れることで取得・使用できるアイテム。
    /// 効果持続時間は原則として 30秒。
    /// </summary>
    public enum EFieldItemType
    {
        GranadeLauncher,    // 特殊武器: グレネードランチャー
        FlameThrower,       // 特殊武器: 火炎放射器
        PowerUpItem,        // 攻撃力2倍 (30秒)
        DefenceUpItem,      // 防御力2倍 (30秒)
        SpeedUpItem,        // 移動速度2倍 (30秒)
        StealthItem,        // キャラ半透明化 (30秒)
        GrenadePack,        // ノーマルグレネード満タン補充
        HealItem,           // HP回復
        WeaponItem          // ドロップされた通常武器。効果ではなく装備そのもの
    }

    /// <summary>
    /// アイテム効果の既定持続時間（秒）。
    /// Timed effects used to hard code 30 seconds on the client only, so the
    /// server had no way to shorten or extend one. Keeping the number in the
    /// shared package lets both sides apply the same value.
    /// </summary>
    public static class FieldItemDefaults
    {
        public const float DurationSeconds = 30.0f;
    }
}