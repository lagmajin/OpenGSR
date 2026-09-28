using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

//using System.Text.Json;



namespace OpenGSCore
{
    public class TeamSurvival : AbstractTeamMatchSetting
    {
        public int SurvivalTimeMinutes { get; set; } = 10;
        public bool LastTeamStanding { get; set; } = true;
        public float HealthMultiplier { get; set; } = 2.0f;

        public TeamSurvival(bool randomTeam, bool teamBalance = true) : base(EGameMode.TeamSurvival)
        {
            RandomTeam = randomTeam;
            TeamBalance = teamBalance;
            MaxPlayerCount = 8;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "TeamSurvival";
            result["SurvivalTimeMinutes"] = SurvivalTimeMinutes;
            result["LastTeamStanding"] = LastTeamStanding;
            result["HealthMultiplier"] = HealthMultiplier;
            return result;

        }

    }
}
