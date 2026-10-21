#nullable enable
using System;

namespace OpenGSCore
{
    public static class MatchRuleFactory
    {
        public static AbstractMatchRule? CreateMatchRule(AbstractMatchSetting setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            return setting.Mode switch
            {
                EGameMode.DeathMatch => setting is DeathMatchSetting deathMatchSetting
                    ? new DeathMatchRule(deathMatchSetting)
                    : new DeathMatchRule(),
                // Both of these used to be built with only a kill limit, so they
                // took the match time of an unrelated rule's default. A mode
                // nobody gave a length ran on another mode's.
                EGameMode.OneShotKill => setting is OneShotKillMatchSetting oneShotKillSetting
                    ? new DeathMatchRule(
                        oneShotKillSetting.MatchTimeMinutes * 60 * 1000,
                        oneShotKillSetting.WinConditionKill)
                    : new DeathMatchRule(),
                EGameMode.ArmsRace => setting is ArmsRaceMatchSetting armsRaceSetting
                    ? new DeathMatchRule(
                        armsRaceSetting.MatchTimeMinutes * 60 * 1000,
                        armsRaceSetting.WinConditionKill)
                    : new DeathMatchRule(),
                EGameMode.TeamDeathMatch => setting is TDMMatchSetting teamDeathMatchSetting
                    ? new TDMMatchRule(teamDeathMatchSetting)
                    : new TDMMatchRule(),
                EGameMode.Survival => setting is SuvMatchSetting suvSetting
                    ? new SuvMatchRule(suvSetting)
                    : new SuvMatchRule(),
                EGameMode.TeamSurvival => setting is TeamSurvivalMatchSetting teamSetting
                    ? new TSuvMatchRule(teamSetting)
                    : new TSuvMatchRule(),
                // The length was dropped here as well as the limit, so the rule took its own
                // ten minute default and the setting's length was thrown away. A
                // room configured for a different capture the flag was played on
                // numbers nobody had asked for.
                EGameMode.CaptureTheFlag => setting is CaptureTheFlagMatchSetting ctfSetting
                    ? new CaptureTheFlagMatchRule(
                        ctfSetting.WinConditionPoint,
                        ctfSetting.MatchTimeMinutes * 60 * 1000)
                    : new CaptureTheFlagMatchRule(),
                // This mode had no case here, so a room built from MissionMatchSetting
                // either threw or, because the setting used to declare itself a
                // survival, was quietly played as one. Its own numbers, the life
                // count and the objective, reach the rule from the setting rather
                // than from whatever another mode's defaults happened to be.
                EGameMode.Mission => setting is MissionMatchSetting missionSetting
                    ? new MissionMatchRule(
                        missionSetting.ObjectiveCount,
                        missionSetting.LifeCount,
                        missionSetting.AllowRespawn,
                        missionSetting.MatchTimeMinutes * 60 * 1000)
                    : new MissionMatchRule(),
                _ => throw new NotSupportedException($"Unsupported game mode: {setting.Mode}")
            };
        }
    }
}

