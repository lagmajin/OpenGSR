using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class LoginAndSignUpScene : AbstractScene
    {
        [SerializeField]
        private Text id;
        [SerializeField]
        private Text pass;

        private Button b;
        private Button c;

        //public GeneralSceneMasterData generalSceneMasterData;
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }
        private void Start()
        {

        }

        private void OnApplicationQuit()
        {
            var b = GameManager().HasBeforeLoginData();

        }

        private void StringCheck()
        {

        }

        public void TryLogin()
        {

        }

        public void TrySignUp(in string id,in string password)
        {

        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }
    }
}
