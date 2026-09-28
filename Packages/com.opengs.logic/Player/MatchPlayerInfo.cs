using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{

    //試合中のプレイヤーデータ
    //#MatchPlayerInfo
    public sealed class MatchPlayerInfo
    {
        private string name_;
        private string guid_;
        private ETeam? team_;

        public PlayerStatus status { get; set; } = new PlayerStatus();


        public MatchPlayerInfo(in string name, in string guid, in ETeam? team)
        {
            name_ = name;
            guid_ = guid;
            team_ = team;

        }

        private bool HasTeam()
        {
            if (team_ == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool NoTeam()
        {
            return !HasTeam();
        }

    }
}
