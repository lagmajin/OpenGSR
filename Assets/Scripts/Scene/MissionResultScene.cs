using System.Collections;
using System.Threading;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UnityEngine;

#pragma warning disable 0414

namespace OpenGS
{
    public class MissionResultScene : AbstractScene
    {
        [SerializeField] private float showTime = 3.0f;
        [SerializeField] private MissionResultUIDirector uiDirector;

        private SynchronizationContext mainThread;
        private JObject missionResultPayload = new JObject();
        private bool isSuccess = false;

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;
            EvaluateMissionResult();
        }

        private void EvaluateMissionResult()
        {
            var matchRoomManager = MatchRoomManager();
            if (matchRoomManager?.WaitRoom == null)
            {
                return;
            }

            var players = matchRoomManager.WaitRoom.AllPlayers();
            var setting = matchRoomManager.WaitRoom.GetOrCreateSetting();

            var evaluator = OpenGSCore.MissionResultEvaluatorFactory.CreateEvaluator(setting?.Mode ?? EGameMode.Unknown);
            if (evaluator != null)
            {
                var result = evaluator.Evaluate(null, players);
                isSuccess = result["Success"]?.ToObject<bool>() ?? false;
                missionResultPayload = result;
            }
        }

        private void Start()
        {
            StartCoroutine(WaitCoroutine());
            ShowResultUI();
        }

        private void ShowResultUI()
        {
            if (uiDirector != null)
            {
                uiDirector.ShowMissionResult(missionResultPayload["LifeRemaining"]?.ToObject<int>() ?? 0,
                    missionResultPayload["Score"]?.ToObject<int>() ?? 0,
                    isSuccess);
            }
        }

        private IEnumerator WaitCoroutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, showTime));
            GoToMissionLobby();
        }

        private void GoToMissionLobby()
        {
            var nextScene = generalSceneMasterData != null
                ? generalSceneMasterData.MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            RequestSceneTransition(nextScene, "MissionResultToMissionLobby");
        }

        private void OnApplicationQuit()
        {
        }

        public MatchRoomManager MatchRoomManager()
        {
            try
            {
                return DependencyInjectionConfig.Resolve<MatchRoomManager>();
            }
            catch
            {
                return null;
            }
        }

        public int GetLifeRemaining()
        {
            return missionResultPayload["LifeRemaining"]?.ToObject<int>() ?? 0;
        }

        public int GetScore()
        {
            return missionResultPayload["Score"]?.ToObject<int>() ?? 0;
        }

        public bool IsSuccess()
        {
            return isSuccess;
        }
    }
}
