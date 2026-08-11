

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
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }

        private void Start()
        {

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
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            var nextScene = generalSceneMasterData != null
                ? generalSceneMasterData.OfflineWaitRoomScene()
                : GeneralSceneMasterData.Instance().OfflineWaitRoomScene();
            SceneManager.LoadSceneAsync(nextScene);
        }

    }
}
