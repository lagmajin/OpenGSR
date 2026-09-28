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
                EGameMode.OneShotKill => setting is OneShotKillMatchSetting oneShotKillSetting
                    ? new DeathMatchRule(oneShotKillSetting.WinConditionKill)
                    : new DeathMatchRule(1),
                EGameMode.ArmsRace => setting is ArmsRaceMatchSetting armsRaceSetting
                    ? new DeathMatchRule(armsRaceSetting.WinConditionKill)
                    : new DeathMatchRule(30),
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

