using System.Collections;
using System.Threading;
using UnityEngine;


#pragma warning disable 0414


namespace OpenGS
{
    public class MissionResultScene : AbstractScene
    {
        private float showTime = 3.0f;
        private SynchronizationContext mainThread;
        public override SynchronizationContext MainThread()
        {
            return null;
        }

        IEnumerator WaitCoroutine()
        {

            yield return null;

        }
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

            mainThread=SynchronizationContext.Current;
            
        }
        void Start()
        {

            StartCoroutine(WaitCoroutine());

        }
        private void OnApplicationQuit()
        {

        }

    }


}
