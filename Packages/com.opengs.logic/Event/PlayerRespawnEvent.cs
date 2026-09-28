#nullable enable
using System;
using System.Numerics;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーのリスポーンイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerRespawnEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // リスポーン位置
        public Vector2 RespawnPosition { get; set; }

        [Key(2)]
        // リスポーン時刻（サーバ時刻）
        public DateTime RespawnTime { get; set; }

        [Key(3)]
        // 使用したスポーンポイントの識別子（任意）
        public string? SpawnPointId { get; set; }

        [Key(4)]
        // リスポーンが強制的に行われたか
        public bool IsForced { get; set; }

        [Key(5)]
        // リスポーン理由（デスやタイムアウト等）
        [MemoryPackAllowSerialize]
        public DeadReason Reason { get; set; } = DeadReason.Unknown;
    }
}

