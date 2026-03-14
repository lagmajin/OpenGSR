using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    /// <summary>
    /// プロジェクト全体の共通システムシーン（非戦闘シーン）を定義するマスターデータ。
    /// 文字列のハードコードを避け、SceneObject を通じてインスペクターで安全に設定可能にする。
    /// </summary>
    [CreateAssetMenu(menuName = "MasterData/Scene/GeneralSceneMasterData")]
    public class GeneralSceneMasterData : ScriptableObject
    {
        [Header("Startup & Auth")]
        [SerializeField] private SceneObject splashScene = "SplashScreen";
        [SerializeField] private SceneObject titleScene = "TitleScene";
        [SerializeField] private SceneObject loginScene = "LoginServerScene";

        [Header("Menu & Social")]
        [SerializeField] private SceneObject lobbyScene = "LobbyScene";
        [SerializeField] private SceneObject shopScene = "ShopScene";

        [Header("Waiting Room")]
        [SerializeField] private SceneObject onlineWaitRoomScene = "OnlineWaitRoomScene";
        [SerializeField] private SceneObject offlineWaitRoomScene = "OfflineWaitRoom";

        [Header("Loading & Result")]
        [SerializeField] private SceneObject onlineLoadingScene = "OnlineLoadingScene";
        [SerializeField] private SceneObject resultScene = "ResultScene";

        private static GeneralSceneMasterData _instance;

        public static GeneralSceneMasterData Instance()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GeneralSceneMasterData>("ScriptableObject/Scene/GeneralSceneMasterData");
                if (_instance == null)
                {
                    // フォールバック（旧パス）
                    _instance = Resources.Load<GeneralSceneMasterData>("MasterData/Scene/GeneralSceneMasterData");
                }
            }
            return _instance;
        }

        // Accessors (Title Case as per conventions)
        public string SplashScene() => splashScene;
        public string TitleScene() => titleScene;
        public string LoginScene() => loginScene;
        public string LobbyScene() => lobbyScene;
        public string ShopScene() => shopScene;
        public string OnlineWaitRoomScene() => onlineWaitRoomScene;
        public string OfflineWaitRoomScene() => offlineWaitRoomScene;
        public string OnlineLoadingScene() => onlineLoadingScene;
        public string ResultScene() => resultScene;
    }
}
