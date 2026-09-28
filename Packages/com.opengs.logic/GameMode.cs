using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EGameMode : byte
    {
        DeathMatch = 0,
        TeamDeathMatch,
        Practice,
        FreeStyle,
        OneShotKill,
        Sniper,
        TowerMatch,
        Survival,
        TeamSurvival,
        CaptureTheFlag,
        ArmsRace,
        Unknown,
    }

    public class GameMode
    {
        private EGameMode mode = EGameMode.Unknown;
        private string str = "unknown";

        public static List<EGameMode> AllGameMode()
        {
            return new List<EGameMode>
            {
                EGameMode.DeathMatch,
                EGameMode.TeamDeathMatch,
                EGameMode.Survival,
                EGameMode.TeamSurvival,
                EGameMode.CaptureTheFlag,
                EGameMode.OneShotKill,
                EGameMode.ArmsRace
            };
        }

        public GameMode(EGameMode mode)
        {
            this.mode = mode;
            this.str = mode.ToString().ToLower();
        }

        public GameMode(string modeStr)
        {
            if (string.IsNullOrWhiteSpace(modeStr)) return;

            var temp = modeStr.Trim().ToLower();

            if (temp == "deathmatch" || temp == "dm")
            {
                mode = EGameMode.DeathMatch;
                str = "dm";
            }
            else if (temp == "teamdeathmatch" || temp == "tdm")
            {
                mode = EGameMode.TeamDeathMatch;
                str = "tdm";
            }
            else if (temp == "survival" || temp == "suv")
            {
                mode = EGameMode.Survival;
                str = "suv";
            }
            else if (temp == "teamsurvival" || temp == "tsuv")
            {
                mode = EGameMode.TeamSurvival;
                str = "tsuv";
            }
            else if (temp == "capturetheflag" || temp == "ctf")
            {
                mode = EGameMode.CaptureTheFlag;
                str = "ctf";
            }
            else
            {
                if (Enum.TryParse<EGameMode>(modeStr, true, out var result))
                {
                    mode = result;
                    str = mode.ToString().ToLower();
                }
            }
        }

        public string Name() => str;

        public bool Valid() => mode != EGameMode.Unknown;

        public EGameMode Mode { get => mode; set => mode = value; }
    }
}
