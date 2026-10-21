using System;

namespace OpenGSCore
{
    /// <summary>
    /// What a mission match is played on: a clock, a life count and an objective.
    /// <para>
    /// There was no rule for this mode at all, so a room built from
    /// MissionMatchSetting was handed the survival rule and played as one. This is
    /// the rule that ends a mission instead, and it ends a mission on the three
    /// things a mission can end on, whichever happens first.
    /// </para>
    /// <para>
    /// The life count is the rule's own, not a player's. It was declared on
    /// PlayerInfo as a number of three and never written by anything, so asking
    /// how many goes a player had left always answered three.
    /// </para>
    /// </summary>
    public sealed class MissionMatchRule : AbstractMatchRule
    {
        private readonly int objectiveCount;
        private readonly bool allowRespawn;

        public MissionMatchRule(
            int objectiveCount = 1,
            int lifeCount = 3,
            bool allowRespawn = false,
            int matchTimeMsec = 600000)
            : base(EGameMode.Mission, matchTimeMsec)
        {
            ObjectiveCount = Math.Max(0, objectiveCount);
            LifeCount = Math.Max(0, lifeCount);

            // A mission starts on all of the goes it was configured with. Leaving
            // this at zero would end the mission on its first look, before a shot
            // had been fired, because running out of lives is one of its ends.
            LifeRemaining = LifeCount;
            this.allowRespawn = allowRespawn;
        }

        /// <summary>How much of the objective ends the match.</summary>
        public int ObjectiveCount { get; }

        /// <summary>How many times the party may go down.</summary>
        public int LifeCount { get; }

        /// <summary>How much of the objective has been done.</summary>
        public int ObjectiveProgress { get; private set; }

        /// <summary>How many goes are left in total across the party.</summary>
        public int LifeRemaining { get; private set; }

        /// <summary>
        /// Records one step of the objective.
        /// <para>
        /// It cannot go past the target. A client that reported progress, or a
        /// second party member reporting the same step, would otherwise push the
        /// count past the number that ends the match and make the objective read
        /// as more than it was.
        /// </para>
        /// </summary>
        /// <returns>True when this step completed the objective.</returns>
        public bool RecordObjectiveProgress(int steps = 1)
        {
            if (steps <= 0 || ObjectiveProgress >= ObjectiveCount)
            {
                return false;
            }

            ObjectiveProgress = Math.Min(ObjectiveCount, ObjectiveProgress + steps);
            return ObjectiveProgress >= ObjectiveCount;
        }

        /// <summary>
        /// Records that somebody went down, and what is left.
        /// </summary>
        public void RecordLifeLost(int livesRemaining)
        {
            LifeRemaining = Math.Max(0, livesRemaining);
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            // The clock.
            if (situation != null && situation.RemainingTimeSec <= 0f)
            {
                return true;
            }

            // The objective. A mission with no objective is not over on one, which
            // is what keeps a mission that is really a survival on the other two.
            if (ObjectiveCount > 0 && ObjectiveProgress >= ObjectiveCount)
            {
                return true;
            }

            // Out of lives. Only meaningful when there are lives to run out of: a
            // mission with none is endless by this rule and lives on the clock.
            if (LifeCount > 0 && LifeRemaining <= 0)
            {
                return true;
            }

            return false;
        }

        public override bool CanReSpawn()
        {
            return allowRespawn;
        }
    }
}