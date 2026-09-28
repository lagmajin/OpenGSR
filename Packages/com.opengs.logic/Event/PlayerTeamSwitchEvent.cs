#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーのチーム切り替えイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerTeamSwitchEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // 以前のチームID
        public int OldTeamId { get; set; }

        [Key(2)]
        // 新しいチームID
        public int NewTeamId { get; set; }

        [Key(3)]
        // 切替時刻
        public DateTime Time { get; set; }

        [Key(4)]
        // 強制的に切り替えられたか
        public bool IsForced { get; set; }

        [Key(5)]
        // 任意の理由説明
        public string? Reason { get; set; }
    }
}

