

using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class AbstractTeamMatchSetting : AbstractMatchSetting
    {
        public bool RandomTeam { get; set; } = true;

        public bool TeamBalance { get; set; } = false;

        public AbstractTeamMatchSetting(EGameMode mode, bool randomTeam = false, bool teamBalance = true) : base(mode, 8, teamBalance, false)
        {
            RandomTeam = randomTeam;
            TeamBalance = teamBalance;
        }



        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["RandomTeam"] = RandomTeam.ToString();
            result["TeamBalance"] = TeamBalance.ToString();

            return result;
        }


    }
}
