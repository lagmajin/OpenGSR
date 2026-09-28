using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class MissionMatchSetting : AbstractMatchSetting
    {
        public string MissionType { get; set; } = "Default";
        public int ObjectiveCount { get; set; } = 1;
        public bool AllowRespawn { get; set; } = false;

        public MissionMatchSetting(int maxPlayerCapacity = 4, string missionType = "Default", EGameMode mode = EGameMode.Survival)
            : base(mode, maxPlayerCapacity, false)
        {
            MissionType = missionType;
            AllowRespawn = false;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "Mission";
            result["MissionType"] = MissionType;
            result["ObjectiveCount"] = ObjectiveCount;
            result["AllowRespawn"] = AllowRespawn;

            return result;
        }
    }
}
