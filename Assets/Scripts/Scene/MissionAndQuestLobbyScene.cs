using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MissionAndQuestLobbyScene : AbstractNonBattleScene
    {
        [SerializeField] private MissionAndQuestMediateObject mediateObject;
        private SynchronizationContext mainThread;

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void Start()
        {
            Debug.Log("[MissionAndQuestLobbyScene] Started");
        }

        private void Reset()
        {
        }

        protected override void Update()
        {
            base.Update();
        }

        [Button("バトルサーバへ移動")]
        public void ChangeToBattleLobby()
        {
            var missionLobbyScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();
            Debug.Log($"[MissionAndQuestLobbyScene] Switching to mission lobby: {missionLobbyScene}");
            RequestSceneTransition(missionLobbyScene, "MissionAndQuestToMissionLobby");
        }
    }
}
