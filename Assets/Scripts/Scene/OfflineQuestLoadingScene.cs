using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0414

namespace OpenGS
{

    public class OfflineQuestLoadingScene : MonoBehaviour
    {
        private bool loadImmediately = true;
        private bool loadingStarted;
        private bool transitionRequested;
        private Coroutine loadingCoroutine;

        public GeneralSceneMasterData senes;
        [SerializeField] private QuestAndMissionSceneStorage missionSceneStorage;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            if (missionSceneStorage == null)
            {
                missionSceneStorage = FindFirstObjectByType<QuestAndMissionSceneStorage>();
            }
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
            if (loadingStarted)
            {
                return;
            }

            loadingStarted = true;
            loadingCoroutine = StartCoroutine(LoadingCoroutine());
        }

        private void OnDestroy()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
        }

        private IEnumerator LoadingCoroutine()
        {
            yield return new WaitForSecondsRealtime(1);
            var sceneName = missionSceneStorage != null
                ? (string)missionSceneStorage.Mission1Scene()
                : "Mission1";
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                loadingStarted = false;
                Debug.LogError($"[OfflineQuestLoadingScene] Mission scene is not available in build settings: {sceneName}");
                yield break;
            }

            transitionRequested = true;
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (async == null)
            {
                loadingStarted = false;
                transitionRequested = false;
                Debug.LogError($"[OfflineQuestLoadingScene] Failed to load scene: {sceneName}");
                yield break;
            }

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
            if (transitionRequested)
            {
                return;
            }

            var sceneName = senes != null ? senes.OfflineWaitRoomScene() : "OfflineWaitRoom";
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                transitionRequested = false;
                Debug.LogError("[OfflineQuestLoadingScene] Offline wait room scene is not configured.");
                return;
            }

            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            loadingStarted = false;
            transitionRequested = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}
