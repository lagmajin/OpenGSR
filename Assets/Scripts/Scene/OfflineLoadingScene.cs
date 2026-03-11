using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using OpenGSCore;
//using Unity.VisualScripting;

#pragma warning disable 0414

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class OfflineLoadingScene : AbstractLoadingScene, IOfflineLoadingScene
    {
        private bool loadImmediately = true;

        private float count = 0.0f;
        private float timeout = 20.0f;



        public MapSceneMasterData mapMasterdata;
        public GeneralSceneMasterData senes;
        public LoadingSpriteMasterData sp;


        private void Start()
        {


            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugSelect();
            }




            if (loadImmediately)
            {
                LoadingStart();
            }

        }

        private void Update()
        {
            // count += Time.deltaTime;

        }
        public void Debug()
        {

        }


        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);



        }


        private void OnApplicationQuit()
        {

        }
        public void LoadingStart()
        {


            StartCoroutine(LoadingCoroutine());


        }

        private IEnumerator LoadingCoroutine()
        {
            //var manager = OfflineLoadingManager.Insntace;

            //var gauge=manager.Gauge;

            var matchRoomManager = MatchRoomManager();

            var waitRoom=matchRoomManager.WaitRoom;

            var players = waitRoom.AllPlayers();



            //gauge = 20.0f;

            //gauge = 0.0f;

            matchRoomManager.CreateNewOnlineMatchRoom("");

            //var matchRoom = matchRoomManager.OfflineMatchRoom;

            //matchRoom.AddPlayers(players);



            var activeSceneName = SceneManager.GetActiveScene().name;

            //var async = SceneManager.LoadSceneAsync("NewTitleScene");
            var async = SceneManager.LoadSceneAsync("DryDays(Stage)(DM)", LoadSceneMode.Single);


            async.allowSceneActivation = false;


            yield return new WaitForSecondsRealtime(1);
            //yield return new WaitForSeconds(1);
            async.allowSceneActivation = true;

            //SceneManager.SetActiveScene(SceneManager.GetSceneByName("DryDays(Stage)(DM)"));

            //SceneManager.UnloadSceneAsync(activeSceneName);
        }

        void AppointRoomOwner()
        {

        }

        void GoToBattleScene()
        {


        }

        void BackToWaitRoom()
        {


        }

        void BackToTitleScene()
        {


        }


    }





}

