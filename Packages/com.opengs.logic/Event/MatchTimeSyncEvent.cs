using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // マッチ時間同期イベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class MatchTimeSyncEvent
    {
        [Key(0)]
        // サーバ側のマッチ経過時間（秒）
        public double ElapsedSeconds { get; set; }

        [Key(1)]
        // 残り時間（秒）、タイムリミットがある場合
        public double? RemainingSeconds { get; set; }

        [Key(2)]
        // サーバ時刻（UTC）
        public DateTime ServerTimeUtc { get; set; }

        [Key(3)]
        // オプション: シーケンス番号やバージョン
        public long Sequence { get; set; }
    }
}
