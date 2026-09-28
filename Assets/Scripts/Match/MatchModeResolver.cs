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

        /// <summary>
        /// The multiplier a survival player starts on, and the last word on it.
        /// <para>
        /// It used to be a two hard coded against the mode, so a room configured
        /// with anything else was played at two health on this client while the
        /// server played it at its own number, and the two disagreed about how
        /// much health a player had. A setting is not a guess, so the room state
        /// is asked for the value the server is actually using, and the mode's
        /// own default is only the answer when nothing has said otherwise.
        /// </para>
        /// </summary>
        private static float serverHealthMultiplier = float.NaN;

        public static void AdoptServerHealthMultiplier(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
            {
                return;
            }

            serverHealthMultiplier = multiplier;
        }

        public static void ForgetServerHealthMultiplier()
        {
            // A multiplier learned from one room must not follow the player into
            // the next, or a match played at twice health would silently carry
            // that into a mode that does not want it.
            serverHealthMultiplier = float.NaN;
        }

        public static float ResolveHealthMultiplier(EGameMode mode)
        {
            if (!IsSurvivalLike(mode))
            {
                return 1.0f;
            }

            // The setting's own default, so a mode that has never been told
            // anything still gets the number the server would have used.
            if (float.IsNaN(serverHealthMultiplier))
            {
                return mode == EGameMode.Survival
                    ? new OpenGSCore.SuvMatchSetting(8, false).HealthMultiplier
                    : new OpenGSCore.TeamSurvivalMatchSetting().HealthMultiplier;
            }

            return serverHealthMultiplier;
        }
    }
}
