#nullable enable
using OpenGSCore;
using OpenGSCore.Score;
using System.Collections.Generic;
using System.Linq;

namespace OpenGSCore.Score
{
    
    public interface IAllPlayerMatchFinalScore
    {
    }

    // 既存互換を保つ非ジェネリック基底（元のまま null を返す実装を維持）
    public class AbstractAllPlayerMatchFinalScore
    {
        public List<AbstractPlayerMatchFinalScore> AllPlayerFinalScore() { return new List<AbstractPlayerMatchFinalScore>(); }

        public virtual void Clear()
        {
        }
    }

    // 重複を吸収するジェネリック中間基底。元の基底メソッドを隠す（new）ことで
    // 呼び出し時の既存挙動を壊しません。
    public class AbstractAllPlayerMatchFinalScore<T> : AbstractAllPlayerMatchFinalScore
        where T : AbstractPlayerMatchFinalScore
    {
        public List<T> scores = new();

        public int Count => scores.Count;

        public void AddScore(T score)
        {
            TryAddScore(score);
        }

        public bool TryAddScore(T score)
        {
            if (score == null || string.IsNullOrWhiteSpace(score.PlayerId) ||
                scores.Any(existing => existing != null && existing.PlayerId == score.PlayerId))
            {
                return false;
            }

            scores.Add(score);
            return true;
        }

        public T? FindByPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return default;
            }

            return scores.FirstOrDefault(score => score != null && score.PlayerId == playerId);
        }

        public new List<AbstractPlayerMatchFinalScore> AllPlayerFinalScore()
        {
            return scores.Cast<AbstractPlayerMatchFinalScore>().ToList();
        }

        public override void Clear()
        {
            scores.Clear();
        }
    }

    // 派生クラスはそのままジェネリック基底を継承（外部 API を変更しない）
    public class AllPlayerDeathMatchPlayerMatchFinalScore : AbstractAllPlayerMatchFinalScore<PlayerDeathMatchFinalScore>
    {
    }

    public class AllPlayerTeamDeathMatchPlayerMatchFinalScore : AbstractAllPlayerMatchFinalScore<PlayerTeamDeathMatchFinalScore>
    {
    }

    public class AllCaptureTheFlagMatchPlayerFinalScore : AbstractAllPlayerMatchFinalScore<PlayerCTFMatchFinalScore>
    {
    }

}

