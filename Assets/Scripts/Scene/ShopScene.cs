

using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using System.Threading;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ShopScene:AbstractScene
    {

        [Header("UI Reference")]
        [SerializeField] private ShopUIManager shopUIManager;

        public GeneralSceneMasterData generalScene;

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(SceneManager.GetActiveScene().name);
        }

        private void Start()
        {
            Debug.Log("EnterShopScene");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ここでタイトルの戻り先を判定
                GoToTitle();
            }
        }

        public void ChangeTab(string str)
        {
            if (shopUIManager == null) return;

            var temp = str.ToLower();
            if (temp == "player") shopUIManager.SwitchCategory(EShopCategory.Character);
            else if (temp == "booster") shopUIManager.SwitchCategory(EShopCategory.Booster);
            else if (temp == "instantitem") shopUIManager.SwitchCategory(EShopCategory.InstantItem);
            else if (temp == "weapon") shopUIManager.SwitchCategory(EShopCategory.Weapon);
        }

        [Button("ロビー移動テスト")]
        private void BackToLobby()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            GoToLobby();
        }

        [Button("ウェイトルーム移動テスト")]
        private void BackToOnlineWaitroom()
        {
            var nextScene = DetermineReturnScene();
            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                SceneManager.LoadSceneAsync(nextScene);
            }
        }
        [Button("オフラインウェイトルーム移動テスト")]
        private void BackToOfflineWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().OfflineWaitRoomScene());
        }
        [Button("タイトル移動テスト")]
        private void GoToTitle()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;

            SceneManager.LoadSceneAsync(GeneralSceneMasterData.Instance().TitleScene());
        }

        private static string DetermineReturnScene()
        {
            var beforeScene = GameFlagsManager.GetInstance().BeforeSceneName;
            if (beforeScene == GeneralSceneMasterData.Instance().OnlineWaitRoomScene())
            {
                return GeneralSceneMasterData.Instance().OnlineWaitRoomScene();
            }

            if (beforeScene == GeneralSceneMasterData.Instance().OfflineWaitRoomScene())
            {
                return GeneralSceneMasterData.Instance().OfflineWaitRoomScene();
            }

            return GeneralSceneMasterData.Instance().LobbyScene();
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current;
        }
    }


}

