

using UnityEngine;

namespace OpenGS
{
    interface IMetalBreakerResultScene
    {

    }

    public class MetalBreakerResultScene : MonoBehaviour
    {
        public AudioClip fanfare;
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }

        private void Start()
        {

        }

        private void Update()
        {

        }

        void BacktoWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "MetalBreakerResultScene";
        }

    }
}
