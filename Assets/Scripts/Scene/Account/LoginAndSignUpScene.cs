using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenGSCore;

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

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
        }

        private void OnApplicationQuit()
        {
            var hasBeforeLoginData = GameManager().HasBeforeLoginData();
        }

        private void StringCheck()
        {
        }

        public void TryLogin()
        {
            var accountName = id != null ? id.text : "";
            var password = pass != null ? pass.text : "";
            var globalUserId = System.Guid.NewGuid().ToString("N");

            AccountManager.Instance.LoginData(accountName, "", globalUserId);

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                networkManager.SendMessage(new JObject
                {
                    ["MessageType"] = MessageType.LoginRequest,
                    ["AccountName"] = accountName,
                    ["Password"] = password,
                    ["GlobalUserId"] = globalUserId
                });
            }
            catch
            {
            }

            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().TitleScene());
        }

        public void TrySignUp(in string id, in string password)
        {
            var globalUserId = System.Guid.NewGuid().ToString("N");
            AccountManager.Instance.LoginData(id, "", globalUserId);

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                networkManager.SendMessage(new JObject
                {
                    ["MessageType"] = MessageType.CreateAccountRequest,
                    ["AccountName"] = id,
                    ["Password"] = password,
                    ["GlobalUserId"] = globalUserId
                });
            }
            catch
            {
            }
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current;
        }
    }
}
