#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public enum EMissionPhase
    {
        WaitingForPlayers,
        Countdown,
        InProgress,
        Completed,
        Failed
    }

    [DisallowMultipleComponent]
    public class MissionWaitRoomController : AbstractSceneController
    {
        private SynchronizationContext mainThread = null!;
        private MatchRoomManager? matchRoomManager;
        private GeneralServerNetworkManager? generalServer;
        [SerializeField] private GeneralSceneMasterData? generalSceneMasterData;

        [ShowInInspector, ReadOnly]
        private EMissionPhase currentPhase = EMissionPhase.WaitingForPlayers;

        [ShowInInspector, ReadOnly]
        private string currentRoomId = "";

        [ShowInInspector, ReadOnly]
        private int playerLifeCount = 3;
        private bool missionRequestSent;
        private bool missionTransitionRequested;
        private readonly SerialDisposable missionSubscription = new();

        private readonly Subject<JObject> onCompleteNotification = new();
        private readonly Subject<JObject> onFailNotification = new();
        private readonly Subject<JObject> onPlayerJoined = new();
        private readonly Subject<JObject> onPlayerLeft = new();

        public IObservable<JObject> OnCompleteStream => onCompleteNotification.AsObservable();
        public IObservable<JObject> OnFailStream => onFailNotification.AsObservable();
        public IObservable<JObject> OnPlayerJoinedStream => onPlayerJoined.AsObservable();
        public IObservable<JObject> OnPlayerLeftStream => onPlayerLeft.AsObservable();

        private void Awake()
        {
            mainThread = SynchronizationContext.Current ?? new SynchronizationContext();
            if (generalSceneMasterData == null)
            {
                generalSceneMasterData = UnityEngine.Object.FindFirstObjectByType<GeneralSceneMasterData>();
                if (generalSceneMasterData == null)
                {
                    generalSceneMasterData = Resources.Load<GeneralSceneMasterData>("MasterData/GeneralSceneMasterData")
                        ?? Resources.Load<GeneralSceneMasterData>("MasterData/Scene/GeneralSceneMasterData");
                }
            }
        }

        private void Start()
        {
            ResolveDependencies();
            InitializeMissionRoom();
        }

        private void ResolveDependencies()
        {
            try
            {
                matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                generalServer = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MissionWaitRoomController] Failed to resolve dependencies: {ex.Message}");
            }
        }

        public void InitializeMissionRoom()
        {
            var manager = MissionRoomManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[MissionWaitRoomController] MissionRoomManager not available.");
                return;
            }

            var roomName = manager.RoomName();
            var capacity = manager.Capacity();
            currentRoomId = Guid.NewGuid().ToString("N");
            missionRequestSent = false;
            missionTransitionRequested = false;
            currentPhase = EMissionPhase.WaitingForPlayers;

            Debug.Log($"[MissionWaitRoomController] Initialized mission room: {roomName}, capacity: {capacity}");

            if (generalServer != null)
            {
                SubscribeToMissionServer();
            }
        }

        private void SubscribeToMissionServer()
        {
            missionSubscription.Disposable = null;
            if (generalServer == null) return;

            missionSubscription.Disposable = generalServer.DataReceivedStream
                .ObserveOnMainThread()
                .Where(json =>
                {
                    var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
                    return messageType == MessageType.MissionStartNotification
                        || messageType == MessageType.MissionCompleteNotification
                        || messageType == MessageType.MissionFailedNotification
                        || messageType == MessageType.WaitRoomPlayerList;
                })
                .Subscribe(json =>
                {
                    if (json != null)
                    {
                        OnMissionServerMessage(json);
                    }
                })
                ;
        }

        private void OnMissionServerMessage(JObject json)
        {
            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());

            switch (messageType)
            {
                case MessageType.WaitRoomPlayerList:
                    HandlePlayerList(json);
                    break;
                case MessageType.MissionStartNotification:
                    currentPhase = EMissionPhase.InProgress;
                    Debug.Log("[MissionWaitRoomController] Mission started.");
                    break;
                case MessageType.MissionCompleteNotification:
                    if (currentPhase == EMissionPhase.Completed) break;
                    currentPhase = EMissionPhase.Completed;
                    onCompleteNotification.OnNext(json);
                    Debug.Log("[MissionWaitRoomController] Mission completed.");
                    break;
                case MessageType.MissionFailedNotification:
                    if (currentPhase == EMissionPhase.Failed) break;
                    currentPhase = EMissionPhase.Failed;
                    onFailNotification.OnNext(json);
                    Debug.Log("[MissionWaitRoomController] Mission failed.");
                    break;
            }
        }

        private void HandlePlayerList(JObject json)
        {
            if (json["Players"] is JArray players)
            {
                onPlayerJoined.OnNext(json);
                Debug.Log($"[MissionWaitRoomController] Player count: {players.Count}");
            }
        }

        public async Task<bool> StartMissionAsync(CancellationToken ct = default)
        {
            if (missionRequestSent)
            {
                return false;
            }

            if (matchRoomManager?.WaitRoom == null)
            {
                Debug.LogWarning("[MissionWaitRoomController] WaitRoom not available for mission start.");
                return false;
            }

            var room = matchRoomManager.WaitRoom;
            if (room.PlayerCount == 0)
            {
                Debug.LogWarning("[MissionWaitRoomController] No players in room.");
                return false;
            }

            currentPhase = EMissionPhase.Countdown;
            missionRequestSent = true;

            var request = new JObject
            {
                ["MessageType"] = MessageType.GameStartRequest,
                ["RoomID"] = currentRoomId,
                ["PlayerID"] = ResolveLocalPlayerId()
            };

            if (generalServer != null)
            {
                generalServer.SendMessage(request);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                missionRequestSent = false;
                if (currentPhase == EMissionPhase.Countdown)
                {
                    currentPhase = EMissionPhase.WaitingForPlayers;
                }
                return false;
            }
        }

        public void UpdateLifeCount(int life)
        {
            playerLifeCount = Math.Max(0, life);
            Debug.Log($"[MissionWaitRoomController] Life updated: {playerLifeCount}");
        }

        public void AddBotToMission()
        {
            if (MissionRoomManager.Instance == null)
            {
                Debug.LogWarning("[MissionWaitRoomController] MissionRoomManager is not ready.");
                return;
            }

            var bot = new PlayerInfo($"bot_{Guid.NewGuid():N}", $"Bot_{MissionRoomManager.Instance.Capacity() + 1}")
            {
                IsBot = true
            };

            matchRoomManager?.WaitRoom?.AddPlayer(bot);
            Debug.Log("[MissionWaitRoomController] Bot added to mission room.");
        }

        public void RemoveAllBots()
        {
            matchRoomManager?.WaitRoom?.RemoveAllBotPlayer();
            Debug.Log("[MissionWaitRoomController] All bots removed from mission room.");
        }

        public void ProceedToMissionScene()
        {
            if (missionTransitionRequested)
            {
                return;
            }

            if (MissionRoomManager.Instance == null)
            {
                Debug.LogWarning("[MissionWaitRoomController] MissionRoomManager is not ready.");
                return;
            }

            var missionIndex = MissionRoomManager.Instance.MissionIndex();
            var questIndex = MissionRoomManager.Instance.QuestIndex();

            string nextScene;
            if (MissionRoomManager.Instance.IsQuestMode())
            {
                nextScene = ResolveQuestScene(missionIndex);
            }
            else
            {
                nextScene = ResolveMissionScene(missionIndex);
            }

            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                missionTransitionRequested = true;
                RequestSceneTransition(nextScene, "MissionWaitRoomToMission");
            }
            else
            {
                Debug.LogWarning("[MissionWaitRoomController] No valid scene configured for selected mission.");
            }
        }

        private static string ResolveMissionScene(int missionIndex)
        {
            var storage = UnityEngine.Object.FindFirstObjectByType<QuestAndMissionSceneStorage>();
            if (storage == null) return "";

            return missionIndex switch
            {
                1 => (string)storage.Mission1Scene(),
                2 => (string)storage.Mission2Scene(),
                3 => (string)storage.Mission3Scene(),
                4 => (string)storage.Mission4Scene(),
                5 => (string)storage.Mission5Scene(),
                _ => (string)storage.Mission1Scene()
            };
        }

        private static string ResolveQuestScene(int questIndex)
        {
            var storage = UnityEngine.Object.FindFirstObjectByType<QuestAndMissionSceneStorage>();
            if (storage == null) return "";

            return questIndex switch
            {
                1 => (string)storage.Quest1Scene(),
                2 => (string)storage.Quest2Scene(),
                3 => (string)storage.Quest3Scene(),
                _ => (string)storage.Quest1Scene()
            };
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? "local_player" : profile.GlobalUserId;
        }

        public EMissionPhase CurrentPhase => currentPhase;
        public string CurrentRoomId => currentRoomId;
        public int PlayerLifeCount => playerLifeCount;

        public void BackToMissionLobby()
        {
            var lobbyScene = generalSceneMasterData != null
                ? generalSceneMasterData.MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            RequestSceneTransition(lobbyScene, "MissionWaitRoomToMissionLobby");
        }

        private void RequestSceneTransition(string nextSceneName, string reason)
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning($"[MissionWaitRoomController] Invalid transition target. reason={reason}");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[MissionWaitRoomController] Scene is not in build settings: {nextSceneName}. reason={reason}");
                return;
            }

            if (missionTransitionRequested)
            {
                Debug.Log($"[MissionWaitRoomController] Scene transition already requested. reason={reason}");
                return;
            }

            missionTransitionRequested = true;
            SceneManager.LoadScene(nextSceneName);
        }

        private void OnDestroy()
        {
            missionSubscription.Dispose();
            onCompleteNotification.Dispose();
            onFailNotification.Dispose();
            onPlayerJoined.Dispose();
            onPlayerLeft.Dispose();
        }
    }
}
