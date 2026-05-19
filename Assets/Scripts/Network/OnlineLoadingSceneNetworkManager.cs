using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineLoadingSceneNetworkManager : MonoBehaviour
    {
        private GeneralServerNetworkManager generalServerNetworkManager;
        private IOnlineLoadingScene onlineLoadingScene;
        private WaitRoomManager waitRoomManager;
        private OnlineLoadingManager onlineLoadingManager;
        private readonly SerialDisposable subscription = new SerialDisposable();
        private readonly HashSet<string> completedPlayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool enterMapAllowedReceived;

        private void Awake()
        {
            ResetState();
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            SubscribeToServer();
        }

        private void OnDisable()
        {
            subscription.Disposable = null;
        }

        public void SendLoadingSceneEntered()
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.ClientLoadingSceneEntered,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["AccountName"] = ResolveLocalPlayerName()
            };

            SendToServer(json);
        }

        public void SendMatchServerInfoRequest()
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.MatchServerInfoRequest,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = waitRoomManager?.WaitRoom?.RoomId ?? string.Empty
            };

            SendToServer(json);
        }

        public void SendLoadingStart()
        {
            SendLoadingState(MessageType.LoadingStarted, 0f, "loading-started");
        }

        public void SendLoadingProgress(float progress)
        {
            SendLoadingState(MessageType.LoadingProgress, Mathf.Clamp01(progress), "loading-progress");
        }

        public void SendLoadingComplete()
        {
            SendLoadingState(MessageType.LoadingCompleted, 1f, "loading-complete");
        }

        public void SendLoadingMessage(string message)
        {
            var json = new JObject
            {
                ["MessageType"] = MessageType.LoadingMessage,
                ["Message"] = message ?? string.Empty
            };
            SendToServer(json);
        }

        private void ResolveDependencies()
        {
            if (generalServerNetworkManager == null)
            {
                try
                {
                    generalServerNetworkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch
                {
                    generalServerNetworkManager = null;
                }
            }

            if (waitRoomManager == null)
            {
                try
                {
                    waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();
                }
                catch
                {
                    waitRoomManager = null;
                }
            }

            if (onlineLoadingManager == null)
            {
                try
                {
                    onlineLoadingManager = DependencyInjectionConfig.Resolve<OnlineLoadingManager>();
                }
                catch
                {
                    onlineLoadingManager = OnlineLoadingManager.Instance;
                }
            }

            if (onlineLoadingScene == null)
            {
                onlineLoadingScene = FindFirstObjectByType<OnlineLoadingScene>();
            }
        }

        private void SubscribeToServer()
        {
            if (generalServerNetworkManager == null)
            {
                return;
            }

            ResetState();
            subscription.Disposable = generalServerNetworkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(HandleServerMessage);
        }

        private void HandleServerMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (messageType == MessageType.AllowEnterMap)
            {
                enterMapAllowedReceived = true;
                onlineLoadingManager?.SetLoadingMessage(MessageType.AllowEnterMap);
                TryAllowEnterMap();
                return;
            }

            if (messageType == MessageType.MatchServerInfoResponse)
            {
                var ip = json["IP"]?.ToString() ?? json["IPAddress"]?.ToString();
                var port = json["Port"]?.ToObject<int?>();
                var udpPort = json["UdpPort"]?.ToObject<int?>();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    OnlineManager.Instance.MatchServerInfo.IP = ip;
                }

                if (port.HasValue)
                {
                    OnlineManager.Instance.MatchServerInfo.Port = port.Value;
                }

                if (udpPort.HasValue)
                {
                    OnlineManager.Instance.MatchServerInfo.UdpPort = udpPort.Value;
                }

                return;
            }

            if (messageType == MessageType.LoadingFailed)
            {
                ResolveDependencies();
                onlineLoadingManager?.SetLoadingMessage(json["Message"]?.ToString() ?? MessageType.LoadingFailed);
                onlineLoadingScene?.OnLoadingFailed();
                return;
            }

            if (messageType == MessageType.LoadingStartedNotification)
            {
                ResolveDependencies();
                var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(playerId))
                {
                    onlineLoadingManager?.AddLoadingPlayer(playerId);
                    onlineLoadingManager?.UpdateLoading(playerId, 0f);
                }

                onlineLoadingManager?.SetLoadingMessage(MessageType.LoadingStarted);
                return;
            }

            if (messageType == MessageType.LoadingProgressNotification)
            {
                ResolveDependencies();
                var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? string.Empty;
                var progress = Mathf.Clamp01(json["Progress"]?.ToObject<float>() ?? 0f);
                if (!string.IsNullOrWhiteSpace(playerId))
                {
                    onlineLoadingManager?.AddLoadingPlayer(playerId);
                    onlineLoadingManager?.UpdateLoading(playerId, progress);
                }

                return;
            }

            if (messageType == MessageType.LoadingCompletedNotification)
            {
                ResolveDependencies();
                var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(playerId))
                {
                    completedPlayerIds.Add(playerId);
                    onlineLoadingManager?.MarkPlayerLoaded(playerId);
                }

                if (onlineLoadingScene is OnlineLoadingScene concreteScene)
                {
                    concreteScene.OnMatchLoadingCompleted(playerId);
                }
                else
                {
                    onlineLoadingScene?.OnMatchLoadingCompleted();
                }

                TryAllowEnterMap();
            }
        }

        private void TryAllowEnterMap()
        {
            if (!enterMapAllowedReceived)
            {
                return;
            }

            ResolveDependencies();
            var expectedPlayers = Mathf.Max(1, waitRoomManager?.WaitRoom?.PlayerCount ?? 1);
            if (completedPlayerIds.Count < expectedPlayers)
            {
                Debug.Log($"[OnlineLoadingSceneNetworkManager] Waiting loading completed notifications: {completedPlayerIds.Count}/{expectedPlayers}");
                return;
            }

            if (onlineLoadingScene != null)
            {
                onlineLoadingScene.OnEnterMapAllowed();
            }
        }

        private void SendLoadingState(string messageType, float progress, string message)
        {
            var json = new JObject
            {
                ["MessageType"] = messageType,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["AccountName"] = ResolveLocalPlayerName(),
                ["Progress"] = progress,
                ["Message"] = message
            };

            SendToServer(json);
        }

        private void SendToServer(JObject json)
        {
            ResolveDependencies();

            if (generalServerNetworkManager == null)
            {
                Debug.LogWarning($"[OnlineLoadingSceneNetworkManager] No GeneralServerNetworkManager for {json?["MessageType"]}");
                return;
            }

            generalServerNetworkManager.SendMessage(json);
        }

        private void ResetState()
        {
            completedPlayerIds.Clear();
            enterMapAllowedReceived = false;
            onlineLoadingManager?.Clear();
        }

        private static string ResolveLocalPlayerId()
        {
            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            return string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        }

        private static string ResolveLocalPlayerName()
        {
            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            return string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        }
    }
}
