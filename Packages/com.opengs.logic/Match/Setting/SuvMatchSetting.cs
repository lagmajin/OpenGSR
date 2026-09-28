using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class SuvMatchSetting : AbstractMatchSetting
    {
        public int WinConditionKill { get; set; } = 1;
        public float HealthMultiplier { get; set; } = 2.0f;

        public SuvMatchSetting(int maxPlayer, bool teamBalance)
            : base(EGameMode.Survival, maxPlayer, teamBalance)
        {
            TimeLimit = false;
            AllowOvertime = false;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "Survival";
            result["WinConditionKill"] = WinConditionKill;
            result["HealthMultiplier"] = HealthMultiplier;
            result["TeamBalance"] = false;
            return result;
        }
    }
}
