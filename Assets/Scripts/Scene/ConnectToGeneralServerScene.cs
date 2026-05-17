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
        [SerializeField]private int maxReconnectCount = 3;

        private bool moveFlag = false;


        public bool isOverrideServerAddress = false;
        [SerializeField]public string OverrideServerAddress;
        [SerializeField] private string defaultServerAddress = "127.0.0.1";
        [SerializeField] private int defaultServerPort = 60000;



        [SerializeField] private ConnectToLobbyServerSceneMediateObject mediateObject;

        //[Required][OdinSerialize] public ConnectToLobbyNetworkManager networkManager;

        void Start()
        {
            currentContext = SynchronizationContext.Current;
            DependencyInjectionConfig.EnsureLocalTestServerStarted();
            EnsureTitleBgm();

            var serverIP = isOverrideServerAddress && !string.IsNullOrWhiteSpace(OverrideServerAddress)
                ? OverrideServerAddress
                : defaultServerAddress;
            var port = defaultServerPort;

            if (mediateObject != null && mediateObject.networkManager != null)
            {
                mediateObject.networkManager.ConnectToLobbyServer(serverIP, port);
            }
            else
            {
                Debug.LogWarning("ConnectToGeneralServerScene: mediateObject or networkManager is null.");
            }
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
 
            //mediateObject.networkManager.

            //GoToLobby();


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
            var context = MainThread();
            context.Post(__ =>
            {

                var lobbyScene = generalSceneMasterData != null ? generalSceneMasterData.LobbyScene() : GeneralSceneMasterData.Instance().LobbyScene();
                var asyncOperation = SceneManager.LoadSceneAsync(lobbyScene);

                asyncOperation.completed += (operation) =>
                {
                    if (operation.isDone)
                    {
                        Debug.Log("LobbySceneのロードが完了しました");
                    }
                    else
                    {
                        Debug.LogError("LobbySceneのロードが失敗しました");
                    }
                };

            }, null);

        }

        void PlayBeep()
        {


        }

    }


}
