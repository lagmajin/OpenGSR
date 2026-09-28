#nullable enable
using System;
using System.Numerics;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーにダメージが与えられたイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerDamageEvent
    {
        [Key(0)]
        // ダメージを与えたプレイヤー（0 = 環境ダメージなど）
        public int AttackerPlayerID { get; set; }

        [Key(1)]
        // ダメージを受けたプレイヤー
        public int VictimPlayerID { get; set; }

        [Key(2)]
        // 与えたダメージ量
        public int Damage { get; set; }

        [Key(3)]
        // ヒット位置（任意）
        public Vector2? HitPosition { get; set; }

        [Key(4)]
        // 使用された武器IDまたはタイプ
        public string? WeaponId { get; set; }

        [Key(5)]
        // クリティカルなどのフラグ
        public bool IsCritical { get; set; }

        [Key(6)]
        // ダメージ発生時刻
        public DateTime Time { get; set; }

        [Key(7)]
        // 被ダメージ後のヘルス（オプション）
        public int? RemainingHealth { get; set; }
    }
}

