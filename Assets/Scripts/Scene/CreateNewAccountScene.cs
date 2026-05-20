using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGSCore;

namespace OpenGS
{
    public class CreateNewAccountScene:AbstractScene
    {
        [SerializeField] private string defaultAccountName = "Player";

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
            Debug.Log("CreateNewAccountScene started");
        }

        private void OnApplicationQuit()
        {
            
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current;
        }

        public void CreateAccount(string accountName, string password)
        {
            var resolvedName = string.IsNullOrWhiteSpace(accountName) ? defaultAccountName : accountName.Trim();
            var globalUserId = System.Guid.NewGuid().ToString("N");

            AccountManager.Instance.LoginData(resolvedName, "", globalUserId);

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                networkManager.SendMessage(new JObject
                {
                    ["MessageType"] = MessageType.CreateAccountRequest,
                    ["AccountName"] = resolvedName,
                    ["Password"] = password ?? "",
                    ["GlobalUserId"] = globalUserId
                });
            }
            catch
            {
                // ローカルのみでも続行できるようにする
            }

            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().TitleScene());
        }
    }
}
