using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using TMPro;
using UniRx;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ロビー用のギルド操作パネル。
    /// クライアント側で guild の一覧確認、作成、参加、脱退、招待、キックを行う。
    /// </summary>
    public class GuildPanel : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField guildNameInput;
        [SerializeField] private TMP_InputField guildShortNameInput;
        [SerializeField] private TMP_InputField memberIdInput;
        [SerializeField] private TMP_InputField roleInput;
        [SerializeField] private TMP_InputField messageInput;

        [Header("Output")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI guildInfoText;

        private GeneralServerNetworkManager networkManager;
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private void Awake()
        {
            ResolveNetworkManager();
            if (roleInput != null && string.IsNullOrWhiteSpace(roleInput.text))
            {
                roleInput.text = "Member";
            }
        }

        private void OnEnable()
        {
            ResolveNetworkManager();
            SubscribeToNetwork();
        }

        private void OnDisable()
        {
            subscriptions.Clear();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RequestGuildList();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf)
            {
                RequestGuildList();
            }
        }

        public void RequestGuildList()
        {
            SendGuildMessage(MessageType.GuildListRequest);
        }

        public void RequestGuildInfo()
        {
            SendGuildMessage(MessageType.GuildInfoRequest, BuildGuildBasePayload());
        }

        public void RequestCreateGuild()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildCreateRequest;
            payload["LeaderId"] = GetCurrentPlayerId();
            SendGuildMessage(payload);
        }

        public void RequestJoinGuild()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildJoinRequest;
            payload["MemberId"] = GetMemberId();
            payload["Role"] = GetRole();
            SendGuildMessage(payload);
        }

        public void RequestLeaveGuild()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildLeaveRequest;
            payload["MemberId"] = GetMemberId();
            SendGuildMessage(payload);
        }

        public void RequestInviteGuild()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildInviteRequest;
            payload["InviterId"] = GetCurrentPlayerId();
            payload["TargetPlayerId"] = GetMemberId();
            payload["Message"] = GetMessage();
            SendGuildMessage(payload);
        }

        public void RequestKickGuild()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildKickRequest;
            payload["KickerId"] = GetCurrentPlayerId();
            payload["MemberId"] = GetMemberId();
            SendGuildMessage(payload);
        }

        public void RequestGuildChat()
        {
            var payload = BuildGuildBasePayload();
            payload["MessageType"] = MessageType.GuildChatRequest;
            payload["SenderID"] = GetCurrentPlayerId();
            payload["Message"] = GetMessage();
            SendGuildMessage(payload);
        }

        private void SubscribeToNetwork()
        {
            subscriptions.Clear();

            if (networkManager == null)
            {
                return;
            }

            var subscription = networkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(HandleGuildMessage);
            subscriptions.Add(subscription);
        }

        private void HandleGuildMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            switch (messageType)
            {
                case MessageType.GuildListResponse:
                    ShowGuildList(json["Guilds"] as JArray);
                    SetStatus("ギルド一覧を更新しました", false);
                    break;
                case MessageType.GuildInfoResponse:
                    ShowGuildDetail(json["Guild"] as JObject, json["ErrorMessage"]?.ToString());
                    SetStatus(json["Success"]?.ToObject<bool>() == true ? "ギルド情報を取得しました" : "ギルド情報の取得に失敗しました", json["Success"]?.ToObject<bool>() != true);
                    break;
                case MessageType.GuildCreateResponse:
                case MessageType.GuildJoinResponse:
                case MessageType.GuildLeaveResponse:
                case MessageType.GuildInviteResponse:
                case MessageType.GuildKickResponse:
                    UpdateFromActionResponse(json);
                    break;
                case MessageType.GuildInviteNotification:
                    SetStatus(FormatInviteNotification(json), false);
                    break;
                case MessageType.GuildKickNotification:
                    SetStatus(FormatKickNotification(json), true);
                    break;
                case MessageType.GuildChatNotification:
                    SetStatus(
                        json["Success"]?.ToObject<bool>() == true
                            ? FormatGuildChat(json)
                            : json["ErrorMessage"]?.ToString() ?? FormatGuildChat(json),
                        json["Success"]?.ToObject<bool>() != true);
                    break;
                case MessageType.ErrorNotification:
                    SetStatus(json["ErrorMessage"]?.ToString() ?? json["Message"]?.ToString() ?? "Error", true);
                    break;
            }
        }

        private void UpdateFromActionResponse(JObject json)
        {
            var success = json["Success"]?.ToObject<bool>() ?? false;
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            var guild = json["Guild"] as JObject;

            switch (messageType)
            {
                case MessageType.GuildCreateResponse:
                    if (success)
                    {
                        ShowGuildDetail(guild, null);
                    }
                    break;
                case MessageType.GuildJoinResponse:
                case MessageType.GuildLeaveResponse:
                    if (success)
                    {
                        RequestGuildInfo();
                    }
                    break;
            }

            SetStatus(success
                ? $"{messageType} succeeded"
                : json["ErrorMessage"]?.ToString() ?? $"{messageType} failed",
                !success);
        }

        private void ShowGuildList(JArray guilds)
        {
            if (guildInfoText == null)
            {
                return;
            }

            if (guilds == null || guilds.Count == 0)
            {
                guildInfoText.text = "ギルドがありません";
                return;
            }

            var lines = new List<string>();
            foreach (var token in guilds.OfType<JObject>())
            {
                lines.Add(FormatGuildSummary(token));
            }

            guildInfoText.text = string.Join("\n", lines);
        }

        private void ShowGuildDetail(JObject guild, string errorMessage)
        {
            if (guildInfoText == null)
            {
                return;
            }

            if (guild == null)
            {
                guildInfoText.text = string.IsNullOrWhiteSpace(errorMessage) ? "ギルド情報がありません" : errorMessage;
                return;
            }

            guildInfoText.text = FormatGuildDetail(guild);
        }

        private static string FormatGuildSummary(JObject guild)
        {
            var name = guild["GuildName"]?.ToString() ?? "Unknown";
            var shortName = guild["GuildShortName"]?.ToString() ?? name;
            var level = guild["Level"]?.ToObject<int>() ?? 1;
            var exp = guild["Experience"]?.ToObject<long>() ?? 0L;
            var leader = guild["LeaderId"]?.ToString() ?? "";
            var members = guild["MemberCount"]?.ToObject<int>() ?? 0;
            return $"{name} [{shortName}] Lv.{level} EXP {exp} Members {members} Leader {leader}";
        }

        private static string FormatGuildDetail(JObject guild)
        {
            var lines = new List<string>
            {
                $"Guild: {guild["GuildName"]?.ToString() ?? "Unknown"}",
                $"Short: {guild["GuildShortName"]?.ToString() ?? ""}",
                $"Leader: {guild["LeaderId"]?.ToString() ?? ""}",
                $"Level: {guild["Level"]?.ToObject<int>() ?? 1}",
                $"EXP: {guild["Experience"]?.ToObject<long>() ?? 0L}",
                $"Members: {guild["MemberCount"]?.ToObject<int>() ?? 0}"
            };

            if (guild["Members"] is JArray members && members.Count > 0)
            {
                lines.Add("Members:");
                foreach (var member in members.OfType<JObject>())
                {
                    var memberId = member["MemberId"]?.ToString() ?? "";
                    var role = member["Role"]?.ToString() ?? "Member";
                    lines.Add($" - {memberId} ({role})");
                }
            }

            return string.Join("\n", lines);
        }

        private static string FormatInviteNotification(JObject json)
        {
            var guildName = json["GuildName"]?.ToString() ?? "";
            var inviterId = json["InviterId"]?.ToString() ?? "";
            var target = json["TargetPlayerId"]?.ToString() ?? "";
            var message = json["Message"]?.ToString() ?? "";
            return $"招待: {target} -> {guildName} by {inviterId}{(string.IsNullOrWhiteSpace(message) ? string.Empty : $" / {message}")}";
        }

        private static string FormatKickNotification(JObject json)
        {
            var guildName = json["GuildName"]?.ToString() ?? "";
            var memberId = json["MemberId"]?.ToString() ?? "";
            var kickerId = json["KickerId"]?.ToString() ?? "";
            return $"キック: {memberId} was removed from {guildName} by {kickerId}";
        }

        private static string FormatGuildChat(JObject json)
        {
            var guildName = json["GuildName"]?.ToString() ?? "";
            var senderId = json["SenderID"]?.ToString() ?? json["SenderId"]?.ToString() ?? "";
            var message = json["Message"]?.ToString() ?? "";
            return $"[{guildName}] {senderId}: {message}";
        }

        private JObject BuildGuildBasePayload()
        {
            var payload = new JObject
            {
                ["GuildName"] = GetGuildName()
            };

            var shortName = GetGuildShortName();
            if (!string.IsNullOrWhiteSpace(shortName))
            {
                payload["GuildShortName"] = shortName;
            }

            return payload;
        }

        private void SendGuildMessage(string messageType)
        {
            SendGuildMessage(new JObject { ["MessageType"] = messageType });
        }

        private void SendGuildMessage(JObject payload)
        {
            if (payload == null)
            {
                return;
            }

            payload["MessageType"] = MessageType.Normalize(payload["MessageType"]?.ToString());
            ResolveNetworkManager()?.SendMessage(payload);
        }

        private string GetGuildName()
        {
            return guildNameInput != null ? guildNameInput.text.Trim() : string.Empty;
        }

        private string GetGuildShortName()
        {
            return guildShortNameInput != null ? guildShortNameInput.text.Trim() : string.Empty;
        }

        private string GetMemberId()
        {
            if (memberIdInput != null && !string.IsNullOrWhiteSpace(memberIdInput.text))
            {
                return memberIdInput.text.Trim();
            }

            return GetCurrentPlayerId();
        }

        private string GetRole()
        {
            return roleInput != null && !string.IsNullOrWhiteSpace(roleInput.text)
                ? roleInput.text.Trim()
                : "Member";
        }

        private string GetMessage()
        {
            return messageInput != null ? messageInput.text.Trim() : string.Empty;
        }

        private string GetCurrentPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? "local-player" : profile.GlobalUserId;
        }

        private void SetStatus(string message, bool isError)
        {
            if (statusText == null)
            {
                Debug.Log($"{(isError ? "[GuildPanel] ERROR" : "[GuildPanel] INFO")} {message}");
                return;
            }

            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }

        private GeneralServerNetworkManager ResolveNetworkManager()
        {
            if (networkManager != null)
            {
                return networkManager;
            }

            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GuildPanel: Failed to resolve GeneralServerNetworkManager: {ex.Message}");
            }

            return networkManager;
        }
    }
}
