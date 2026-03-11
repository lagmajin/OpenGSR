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
            currentContext=SynchronizationContext.Current;


            string serverIP = "localhost";
            int port = 50000;


            string publicIP = "138.2.5.79";


          mediateObject.networkManager.ConnectToLobbyServer("192.168.0.6", 60000);





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
            SceneManager.LoadScene("NewTitleScene");

        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        public override void GoToLobby()
        {
            currentContext.Post(__ =>
            {

                var asyncOperation =SceneManager.LoadSceneAsync("LobbyScene");

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