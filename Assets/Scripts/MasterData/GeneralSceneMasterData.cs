using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Scene/GeneralSceneMasterData")]
    public class GeneralSceneMasterData : ScriptableObject
    {
        [SerializeField] private SceneObject titleScene = "TitleScene";
        [SerializeField] private SceneObject shopScene = "ShopScene";
        [SerializeField] private SceneObject onlineWaitRoomScene = "OnlineWaitRoomScene";
        [SerializeField] private SceneObject onlineLoadingScene = "OnlineLoadingScene";

        private static GeneralSceneMasterData _instance;

        public static GeneralSceneMasterData Instance()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GeneralSceneMasterData>("MasterData/Scene/GeneralSceneMasterData");
            }
            return _instance;
        }

        public string TitleScene() => titleScene;
        public string ShopScene() => shopScene;
        public string OnlineWaitRoomScene() => onlineWaitRoomScene;
        public string OnlineLoadingScene() => onlineLoadingScene;
    }
}
