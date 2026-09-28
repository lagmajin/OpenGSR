using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// マッチ結果評価インターフェース
    /// 各ゲームモードの勝敗判定ロジックをカプセル化します。
    /// </summary>
    public interface IMatchResultEvaluator
    {
        /// <summary>
        /// 現在のマッチ状況に基づいて勝敗を評価し、結果を返します。
        /// </summary>
        /// <param name="situation">現在のマッチ状況</param>
        /// <param name="players">参加プレイヤー情報</param>
        /// <returns>勝敗情報を含むJObject</returns>
        JObject Evaluate(AbstractMatchSituation situation, System.Collections.Generic.List<PlayerInfo> players);
    }

    public static class MatchResultEvaluatorExtensions
    {
        public static bool TryEvaluate(this IMatchResultEvaluator evaluator, AbstractMatchSituation situation, System.Collections.Generic.List<PlayerInfo> players, out JObject result)
        {
            result = evaluator != null ? evaluator.Evaluate(situation, players) : null;
            return result != null;
        }
    }
}
