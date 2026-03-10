

namespace OpenGS
{
    public class AbstractMatchEvent : AbstractGameEvent
    {

    }

    class ShotEvent : AbstractGameEvent
    {

        ShotEvent()
        {
            EventName = "ShotEvent";

        }
    }

    public class GameStartEvent : AbstractGameEvent
    {
        GameStartEvent()
        {
            EventName = "GameStartEvent";

        }
    }

    public class PlayerBurstEvent : AbstractGameEvent
    {

        PlayerBurstEvent()
        {
            EventName = "PlayerBurstEvent";

        }

        static string EventNameString()
        {
            return "PlayerBurstEvent";
        }

    }



    public class FlagLostEvent : AbstractGameEvent
    {
        FlagLostEvent()
        {
            EventName = "FlagLostEvent";
        }

        public static string EventNameString()
        {
            return "FlagLostEvent";
        }

    }

    public class FlagReturnSuccessEvent : AbstractGameEvent
    {
        public FlagReturnSuccessEvent()
        {
            EventName = EventNameString();

        }

        public static string EventNameString()
        {
            return "FlagReturnEvent";
        }

    }

    public class GameEndEvent : AbstractGameEvent
    {
        GameEndEvent()
        {
            EventName = EventNameString();

        }

        public static string EventNameString()
        {
            return "FlagReturnEvent";
        }


    }

}
