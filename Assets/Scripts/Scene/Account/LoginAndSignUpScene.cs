using System.Threading;
using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UniRx;
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
        private GeneralServerNetworkManager networkManager;
        private IDisposable authSubscription;
        private bool authRequestPending;
        private bool transitionRequested;
        private string pendingAccountName = string.Empty;
        private TMP_InputField idInput;
        private TMP_InputField passwordInput;
        private Button loginButton;
        private Button createAccountButton;

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void Start()
        {
            try
            {
                BindSceneUi();
                networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                BindAuthStream();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoginAndSignUpScene] Failed to bind authentication responses: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            loginButton?.onClick.RemoveListener(TryLogin);
            createAccountButton?.onClick.RemoveListener(TrySignUpFromScene);
            authSubscription?.Dispose();
            authSubscription = null;
            base.OnDestroy();
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
            if (authRequestPending)
            {
                return;
            }

            var accountName = idInput != null ? idInput.text?.Trim() : id != null ? id.text?.Trim() : "";
            var password = passwordInput != null ? passwordInput.text : pass != null ? pass.text : "";
            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(password))
            {
                Debug.LogWarning("[LoginAndSignUpScene] Login requires an account name and password.");
                return;
            }

            if (!TryResolveNetworkManager(out var manager))
            {
                return;
            }

            authRequestPending = true;
            pendingAccountName = accountName;
            Invoke(nameof(ClearAuthRequest), 10f);
            manager.SendMessage(new JObject
            {
                ["MessageType"] = MessageType.LoginRequest,
                ["AccountName"] = accountName,
                ["AccountID"] = accountName,
                ["Password"] = password,
                ["GlobalUserId"] = Guid.NewGuid().ToString("N")
            });
        }

        public void TrySignUp(in string id, in string password)
        {
            if (authRequestPending)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                Debug.LogWarning("[LoginAndSignUpScene] Sign-up requires an account name and password.");
                return;
            }

            if (!TryResolveNetworkManager(out var manager))
            {
                return;
            }

            authRequestPending = true;
            pendingAccountName = id.Trim();
            Invoke(nameof(ClearAuthRequest), 10f);
            manager.SendMessage(new JObject
            {
                ["MessageType"] = MessageType.CreateAccountRequest,
                ["AccountName"] = id.Trim(),
                ["AccountID"] = id.Trim(),
                ["DisplayName"] = id.Trim(),
                ["Password"] = password,
                ["GlobalUserId"] = Guid.NewGuid().ToString("N")
            });
        }

        private void TrySignUpFromScene()
        {
            TrySignUp(
                idInput != null ? idInput.text : string.Empty,
                passwordInput != null ? passwordInput.text : string.Empty);
        }

        private void BindSceneUi()
        {
            idInput = GameObject.Find("InputField (TMP)_1")?.GetComponent<TMP_InputField>();
            passwordInput = GameObject.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
            loginButton = GameObject.Find("LoginButton")?.GetComponent<Button>();
            createAccountButton = GameObject.Find("CreateNewAccount")?.GetComponent<Button>();

            loginButton?.onClick.RemoveListener(TryLogin);
            loginButton?.onClick.AddListener(TryLogin);
            createAccountButton?.onClick.RemoveListener(TrySignUpFromScene);
            createAccountButton?.onClick.AddListener(TrySignUpFromScene);
        }

        private bool TryResolveNetworkManager(out GeneralServerNetworkManager manager)
        {
            manager = networkManager;
            if (manager != null)
            {
                BindAuthStream();
                return true;
            }

            try
            {
                manager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                networkManager = manager;
                BindAuthStream();
                return manager != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoginAndSignUpScene] Authentication service is unavailable: {ex.Message}");
                return false;
            }
        }

        private void BindAuthStream()
        {
            if (authSubscription != null || networkManager == null)
            {
                return;
            }

            authSubscription = networkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(HandleAuthResponse);
        }

        private void HandleAuthResponse(JObject json)
        {
            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            if (messageType != MessageType.LoginResponse && messageType != MessageType.CreateAccountResponse)
            {
                return;
            }

            authRequestPending = false;
            CancelInvoke(nameof(ClearAuthRequest));

            var success = ReadSuccess(json?["Success"]);
            if (!success)
            {
                var errorMessage = json["Error"]?.ToString()
                    ?? json["ErrorMessage"]?.ToString()
                    ?? "Unknown error";
                Debug.LogWarning($"[LoginAndSignUpScene] Authentication failed: {errorMessage}");
                return;
            }

            var accountName = json["AccountName"]?.ToString()
                ?? json["PlayerName"]?.ToString()
                ?? json["AccountID"]?.ToString()
                ?? pendingAccountName;
            var globalUserId = json["GlobalUserId"]?.ToString()
                ?? json["PlayerID"]?.ToString()
                ?? json["AccountID"]?.ToString()
                ?? accountName;
            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(globalUserId))
            {
                Debug.LogWarning("[LoginAndSignUpScene] Authentication response did not contain account identity.");
                return;
            }

            AccountManager.Instance?.LoginData(accountName, "", globalUserId);
            var titleScene = GeneralSceneMasterData.Instance()?.TitleScene();
            if (string.IsNullOrWhiteSpace(titleScene))
            {
                Debug.LogError("[LoginAndSignUpScene] Title scene is not configured.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(titleScene))
            {
                Debug.LogError($"[LoginAndSignUpScene] Title scene is not in build settings: {titleScene}");
                return;
            }

            if (transitionRequested)
            {
                return;
            }

            transitionRequested = true;
            if (SceneManager.LoadSceneAsync(titleScene) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[LoginAndSignUpScene] Failed to load title scene: {titleScene}");
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
                Debug.LogWarning($"[LoginAndSignUpScene] Invalid success value: {ex.Message}");
                return false;
            }
        }

        private void ClearAuthRequest()
        {
            authRequestPending = false;
            Debug.LogWarning("[LoginAndSignUpScene] Authentication request timed out.");
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current ?? new SynchronizationContext();
        }
    }
}
