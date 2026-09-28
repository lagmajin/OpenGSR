#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // 投票イベント（例: マップ投票、キック投票）
    [MemoryPackable]
    [MessagePackObject]
    public partial class VoteEvent
    {
        [Key(0)]
        public string VoteId { get; set; } = string.Empty;

        [Key(1)]
        public string InitiatorPlayerId { get; set; } = string.Empty;

        [Key(2)]
        public string VoteType { get; set; } = string.Empty;

        [Key(3)]
        // 追加オプション（対象プレイヤーIDやマップ名など）
        public string? Target { get; set; }

        [Key(4)]
        public DateTime StartTime { get; set; }

        [Key(5)]
        // 投票の期限（秒）
        public int DurationSeconds { get; set; }
    }
}

