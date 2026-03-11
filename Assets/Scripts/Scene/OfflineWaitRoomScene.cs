
using System.Threading;
using DG.Tweening;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenGSCore;
using Sirenix.OdinInspector;


#pragma warning disable 0219

namespace OpenGS
{

    //Events
    public partial class OfflineWaitRoomScene
    {
        void OnApplicationQuit()
        {

            //GameGeneralManager.GetInstance.SaveDebugSelect();


        }


    }

    public partial class OfflineWaitRoomScene
    {

    }


    public partial class OfflineWaitRoomScene : AbstractNonBattleScene, IOfflineWaitRoom
    {
        public Button chara;
        public Button map;

        public Button PlusButton;
        public Button MinusButton;


        [Required] public GameObject charaSelectDialog;
        public GameObject dmMapSelectDialog;
        public GameObject tdmMapSelectDialog;
        public GameObject ctFMapSelectDialog;
        public GameObject suvMapSelectDialog;

        public GameObject weaponLimitDialog;

        public GameObject instantItemSelectDialog;



        public GameObject teamBalanceText;
        public GameObject playerCountText;

        public AudioClip bgm;

        public AudioClip gameStart;
        public AudioClip Click;


        //public StageThumbnail stageThumbnail;

        // Start is called before the first frame update

        void Awake()
        {
            DebugFlagManager.SetFirstSceneName("OfflineWaitRoom");

            GameGeneralManager.GetInstance.CreatePlayerWaitRoomInfo("test");

            //var waitRoom = GameGeneralManager.GetInstance.LocalWaitRoom;

        }

        void Start()
        {



            if (PlaySound.IsPlayingBGM())
            {
                PlaySound.StopBGM();
            }

            PlaySound.PlayBGM(bgm);

            SceneManager.sceneLoaded += GameSceneLoaded;


            var gameManager = GameGeneralManager.GetInstance;

            //var matchData = gameManager.MatchRoom.Data;

            //var data = new MatchData();


        }

        // Update is called once per frame
        void Update()
        {
            //#if UNITY_EDITOR

            if (Input.GetKey(KeyCode.F10))
            {
                GameStart();
            }


            if (Input.GetKey(KeyCode.Keypad0))
            {

            }

            if (Input.GetKey(KeyCode.Keypad1))
            {

            }

            if (Input.GetKey(KeyCode.Keypad2))
            {

            }

            if (Input.GetKey(KeyCode.Keypad3))
            {

            }

            if (Input.GetKeyDown(KeyCode.Space))
            {

            }

            if (Input.GetKey(KeyCode.Escape))
            {
                GotoTitleScene();
            }


            //#endif

        }



        public void setDisableUI()
        {

        }

        public void ShowCharacterSelectDialog()
        {
            if (charaSelectDialog)
            {
                charaSelectDialog.SetActive(true);
                //var image = charaSelectDialog.GetComponent<Image>();

                //var c = image.color;

                //c.a = 0.0f;

                //image.color = c;

                //DOTween.ToAlpha(() => image.color, color => image.color = color, 1.0f, 0.5f);




            }
        }

        public void ShowDMMapSelectDialog()
        {
            var dif = dmMapSelectDialog.GetComponent<IMapSelectDialog>();

            if (dmMapSelectDialog)
            {
                dmMapSelectDialog.SetActive(true);

            }
        }

        public void ShowTDMMapSelectDialog()
        {
            var dif = tdmMapSelectDialog.GetComponent<IMapSelectDialog>();

            if (tdmMapSelectDialog)
            {
                dmMapSelectDialog.SetActive(true);

            }
        }

        public void ShowSUVMapSelectDialog()
        {
            var dif = suvMapSelectDialog.GetComponent<IMapSelectDialog>();

        }

        public void ShowCTFMapSelectDialog()
        {
            // var dif = ctfMapSelectDialog.GetComponent<IMapSelectDialog>();

        }

        public void ShowWeaponLimitDialog()
        {


        }

        public void HideWeaponDialog()
        {

        }

        public void HideAllDialog()
        {

        }

        public void CharacterChanged(string str)
        {
            Debug.Log(str);

        }

        private void EditSelect(OpenGSCore.EGameMode mode, EMap map)
        {
            var selrct = new OfflineGameModeSelect();





        }

        public void DMMapChanged(string str)
        {
            //var manager = MatchRoomManager.Instance;


            //manager.MapInfo.GameMode = eGameMode.DeathMatch;

            switch (str)
            {
                case "CityOfDarkness1":

                    break;

                case "CityOfDarkness2":

                    break;
                case "GreenHill1":

                    break;
                case "GreenHill2":

                    break;

                case "DrayDays":
                    break;

                case "Jungle1":
                    break;
                case "Jungle2":
                    break;


            }

        }

        public void TDMMapChanged(string str)
        {
            //var manager = MatchRoomManager.Instance;

            //manager.MapInfo.GameMode = eGameMode.TeamDeathMatch;


            //manager.StageInfo.

            //Debug.Log("TDM:" + str);

            switch (str)
            {
                case "CityOfDarkness1":
                    //manager.Stage.MapName = "CityOfDarkness1";
                    break;

                case "CityOfDarkness2":

                    //manager.Stage.MapName = "CityOfDarkness2";
                    break;

                case "Forest":

                    //manager.Stage.MapName = "Forest";
                    break;

                case "Jungle1":

                    //manager.Stage.MapName = "Jungle1";

                    break;

                case "Jungle2":

                    //manager.Stage.MapName = "Jungle2";

                    break;

                case "Ruin":

                    break;



                case "DrayDays":
                    //manager.Stage.MapName = "DryDays";

                    break;
                case "Nocturne":
                    //manager.Stage.MapName = "Nocturne";
                    break;

                case "SecretFactory":

                    break;

            }
        }
        public void SUVMapChanged(string str)
        {
            //var manager = GameGeneralManager.GetInstance;

            switch (str)
            {
                case "":

                    break;
            }
        }

        public void CTFMapChanged(string str)
        {
            //var manager = MatchRoomManager.Instance;


            Debug.Log("CTF:" + str);

            switch (str)
            {
                case "CityOfDarkness1":
                    //manager.Stage.MapName = "CityOfDarkness1";
                    break;

                case "CityOfDarkness2":

                    //manager.Stage.MapName = "CityOfDarkness2";
                    break;

                case "Forest":

                    //manager.Stage.MapName = "Forest";
                    break;

                case "House":
                    //manager.Stage.MapName = "House";

                    break;

                case "Jungle1":

                    //manager.Stage.MapName = "Jungle1";

                    break;

                case "Jungle2":

                    //manager.Stage.MapName = "Jungle2";

                    break;

                case "DrayDays":
                    //manager.Stage.MapName = "DryDays";

                    break;

                case "Nocturne":
                    //manager.Stage.MapName = "Nocturne";
                    break;
            }
        }





        public void SetRandomMap()
        {


        }

        private void GameSceneLoaded(Scene next, LoadSceneMode mode)
        {

            SceneManager.sceneLoaded -= GameSceneLoaded;
        }




        public void GameStart()
        {
            Debug.Log("GameStart");

            Debug.Log("GameRule:" + "");



            GameFlagsManager.GetInstance().BeforeSceneName = "OfflineWaitRoom";

            SceneManager.LoadSceneAsync("OfflineLoadingScene");


            string pass = "";

        }

        void ChangeGameMode()
        {

        }

        void ChangeGameModeToCTF(CTFMatchRule rule)
        {
            var ctf = new CTFMatchRule();


        }

        void ChangeGameModeToDeathMatch(DeathMatchRule rule)
        {



        }

        void ChangeGameModeToTeamDeathMatch(TeamDeathMatchRule rule)
        {

        }

        void ChangeGameModeToSurvival()
        {
            //GameGeneralManager

        }
        void ChangeGameModeToTeamSurvival()
        {
            //GameGeneralManager

        }
        public void Plus()
        {
            var rule = 9;


        }

        public void Minus()
        {
            var rule = 9;
        }

        public void AddBot()
        {

        }

        public void FillBot()
        {

        }

        public void AllBot()
        {

        }

        public void RemoveAllBot()
        {

        }


        public void ShowInstantItemDialog()
        {
            //instantItemSelectDialog.GetComponent<Iins>


        }

        public void GotoTitleScene()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "OfflineWaitRoomScene";

            SceneManager.LoadSceneAsync("NewTitleScene");
        }

        public void GotoShopScene()
        {


            GameFlagsManager.GetInstance().BeforeSceneName = "ShopScene";

            SceneManager.LoadSceneAsync("ShopScene");
        }


        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnStartUnityEditor()
        {
            //throw new NotImplementedException();
        }

        protected override void OnQuitUnityEditor()
        {
            //throw new NotImplementedException();
        }

        protected override void OnStartFromEditorDirectly()
        {
            //throw new NotImplementedException();
        }
    }

}
