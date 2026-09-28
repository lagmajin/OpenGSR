using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class CaptureTheFlagMatchSetting : AbstractMatchSetting
    {
        private int winConditionPoint = 3;
        public int WinConditionPoint => winConditionPoint;

        public CaptureTheFlagMatchSetting(int winCondition = 3, bool teamBalance = true)
            : base(EGameMode.CaptureTheFlag, 8, teamBalance, false)
        {
            winConditionPoint = winCondition;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "CaptureTheFlag";
            result["WinConditionPoint"] = winConditionPoint;
            result["TeamBalance"] = true;
            return result;
        }
    }
}
