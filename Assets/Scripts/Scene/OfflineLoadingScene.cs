using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenGSCore;

#pragma warning disable 0414

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class OfflineLoadingScene : AbstractLoadingScene, IOfflineLoadingScene
    {
        private bool loadImmediately = true;
        private float count = 0.0f;
        private float timeout = 20.0f;

        public MapSceneMasterData mapMasterdata;
        public GeneralSceneMasterData senes;
        public LoadingSpriteMasterData sp;

        private void Start()
        {
            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugSelect();
            }

            if (loadImmediately)
            {
                LoadingStart();
            }
        }

        private void Update()
        {
        }

        public void DebugScene()
        {
        }

        private void Awake()
        {
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void OnApplicationQuit()
        {
        }

        public void LoadingStart()
        {
            StartCoroutine(LoadingCoroutine());
        }

        private IEnumerator LoadingCoroutine()
        {
            var matchRoomManager = MatchRoomManager();
            if (!matchRoomManager.IsValidOfflineWaitRoom())
            {
                matchRoomManager.CreateNewOfflineWaitRoom("OfflineRoom");
            }

            var waitRoom = matchRoomManager.WaitRoom;
            if (waitRoom != null)
            {
                var players = waitRoom.AllPlayers();
                UnityEngine.Debug.Log($"Offline loading players={players.Count}");
            }

            matchRoomManager.CreateNewOfflineMatchRoom();

            var select = GameModeSelectManager.Instance.OfflineGameSelect;
            var sceneName = ResolveOfflineBattleSceneName(select?.GameMode ?? EGameMode.DeathMatch, select?.Map ?? EMap.DryDays);
            UnityEngine.Debug.Log($"Offline loading scene={sceneName}");

            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            async.allowSceneActivation = false;

            yield return new WaitForSecondsRealtime(1);
            async.allowSceneActivation = true;
        }

        void AppointRoomOwner()
        {
        }

        void GoToBattleScene()
        {
        }

        void BackToWaitRoom()
        {
        }

        void BackToTitleScene()
        {
        }

        private static string ResolveOfflineBattleSceneName(EGameMode mode, EMap map)
        {
            var resolved = ResolveSceneFromMapAsset(map);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }

            return mode switch
            {
                EGameMode.CaptureTheFlag => "DryDays(Stage)(CTF)",
                EGameMode.TeamDeathMatch => "GreenHill1",
                EGameMode.Survival => "DryDays",
                EGameMode.TeamSurvival => "DryDays",
                _ => "DryDays(Stage)(DM)",
            };
        }

        private static string ResolveSceneFromMapAsset(EMap map)
        {
            var assets = Resources.LoadAll<MapInfoMasterData>("MasterData/Map");
            foreach (var asset in assets)
            {
                if (asset == null || asset.MapType() != map)
                {
                    continue;
                }

                var scene = asset.MapScene();
                var sceneName = scene != null ? scene.SceneName() : string.Empty;
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    return sceneName;
                }
            }

            return string.Empty;
        }
    }
}
