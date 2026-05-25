using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

#pragma warning disable 0414

namespace OpenGS
{
    public class GameSettingScene : AbstractScene, IGameSettingScene
    {
        [SerializeField] [Required] public GameSettingSceneMediateObject mediateObject;
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
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ApplyGameSetting();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitGame();
            }
        }

        private void OnApplicationQuit()
        {
        }

        private void ApplyGameSetting()
        {
            var manager = GameGeneralManager.GetInstance;
            Debug.Log("[GameSettingScene] ApplyGameSetting");
        }

        public override SynchronizationContext MainThread()
        {
            return mainThread ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        private void ExitGame()
        {
            Application.Quit();
        }
    }
}
