using System.Collections;
using System.Threading;
using UnityEngine;

#pragma warning disable 0414

namespace OpenGS
{
    public class MissionResultScene : AbstractScene
    {
        [SerializeField] private float showTime = 3.0f;
        private SynchronizationContext mainThread;

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;
        }

        private void Start()
        {
            StartCoroutine(WaitCoroutine());
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
    }
}
