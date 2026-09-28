using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class EventPlayerInfo
    {
        string playerID;

        string playerDisplayName;

        public EventPlayerInfo(string playerID, string playerDisplayName)
        {
            this.PlayerID = playerID;
            this.PlayerDisplayName = playerDisplayName;
        }

        public string PlayerID { get => playerID; set => playerID = value; }
        public string PlayerDisplayName { get => playerDisplayName; set => playerDisplayName = value; }
    }


    public class TeamEventPlayerInfo
    {
        eTeam? team;

        string playerID;

        string playerDisplayName;

        public TeamEventPlayerInfo(eTeam? team, string playerID, string playerDisplayName)
        {
            this.Team = team;
            this.PlayerID = playerID;
            this.PlayerDisplayName = playerDisplayName;
        }

        public eTeam? Team { get => team; set => team = value; }
        public string PlayerID { get => playerID; set => playerID = value; }
        public string PlayerDisplayName { get => playerDisplayName; set => playerDisplayName = value; }
    }




}
