using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class DeathMatchSetting : AbstractMatchSetting
    {
        private int winConditionKill_ = 10;
        public int WinConditionKill => winConditionKill_;
        public DeathMatchSetting(int winConditionKill = 20, bool teamBalance = true) : base(EGameMode.DeathMatch, 0, teamBalance)
        {
            winConditionKill_ = winConditionKill;

        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "DeathMatch";
            result["WinConditionKill"] = winConditionKill_;
            result["TeamBalance"] = "true";

            return result;

        }
    }
}
