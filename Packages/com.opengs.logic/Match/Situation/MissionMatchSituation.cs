namespace OpenGSCore
{
    /// <summary>
    /// The progress of a mission: how much of the objective has been done and how
    /// many goes the party has left.
    /// <para>
    /// It had no situation of its own, so a mission was played on the survival
    /// situation's numbers, which are a kill count and an alive count, neither of
    /// which means anything for a mission. This is where its numbers go.
    /// </para>
    /// </summary>
    public class MissionMatchSituation : AbstractMatchSituation
    {
        private int objectiveCount = 1;

        public int ObjectiveCount
        {
            get => objectiveCount;
            set => objectiveCount = System.Math.Max(0, value);
        }

        /// <summary>How many goes the party started with.</summary>
        public int LifeCount { get; set; } = 3;

        /// <summary>How many of them are left.</summary>
        public int LifeRemaining { get; set; } = 3;

        /// <summary>How much of the objective has been done.</summary>
        public int ObjectiveProgress { get; set; }

        public bool IsObjectiveComplete()
        {
            return ObjectiveCount > 0 && ObjectiveProgress >= ObjectiveCount;
        }

        /// <summary>
        /// Puts the situation in step with the room that is playing it.
        /// <para>
        /// The rule carries the numbers the match is judged by and the situation is
        /// what the result is read out of. They are set from each other in one
        /// place so the two can never be built from different values.
        /// </para>
        /// </summary>
        public void AdoptFrom(MissionMatchRule rule)
        {
            if (rule == null)
            {
                return;
            }

            ObjectiveCount = rule.ObjectiveCount;
            LifeCount = rule.LifeCount;
            LifeRemaining = rule.LifeRemaining;
            ObjectiveProgress = rule.ObjectiveProgress;
        }
    }
}