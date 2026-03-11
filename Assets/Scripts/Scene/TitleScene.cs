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

        // Start is called before the first frame update

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);

        }
        void Start()
        {

            if (playerNameField)
            {
                playerNameField.text = testName;
            }

            var args = System.Environment.GetCommandLineArgs();

            if ("ExportAssetFiles" == args[0])
            {
                GoToExportAssetsScene();
            }

            Debug.Log("TitleScene");


            var gameManager = GameGeneralManager.GetInstance;

            var info = new PlayerWaitRoomInfo();

            info.Name = "aaa";

            LoadSettingFile();


        }

        // Update is called once per frame
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

        }


        void LoadSettingFile()
        {

        }
        public void ChangeName(string str)
        {

        }



        private void OnActiveSceneChanged(Scene i_preChangedScene, Scene i_postChangedScene)
        {

        }

        private void OnSceneLoaded(Scene i_loadedScene, LoadSceneMode i_mode)
        {

        }

        private void OnSceneUnloaded(Scene i_unloadedScene)
        {


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

        }
        public void ConnectOnlineLobby()
        {
            bgmFlag = true;

            SceneManager.LoadScene("ConnectToServerScene");

            GameFlagsManager.GetInstance().BeforeSceneName = "TitleScene";
        }


        [Button("オフラインウェイトルーム")]
        public void GoToOfflineWaitRoom()
        {
            bgmFlag = true;

            SceneManager.LoadScene("OfflineWaitRoom");

            GameFlagsManager.GetInstance().BeforeSceneName = "TitleScene";
        }

        [Button("アセットエクスポートシーンへ移動")]
        public void GoToExportAssetsScene()
        {

        }

        [Button("自動セット")]
        public void AutoSet()
        {

        }


    }

}
