
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0414

namespace OpenGS
{

    public class OfflineQuestLoadingScene : MonoBehaviour
    {
        private bool loadImmediately = true;
        private bool asyncLoadScene = true;

        public GeneralSceneMasterData senes;

        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);



        }

        private void Start()
        {

            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugMissionSelect();
            }

            if (loadImmediately)
            {
                LoadingStart();
            }
        }

        private void OnApplicationQuit()
        {



        }

        void Update()
        {

        }

        public void LoadingStart()
        {
            StartCoroutine(LoadingCorutine());
            int number = Random.Range(0, 0);
            var r1 = new System.Random();



            //var v = r1.Next(0, 8);

            //img.sprite = sprites[v];




        }

        private IEnumerator LoadingCorutine()
        {

            yield return new WaitForSecondsRealtime(1);
        }

        private void GotoMission()
        {

        }

        private void BackToOfflineWaitRoom()
        {
            SceneManager.LoadScene(GeneralSceneMasterData.Instance().TitleScene());

        }
    }
}
