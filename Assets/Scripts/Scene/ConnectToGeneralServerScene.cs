using System.Threading;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

using Sirenix.Serialization;
using System.ComponentModel;

#pragma warning disable 0414
#pragma warning disable 0219

namespace OpenGS
{
    public class ConnectToGeneralServerScene : AbstractNonBattleScene
    {
        private SynchronizationContext currentContext;

        private bool connectSucceeded = false;
        private bool isTimeout = false;
        private int reCconectCount = 0;
        [SerializeField] private int maxReconnectCount = 3;

        private bool moveFlag = false;
        private Coroutine reconnectRoutine;
        private string connectionAddress;
        private int connectionPort;

        public bool isOverrideServerAddress = false;
        [SerializeField] public string OverrideServerAddress;
        [SerializeField] private string defaultServerAddress = "127.0.0.1";
        [SerializeField] private int defaultServerPort = 60000;

        [SerializeField] private ConnectToLobbyServerSceneMediateObject mediateObject;

        //[Required][OdinSerialize] public ConnectToLobbyNetworkManager networkManager;

        protected override void Awake()
        {
            defaultServerAddress = defaultServerAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(defaultServerAddress))
            {
                defaultServerAddress = "127.0.0.1";
            }

            OverrideServerAddress = OverrideServerAddress?.Trim();
            defaultServerPort = Mathf.Clamp(defaultServerPort, 1, 65535);
            maxReconnectCount = Mathf.Max(0, maxReconnectCount);
            sceneMediateObject = null;
            base.Awake();
        }

        void Start()
        {
            currentContext = SynchronizationContext.Current;
            DependencyInjectionConfig.EnsureLocalTestServerStarted();
            EnsureTitleBgm();

            var serverIP = isOverrideServerAddress && !string.IsNullOrWhiteSpace(OverrideServerAddress)
                ? OverrideServerAddress
                : defaultServerAddress;
            var port = ResolveServerPort();
            connectionAddress = serverIP;
            connectionPort = port;
            Debug.Log($"[ConnectToGeneralServerScene] Connecting to lobby server at {serverIP}:{port}");

            if (mediateObject != null && mediateObject.networkManager != null)
            {
                mediateObject.networkManager.ConnectToLobbyServer(serverIP, port);
            }
            else
            {
                Debug.LogWarning("ConnectToGeneralServerScene: mediateObject or networkManager is null.");
                GoToLobby();
            }
        }

        private int ResolveServerPort()
        {
            DebugSettingsManager.EnsureLoaded();
            var settings = DebugSettingsManager.settings;
            if (settings != null && settings.localTCPPort > 0)
            {
                var debugPort = Mathf.Clamp(settings.localTCPPort, 1, 65535);
                Debug.Log($"[ConnectToGeneralServerScene] Using debug settings TCP port: {debugPort}");
                return debugPort;
            }

            Debug.Log($"[ConnectToGeneralServerScene] Using default TCP port: {defaultServerPort}");
            return Mathf.Clamp(defaultServerPort, 1, 65535);
        }

        private void EnsureTitleBgm()
        {
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("[ConnectToGeneralServerScene] SoundManager is not ready; skipping title BGM.");
                return;
            }

            if (SoundManager.Instance.IsBgmPlaying(EBgm.Title))
            {
                Debug.Log("[ConnectToGeneralServerScene] Title BGM is already playing.");
                return;
            }

            Debug.Log("[ConnectToGeneralServerScene] Switching to Title BGM.");
            SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void OnDestroy()
        {
            moveFlag = true;
            if (reconnectRoutine != null)
            {
                StopCoroutine(reconnectRoutine);
                reconnectRoutine = null;
            }
            base.OnDestroy();
            //networkManager.DisconnectFromServer();
        }

        void LoginSucceeded()
        {
            // BacktoTitle();
        }

        void LoginFail()
        {
            //BacktoTitle();
        }

        public void Timeout()
        {
            Debug.Log("Timeout");

            PlayBeep();
            ScheduleReconnectOrBackToTitle();
        }

        public void OnConnected()
        {
            reCconectCount = 0;
            isTimeout = false;
            Debug.Log("[ConnectToGeneralServerScene] Connected to lobby server.");
        }

        public void OnDisconnected()
        {
            ScheduleReconnectOrBackToTitle();
        }

        public void OnLoginFailed()
        {
            //soundManager.PlayBeep();
            BackToTitle();
        }

        private void OnApplicationQuit()
        {
            //networkManager.DisconnectFromServer();
        }

        public void EnterServerAccepted()
        {
            connectSucceeded = true;
            if (!moveFlag)
            {
                moveFlag = true;
                GoToLobby();
            }
        }

        public void KickFromServer()
        {
            BackToTitle();
        }

        public ConnectToLobbyNetworkManager NetworkManagerScript()
        {
            return mediateObject.networkManager;
        }

        void BackToTitle()
        {
            Debug.Log("BackToTitle");
            GameFlagsManager.GetInstance().BeforeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            GoToTitleScene();
        }

        public override SynchronizationContext MainThread()
        {
            return currentContext ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public override void GoToLobby()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            base.GoToLobby();
        }

        private void ScheduleReconnectOrBackToTitle()
        {
            if (moveFlag || reconnectRoutine != null)
            {
                return;
            }

            if (reCconectCount >= Mathf.Max(0, maxReconnectCount) ||
                string.IsNullOrWhiteSpace(connectionAddress) || connectionPort <= 0)
            {
                BackToTitle();
                return;
            }

            reconnectRoutine = StartCoroutine(ReconnectAfterDelay());
        }

        private IEnumerator ReconnectAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            reconnectRoutine = null;

            if (moveFlag || mediateObject == null || mediateObject.networkManager == null)
            {
                yield break;
            }

            reCconectCount++;
            Debug.Log($"[ConnectToGeneralServerScene] Retrying lobby connection ({reCconectCount}/{maxReconnectCount})...");
            mediateObject.networkManager.ConnectToLobbyServer(connectionAddress, connectionPort);
        }

        void PlayBeep()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySystemSound(ESystemSound.Error);
            }
        }
    }
}
