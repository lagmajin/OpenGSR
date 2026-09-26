using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Client-side cache for daily and guild responses. UI screens can subscribe
    /// here without coupling themselves to the transport manager or JSON routing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineMetaStateStore : MonoBehaviour
    {
        public static OnlineMetaStateStore Instance { get; private set; }

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

        private ClientNetworkManager network;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            network = GetComponent<ClientNetworkManager>();
            if (network == null)
            {
                Debug.LogWarning("[OnlineMetaStateStore] ClientNetworkManager was not found.");
                if (Instance == this)
                {
                    Instance = null;
                }
                Destroy(this);
                return;
            }

            network.DailyListResponseReceived += OnDailyList;
            network.DailyProgressResponseReceived += OnDailyProgress;
            network.DailyClaimResponseReceived += OnDailyClaim;
            network.GuildListResponseReceived += OnGuildList;
            network.GuildInfoResponseReceived += OnGuildInfo;
            network.GuildRoleResponseReceived += OnGuildOperation;
            network.GuildCreateResponseReceived += OnGuildOperation;
            network.GuildJoinResponseReceived += OnGuildOperation;
            network.GuildLeaveResponseReceived += OnGuildOperation;
            network.GuildInviteResponseReceived += OnGuildOperation;
            network.GuildKickResponseReceived += OnGuildOperation;
            network.GuildInviteNotificationReceived += OnGuildOperation;
            network.GuildKickNotificationReceived += OnGuildOperation;
            network.GuildChatNotificationReceived += OnGuildOperation;
        }

        private void OnDestroy()
        {
            if (network != null)
            {
                network.DailyListResponseReceived -= OnDailyList;
                network.DailyProgressResponseReceived -= OnDailyProgress;
                network.DailyClaimResponseReceived -= OnDailyClaim;
                network.GuildListResponseReceived -= OnGuildList;
                network.GuildInfoResponseReceived -= OnGuildInfo;
                network.GuildRoleResponseReceived -= OnGuildOperation;
                network.GuildCreateResponseReceived -= OnGuildOperation;
                network.GuildJoinResponseReceived -= OnGuildOperation;
                network.GuildLeaveResponseReceived -= OnGuildOperation;
                network.GuildInviteResponseReceived -= OnGuildOperation;
                network.GuildKickResponseReceived -= OnGuildOperation;
                network.GuildInviteNotificationReceived -= OnGuildOperation;
                network.GuildKickNotificationReceived -= OnGuildOperation;
                network.GuildChatNotificationReceived -= OnGuildOperation;
            }

            LatestDailyList = null;
            LatestDailyProgress = null;
            LatestDailyClaim = null;
            LatestGuildList = null;
            LatestGuildInfo = null;
            LatestGuildOperation = null;
            DailyListUpdated = null;
            DailyProgressUpdated = null;
            DailyClaimUpdated = null;
            GuildListUpdated = null;
            GuildInfoUpdated = null;
            GuildOperationUpdated = null;

            if (Instance == this) Instance = null;
        }

        public void Refresh()
        {
            network?.RequestDailyList();
            network?.RequestGuildList();
        }

        public void Clear()
        {
            LatestDailyList = null;
            LatestDailyProgress = null;
            LatestDailyClaim = null;
            LatestGuildList = null;
            LatestGuildInfo = null;
            LatestGuildOperation = null;
        }

        private void OnDailyList(JObject message)
        {
            LatestDailyList = message;
            InvokeSafely(DailyListUpdated, message, nameof(DailyListUpdated));
        }

        private void OnDailyProgress(JObject message)
        {
            LatestDailyProgress = message;
            InvokeSafely(DailyProgressUpdated, message, nameof(DailyProgressUpdated));
        }

        private void OnDailyClaim(JObject message)
        {
            LatestDailyClaim = message;
            InvokeSafely(DailyClaimUpdated, message, nameof(DailyClaimUpdated));
            network?.RequestDailyList();
        }

        private void OnGuildList(JObject message)
        {
            LatestGuildList = message;
            InvokeSafely(GuildListUpdated, message, nameof(GuildListUpdated));
        }

        private void OnGuildInfo(JObject message)
        {
            LatestGuildInfo = message;
            InvokeSafely(GuildInfoUpdated, message, nameof(GuildInfoUpdated));
        }

        private void OnGuildOperation(JObject message)
        {
            LatestGuildOperation = message;
            InvokeSafely(GuildOperationUpdated, message, nameof(GuildOperationUpdated));
            if (ShouldRefreshGuildList(message))
            {
                network?.RequestGuildList();
            }
        }

        private static bool ShouldRefreshGuildList(JObject message)
        {
            var type = OpenGSCore.MessageType.Normalize(message?["MessageType"]?.ToString());
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
                    Debug.LogError($"[OnlineMetaStateStore] {eventName} subscriber failed: {ex}");
                }
            }
        }
    }
}
