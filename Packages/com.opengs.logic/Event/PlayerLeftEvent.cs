#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーがルームを退出したイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerLeftEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        public string? PlayerName { get; set; }

        [Key(2)]
        // 退出先の理由やターゲット
        public string? Reason { get; set; }

        [Key(3)]
        public string? RoomID { get; set; }

        [Key(4)]
        public DateTime Time { get; set; }
    }
}

