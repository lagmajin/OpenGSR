

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
            DebugFlagManager.SetFirstSceneName("ShopScene");
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
            

            GameFlagsManager.GetInstance().BeforeSceneName = "ShopScene";

        }

        [Button("ウェイトルーム移動テスト")]
        private void BackToOnlineWaitroom()
        {

        }
        [Button("オフラインウェイトルーム移動テスト")]
        private void BackToOfflineWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "ShopScene";
        }
        [Button("タイトル移動テスト")]
        private void GoToTitle()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = "ShopScene";

            SceneManager.LoadSceneAsync("TitleScene");
        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }
    }


}

