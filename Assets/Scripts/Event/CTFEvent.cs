

namespace OpenGS
{


    internal class FlagDropEvent : AbstractGameEvent
    {
        private string playerName;
        private ETeam team;
    }
    internal class FlagBurstEvent : AbstractGameEvent
    {
        private ETeam team;

        FlagBurstEvent(ETeam team)
        {


        }
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
