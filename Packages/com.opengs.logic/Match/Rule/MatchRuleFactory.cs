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
                EGameMode.CaptureTheFlag => setting is CaptureTheFlagMatchSetting ctfSetting
                    ? new CaptureTheFlagMatchRule(ctfSetting.WinConditionPoint)
                    : new CaptureTheFlagMatchRule(),
                _ => throw new NotSupportedException($"Unsupported game mode: {setting.Mode}")
            };
        }
    }
}

