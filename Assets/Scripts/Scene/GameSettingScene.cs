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

        private void ApplyGameSetting()
        {
            try
            {
                var settingsManager = SettingsManager.Instance;
                var settings = settingsManager.GetSettings();
                settingsManager.ApplyGraphicsSettings(settings.Graphics);
                settingsManager.ApplySoundSettings(settings.Sound);
                settingsManager.ApplyControlSettings(settings.Control);
                Debug.Log("[GameSettingScene] Settings applied and saved.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSettingScene] Failed to apply settings: {ex.Message}");
            }
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
