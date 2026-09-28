using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class OneShotKillMatchSetting : AbstractMatchSetting
    {
        private int winConditionKill = 1;
        public int WinConditionKill => winConditionKill;

        public OneShotKillMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.OneShotKill, maxPlayerCapacity, teamBalance)
        {
            winConditionKill = 1;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "OneShotKill";
            result["WinConditionKill"] = winConditionKill;
            result["Description"] = "First kill wins";

            return result;
        }
    }
}
