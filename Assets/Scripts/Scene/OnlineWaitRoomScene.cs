#pragma warning disable 0219
#pragma warning disable 0105

using System.Threading;
using DG.Tweening;
using OpenGSCore;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;




namespace OpenGS
{



    public partial class OnlineWaitRoomScene : AbstractNonBattleScene, IOnlineWaitRoom, IWaitRoom
    {
        private GeneralServerNetworkManager generalNetworkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
        public Button chara;
        public Button map;




        [SerializeField]
        public InputField inputField;
        [SerializeField]
        public Text text;

        [Required]
        public WaitRoomNetworkManager networkManager;

        [SerializeField] [Required] private WaitRoomMediateObject mediateObject;

        private bool roomOwner =true;


        [SerializeField] private WaitRoomPlayerSlot mySlot;

        private SynchronizationContext mainThread;


        

        public override SynchronizationContext MainThread()
        {
            return mainThread;
        }

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

            mainThread=SynchronizationContext.Current;
            

        }
        void Start()
        {
            if (roomOwner)
            {

            }
            else
            {

            }

            timer.timeupEvent.AddListener(TimeUp);


            LoadRoomSetting();


        }

        


        void Update()
        {
            if (Input.anyKey)
            {
                timer.ReStartTimer();

            }


        }
        private void DebugConnect()
        {
            //generalNetworkManager.ConnectToGeneralServerSync("127.0.0.1", 50000, "test", "test");

            DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>().ConnectToGeneralServerSync("127.0.0.1", 50000, "test", "test");


        }
        protected override void OnStartFromEditorDirectly()
        {
            //throw new NotImplementedException();
        }
        protected override void OnStartUnityEditor()
        {

        }

        protected override void OnQuitUnityEditor()
        {

        }


        private void Reset()
        {
            if (!timer)
            {
                timer = GetComponent<GameTimer>();
            }

            if (!mediateObject)
            {
                mediateObject = GetComponent<WaitRoomMediateObject>();
            }
        }



        void OnBackRoomFromBattle()
        {

        }

        public void ChangeGameMode()
        {

        }

        public void ChangeGameMode(EGameMode mode)
        {

        }

        public void ChangeMap(EMap map)
        {
            Debug.Log("Map " + map.ToString());

            //GameModeSelectManager.Instance;


        }

        public void ChangeTeamBalance(bool balance)
        {


        }
        public bool IsRoomOwner()
        {

            return roomOwner;

        }

        public void ResignOwner()
        {
            if (IsRoomOwner())
            {

            }

        }

        private void GameSceneLoaded(Scene next, LoadSceneMode mode)
        {

            SceneManager.sceneLoaded -= GameSceneLoaded;
        }

        private void WaitRoomSettingChanged()
        {

        }

        void Ready(bool b)
        {
            if (b == true)
            {

            }
            else
            {

            }


        }
        void LoadGameScene()
        {

            Debug.Log("Go to loading Scene...");

            Debug.Log("GameRule:" + "");



            GameFlagsManager.GetInstance().BeforeSceneName = "OfflineWaitRoom";

            SceneManager.LoadSceneAsync(generalSceneMasterData.OnlineLoadingScene());


            string pass = "";




        }


        public void Plus()
        {

            if (IsRoomOwner())
            {
                //var waitRoom=MatchRoomManagerInstance().WaitRoom;



            }
            else
            {

            }


        }

        public void Minus()
        {
            if (IsRoomOwner())
            {
                //var waitRoom = MatchRoomManagerInstance().WaitRoom;


            }
            else
            {

            }

        }

        [Button("チャット送信テスト")]
        public void SendChat(string str)
        {
            Debug.Log("SendChatAA");

            //generalNetworkManager.SendWaitRoomChat(text.text);

            //var form = GameObject.Find("ChatInputField").GetComponent<InputField>().text="";

        }

        public void ExitWaitRoom()
        {

            //MatchRoomManagerInstance().RemoveOnlineWaitRoom();

            GoToLobby();
        }


        public void TimeUp()
        {
            if (IsRoomOwner())
            {

                ResignOwner();


            }
            else
            {

            }

            ExitWaitRoom();

        }

        private void LoadRoomSetting()
        {
            //var instance = WaitRoomManager.Instance;


            var instance = DependencyInjectionConfig.Resolve<WaitRoomManager>();

            var room=instance.WaitRoom;

            var uiManager = mediateObject.WaitRoomUiManager();

            uiManager.ChangeRoomTitle(room.RoomName);

            uiManager.ChangeRoomCapacity(room.Capacity);

            uiManager.ChangeGameMode(EGameMode.DeathMatch);




        }



    }
}

