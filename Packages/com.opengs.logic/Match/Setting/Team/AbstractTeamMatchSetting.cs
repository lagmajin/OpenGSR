

using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class AbstractTeamMatchSetting : AbstractMatchSetting
    {
        private bool randomTeam = true;

        public bool RandomTeam { get => randomTeam; set => Set(ref randomTeam, value); }

        private bool teamBalance = false;

        public bool TeamBalance { get => teamBalance; set => Set(ref teamBalance, value); }

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

