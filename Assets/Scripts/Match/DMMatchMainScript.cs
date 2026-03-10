using UnityEngine;
//using KanKikuchi.AudioManager;
//using Cinemachine;
using Newtonsoft.Json.Linq;
using System;
using Sirenix.OdinInspector;


using UnityEngine.SceneManagement;
//using Cysharp.Threading.Tasks;
using OpenGSCore;


#pragma warning disable 0414

namespace OpenGS
{



    //#DeathMatch
    [DisallowMultipleComponent]
    public class DMMatchMainScript : AbstractMatchMainScript, IDMMatchMainScript
    {
        private float nowTime = 0.0f;
        public GameObject uiManager;


        

        [SerializeField] [Required] public ReSpawnPoints respawnPoints;
        //public AudioClip randomSound2;



        private float testTime = 5;


        // Start is called before the first frame update


        


        private void Awake()
        {

            Application.targetFrameRate = 60;
        }

        

        protected new void Start()
        {
            base.Start();

            if (!CompareTag("MainScript"))
            {
                gameObject.tag = "MainScript";
            }

            SetupGame();

            Debug.Log("DeathMatch: GameStart");
            Invoke(nameof(GameStart), 0.1f);

            battleSceneMediateObject.mainscript = this;
            ShakeCamera();
        }
        // Update is called once per frame
        private void Update()
        {
            nowTime += Time.deltaTime;

            testGameTime -= Time.deltaTime;

            if ((!endFlag) && testGameTime <= 0)
            {
                GameEnd();
            }

            //Debug.Log(testTime.ToString());

        }

        public override void OnMyPlayerDead()
        {
            Debug.Log("MyPlayerDead");

            //await UniTask.Delay()

            Invoke(nameof(HandleMyPlayerRespawn), 3f);
            battleSceneMediateObject.uiManager.ShowRespawnGauge(3.0f);


        }

        private void OnApplicationQuit()
        {

        }

        private void SetupGame()
        {
            //var database=matchRoomManager.OnlineMatchRoom.database;

            //database.player


        }

        void GameStart()
        {
            // 自プレイヤーをランダムな位置に生成
            Vector3 spawnPos = GetRandomSpawnPoint(respawnPoints);
            CreateMyPlayer(spawnPos, ETeam.NoTeam);

            SetUpUI();
        }

        void SetUpUI()
        {
            // UI管理システムの初期化など（必要に応じて）
            // Instantiate(uiCanvasMasterData.PlayerStatusUI);
        }

        void SuddenDeathStart()
        {
            var canvasIf = uiManager.GetComponent(typeof(IBattleSceneUIManager)) as IBattleSceneUIManager;


        }

        void GameEnd()
        {
            endFlag = true;

            //Time.timeScale = 0.4f;
            bool won = false;

            // var canvasIf = uiManager.GetComponent<IBattleSceneUIManager>();
            var canvasIf = uiManager.GetComponent(typeof(IBattleSceneUIManager)) as IBattleSceneUIManager;




            if (won)
            {
                canvasIf.ShowGameWin();
            }
            else
            {
                canvasIf.ShowGameDefatead();
            }



            //GoToResult();

            Debug.Log("GameEnd");

            Invoke("GoToResult", gotoResultSceneWaitTime);

        }


        private void OnlineEventParser(AbstractGameEvent e)
        {
            var eventName = e.EventName;

            if ("FlagReturnEvent" == eventName)
            {

            }

            if ("FlagLostEvent" == eventName)
            {
                //PlaySound.PlayBGM()
            }
        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            var typeName = e.GetType().FullName;

            if (typeName == typeof(PlayerDeadEvent).FullName)
            {
                var deadEvent = e as PlayerDeadEvent;

                //var data=MatchRoom().Data;

                //data.Players[e.]

            }

            if (typeName == typeof(PlayerBurstEvent).FullName)
            {

            }

        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (GameManager.IsOnlineGameMode)
            {

                //OfflineEventParser(e);
            }
            else
            {
                //OnlineEventParser(e);
            }


        }

        protected override void OnNetworkDataRecved(JObject obj)
        {

        }
        protected override void OnOneSec()
        {
            //Debug.Log("ov1Sec");
        }

        protected override void OnOneMin()
        {
            //Debug.Log("ov1Min");
        }

        private void HandleMyPlayerRespawn()
        {
            Debug.Log("MyPlayerRespawn");
        }

        public override void OnStartUnityEditor()
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

            var room=matchRoomManager.OnlineMatchRoom;

            //var pm=room.playerma

            var playerCharacter = EPlayerCharacter.Ami;

            var playerInfo=new PlayerInfo();

            //playerInfo.playerCharacter = playerCharacter;

            //room.PlayerManager.AddPlayer()

        }
    }


}