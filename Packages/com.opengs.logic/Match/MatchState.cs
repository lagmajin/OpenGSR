using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class MatchResult
    {
        public enum Status
        {
            Win,        // 勝ち
            Lose,       // 負け
            Draw,       // 引き分け
            InProgress, // 進行中
            NoResult    // 結果なし
        }

        public Status CurrentStatus { get; set; }

        public MatchResult(Status status)
        {
            CurrentStatus = status;
        }

        public string GetResultDescription()
        {
            return CurrentStatus switch
            {
                Status.Win => "勝ち",
                Status.Lose => "負け",
                Status.Draw => "引き分け",
                Status.InProgress => "進行中",
                Status.NoResult => "結果なし",
                _ => "不明",
            };
        }
    }
}
