using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class TeamSurvivalMatchSetting : AbstractTeamMatchSetting
    {
        private int survivalTimeMinutes = 10;

        public int SurvivalTimeMinutes { get => survivalTimeMinutes; set => Set(ref survivalTimeMinutes, value); }
        private bool lastTeamStanding = true;

        public bool LastTeamStanding { get => lastTeamStanding; set => Set(ref lastTeamStanding, value); }
        private float healthMultiplier = 2.0f;

        public float HealthMultiplier { get => healthMultiplier; set => Set(ref healthMultiplier, value); }

        public TeamSurvivalMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.TeamSurvival, true, teamBalance)
        {
            MaxPlayerCount = maxPlayerCapacity;
            SurvivalTimeMinutes = 10;
            LastTeamStanding = true;
        }

        public new JObject ToJson()
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

