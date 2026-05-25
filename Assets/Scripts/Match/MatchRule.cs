using Newtonsoft.Json;
using OpenGSCore;
using UnityEngine;

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
            this.boosterPower = boosterPower;
            this.attackPower = attackPower;
            this.defPower = defPower;
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
        public SurvivalMatchRule()
        {
            Mode = EGameMode.Survival;
            CanRespwn = false;
        }

        public override bool D(in MatchData d)
        {
            return d != null && d.AlivePlayerCount <= 1;
        }
    }

    [JsonObject]
    public class TeamSurvivalMatchRule : AbstractMatchRule
    {
        public TeamSurvivalMatchRule()
        {
            Mode = EGameMode.TeamSurvival;
            CanRespwn = false;
        }

        public override bool D(in MatchData d)
        {
            return d != null && d.AlivePlayerCount <= 1;
        }
    }

    [JsonObject]
    public class DeathMatchRule : AbstractMatchRule
    {
        private static int defaultKillCount = 20;
        private static int defaultMaxKillCount = 100;
        private static int minKillCount = 5;
        private static int killUpCount = 5;

        private int killCount = defaultKillCount;
        public int KillCount { get => killCount; set => killCount = value; }

        public DeathMatchRule()
        {
            Mode = EGameMode.DeathMatch;
            CanRespwn = true;
        }

        public override void Up()
        {
            killCount = Mathf.Min(killCount + killUpCount, defaultMaxKillCount);
        }

        public override void Down()
        {
            killCount = Mathf.Max(killCount - killUpCount, minKillCount);
        }

        public override bool D(in MatchData d)
        {
            return d != null && d.MaxPlayerKillCount >= killCount;
        }
    }

    [JsonObject]
    public class TeamDeathMatchRule : AbstractMatchRule
    {
        private static int defaultKillCount = 20;
        private static int maxKillCount = 100;

        private int killCount = defaultKillCount;
        public int KillCount { get => killCount; set => killCount = value; }

        public TeamDeathMatchRule()
        {
            Mode = EGameMode.TeamDeathMatch;
            CanRespwn = true;
        }

        public override void Up()
        {
            killCount = Mathf.Min(killCount + 1, maxKillCount);
        }

        public override void Down()
        {
            killCount = Mathf.Max(killCount - 1, 1);
        }

        public override bool D(in MatchData d)
        {
            return d != null && Mathf.Max(d.RedTeamKill, d.BlueTeamKill) >= killCount;
        }
    }

    [JsonObject]
    public class CTFMatchRule : AbstractMatchRule
    {
        private static int defaultFlagCount = 3;
        private static int defaultMaxFlagCount = 5;

        private int flagReturnCount = defaultFlagCount;
        private int flagUpCount = 1;
        public int FlagReturnCount { get => flagReturnCount; set => flagReturnCount = value; }

        public CTFMatchRule()
        {
            Mode = EGameMode.CaptureTheFlag;
            CanRespwn = true;
        }

        public override void Up()
        {
            flagReturnCount = Mathf.Min(flagReturnCount + flagUpCount, defaultMaxFlagCount);
        }

        public override void Down()
        {
            flagReturnCount = Mathf.Max(flagReturnCount - flagUpCount, 1);
        }

        public override bool D(in MatchData d)
        {
            return d != null && Mathf.Max(d.RedTeamFlagReturn, d.BlueTeamFlagReturn) >= flagReturnCount;
        }
    }

    [JsonObject]
    public class ArmsMatchRaceRule : AbstractMatchRule
    {
        private int killCount = 10;

        public ArmsMatchRaceRule()
        {
            Mode = EGameMode.ArmsRace;
            CanRespwn = true;
        }

        public override void Up()
        {
            killCount = Mathf.Min(killCount + 1, 100);
        }

        public override void Down()
        {
            killCount = Mathf.Max(killCount - 1, 1);
        }

        public override bool D(in MatchData d)
        {
            return d != null && d.MaxPlayerKillCount >= killCount;
        }
    }
}
