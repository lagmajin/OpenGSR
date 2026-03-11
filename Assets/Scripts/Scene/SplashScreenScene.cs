using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{


    [DisallowMultipleComponent]
    public class SplashScreenScene : AbstractScene
    {
        [SerializeField] private float time = 1.0f;

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        void Start()
        {
            /*

            if (BGMManager.Instance.IsPlaying())
            {
                BGMManager.Instance.Stop();
            }
            BGMManager.Instance.Play(bgm);


            


    */

            Invoke(nameof(GoToTitleScene), time);

        }


        // Update is called once per frame
        void Update()
        {

        }




    }
}
