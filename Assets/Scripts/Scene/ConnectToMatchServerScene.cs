using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGSCore;

namespace OpenGS
{
    public class ConnectToMatchServerScene : MonoBehaviour
    {
        [SerializeField] private string fallbackSceneName = "";
        [SerializeField] private float timeoutSeconds = 10f;

        private ClientNetworkManager clientNetworkManager;
        private float elapsedSeconds;
        private bool transitionRequested;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(GetType().FullName);
            clientNetworkManager = ClientNetworkManager.EnsureExists();
        }

        private void OnEnable()
        {
            if (clientNetworkManager == null)
            {
                clientNetworkManager = ClientNetworkManager.EnsureExists();
            }

            clientNetworkManager.MatchServerConnected += HandleMatchServerConnected;
            clientNetworkManager.MatchServerDisconnected += HandleMatchServerDisconnected;
        }

        private void Start()
        {
            elapsedSeconds = 0f;
            transitionRequested = false;

            if (clientNetworkManager.IsMatchServerConnected)
            {
                HandleMatchServerConnected();
                return;
            }

            clientNetworkManager.EnsureMatchUdpConnection();
        }

        private void Update()
        {
            if (transitionRequested)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds < timeoutSeconds)
            {
                return;
            }

            Debug.LogWarning("[ConnectToMatchServerScene] Timed out waiting for match server connection.");
            LoadFallbackScene();
        }

        private void OnDisable()
        {
            if (clientNetworkManager == null)
            {
                return;
            }

            clientNetworkManager.MatchServerConnected -= HandleMatchServerConnected;
            clientNetworkManager.MatchServerDisconnected -= HandleMatchServerDisconnected;
        }

        private void HandleMatchServerConnected()
        {
            if (transitionRequested)
            {
                return;
            }

            transitionRequested = true;
            var targetScene = ResolveTargetSceneName();
            if (string.IsNullOrWhiteSpace(targetScene))
            {
                Debug.LogWarning("[ConnectToMatchServerScene] No target scene resolved after match server connection.");
                return;
            }

            SceneManager.LoadSceneAsync(targetScene);
        }

        private void HandleMatchServerDisconnected()
        {
            Debug.LogWarning("[ConnectToMatchServerScene] Match server disconnected.");
        }

        private string ResolveTargetSceneName()
        {
            var selectedMap = GameModeSelectManager.Instance?.OnlineGameSelect?.Map ?? EMap.Unknown;
            var mediateObject = FindFirstObjectByType<OnlineLoadingSceneMediateObject>();
            var mapMaster = mediateObject != null ? mediateObject.MapSceneMasterData() : null;
            return mapMaster?.Map(selectedMap)?.MapScene();
        }

        private void LoadFallbackScene()
        {
            if (transitionRequested)
            {
                return;
            }

            transitionRequested = true;
            var sceneName = !string.IsNullOrWhiteSpace(fallbackSceneName)
                ? fallbackSceneName
                : GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
