#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // プレイヤーの観戦（スペクテイト）状態変更イベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class PlayerSpectatingEvent
    {
        [Key(0)]
        // 観戦者のプレイヤーID
        public int SpectatorPlayerID { get; set; }

        [Key(1)]
        // 観戦対象のプレイヤーID（nullならフリービュー）
        public int? TargetPlayerID { get; set; }

        [Key(2)]
        // 観戦開始(true) / 終了(false)
        public bool IsSpectating { get; set; }

        [Key(3)]
        // 観戦モード（例: "FirstPerson", "ThirdPerson", "FreeCam"）
        public string? Mode { get; set; }

        [Key(4)]
        // イベント発生時刻
        public DateTime Time { get; set; }
    }
}

