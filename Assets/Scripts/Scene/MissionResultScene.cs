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
        private Coroutine waitCoroutine;

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        protected override void Awake()
        {
            base.Awake();
            showTime = float.IsFinite(showTime) ? Mathf.Max(0.1f, showTime) : 3f;
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
            waitCoroutine = StartCoroutine(WaitCoroutine());
            ShowResultUI();
        }

        protected override void OnDestroy()
        {
            if (waitCoroutine != null)
            {
                StopCoroutine(waitCoroutine);
                waitCoroutine = null;
            }
            base.OnDestroy();
        }

        private void ShowResultUI()
        {
            if (uiDirector != null)
            {
                uiDirector.ShowMissionResult(ReadNonNegativeInt("LifeRemaining"),
                    ReadNonNegativeInt("Score"),
                    isSuccess);
            }
        }

        private IEnumerator WaitCoroutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, showTime));
            waitCoroutine = null;
            GoToMissionLobby();
        }

        private void GoToMissionLobby()
        {
            var nextScene = generalSceneMasterData != null
                ? generalSceneMasterData.MissionLobbyScene()
                : GeneralSceneMasterData.Instance().MissionLobbyScene();

            RequestSceneTransition(nextScene, "MissionResultToMissionLobby");
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
            return ReadNonNegativeInt("LifeRemaining");
        }

        public int GetScore()
        {
            return ReadNonNegativeInt("Score");
        }

        private int ReadNonNegativeInt(string key)
        {
            try
            {
                return Mathf.Max(0, missionResultPayload[key]?.ToObject<int>() ?? 0);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MissionResultScene] Invalid result value for {key}: {ex.Message}");
                return 0;
            }
        }

        public bool IsSuccess()
        {
            return isSuccess;
        }
    }
}
