using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class ArmsRaceMatchSetting : AbstractMatchSetting
    {
        private int winConditionKill = 30;
        public int WinConditionKill => winConditionKill;

        public ArmsRaceMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.ArmsRace, maxPlayerCapacity, teamBalance)
        {
            winConditionKill = 30;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "ArmsRace";
            result["WinConditionKill"] = winConditionKill;
            result["Description"] = "Collect weapons and eliminate enemies";

            return result;
        }
    }
}
