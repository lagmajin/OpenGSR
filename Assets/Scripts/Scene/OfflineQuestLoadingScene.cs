using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0414

namespace OpenGS
{

    public class OfflineQuestLoadingScene : MonoBehaviour
    {
        private bool loadImmediately = true;

        public GeneralSceneMasterData senes;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
            if (loadImmediately)
            {
                LoadingStart();
            }
        }

        public void LoadingStart()
        {
            StartCoroutine(LoadingCoroutine());
        }

        private IEnumerator LoadingCoroutine()
        {
            yield return new WaitForSecondsRealtime(1);
            var sceneName = "Mission1";
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            async.allowSceneActivation = false;
            yield return new WaitForSecondsRealtime(1);
            async.allowSceneActivation = true;
        }

        private void GotoMission()
        {
            LoadingStart();
        }

        private void BackToOfflineWaitRoom()
        {
            var sceneName = senes != null ? senes.OfflineWaitRoomScene() : "OfflineWaitRoom";
            SceneManager.LoadScene(sceneName);
        }
    }
}
