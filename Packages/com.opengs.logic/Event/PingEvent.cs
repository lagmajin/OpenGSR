#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // Ping イベント（クライアントからの ping 要求を表現する場合など）
    [MemoryPackable]
    [MessagePackObject]
    public partial class PingEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        public long ClientTimestampTicks { get; set; }

        [Key(2)]
        // 任意：クライアント送信時刻の文字列形式
        public string? ClientTimestampString { get; set; }

        [Key(3)]
        public DateTime ServerReceivedTime { get; set; }
    }
}

