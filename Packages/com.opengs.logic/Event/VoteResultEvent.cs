#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // 投票結果イベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class VoteResultEvent
    {
        [Key(0)]
        public string VoteId { get; set; } = string.Empty;

        [Key(1)]
        public bool Passed { get; set; }

        [Key(2)]
        public int YesCount { get; set; }

        [Key(3)]
        public int NoCount { get; set; }

        [Key(4)]
        public DateTime EndTime { get; set; }

        [Key(5)]
        // 任意: 詳細結果や理由など
        public string? Detail { get; set; }
    }
}

