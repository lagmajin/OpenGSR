
using OpenGSCore;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using UniRx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
//using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Zenject;

#pragma warning disable 0414
#pragma warning disable 0219


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class OnlineLoadingScene : AbstractLoadingScene, IOnlineLoadingScene
    {
        private bool loadImmediately = true;
        private float count = 0.0f;
        private float timeout = 10.0f;
        static bool loadingErrorFlag = false;
        public LoadingSpriteBGMasterData bgMasterData;

        [SerializeField][Required][SceneObjectsOnly]public OnlineLoadingSceneMediateObject mediateObject;

        [Inject] private OnlineLoadingManager onlineLoadingManager;

        [SerializeField]public OnlineLoadingSceneNetworkManager networkManager;

        private readonly ReactiveProperty<float> progress = new ReactiveProperty<float>(0f);
        public IReadOnlyReactiveProperty<float> Progress => progress;


        private AsyncOperation _sceneLoadOp;
        public void DebugCreateMatchRoom()
        {

            Debug.Log("Debug room");

            if (DebugFlagManager.FirstSceneName == this.GetType().FullName)
            {
                GameModeSelectManager.Instance.LoadDebugOnlineSelectFromFile();

                Debug.Log("FirstScene");

            }

            DebugConnect();

            var manager = MatchRoomManager();


            manager.CreateNewOnlineWaitRoom("id", 8);


            var waitRoom = manager.OnlineWaitRoom;

            waitRoom.AddPlayer("id", "MyPlayer");

            manager.CreateNewOnlineMatchRoom();





        }

        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

            Application.targetFrameRate = 30;


        }
        private void Start()
        {
            loadingErrorFlag = false;

            if (PlaySound.IsPlayingBGM())
            {

            }
            else
            {

            }

            if (DebugFlagManager.IsDebug())
            {
                //DebugCreateMatchRoom();

            }


 


            SceneManager.sceneLoaded+= OnSceneLoaded;
 

            StartCoroutine(Loading());



        }
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            //SceneManager.MoveGameObjectsToScene()
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //throw new NotImplementedException();
        }

        void Reset()
        {
            if (!mediateObject)
            {
                mediateObject = FindFirstObjectByType<OnlineLoadingSceneMediateObject>();
            }


        }

        private void Update()
        {
            count += Time.deltaTime;

            if (count >= timeout)
            {
                BackToWaitRoom();
            }

        }

        private void OnApplicationQuit()
        {
            


        }
        public void DebugConnect()
        {

        }


        private IEnumerator Loading()
        {
            PrettyLogger.Bold("Network","LoadingStart");

            

            MatchRoomManager().CreateNewOnlineMatchRoom();

            yield return new WaitForSecondsRealtime(1);

            var map=mediateObject.MapSceneMasterData().Map(EMap.DryDays);

            _sceneLoadOp = SceneManager.LoadSceneAsync(map.MapScene(), LoadSceneMode.Single);

            _sceneLoadOp.allowSceneActivation = false;

            networkManager.SendLoadingStart();

            float progress = 0f;
            while (!_sceneLoadOp.isDone)
            {
                progress = Mathf.Clamp01(_sceneLoadOp.progress / 0.9f);
                // サーバに進捗送信（モック）
                //Debug.Log($"Sending progress {progress * 100}% to server");
                networkManager.SendLoadingProgress(progress);
                yield return null;
            }
            
            networkManager.SendLoadingComplete(); 


            yield return new WaitUntil(() => _sceneLoadOp.allowSceneActivation);


        }

        private void SendLoadingStartToServer()
        {
            var json = new AIXJsonObject();
            json["MessageType"] = "LoadingStarted";

            networkManager.SendLoadingStart();
        }

        private void SendProgressPercentToServer(float progress)
        {
            var json = new AIXJsonObject();
            json["MessageType"] = "LoadingProgress";
            json["PlayerID"] ="";
            json["Progress"] = progress;

            //networkManager.SendMessage(json);

        }

        public void TryConnectToMatchServer()
        {
            


        }


        public void OnMatchLoadingCompleted()
        {

        }

        public void OnMatchServerConnected()
        {

        }

        private void SendLoadingError()
        {

        }

        public void OnLoadingFailed()
        {

        }

        void GoToBattleScene()
        {


        }

        void BackToWaitRoom()
        {
            loadingErrorFlag = true;

            //mediateObject.BGMAndBGNManager().


        }

        void SendChat(string message)
        {
            var myId = "";

            


        }


        public void ParseServerMessage()
        {

        }


        [Button("ローディング")]
        public void LoadingScene(EGameMode mode = EGameMode.DeathMatch, EMap map = EMap.DryDays)
        {

            var matchRoomManager = MatchRoomManager();

            matchRoomManager.CreateNewOnlineMatchRoom("");


            networkManager?.SendMessage("");





        }

        [Button("デバッグ選択")]
        public void CreateDebugSelect(EGameMode mode, EMap map)
        {


            var select = new OnlineGameModeSelect
            {
                GameMode = mode,
                Map = map
            };

            var instance = GameModeSelectManager.Instance;
            instance.OnlineGameSelect = select;

            instance.SaveDebugOnlineSelectToFile();


        }

        [Button("デバッグステージへ")]
        public void GoToTestScene()
        {
            var mapInfo = mapSelectMasterData.Map(EMap.DryDays);


            //var sc = mapInfo.map;



            //SceneManager.LoadScene(sc);

        }

        protected override void OnStartUnityEditor()
        {
            //PrettyLogger.Log("System", "Test");

            //throw new NotImplementedException();
        }

        protected override void OnQuitUnityEditor()
        {
            //PrettyLogger.Log("System", "Test");
        }

        protected override void OnStartFromEditorDirectly()
        {
            PrettyLogger.Log("System", "Test");

            //SetOnlineMode(true);

            IsOnlineMode = true;
        }

        public void OnEnterMapAllowed()
        {

            _sceneLoadOp.allowSceneActivation = true;

        }
    }
}

