using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class TDMMatchSetting : AbstractTeamMatchSetting
    {
        public TDMMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.TeamDeathMatch, true, teamBalance)
        {
            MaxPlayerCount = maxPlayerCapacity;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "TeamDeathMatch";
            result["MaxPlayerCount"] = MaxPlayerCount;
            return result;
        }
    }
}
