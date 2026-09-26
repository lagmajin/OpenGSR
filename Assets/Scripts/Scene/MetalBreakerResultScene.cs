

using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    interface IMetalBreakerResultScene
    {

    }

    public class MetalBreakerResultScene : MonoBehaviour
    {
        public AudioClip fanfare;
        [SerializeField] private GeneralSceneMasterData generalSceneMasterData;
        private bool transitionRequested;
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }

        private void Update()
        {
            if (Input.anyKeyDown)
            {
                BacktoWaitRoom();
            }
        }

        void BacktoWaitRoom()
        {
            if (transitionRequested)
            {
                return;
            }

            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            var nextScene = generalSceneMasterData != null
                ? generalSceneMasterData.OfflineWaitRoomScene()
                : GeneralSceneMasterData.Instance().OfflineWaitRoomScene();

            if (string.IsNullOrWhiteSpace(nextScene) || !Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Debug.LogError($"[MetalBreakerResultScene] Offline wait room scene is not available in build settings: {nextScene}");
                return;
            }

            transitionRequested = true;
            if (SceneManager.LoadSceneAsync(nextScene) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[MetalBreakerResultScene] Failed to load scene: {nextScene}");
            }
        }

    }
}
