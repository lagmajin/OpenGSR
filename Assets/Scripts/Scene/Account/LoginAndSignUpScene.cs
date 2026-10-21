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
        private ClientNetworkManager authoritativeClient;
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

            if (idInput != null)
            {
                idInput.onValueChanged.RemoveListener(OnFieldChanged);
            }

            if (passwordInput != null)
            {
                passwordInput.onValueChanged.RemoveListener(OnFieldChanged);
            }

            if (authoritativeClient != null)
            {
                authoritativeClient.TcpMessageReceived -= HandleAuthResponse;
                authoritativeClient = null;
            }

            authSubscription?.Dispose();
            authSubscription = null;
            base.OnDestroy();
        }

        private void OnApplicationQuit()
        {
            var hasBeforeLoginData = GameManager().HasBeforeLoginData();
        }

        /// <summary>
        /// The input fields, checked for something the server will accept.
        /// <para>
        /// Both buttons can be pressed with the fields empty, and the request
        /// went out anyway to be rejected by the server. This is only a check on
        /// the client; TryLogin still validates before it sends.
        /// </para>
        /// </summary>
        private void StringCheck()
        {
            var accountName = idInput != null ? idInput.text : id != null ? id.text : string.Empty;
            var password = passwordInput != null ? passwordInput.text : pass != null ? pass.text : string.Empty;

            var canSubmit = !string.IsNullOrWhiteSpace(accountName) && !string.IsNullOrWhiteSpace(password);

            if (loginButton != null)
            {
                loginButton.interactable = canSubmit;
            }

            if (createAccountButton != null)
            {
                createAccountButton.interactable = canSubmit;
            }
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

            if (!TrySendAuthRequest(new JObject
            {
                ["MessageType"] = MessageType.LoginRequest,
                ["AccountName"] = accountName,
                ["AccountID"] = accountName,
                ["Password"] = password,
                ["GlobalUserId"] = Guid.NewGuid().ToString("N")
            }, accountName))
            {
                Debug.LogWarning("[LoginAndSignUpScene] The login request could not be sent.");
            }
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

            TrySendAuthRequest(new JObject
            {
                ["MessageType"] = MessageType.CreateAccountRequest,
                ["AccountName"] = id.Trim(),
                ["AccountID"] = id.Trim(),
                ["DisplayName"] = id.Trim(),
                ["Password"] = password,
                ["GlobalUserId"] = Guid.NewGuid().ToString("N")
            }, id.Trim());
        }

        /// <summary>
        /// Sends an authentication request to whichever server this client is
        /// meant to be talking to.
        /// <para>
        /// It used to always go to the local simulation manager. That manager
        /// discards the address it is given and keeps its accounts in a file next
        /// to the game, so in the authoritative configuration the credentials were
        /// checked against a stub and the real server never heard of the account.
        /// The gate already existed to say which of the two this client uses; the
        /// login screen was simply not asking it.
        /// </para>
        /// </summary>
        private bool TrySendAuthRequest(JObject request, string accountName = "")
        {
            if (NetworkAuthorityGate.IsLocalSimulation())
            {
                if (!TryResolveNetworkManager(out var manager))
                {
                    return false;
                }

                authRequestPending = true;
                pendingAccountName = accountName;
                Invoke(nameof(ClearAuthRequest), 10f);
                manager.SendMessage(request);
                return true;
            }

            var client = FindFirstObjectByType<ClientNetworkManager>();
            if (client == null)
            {
                Debug.LogWarning(
                    "[LoginAndSignUpScene] ClientNetworkManager is not present; the authoritative server cannot be asked.");
                return false;
            }

            authRequestPending = true;
            pendingAccountName = accountName;
            Invoke(nameof(ClearAuthRequest), 10f);

            // The client network manager keeps the resume token and settles the
            // player id when the answer comes back, so the request is only the
            // credentials going out.
            client.SendTcpMessage(request);
            return true;
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

            // The buttons follow the fields, so an empty form cannot be submitted
            // at all rather than being sent and refused.
            if (idInput != null)
            {
                idInput.onValueChanged.AddListener(OnFieldChanged);
            }

            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnFieldChanged);
            }

            StringCheck();
        }

        private void OnFieldChanged(string _)
        {
            StringCheck();
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

        /// <summary>
        /// Listens for the answer on whichever transport the request goes out on.
        /// <para>
        /// It used to subscribe to the local simulation manager only. The
        /// authoritative path sends its request over the real client, so the
        /// answer came back on a stream this screen was not listening to: the
        /// login succeeded, the resume token was stored, and the screen sat there
        /// having done nothing about it.
        /// </para>
        /// </summary>
        private void BindAuthStream()
        {
            if (NetworkAuthorityGate.IsLocalSimulation())
            {
                if (authSubscription != null || networkManager == null)
                {
                    return;
                }

                authSubscription = networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(HandleAuthResponse);
                return;
            }

            if (authoritativeClient != null)
            {
                return;
            }

            authoritativeClient = FindFirstObjectByType<ClientNetworkManager>();
            if (authoritativeClient == null)
            {
                Debug.LogWarning(
                    "[LoginAndSignUpScene] ClientNetworkManager is not present; the answer to an authoritative " +
                    "login will not be seen by this screen.");
                return;
            }

            authoritativeClient.TcpMessageReceived += HandleAuthResponse;
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
