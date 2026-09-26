

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
        private bool transitionRequested;

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
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("[ShopScene] SoundManager is not ready; skipping title BGM setup.");
                return;
            }

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
            if (shopUIManager == null || string.IsNullOrWhiteSpace(str)) return;

            var temp = str.ToLower();
            if (temp == "player") SwitchTabAsync(EShopCategory.Character).Forget();
            else if (temp == "booster") SwitchTabAsync(EShopCategory.Booster).Forget();
            else if (temp == "instantitem") SwitchTabAsync(EShopCategory.InstantItem).Forget();
            else if (temp == "weapon") SwitchTabAsync(EShopCategory.Weapon).Forget();
        }

        private async UniTask SwitchTabAsync(EShopCategory category)
        {
            try
            {
                if (shopUIManager != null && shopUIManager.isActiveAndEnabled)
                {
                    await shopUIManager.SwitchCategory(category);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopScene] Failed to switch shop tab to {category}: {ex}");
            }
        }

        [Button("ロビー移動テスト")]
        public void BackToLobby()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
            var lobbyScene = generalSceneMasterData != null
                ? generalSceneMasterData.LobbyScene()
                : GeneralSceneMasterData.Instance().LobbyScene();
            LoadConfiguredScene(lobbyScene, "BackToLobby");
        }

        [Button("ウェイトルーム移動テスト")]
        private void BackToOnlineWaitroom()
        {
            if (transitionRequested)
            {
                return;
            }

            var nextScene = DetermineReturnScene();
            LoadConfiguredScene(nextScene, "BackToOnlineWaitroom");
        }
        [Button("オフラインウェイトルーム移動テスト")]
        private void BackToOfflineWaitRoom()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;
                LoadConfiguredScene(GeneralSceneMasterData.Instance().OfflineWaitRoomScene(), "BackToOfflineWaitRoom");
        }
        [Button("タイトル移動テスト")]
        private void GoToTitle()
        {
            GameFlagsManager.GetInstance().BeforeSceneName = SceneManager.GetActiveScene().name;

            LoadConfiguredScene(GeneralSceneMasterData.Instance().TitleScene(), "GoToTitle");
        }

        private void LoadConfiguredScene(string sceneName, string source)
        {
            if (transitionRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"[ShopScene] Scene name is not configured. source={source}");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ShopScene] Scene is not in build settings: {sceneName}. source={source}");
                return;
            }

            transitionRequested = true;
            if (SceneManager.LoadSceneAsync(sceneName) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[ShopScene] Failed to load scene: {sceneName}. source={source}");
            }
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
            return SynchronizationContext.Current ?? new SynchronizationContext();
        }

        protected override void OnStartUnityEditor()
        {
            EnsureTitleBgm();
        }
    }


}

