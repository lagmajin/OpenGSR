using System;
using Newtonsoft.Json.Linq;
using UniRx;

namespace OpenGS
{
    /// <summary>
    /// Bridges daily/guild responses from the general network stream to UI-facing events.
    /// </summary>
    public sealed class OnlineMetaStateAdapter : IDisposable
    {
        private readonly GeneralServerNetworkManager _network;
        private readonly IDisposable _subscription;
        private bool disposed;

        public event Action<JObject> DailyListUpdated;
        public event Action<JObject> DailyProgressUpdated;
        public event Action<JObject> DailyClaimUpdated;
        public event Action<JObject> GuildListUpdated;
        public event Action<JObject> GuildInfoUpdated;
        public event Action<JObject> GuildOperationUpdated;

        public JObject LatestDailyList { get; private set; }
        public JObject LatestDailyProgress { get; private set; }
        public JObject LatestDailyClaim { get; private set; }
        public JObject LatestGuildList { get; private set; }
        public JObject LatestGuildInfo { get; private set; }
        public JObject LatestGuildOperation { get; private set; }

        public OnlineMetaStateAdapter(GeneralServerNetworkManager network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _subscription = _network.DataReceivedStream.Subscribe(OnMessage);
        }

        public void Refresh()
        {
            if (disposed)
            {
                return;
            }

            _network.RequestDailyList();
            _network.RequestGuildList();
        }

        private void OnMessage(JObject message)
        {
            if (disposed)
            {
                return;
            }

            var type = OpenGSCore.MessageType.Normalize(message?["MessageType"]?.ToString());
            if (ShouldRefreshGuildList(type))
            {
                _network.RequestGuildList();
            }

            switch (type)
            {
                case OpenGSCore.MessageType.DailyListResponse:
                    LatestDailyList = message;
                    InvokeSafely(DailyListUpdated, message, nameof(DailyListUpdated));
                    break;
                case OpenGSCore.MessageType.DailyProgressResponse:
                    LatestDailyProgress = message;
                    InvokeSafely(DailyProgressUpdated, message, nameof(DailyProgressUpdated));
                    break;
                case OpenGSCore.MessageType.DailyClaimResponse:
                    LatestDailyClaim = message;
                    InvokeSafely(DailyClaimUpdated, message, nameof(DailyClaimUpdated));
                    _network.RequestDailyList();
                    break;
                case OpenGSCore.MessageType.GuildListResponse:
                    LatestGuildList = message;
                    InvokeSafely(GuildListUpdated, message, nameof(GuildListUpdated));
                    break;
                case OpenGSCore.MessageType.GuildInfoResponse:
                    LatestGuildInfo = message;
                    InvokeSafely(GuildInfoUpdated, message, nameof(GuildInfoUpdated));
                    break;
                case OpenGSCore.MessageType.GuildCreateResponse:
                case OpenGSCore.MessageType.GuildJoinResponse:
                case OpenGSCore.MessageType.GuildLeaveResponse:
                case OpenGSCore.MessageType.GuildRoleResponse:
                case OpenGSCore.MessageType.GuildInviteResponse:
                case OpenGSCore.MessageType.GuildKickResponse:
                case OpenGSCore.MessageType.GuildInviteNotification:
                case OpenGSCore.MessageType.GuildKickNotification:
                case OpenGSCore.MessageType.GuildChatNotification:
                    LatestGuildOperation = message;
                    InvokeSafely(GuildOperationUpdated, message, nameof(GuildOperationUpdated));
                    break;
            }
        }

        private static bool ShouldRefreshGuildList(string type)
        {
            return type == OpenGSCore.MessageType.GuildCreateResponse ||
                   type == OpenGSCore.MessageType.GuildJoinResponse ||
                   type == OpenGSCore.MessageType.GuildLeaveResponse ||
                   type == OpenGSCore.MessageType.GuildRoleResponse ||
                   type == OpenGSCore.MessageType.GuildKickResponse;
        }

        private static void InvokeSafely(Action<JObject> handlers, JObject message, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<JObject> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[OnlineMetaStateAdapter] {eventName} subscriber failed: {ex}");
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            _subscription.Dispose();
            DailyListUpdated = null;
            DailyProgressUpdated = null;
            DailyClaimUpdated = null;
            GuildListUpdated = null;
            GuildInfoUpdated = null;
            GuildOperationUpdated = null;
            LatestDailyList = null;
            LatestDailyProgress = null;
            LatestDailyClaim = null;
            LatestGuildList = null;
            LatestGuildInfo = null;
            LatestGuildOperation = null;
        }
    }
}
