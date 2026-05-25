

using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;


#pragma warning disable 0414

namespace OpenGS
{
    public class SkyFighterResultScene : AbstractScene
    {
        [SerializeField]
        private float showTime = 2.0f;

        public GameObject skyFighterCanvas;
        private SynchronizationContext mainThread;
        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            mainThread = SynchronizationContext.Current;

        }
        private void Start()
        {

        }

        protected override void Update()
        {
            base.Update();
            if (Input.anyKeyDown)
            {
                BacktoWaitRoom();
            }
        }

        private void BacktoWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;

            var nextScene = generalSceneMasterData != null
                ? generalSceneMasterData.LobbyScene()
                : GeneralSceneMasterData.Instance().LobbyScene();

            RequestSceneTransition(nextScene, "SkyFighterResultToLobby");

        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }
    }

}
