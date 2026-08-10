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

        public event Action<JObject> DailyListUpdated;
        public event Action<JObject> DailyClaimUpdated;
        public event Action<JObject> GuildListUpdated;
        public event Action<JObject> GuildInfoUpdated;
        public event Action<JObject> GuildOperationUpdated;

        public OnlineMetaStateAdapter(GeneralServerNetworkManager network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _subscription = _network.DataReceivedStream.Subscribe(OnMessage);
        }

        public void Refresh()
        {
            _network.RequestDailyList();
            _network.RequestGuildList();
        }

        private void OnMessage(JObject message)
        {
            var type = OpenGSCore.MessageType.Normalize(message?["MessageType"]?.ToString());
            switch (type)
            {
                case OpenGSCore.MessageType.DailyListResponse:
                    DailyListUpdated?.Invoke(message);
                    break;
                case OpenGSCore.MessageType.DailyClaimResponse:
                    DailyClaimUpdated?.Invoke(message);
                    break;
                case OpenGSCore.MessageType.GuildListResponse:
                    GuildListUpdated?.Invoke(message);
                    break;
                case OpenGSCore.MessageType.GuildInfoResponse:
                    GuildInfoUpdated?.Invoke(message);
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
                    GuildOperationUpdated?.Invoke(message);
                    break;
            }
        }

        public void Dispose() => _subscription.Dispose();
    }
}
