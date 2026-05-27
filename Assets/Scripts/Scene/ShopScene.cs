

using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ShopScene:AbstractScene
    {

        [Header("UI Reference")]
        [SerializeField] private ShopUIManager shopUIManager;

        public GeneralSceneMasterData generalScene;

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(SceneManager.GetActiveScene().name);
            if (shopUIManager == null)
            {
                shopUIManager = FindFirstObjectByType<ShopUIManager>();
            }
            if (shopUIManager == null)
            {
                var shopCanvas = GameObject.Find("ShopCanvas");
                if (shopCanvas != null)
                {
                    shopUIManager = shopCanvas.GetComponent<ShopUIManager>();
                    if (shopUIManager == null)
                    {
                        shopUIManager = shopCanvas.AddComponent<ShopUIManager>();
                    }
                }
            }
        }

        private void Start()
        {
            EnsureTitleBgm();
            Debug.Log("EnterShopScene");
        }

        private void EnsureTitleBgm()
        {
            if (SoundManager.Instance.IsBgmPlaying(EBgm.Title))
            {
                Debug.Log("[ShopScene] Title BGM is already playing.");
                return;
            }

            Debug.Log("[ShopScene] Switching to Title BGM.");
            SoundManager.Instance.EnsureBgm(EBgm.Title, 0f);
        }

        protected override void Update()
        {
            base.Update();
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
            if (temp == "player") shopUIManager.SwitchCategory(EShopCategory.Character).Forget();
            else if (temp == "booster") shopUIManager.SwitchCategory(EShopCategory.Booster).Forget();
            else if (temp == "instantitem") shopUIManager.SwitchCategory(EShopCategory.InstantItem).Forget();
            else if (temp == "weapon") shopUIManager.SwitchCategory(EShopCategory.Weapon).Forget();
        }

        [Button("ロビー移動テスト")]
        public void BackToLobby()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            var lobbyScene = generalSceneMasterData != null
                ? generalSceneMasterData.LobbyScene()
                : GeneralSceneMasterData.Instance().LobbyScene();
            SceneManager.LoadSceneAsync(lobbyScene);
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

        protected override void OnStartUnityEditor()
        {
            EnsureTitleBgm();
        }
    }


}

