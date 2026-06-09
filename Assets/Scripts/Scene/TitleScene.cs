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
            if (args != null && args.Length > 0 && "ExportAssetFiles" == args[0])
            {
                GoToExportAssetsScene();
            }

            Debug.Log("TitleScene");

            SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);

            var gameManager = GameGeneralManager.GetInstance;

            var info = new PlayerWaitRoomInfo();
            info.Name = "aaa";

            LoadSettingFile();
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.Keypad1))
            {
                //Invoke("gotoTitleScene", 1.5f);
            }

            if (Input.GetKey(KeyCode.Keypad2))
            {
                //Invoke("gotoTitleScene", 1.5f);
            }

            if (Input.GetKey(KeyCode.Keypad3))
            {
            }

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
            SceneManager.LoadScene(GeneralSceneMasterData.Instance().ConnectToServerScene());
        }

        [Button("オフラインウェイトルーム")]
        public void GoToOfflineWaitRoom()
        {
            bgmFlag = true;
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(GeneralSceneMasterData.Instance().OfflineWaitRoomScene());
        }

        [Button("アセットエクスポートシーンへ移動")]
        public void GoToExportAssetsScene()
        {
            bgmFlag = true;
            Debug.Log("[TitleScene] GoToExportAssetsScene");
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(GeneralSceneMasterData.Instance().ExportAssetScene());
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
