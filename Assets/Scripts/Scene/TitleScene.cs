using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0414

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TitleScene : MonoBehaviour
    {
        private string testName = "Player1234";
        private bool sceneTransitionRequested;

        static bool bgmFlag = false;

        //[SerializeField]
        //private GameObject sceneStorage;

        [SerializeField]
        private InputField playerNameField;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void Start()
        {
            if (playerNameField)
            {
                playerNameField.text = testName;
            }

            var args = System.Environment.GetCommandLineArgs();
            if (args != null && System.Array.Exists(args, arg => string.Equals(arg, "ExportAssetFiles", System.StringComparison.OrdinalIgnoreCase)))
            {
                GoToExportAssetsScene();
            }

            Debug.Log("TitleScene");

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);
            }
            else
            {
                Debug.LogWarning("[TitleScene] SoundManager is not ready; skipping title BGM setup.");
            }

            var gameManager = GameGeneralManager.GetInstance;

            var info = new PlayerWaitRoomInfo();
            info.Name = "aaa";

            LoadSettingFile();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                quit();
            }
        }

        void OnApplicationQuit()
        {
            Debug.Log("[TitleScene] Application quitting");
            UnsubscribeSceneEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeSceneEvents();
        }

        void LoadSettingFile()
        {
            if (playerNameField != null && string.IsNullOrWhiteSpace(playerNameField.text))
            {
                playerNameField.text = testName;
            }
        }

        public void ChangeName(string str)
        {
            testName = string.IsNullOrWhiteSpace(str) ? testName : str.Trim();

            if (playerNameField != null)
            {
                playerNameField.text = testName;
            }
        }

        private void OnActiveSceneChanged(Scene i_preChangedScene, Scene i_postChangedScene)
        {
            Debug.Log($"[TitleScene] Active scene changed: {i_preChangedScene.name} -> {i_postChangedScene.name}");
        }

        private void OnSceneLoaded(Scene i_loadedScene, LoadSceneMode i_mode)
        {
            Debug.Log($"[TitleScene] Scene loaded: {i_loadedScene.name}");
        }

        private void OnSceneUnloaded(Scene i_unloadedScene)
        {
            Debug.Log($"[TitleScene] Scene unloaded: {i_unloadedScene.name}");
        }

        private void UnsubscribeSceneEvents()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        void quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            UnityEngine.Application.Quit();
#endif
        }

        [Button("エラーメッセージ表示テスト")]
        public void ShowErrorMessage()
        {
            Debug.LogWarning("[TitleScene] Error message test");
        }

        public void ConnectOnlineLobby()
        {
            bgmFlag = true;
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            LoadConfiguredScene(GeneralSceneMasterData.Instance().ConnectToServerScene(), "ConnectOnlineLobby");
        }

        [Button("オフラインウェイトルーム")]
        public void GoToOfflineWaitRoom()
        {
            bgmFlag = true;
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            LoadConfiguredScene(GeneralSceneMasterData.Instance().OfflineWaitRoomScene(), "GoToOfflineWaitRoom");
        }

        [Button("アセットエクスポートシーンへ移動")]
        public void GoToExportAssetsScene()
        {
            bgmFlag = true;
            Debug.Log("[TitleScene] GoToExportAssetsScene");
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            LoadConfiguredScene(GeneralSceneMasterData.Instance().ExportAssetScene(), "GoToExportAssetsScene");
        }

        private void LoadConfiguredScene(string sceneName, string source)
        {
            if (sceneTransitionRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"[TitleScene] Scene name is not configured. source={source}");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[TitleScene] Scene is not in build settings: {sceneName}. source={source}");
                return;
            }

            sceneTransitionRequested = true;
            SceneManager.LoadScene(sceneName);
        }

        [Button("自動セット")]
        public void AutoSet()
        {
            if (playerNameField == null)
            {
                playerNameField = FindFirstObjectByType<InputField>();
            }

            if (playerNameField != null && string.IsNullOrWhiteSpace(playerNameField.text))
            {
                playerNameField.text = testName;
            }
        }
    }
}
