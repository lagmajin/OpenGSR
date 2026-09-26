#nullable enable
using System.Collections;
using System.Threading;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineMissionWaitRoom : AbstractNonBattleScene
    {
        [SerializeField] [Required] private MissionWaitRoomMediateObject mediateObject = null!;
        [SerializeField] private QuestAndMissionSceneStorage missionSceneStorage;
        [SerializeField] private int maxPlayers = 3;

        private SynchronizationContext mainThread = null!;
        private GeneralServerNetworkManager? networkManager;
        private MatchRoomManager? matchRoomManager;
        private bool missionTransitionRequested;
        private Coroutine dependencyRetryRoutine;
        private bool missionServerSubscribed;

        protected override void Awake()
        {
            base.Awake();
            maxPlayers = Mathf.Clamp(maxPlayers, 1, 64);
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current ?? new SynchronizationContext();

            if (missionSceneStorage == null)
            {
                missionSceneStorage = FindFirstObjectByType<QuestAndMissionSceneStorage>();
            }
        }

        private void Start()
        {
            ResolveDependencies();
            missionTransitionRequested = false;
            SubscribeToMissionServer();
            dependencyRetryRoutine = StartCoroutine(RetryMissionServerSubscription());
        }

        protected override void OnDestroy()
        {
            if (dependencyRetryRoutine != null)
            {
                StopCoroutine(dependencyRetryRoutine);
                dependencyRetryRoutine = null;
            }

            base.OnDestroy();
        }

        private void ResolveDependencies()
        {
            try
            {
                matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                Debug.LogWarning("[OnlineMissionWaitRoom] Failed to resolve dependencies.");
            }
        }

        private void SubscribeToMissionServer()
        {
            if (networkManager == null || missionServerSubscribed) return;

            networkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Where(json =>
                {
                    var msg = OpenGSCore.MessageType.Normalize(json?["MessageType"]?.ToString());
                    return msg == OpenGSCore.MessageType.WaitRoomPlayerList
                        || msg == OpenGSCore.MessageType.WaitRoomStartCountdown
                        || msg == OpenGSCore.MessageType.GameStartNotification;
                })
                .Subscribe(OnMissionServerMessage)
                .AddTo(this);
            missionServerSubscribed = true;
        }

        private IEnumerator RetryMissionServerSubscription()
        {
            while (isActiveAndEnabled && !missionServerSubscribed)
            {
                ResolveDependencies();
                SubscribeToMissionServer();
                if (missionServerSubscribed)
                {
                    dependencyRetryRoutine = null;
                    yield break;
                }

                yield return null;
            }

            dependencyRetryRoutine = null;
        }

        private void OnMissionServerMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = OpenGSCore.MessageType.Normalize(json?["MessageType"]?.ToString());

            switch (messageType)
            {
                case OpenGSCore.MessageType.WaitRoomPlayerList:
                    HandlePlayerList(json);
                    break;
                case OpenGSCore.MessageType.GameStartNotification:
                    OnMissionStart();
                    break;
            }
        }

        private void HandlePlayerList(JObject? json)
        {
            if (json == null)
            {
                return;
            }

            var count = ReadPlayerCount(json["Players"]?["Count"]);
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? "";
            var roomName = json["RoomName"]?.ToString() ?? "";

            if (!string.IsNullOrWhiteSpace(roomId))
            {
                MissionRoomManager.Instance.SetRoomId(roomId);
            }

            Debug.Log($"[OnlineMissionWaitRoom] Player list updated: {count} players in room {roomName}");
        }

        private int ReadPlayerCount(JToken token)
        {
            try
            {
                return Mathf.Clamp(token?.ToObject<int>() ?? 0, 0, maxPlayers);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OnlineMissionWaitRoom] Invalid player count: {ex.Message}");
                return 0;
            }
        }

        private void OnMissionStart()
        {
            if (missionTransitionRequested)
            {
                return;
            }

            if (MissionRoomManager.Instance == null)
            {
                Debug.LogWarning("[OnlineMissionWaitRoom] Mission start received before room state was initialized.");
                return;
            }

            var missionIndex = MissionRoomManager.Instance.MissionIndex();
            var questIndex = MissionRoomManager.Instance.QuestIndex();

            string nextScene;
            if (MissionRoomManager.Instance.IsQuestMode())
            {
                nextScene = ResolveQuestScene(questIndex);
            }
            else
            {
                nextScene = ResolveMissionScene(missionIndex);
            }

            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                missionTransitionRequested = true;
                RequestSceneTransition(nextScene, "OnlineMissionWaitRoomToMission");
            }
            else
            {
                Debug.LogWarning("[OnlineMissionWaitRoom] Mission start received, but no destination scene is configured.");
            }
        }

        private string ResolveMissionScene(int missionIndex)
        {
            if (missionSceneStorage == null) return "";

            return missionIndex switch
            {
                1 => (string)missionSceneStorage.Mission1Scene(),
                2 => (string)missionSceneStorage.Mission2Scene(),
                3 => (string)missionSceneStorage.Mission3Scene(),
                4 => (string)missionSceneStorage.Mission4Scene(),
                5 => (string)missionSceneStorage.Mission5Scene(),
                _ => (string)missionSceneStorage.Mission1Scene()
            };
        }

        private string ResolveQuestScene(int questIndex)
        {
            if (missionSceneStorage == null) return "";

            return questIndex switch
            {
                1 => (string)missionSceneStorage.Quest1Scene(),
                2 => (string)missionSceneStorage.Quest2Scene(),
                3 => (string)missionSceneStorage.Quest3Scene(),
                _ => (string)missionSceneStorage.Quest1Scene()
            };
        }

        public void SendReady()
        {
            SendReadyState(true);
        }

        public void SendUnready()
        {
            SendReadyState(false);
        }

        private void SendReadyState(bool ready)
        {
            if (networkManager == null || MissionRoomManager.Instance == null)
            {
                Debug.LogWarning("[OnlineMissionWaitRoom] Cannot change ready state before dependencies are ready.");
                return;
            }

            var roomId = MissionRoomManager.Instance.RoomId();
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogWarning("[OnlineMissionWaitRoom] Cannot change ready state without a room ID.");
                return;
            }

            var json = new Newtonsoft.Json.Linq.JObject
            {
                ["MessageType"] = ready
                    ? OpenGSCore.MessageType.WaitRoomPlayerReady
                    : OpenGSCore.MessageType.WaitRoomPlayerUnready,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = roomId
            };

            networkManager.SendMessage(json);
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? "local_player" : profile.GlobalUserId;
        }

        public void BackToMissionLobby()
        {
            var lobbyScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            RequestSceneTransition(lobbyScene, "OnlineMissionWaitRoomToMissionLobby");
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }
    }
}
