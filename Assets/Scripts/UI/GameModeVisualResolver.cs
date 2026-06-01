using OpenGSCore;

namespace OpenGS
{
    public static class GameModeVisualResolver
    {
        public static string GetDisplayName(EGameMode mode)
        {
            return mode switch
            {
                EGameMode.DeathMatch => "デスマッチ (DM)",
                EGameMode.TeamDeathMatch => "チームデスマッチ (TDM)",
                EGameMode.Survival => "サバイバル (SUV)",
                EGameMode.TeamSurvival => "チームサバイバル (TSUV)",
                EGameMode.CaptureTheFlag => "キャプチャー・ザ・フラッグ (CTF)",
                EGameMode.OneShotKill => "ワンショットキル",
                EGameMode.ArmsRace => "アームズレース",
                EGameMode.Practice => "プラクティス",
                EGameMode.FreeStyle => "フリースタイル",
                EGameMode.Sniper => "スナイパー",
                EGameMode.TowerMatch => "タワーマッチ",
                _ => mode.ToString()
            };
        }
    }
}
