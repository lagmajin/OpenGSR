

using Newtonsoft.Json;
using OpenGSCore;

#pragma warning disable 0414
#pragma warning disable 0219

namespace OpenGS
{

    [JsonObject]
    public class AbstractMatchRule : IMatchRule
    {
        private EGameMode mode = new EGameMode();
        [JsonProperty("CanRespawn")]
        private bool canRespwn = true;
        [JsonProperty("RespawnTime")]
        private int respwnTime = 5000;
        [JsonProperty("Stage")]
        private string stage = "";
        [JsonProperty("CanUseBooster")]
        private bool canUseBooster = true;
        [JsonProperty("TimeLimit")]
        private bool timelimit = true;
        [JsonProperty("Time")]
        private int time = 600;
        [JsonProperty("BoosterPower")]
        private float boosterPower = 1.0f;
        [JsonProperty("AttackPower")]
        private float attackPower = 1.0f;
        [JsonProperty("DefencePower")]
        private float defPower = 1.0f;
        [JsonProperty("LifePower")]
        private float lifePower = 1.0f;


        public AbstractMatchRule(EGameMode mode = EGameMode.Unknown, float boosterPower = 1.0f, float attackPower = 1.0f, float defPower = 1.0f)
        {
            this.mode = mode;

        }

        public string MatchRuleName()
        {
            return mode.ToString();
        }

        public bool CanRespwn { get => canRespwn; set => canRespwn = value; }
        internal EGameMode Mode { get => mode; set => mode = value; }

        public virtual void Up()
        {

        }

        public virtual void Down()
        {

        }

        public virtual bool D(in MatchData d)
        {
            return false;
        }
    }
    [JsonObject]
    public class SurvivalMatchRule : AbstractMatchRule
    {
        SurvivalMatchRule()
        {
            Mode = EGameMode.TeamSurvival;
            CanRespwn = false;

        }

        public virtual bool D(MatchData d)
        {
            if (d.AlivePlayerCount == 1)
            {
                return true;
            }

            return false;

        }


    }
    [JsonObject]
    public class TeamSurvivalMatchRule : AbstractMatchRule
    {
        TeamSurvivalMatchRule()
        {
            Mode = EGameMode.TeamSurvival;
            CanRespwn = false;

        }

        public virtual bool D(MatchData d)
        {
            bool redFlag = false;
            bool blueFlag = false;

            if (d.AlivePlayerCount == 1)
            {

            }

            return false;
        }


    }

    [JsonObject]
    public class DeathMatchRule : AbstractMatchRule
    {

        static int defaultKillCount = 20;

        static int defaultMaxKillCount = 100;
        static int minKillCount = 5;
        static int maxKillCount = 100;
        static int killUpCount = 5;

        int killCount = defaultKillCount;
        public int KillCount { get => killCount; set => killCount = value; }

        DeathMatchRule()
        {
            Mode = EGameMode.DeathMatch;
            CanRespwn = true;

        }

        public override void Up()
        {
            if (defaultMaxKillCount <= killCount)
            {
                killCount = defaultMaxKillCount;
            }

        }

        public override void Down()
        {
            if (killCount <= minKillCount)
            {
                killCount = minKillCount;
            }

        }

        public override bool D(in MatchData d)
        {
            //var result = new MatchDiscriment();

            if (defaultMaxKillCount > d.MaxPlayerKillCount)
            {

            }

            return false;
        }
    }
    [JsonObject]
    public class TeamDeathMatchRule : AbstractMatchRule
    {
        static int defaultKillCount = 20;
        static int MaxKillCount = 100;

        int killCount = 20;
        public int KillCount { get => killCount; set => killCount = value; }
        TeamDeathMatchRule()
        {
            Mode = EGameMode.TeamDeathMatch;
            CanRespwn = true;
        }



        public override void Up()
        {

        }

        public override void Down()
        {

        }

        public override bool D(in MatchData d)
        {
            return false;
        }
    }
    [JsonObject]
    public class CTFMatchRule : AbstractMatchRule
    {
        static int defaultFlagCount = 3;
        static int defaultMaxFlagCount = 5;

        int flagReturnCount = 3;
        int flagUpCount = 1;
        public int FlagReturnCount { get => flagReturnCount; set => flagReturnCount = value; }


        public CTFMatchRule()
        {
            Mode = EGameMode.CaptureTheFlag;
            CanRespwn = true;
        }




        public override void Up()
        {
            if (defaultFlagCount >= flagReturnCount)
            {
                flagReturnCount++;
            }

        }

        public override void Down()
        {
            if (flagReturnCount > 1)
            {
                flagReturnCount--;
            }

        }
        public override bool D(in MatchData d)
        {

            return false;
        }

    }

    [JsonObject]
    public class ArmsMatchRaceRule : AbstractMatchRule
    {

        ArmsMatchRaceRule()
        {

        }

        public override void Up()
        {


        }

        public override void Down()
        {


        }

        public override bool D(in MatchData d)
        {

            return false;
        }

    }




}
