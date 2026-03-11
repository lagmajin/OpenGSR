

using System.Threading;
using UnityEngine;


#pragma warning disable 0414

namespace OpenGS
{
    public class SkyFighterResultScene : AbstractScene
    {
        [SerializeField]
        private float showTime = 2.0f;

        public GameObject skyFighterCanvas;
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

            }
        }

        void BacktoWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "SkyFighterRsultScene";

        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }
    }

}
