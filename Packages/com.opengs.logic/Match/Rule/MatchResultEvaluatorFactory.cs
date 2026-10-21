using System;

namespace OpenGSCore
{
    /// <summary>
    /// IMatchResultEvaluatorを生成するファクトリ
    /// </summary>
    public static class MatchResultEvaluatorFactory
    {
        public static IMatchResultEvaluator CreateEvaluator(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.DeathMatch => new DeathMatchResultEvaluator(),
                EGameMode.OneShotKill => new DeathMatchResultEvaluator(),
                EGameMode.ArmsRace => new DeathMatchResultEvaluator(),
                EGameMode.Practice => new DeathMatchResultEvaluator(),
                EGameMode.FreeStyle => new DeathMatchResultEvaluator(),
                EGameMode.Sniper => new DeathMatchResultEvaluator(),
                EGameMode.TowerMatch => new DeathMatchResultEvaluator(),
                EGameMode.TeamDeathMatch => new TeamDeathMatchResultEvaluator(),
                EGameMode.Survival => new SurvivalResultEvaluator(),
                EGameMode.TeamSurvival => new TeamSurvivalResultEvaluator(),
EGameMode.CaptureTheFlag => new CaptureTheFlagResultEvaluator(),

                // Had no case here, so a mission either threw at the end of the
                // match or, because the setting used to declare itself a survival,
                // was settled as one. A mission has an outcome rather than a
                // winning team, so it gets the evaluator that says which.
                EGameMode.Mission => new MissionResultEvaluator(),

                _ => throw new NotSupportedException($"Unsupported match result mode: {mode}")
            };
        }
    }
}
