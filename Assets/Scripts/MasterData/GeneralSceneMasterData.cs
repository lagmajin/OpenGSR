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
        [SerializeField] private SceneObject connectToServerScene = "ConnectToServerScene";
        [SerializeField] private SceneObject loginScene = "LoginServerScene";

        [Header("Menu & Social")]
        [SerializeField] private SceneObject lobbyScene = "LobbyScene";
        [SerializeField] private SceneObject shopScene = "ShopScene";

        [Header("Waiting Room")]
        [SerializeField] private SceneObject onlineWaitRoomScene = "OnlineWaitRoom";
        [SerializeField] private SceneObject offlineWaitRoomScene = "OfflineWaitRoom";

        [Header("Loading & Result")]
        [SerializeField] private SceneObject offlineLoadingScene = "OfflineLoadingScene";
        [SerializeField] private SceneObject onlineLoadingScene = "OnlineLoadingScene";
        [SerializeField] private SceneObject resultScene = "ResultScene";

        [Header("Mission & Setting")]
        [SerializeField] private SceneObject missionLobbyScene = "MissionLobbyScene";
        [SerializeField] private SceneObject missionResultScene = "MissionResultScene";
        [SerializeField] private SceneObject gameSettingScene = "GameSettingScene";
        [SerializeField] private SceneObject exportAssetScene = "ExportAssetScene";

        private static GeneralSceneMasterData _instance;

        public static GeneralSceneMasterData Instance()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GeneralSceneMasterData>("MasterData/GeneralSceneMasterData");
            }
            return _instance;
        }

        // Accessors (Title Case as per conventions)
        public string SplashScene() => splashScene;
        public string TitleScene() => titleScene;
        public string ConnectToServerScene() => connectToServerScene;
        public string LoginScene() => loginScene;
        public string LobbyScene() => lobbyScene;
        public string ShopScene() => shopScene;
        public string OnlineWaitRoomScene() => onlineWaitRoomScene;
        public string OfflineWaitRoomScene() => offlineWaitRoomScene;
        public string OfflineLoadingScene() => offlineLoadingScene;
        public string OnlineLoadingScene() => onlineLoadingScene;
        public string ResultScene() => resultScene;
        public string MissionLobbyScene() => missionLobbyScene;
        public string MissionResultScene() => missionResultScene;
        public string GameSettingScene() => gameSettingScene;
        public string ExportAssetScene() => exportAssetScene;
    }
}
