#nullable enable
using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // バフの期限切れイベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class BuffExpiredEvent
    {
        [Key(0)]
        // 対象プレイヤーID
        public int PlayerID { get; set; }

        [Key(1)]
        // バフの識別子
        public string BuffId { get; set; } = string.Empty;

        [Key(2)]
        // 期限切れ時刻（サーバ時刻）
        public DateTime ExpireTime { get; set; }

        [Key(3)]
        // バフが自然に切れたのか強制解除されたのか
        public bool WasRemoved { get; set; }

        [Key(4)]
        // オプション: 解除した原因またはソース
        public string? Source { get; set; }
    }
}

