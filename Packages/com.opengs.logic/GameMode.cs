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

        /// <summary>
        /// The maps a mode can be played on.
        /// <para>
        /// A room picks a map, and a mode is only playable on maps built for it.
        /// This list used to exist only in the client's offline scene, so a room
        /// created online carried no map at all: the server left it unknown and
        /// the client fell back to a map that had no flag stands, which is a
        /// capture the flag map with nothing to capture. A room with no map has to
        /// be given one, and the only sensible thing to give it is one the mode
        /// can actually be played on, which is a question about the mode rather
        /// than about whoever is asking.
        /// </para>
        /// </summary>
        public static IReadOnlyList<EMap> MapsFor(EGameMode mode)
        {
            switch (mode)
            {
                case EGameMode.CaptureTheFlag:
                    return CaptureTheFlagMaps;

                case EGameMode.Survival:
                case EGameMode.TeamSurvival:
                    return SurvivalMaps;

                default:
                    return DeathMatchMaps;
            }
        }

        /// <summary>
        /// A map to play this mode on, or null when the mode has none at all.
        /// </summary>
        public static EMap? DefaultMapFor(EGameMode mode)
        {
            var maps = MapsFor(mode);
            return maps.Count > 0 ? maps[0] : null;
        }

        private static readonly EMap[] CaptureTheFlagMaps =
        {
            EMap.BattlePortCTF,
            EMap.TheParkCTF,
            EMap.SkyHighCTF,
        };

        private static readonly EMap[] SurvivalMaps =
        {
            EMap.DryDays,
            EMap.Nocturne,
            EMap.GreenHillSide1,
            EMap.GhostHouse,
        };

        private static readonly EMap[] DeathMatchMaps =
        {
            EMap.DryDays,
            EMap.GreenHillSide1,
            EMap.GreenHillSide2,
            EMap.CityOfDarkness1,
            EMap.CityOfDarkness2,
            EMap.BluffStructure1,
            EMap.BluffStructure2,
        };

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
