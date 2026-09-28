using System;
using MessagePack;
using MemoryPack;

namespace OpenGSCore
{
    // スコア更新イベント
    [MemoryPackable]
    [MessagePackObject]
    public partial class ScoreUpdateEvent
    {
        [Key(0)]
        public int PlayerID { get; set; }

        [Key(1)]
        // 更新されたスコアの種類（キル、アシスト、フラグ等）
        public string Type { get; set; }

        [Key(2)]
        // 変化量（正負）
        public int Delta { get; set; }

        [Key(3)]
        // 新しい合計スコア（オプション）
        public int? NewTotal { get; set; }

        [Key(4)]
        // イベント発生時刻
        public DateTime Time { get; set; }
    }
}
