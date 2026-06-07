using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineMissionWaitRoom : AbstractNonBattleScene
    {
        [SerializeField] [Required] private MissionWaitRoomMediateObject mediateObject;
        [SerializeField] private QuestAndMissionSceneStorage missionSceneStorage;
        [SerializeField] private int maxPlayers = 3;

        private SynchronizationContext mainThread;
        private GeneralServerNetworkManager? networkManager;
        private MatchRoomManager? matchRoomManager;

        protected override void Awake()
        {
            base.Awake();
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
            SubscribeToMissionServer();
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
            if (networkManager == null) return;

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
        }

        private void OnMissionServerMessage(JObject json)
        {
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

        private void HandlePlayerList(JObject json)
        {
            var count = json["Players"]?["Count"]?.ToObject<int>() ?? 0;
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? "";
            var roomName = json["RoomName"]?.ToString() ?? "";

            Debug.Log($"[OnlineMissionWaitRoom] Player list updated: {count} players in room {roomName}");
        }

        private void OnMissionStart()
        {
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
                RequestSceneTransition(nextScene, "OnlineMissionWaitRoomToMission");
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
            var json = new Newtonsoft.Json.Linq.JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.WaitRoomPlayerReady,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = MissionRoomManager.Instance.RoomName()
            };

            networkManager?.SendMessage(json);
        }

        public void SendUnready()
        {
            var json = new Newtonsoft.Json.Linq.JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.WaitRoomPlayerUnready,
                ["PlayerID"] = ResolveLocalPlayerId(),
                ["RoomID"] = MissionRoomManager.Instance.RoomName()
            };

            networkManager?.SendMessage(json);
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