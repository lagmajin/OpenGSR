#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーがルームに参加したイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerJoinedEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        public string PlayerName { get; set; } = string.Empty;

        [Key(2)]
        // オプショナル: アカウントID
        public string? AccountID { get; set; }

        [Key(3)]
        // 参加先ルームID
        public string? RoomID { get; set; }

        [Key(4)]
        public DateTime Time { get; set; }

        [Key(5)]
        // プレイヤーがボットか
        public bool IsBot { get; set; } = false;

        [Key(6)]
        // optional team id if applicable
        public int TeamId { get; set; } = -1;
    }
}

