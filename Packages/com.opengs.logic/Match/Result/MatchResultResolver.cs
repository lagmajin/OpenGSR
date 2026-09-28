using System;

namespace OpenGSCore
{
    /// <summary>
    /// Centralized match-result factory for both legacy and current score models.
    /// </summary>
    public static class MatchResultResolver
    {
        public static AbstractMatchResult Create(AbstractMatchFinalScore score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            return score switch
            {
                DeathMatchFinalScore deathMatchScore => new DeathMatchResult(deathMatchScore),
                TeamDeathMatchFinalScore teamDeathMatchScore => new TeamDeathMatchResult(teamDeathMatchScore),
                CTFMatchFinalScore ctfMatchScore => new CTFMatchResult(ctfMatchScore),
                _ => Create(score.Mode)
            };
        }

        public static AbstractMatchResult Create(AbstractFinalScore score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            return Create(score.mode);
        }

        /// <summary>
        /// The modes that share the solo death match result. They are listed
        /// explicitly rather than caught by a default, so adding a mode without
        /// deciding its result fails instead of drifting silently into death
        /// match.
        /// </summary>
        private static bool IsSoloDeathMatchMode(EGameMode mode)
        {
            return mode
                is EGameMode.DeathMatch
                    or EGameMode.OneShotKill
                    or EGameMode.ArmsRace
                    or EGameMode.Practice
                    or EGameMode.FreeStyle
                    or EGameMode.Sniper
                    or EGameMode.TowerMatch;
        }

        public static AbstractMatchResult Create(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.TeamDeathMatch => new TeamDeathMatchResult(),
                EGameMode.CaptureTheFlag => new CTFMatchResult(),
                EGameMode.Survival => new SuvMatchResult(),
                EGameMode.TeamSurvival => new TSuvMatchResult(),
                _ when IsSoloDeathMatchMode(mode) => new DeathMatchResult(),
                _ => throw new NotSupportedException(
                    $"Unsupported match result mode: {mode}. Sniper, TowerMatch, " +
                    "Practice, and FreeStyle have no rule or setting type of their own yet, " +
                    "so they only resolve while they stay on the death match result.")
            };
        }
    }
}
