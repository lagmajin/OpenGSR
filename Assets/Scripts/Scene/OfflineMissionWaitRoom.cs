using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OfflineMissionWaitRoom : AbstractScene
    {
        [SerializeField] [Required] private MissionWaitRoomMediateObject mediateObject;
        [SerializeField] private QuestAndMissionSceneStorage missionSceneStorage;
        [SerializeField] private GameObject missionSelectDialog;

        private SynchronizationContext mainThread;
        private bool isRoomOwner;
        private bool isQuestMode;
        private int selectedMissionIndex = 1;
        private int selectedQuestIndex = 1;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;

            if (missionSceneStorage == null)
            {
                missionSceneStorage = FindFirstObjectByType<QuestAndMissionSceneStorage>();
            }
        }

        private void Start()
        {
            Application.targetFrameRate = 30;
            Debug.Log("[OfflineMissionWaitRoom] Started");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMissionLobby();
            }
        }

        private void OnApplicationQuit()
        {
            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.SaveDebugMissionSelect();
            }
        }

        private void AppointedRoomOwner()
        {
            if (isRoomOwner)
            {
                return;
            }

            isRoomOwner = true;
            MissionRoomManager.Instance.CreateNewRoom("OfflineMissionRoom");
            Debug.Log("[OfflineMissionWaitRoom] Room owner appointed.");
        }

        public void ShowMissionSelectDialog()
        {
            AppointedRoomOwner();

            if (missionSelectDialog != null)
            {
                missionSelectDialog.SetActive(true);
            }

            Debug.Log("[OfflineMissionWaitRoom] Mission select dialog shown.");
        }

        public void MissionDifficlucyChanged()
        {
            if (isQuestMode)
            {
                MissionRoomManager.Instance.SetQuestIndex(selectedQuestIndex);
            }
            else
            {
                MissionRoomManager.Instance.SetMissionIndex(selectedMissionIndex);
            }

            Debug.Log($"[OfflineMissionWaitRoom] Difficulty changed. mode={(isQuestMode ? "Quest" : "Mission")}, mission={selectedMissionIndex}, quest={selectedQuestIndex}");
        }

        [Button("ミッション1")]
        public void SelectMission1() => SelectMission(1);

        [Button("ミッション2")]
        public void SelectMission2() => SelectMission(2);

        [Button("ミッション3")]
        public void SelectMission3() => SelectMission(3);

        [Button("ミッション4")]
        public void SelectMission4() => SelectMission(4);

        [Button("ミッション5")]
        public void SelectMission5() => SelectMission(5);

        [Button("クエスト1")]
        public void SelectQuest1() => SelectQuest(1);

        [Button("クエスト2")]
        public void SelectQuest2() => SelectQuest(2);

        [Button("クエスト3")]
        public void SelectQuest3() => SelectQuest(3);

        [Button("ミッションへ進む")]
        public void EnterMission()
        {
            var nextScene = ResolveSelectedScene();
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogWarning("[OfflineMissionWaitRoom] Mission scene is not configured.");
                return;
            }

            MissionDifficlucyChanged();
            RequestSceneTransition(nextScene, "OfflineMissionWaitRoomEnterMission");
        }

        [Button("ミッションロビーへ戻る")]
        public void BackToMissionLobby()
        {
            var missionLobbyScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            RequestSceneTransition(missionLobbyScene, "OfflineMissionWaitRoomBackToMissionLobby");
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void SelectMission(int missionIndex)
        {
            isQuestMode = false;
            selectedMissionIndex = Mathf.Max(1, missionIndex);
            MissionRoomManager.Instance.SetMissionIndex(selectedMissionIndex);
            Debug.Log($"[OfflineMissionWaitRoom] Selected mission {selectedMissionIndex}.");
        }

        private void SelectQuest(int questIndex)
        {
            isQuestMode = true;
            selectedQuestIndex = Mathf.Max(1, questIndex);
            MissionRoomManager.Instance.SetQuestIndex(selectedQuestIndex);
            Debug.Log($"[OfflineMissionWaitRoom] Selected quest {selectedQuestIndex}.");
        }

        private string ResolveSelectedScene()
        {
            if (missionSceneStorage == null)
            {
                missionSceneStorage = FindFirstObjectByType<QuestAndMissionSceneStorage>();
            }

            if (missionSceneStorage == null)
            {
                return GeneralSceneMasterData.Instance().MissionLobbyScene();
            }

            if (isQuestMode)
            {
                return selectedQuestIndex switch
                {
                    1 => (string)missionSceneStorage.Quest1Scene(),
                    2 => (string)missionSceneStorage.Quest2Scene(),
                    3 => (string)missionSceneStorage.Quest3Scene(),
                    _ => (string)missionSceneStorage.Quest1Scene()
                };
            }

            return selectedMissionIndex switch
            {
                1 => (string)missionSceneStorage.Mission1Scene(),
                2 => (string)missionSceneStorage.Mission2Scene(),
                3 => (string)missionSceneStorage.Mission3Scene(),
                4 => (string)missionSceneStorage.Mission4Scene(),
                5 => (string)missionSceneStorage.Mission5Scene(),
                _ => (string)missionSceneStorage.Mission1Scene()
            };
        }
    }
}
