using System;
using OpenGSCore;

namespace OpenGS
{
    public static class MatchModeResolver
    {
        public static EGameMode ResolveCurrentGameMode()
        {
            try
            {
                var matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                if (matchRoomManager != null)
                {
                    if (matchRoomManager.OnlineMatchRoom != null && matchRoomManager.OnlineMatchRoom.GameMode != EGameMode.Unknown)
                    {
                        return matchRoomManager.OnlineMatchRoom.GameMode;
                    }

                    if (matchRoomManager.OfflineMatchRoom != null && matchRoomManager.OfflineMatchRoom.GameMode != EGameMode.Unknown)
                    {
                        return matchRoomManager.OfflineMatchRoom.GameMode;
                    }

                    if (matchRoomManager.WaitRoom != null && matchRoomManager.WaitRoom.GameMode != EGameMode.Unknown)
                    {
                        return matchRoomManager.WaitRoom.GameMode;
                    }

                    if (matchRoomManager.OnlineWaitRoom != null && matchRoomManager.OnlineWaitRoom.GameMode != EGameMode.Unknown)
                    {
                        return matchRoomManager.OnlineWaitRoom.GameMode;
                    }
                }
            }
            catch
            {
            }

            try
            {
                var online = GameModeSelectManager.Instance?.OnlineGameSelect;
                if (online != null && online.GameMode != EGameMode.Unknown)
                {
                    return online.GameMode;
                }

                var offline = GameModeSelectManager.Instance?.OfflineGameSelect;
                if (offline != null && offline.GameMode != EGameMode.Unknown)
                {
                    return offline.GameMode;
                }
            }
            catch
            {
            }

            return EGameMode.Unknown;
        }

        public static bool CanRespawnCurrentMatch()
        {
            return !IsSurvivalLike(ResolveCurrentGameMode());
        }

        public static bool IsSurvivalLike(EGameMode mode)
        {
            return mode == EGameMode.Survival || mode == EGameMode.TeamSurvival;
        }

        public static float ResolveHealthMultiplier(EGameMode mode)
        {
            return IsSurvivalLike(mode) ? 2.0f : 1.0f;
        }
    }
}
