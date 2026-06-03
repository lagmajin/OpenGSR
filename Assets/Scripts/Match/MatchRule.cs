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

        public int TimeLimitSeconds => time;
        public bool TimeLimitEnabled => timelimit;

        public bool CanRespwn { get => canRespwn; set => canRespwn = value; }
        internal EGameMode Mode { get => mode; set => mode = value; }

        public virtual void Up()
        {
            boosterPower = Mathf.Min(boosterPower + 0.1f, 3.0f);
            attackPower = Mathf.Min(attackPower + 0.1f, 3.0f);
            defPower = Mathf.Min(defPower + 0.1f, 3.0f);
            lifePower = Mathf.Min(lifePower + 0.1f, 3.0f);
            time = Mathf.Min(time + 10, 3600);
        }

        public virtual void Down()
        {
            boosterPower = Mathf.Max(0.1f, boosterPower - 0.1f);
            attackPower = Mathf.Max(0.1f, attackPower - 0.1f);
            defPower = Mathf.Max(0.1f, defPower - 0.1f);
            lifePower = Mathf.Max(0.1f, lifePower - 0.1f);
            time = Mathf.Max(10, time - 10);
        }

        public virtual bool D(in MatchData d)
        {
            return d != null && d.AlivePlayerCount <= 0;
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
        private static int defaultFlagCount = 5;
        private static int defaultMaxFlagCount = 5;

        private int flagCaptureCount = defaultFlagCount;
        private int flagUpCount = 1;
        [System.Obsolete("Use FlagCaptureCount instead.")]
        public int FlagReturnCount { get => flagCaptureCount; set => flagCaptureCount = value; }
        public int FlagCaptureCount { get => flagCaptureCount; set => flagCaptureCount = value; }

        public CTFMatchRule()
        {
            Mode = EGameMode.CaptureTheFlag;
            CanRespwn = true;
        }

        public override void Up()
        {
            flagCaptureCount = Mathf.Min(flagCaptureCount + flagUpCount, defaultMaxFlagCount);
        }

        public override void Down()
        {
            flagCaptureCount = Mathf.Max(flagCaptureCount - flagUpCount, 1);
        }

        public override bool D(in MatchData d)
        {
            return d != null && Mathf.Max(d.RedTeamFlagScore, d.BlueTeamFlagScore) >= flagCaptureCount;
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
