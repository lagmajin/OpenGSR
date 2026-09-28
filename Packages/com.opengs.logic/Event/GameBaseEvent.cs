using System;

namespace OpenGSCore
{
    public enum EPlayerPoseState
    {
        Stand,
        Sit,
        LieDown
    }

    public class PlayerPoseEvent : AbstractGameEvent
    {
        private string playerID_;
        private EPlayerPoseState poseState_;

        public PlayerPoseEvent(string playerId, EPlayerPoseState poseState)
        {
            playerID_ = playerId;
            poseState_ = poseState;
        }

        public string PlayerID() => playerID_;
        public EPlayerPoseState PoseState() => poseState_;
    }

    public class PlayerDeadEvent : AbstractGameEvent
    {
        private DeadReason reason_ = DeadReason.Unknown;
        private string playerName_;
        private string playerID_;
        private ETeam playerTeam_;

        public PlayerDeadEvent(DeadReason reason, string playerName, string playerID, ETeam team)
        {
            reason_ = reason;
            playerName_ = playerName;
            playerID_ = playerID;
            playerTeam_ = team;
        }

        public string PlayerName()
        {
            return playerName_;
        }

        public ETeam PlayerTeam()
        {
            return playerTeam_;
        }
    }
}
