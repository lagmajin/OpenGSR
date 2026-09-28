using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public sealed class MissionRule
    {
        public bool CanRespawn { get; private set; } = true;

        public int MaxLife { get; private set; } = 5;
        public int MaxPlayer { get; private set; } = 3;
        public bool LifeLimit { get; private set; } = true;
        public int LifeRemaining { get; set; } = 3;

        public MissionRule(in MissionSetting setting)
        {
            if (setting == null)
            {
                return;
            }

            LifeLimit = setting.LifeLimit;
            MaxLife = setting.LifeCount;
            MaxPlayer = setting.MaxPlayer;
            CanRespawn = setting.LifeLimit;
            LifeRemaining = setting.LifeCount;
        }

        public MissionRule(int lifeRemaining = 3, int maxLife = 5, int maxPlayer = 3)
        {
            LifeRemaining = lifeRemaining;
            MaxLife = maxLife;
            MaxPlayer = maxPlayer;
        }

    }
}
