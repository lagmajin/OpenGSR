using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class MissionLobbyScene : AbstractNonBattleScene
    {
        [SerializeField] [Required] private MissionLobbySceneMediateObject mediateObject;
        [SerializeField] private QuestAndMissionSceneStorage missionSceneStorage;
        private SynchronizationContext mainThread;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;

            if (missionSceneStorage == null)
            {
                missionSceneStorage = FindFirstObjectByType<QuestAndMissionSceneStorage>();
            }
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void Start()
        {
            Application.targetFrameRate = 30;
            Debug.Log("[MissionLobbyScene] Started");
        }

        private void Update()
        {
        }

        private void FilterRoom()
        {
        }

        private void SendChat(in string chat)
        {
            Debug.Log($"[MissionLobbyScene] Chat: {chat}");
        }

        public void CreateNewRoom()
        {
            Debug.Log("[MissionLobbyScene] CreateNewRoom");
        }

        public void EnterRoom()
        {
            ChangeToBattleLobby();
        }

        [Button("ミッション1")]
        public void EnterMission1() => LoadSceneFromStorage(missionSceneStorage?.Mission1Scene(), "MissionLobbyMission1");

        [Button("ミッション2")]
        public void EnterMission2() => LoadSceneFromStorage(missionSceneStorage?.Mission2Scene(), "MissionLobbyMission2");

        [Button("ミッション3")]
        public void EnterMission3() => LoadSceneFromStorage(missionSceneStorage?.Mission3Scene(), "MissionLobbyMission3");

        [Button("ミッション4")]
        public void EnterMission4() => LoadSceneFromStorage(missionSceneStorage?.Mission4Scene(), "MissionLobbyMission4");

        [Button("ミッション5")]
        public void EnterMission5() => LoadSceneFromStorage(missionSceneStorage?.Mission5Scene(), "MissionLobbyMission5");

        [Button("クエスト1")]
        public void EnterQuest1() => LoadSceneFromStorage(missionSceneStorage?.Quest1Scene(), "MissionLobbyQuest1");

        [Button("クエスト2")]
        public void EnterQuest2() => LoadSceneFromStorage(missionSceneStorage?.Quest2Scene(), "MissionLobbyQuest2");

        [Button("クエスト3")]
        public void EnterQuest3() => LoadSceneFromStorage(missionSceneStorage?.Quest3Scene(), "MissionLobbyQuest3");

        [Button("ロビーに戻る")]
        public void BackToConnectLobbyScene()
        {
            var lobbyScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().LobbyScene()
                : GeneralSceneMasterData.Instance().LobbyScene();

            RequestSceneTransition(lobbyScene, "MissionLobbyBackToLobby");
        }

        [Button("ミッション開始")]
        public void ChangeToBattleLobby()
        {
            EnterMission1();
        }

        private void LoadSceneFromStorage(SceneObject sceneObject, string reason)
        {
            var nextScene = sceneObject != null ? (string)sceneObject : string.Empty;
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogWarning($"[MissionLobbyScene] Scene is not configured for {reason}.");
                return;
            }

            Debug.Log($"[MissionLobbyScene] Loading scene: {nextScene}");
            RequestSceneTransition(nextScene, reason);
        }

    }

}
