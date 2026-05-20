using System.Threading;
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

        public bool isOverrideServerAddress = false;
        [SerializeField] public string OverrideServerAddress;
        [SerializeField] private string defaultServerAddress = "127.0.0.1";
        [SerializeField] private int defaultServerPort = 60000;

        [SerializeField] private ConnectToLobbyServerSceneMediateObject mediateObject;

        //[Required][OdinSerialize] public ConnectToLobbyNetworkManager networkManager;

        protected override void Awake()
        {
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
                Debug.Log($"[ConnectToGeneralServerScene] Using debug settings TCP port: {settings.localTCPPort}");
                return settings.localTCPPort;
            }

            Debug.Log($"[ConnectToGeneralServerScene] Using default TCP port: {defaultServerPort}");
            return defaultServerPort;
        }

        private void EnsureTitleBgm()
        {
            if (SoundManager.Instance.IsBgmPlaying(EBgm.Title))
            {
                Debug.Log("[ConnectToGeneralServerScene] Title BGM is already playing.");
                return;
            }

            Debug.Log("[ConnectToGeneralServerScene] Switching to Title BGM.");
            SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);
        }

        void Update()
        {
        }

        void OnDestroy()
        {
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

            //BacktoTitle();
        }

        public void OnConnected()
        {
            Debug.Log("[ConnectToGeneralServerScene] Connected to lobby server.");
        }

        public void OnDisconnected()
        {
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
        }

        public ConnectToLobbyNetworkManager NetworkManagerScript()
        {
            return mediateObject.networkManager;
        }

        void BackToTitle()
        {
            Debug.Log("BackToTitle");
            GameFlagsManager.GetInstance().BeforeSceneName = "ConnectToServerScene";
            GoToTitleScene();
        }

        public override SynchronizationContext MainThread()
        {
            return currentContext ?? SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public override void GoToLobby()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "ConnectToServerScene";
            base.GoToLobby();
        }

        void PlayBeep()
        {
        }
    }
}
