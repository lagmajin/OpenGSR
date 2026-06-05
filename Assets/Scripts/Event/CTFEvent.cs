

namespace OpenGS
{


    internal class FlagDropEvent : AbstractGameEvent
    {
        private readonly string playerName;
        private readonly ETeam team;

        public FlagDropEvent(string playerName, ETeam team)
        {
            this.playerName = playerName;
            this.team = team;
        }

        public string PlayerName() => playerName;
        public ETeam Team() => team;
    }
    internal class FlagBurstEvent : AbstractGameEvent
    {
        private readonly ETeam team;

        public FlagBurstEvent(ETeam team)
        {
            this.team = team;
        }

        public ETeam Team() => team;
    }

    class FlagRecoveryRequestEvent : AbstractGameEvent
    {

    }


    /*
    class FlagReturnEvent : AbstractGameEvent
    {
        private string playerName;
        private eTeam team;

        FlagReturnEvent()
        {

        }


    }
    */
}
