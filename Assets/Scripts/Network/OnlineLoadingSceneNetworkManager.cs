using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OnlineLoadingSceneNetworkManager : MonoBehaviour
    {
        private GeneralServerNetworkManager generalServerNetworkManager;
        private IOnlineLoadingScene onlineLoadingScene;
        private readonly SerialDisposable subscription = new SerialDisposable();

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            SubscribeToServer();
        }

        private void OnDisable()
        {
            subscription.Disposable = null;
        }

        public void SendLoadingStart()
        {
            SendLoadingState("LoadingStarted", 0f, "loading-started");
        }

        public void SendLoadingProgress(float progress)
        {
            SendLoadingState("LoadingProgress", Mathf.Clamp01(progress), "loading-progress");
        }

        public void SendLoadingComplete()
        {
            SendLoadingState("LoadingCompleted", 1f, "loading-complete");
        }

        public void SendLoadingMessage(string message)
        {
            var json = new JObject
            {
                ["MessageType"] = "LoadingMessage",
                ["Message"] = message ?? string.Empty
            };
            SendToServer(json);
        }

        private void ResolveDependencies()
        {
            if (generalServerNetworkManager == null)
            {
                try
                {
                    generalServerNetworkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                }
                catch
                {
                    generalServerNetworkManager = null;
                }
            }

            if (onlineLoadingScene == null)
            {
                onlineLoadingScene = FindFirstObjectByType<OnlineLoadingScene>();
            }
        }

        private void SubscribeToServer()
        {
            if (generalServerNetworkManager == null)
            {
                return;
            }

            subscription.Disposable = generalServerNetworkManager.DataReceivedStream
                .ObserveOnMainThread()
                .Subscribe(HandleServerMessage);
        }

        private void HandleServerMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (messageType == "AllowEnterMap")
            {
                ResolveDependencies();
                onlineLoadingScene?.OnEnterMapAllowed();
            }
            else if (messageType == "LoadingFailed")
            {
                ResolveDependencies();
                onlineLoadingScene?.OnLoadingFailed();
            }
            else if (messageType == "LoadingCompletedNotification")
            {
                ResolveDependencies();
                onlineLoadingScene?.OnMatchLoadingCompleted();
            }
        }

        private void SendLoadingState(string messageType, float progress, string message)
        {
            var json = new JObject
            {
                ["MessageType"] = messageType,
                ["PlayerID"] = AccountManager.Instance.CurrentProfile.GlobalUserId,
                ["AccountName"] = AccountManager.Instance.CurrentProfile.DisplayName,
                ["Progress"] = progress,
                ["Message"] = message
            };

            SendToServer(json);
        }

        private void SendToServer(JObject json)
        {
            ResolveDependencies();

            if (generalServerNetworkManager == null)
            {
                Debug.LogWarning($"[OnlineLoadingSceneNetworkManager] No GeneralServerNetworkManager for {json?["MessageType"]}");
                return;
            }

            generalServerNetworkManager.SendMessage(json);
        }
    }
}
