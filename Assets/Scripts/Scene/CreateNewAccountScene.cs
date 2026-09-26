using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGSCore;

namespace OpenGS
{
    public class CreateNewAccountScene:AbstractScene
    {
        [SerializeField] private string defaultAccountName = "Player";
        private GeneralServerNetworkManager networkManager;
        private IDisposable accountSubscription;
        private bool requestPending;
        private bool transitionRequested;
        private string pendingAccountName = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
            Debug.Log("CreateNewAccountScene started");
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                BindAccountStream();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CreateNewAccountScene] Failed to bind account response: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            accountSubscription?.Dispose();
            accountSubscription = null;
            base.OnDestroy();
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public void CreateAccount(string accountName, string password)
        {
            if (requestPending)
            {
                return;
            }

            var resolvedName = string.IsNullOrWhiteSpace(accountName) ? defaultAccountName : accountName.Trim();
            if (string.IsNullOrWhiteSpace(password))
            {
                Debug.LogWarning("[CreateNewAccountScene] Account creation requires a password.");
                return;
            }

            if (networkManager == null)
            {
                try
                {
                    networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CreateNewAccountScene] Account service is unavailable: {ex.Message}");
                    return;
                }
            }

            BindAccountStream();

            requestPending = true;
            pendingAccountName = resolvedName;
            Invoke(nameof(ClearRequest), 10f);
            networkManager.SendMessage(new JObject
            {
                ["MessageType"] = MessageType.CreateAccountRequest,
                ["AccountName"] = resolvedName,
                ["AccountID"] = resolvedName,
                ["DisplayName"] = resolvedName,
                ["Password"] = password,
                ["GlobalUserId"] = Guid.NewGuid().ToString("N")
            });
        }

        private void BindAccountStream()
        {
            if (accountSubscription != null || networkManager == null)
            {
                return;
            }

            accountSubscription = networkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(HandleCreateAccountResponse);
        }

        private void HandleCreateAccountResponse(JObject json)
        {
            if (MessageType.Normalize(json?["MessageType"]?.ToString()) != MessageType.CreateAccountResponse)
            {
                return;
            }

            requestPending = false;
            CancelInvoke(nameof(ClearRequest));
            if (!ReadSuccess(json?["Success"]))
            {
                var errorMessage = json["Error"]?.ToString()
                    ?? json["ErrorMessage"]?.ToString()
                    ?? "Unknown error";
                Debug.LogWarning($"[CreateNewAccountScene] Account creation failed: {errorMessage}");
                return;
            }

            var resolvedName = json["AccountName"]?.ToString()
                ?? json["PlayerName"]?.ToString()
                ?? json["AccountID"]?.ToString()
                ?? pendingAccountName;
            var globalUserId = json["GlobalUserId"]?.ToString()
                ?? json["PlayerID"]?.ToString()
                ?? json["AccountID"]?.ToString()
                ?? resolvedName;
            if (string.IsNullOrWhiteSpace(resolvedName) || string.IsNullOrWhiteSpace(globalUserId))
            {
                Debug.LogWarning("[CreateNewAccountScene] Account response did not contain identity.");
                return;
            }

            AccountManager.Instance?.LoginData(resolvedName, "", globalUserId);
            var titleScene = GeneralSceneMasterData.Instance()?.TitleScene();
            if (string.IsNullOrWhiteSpace(titleScene))
            {
                Debug.LogError("[CreateNewAccountScene] Title scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(titleScene))
            {
                Debug.LogError($"[CreateNewAccountScene] Title scene is not in build settings: {titleScene}");
                return;
            }

            if (transitionRequested)
            {
                return;
            }

            transitionRequested = true;
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            if (SceneManager.LoadSceneAsync(titleScene) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[CreateNewAccountScene] Failed to load title scene: {titleScene}");
            }
        }

        private static bool ReadSuccess(JToken token)
        {
            try
            {
                return token?.ToObject<bool>() ?? false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CreateNewAccountScene] Invalid success value: {ex.Message}");
                return false;
            }
        }

        private void ClearRequest()
        {
            requestPending = false;
            Debug.LogWarning("[CreateNewAccountScene] Account creation request timed out.");
        }
    }
}
