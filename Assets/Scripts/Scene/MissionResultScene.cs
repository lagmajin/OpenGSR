using System;
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

        /// <summary>
        /// Puts the server's ruling on screen.
        /// <para>
        /// It used to build a result here, on this client, from the wait room's
        /// player list and an evaluator chosen by mode. That is the client deciding
        /// how a mission went, out of data the server owns, so a mission the
        /// server had already failed could be shown as a success. The result is
        /// the room's, and this only displays it.
        /// </para>
        /// </summary>
        private void EvaluateMissionResult()
        {
            var manager = MatchRoomManager();
            var rule = manager?.OnlineMatchRoom?.Rule as MissionMatchRule
                ?? manager?.OfflineMatchRoom?.Rule as MissionMatchRule;
            if (rule == null)
            {
                Debug.LogWarning(
                    "[MissionResultScene] The room is not being played by a mission rule.");
                return;
            }

            var objectiveDone = rule.ObjectiveCount > 0 && rule.ObjectiveProgress >= rule.ObjectiveCount;
            var outOfLives = rule.LifeCount > 0 && rule.LifeRemaining <= 0;
            isSuccess = objectiveDone || !outOfLives;

            missionResultPayload = new JObject
            {
                ["ResultType"] = "Mission",
                ["Result"] = objectiveDone ? "Clear" : (outOfLives ? "Fail" : "TimeUp"),
                ["Success"] = isSuccess,
                ["LifeRemaining"] = rule.LifeRemaining,
                ["ObjectiveProgress"] = rule.ObjectiveProgress,
                ["ObjectiveCount"] = rule.ObjectiveCount
            };
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
