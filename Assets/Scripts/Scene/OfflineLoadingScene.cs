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
        private bool loadingStarted;
        private Coroutine loadingCoroutine;

        public MapSceneMasterData mapMasterdata;
        public GeneralSceneMasterData senes;
        public LoadingSpriteMasterData sp;

        private void Start()
        {
            EnsureLoadingBgm();

            if (DebugFlagManager.IsDebug())
            {
                //GameGeneralManager.GetInstance.LoadDebugSelect();
            }

            if (loadImmediately)
            {
                LoadingStart();
            }
        }

        protected override void Update()
        {
            base.Update();
        }

        public void DebugScene()
        {
            LoadingStart();
        }

        protected override void Awake()
        {
            base.Awake();
            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
        }

        private void EnsureLoadingBgm()
        {
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("[OfflineLoadingScene] SoundManager is not ready; skipping loading BGM.");
                return;
            }

            if (SoundManager.Instance.IsBgmPlaying(EBgm.WaitRoom))
            {
                return;
            }

            if (!SoundManager.Instance.IsBgmPlaying())
            {
                SoundManager.Instance.EnsureBgm(EBgm.WaitRoom, 0f);
            }
        }

        public void LoadingStart()
        {
            if (loadingStarted)
            {
                return;
            }

            loadingStarted = true;
            loadingCoroutine = StartCoroutine(LoadingCoroutine());
        }

        protected override void OnDestroy()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }

            base.OnDestroy();
        }

        private IEnumerator LoadingCoroutine()
        {
            var matchRoomManager = MatchRoomManager();
            if (matchRoomManager == null)
            {
                Debug.LogError("[OfflineLoadingScene] MatchRoomManager is not available.");
                loadingStarted = false;
                yield break;
            }

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

            var select = GameModeSelectManager.Instance != null
                ? GameModeSelectManager.Instance.OfflineGameSelect
                : null;
            var sceneName = ResolveOfflineBattleSceneName(select?.GameMode ?? EGameMode.DeathMatch, select?.Map ?? EMap.DryDays);
            UnityEngine.Debug.Log($"Offline loading scene={sceneName}");

            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[OfflineLoadingScene] Battle scene is not available in build settings: {sceneName}");
                loadingStarted = false;
                yield break;
            }

            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (async == null)
            {
                Debug.LogError($"[OfflineLoadingScene] Failed to load battle scene: {sceneName}");
                loadingStarted = false;
                yield break;
            }
            async.allowSceneActivation = false;

            yield return new WaitForSecondsRealtime(1);
            async.allowSceneActivation = true;
        }

        void AppointRoomOwner()
        {
            var manager = MatchRoomManager();
            if (manager != null && !manager.IsValidOfflineWaitRoom())
            {
                manager.CreateNewOfflineWaitRoom("OfflineRoom");
            }
        }

        void GoToBattleScene()
        {
            LoadingStart();
        }

        void BackToWaitRoom()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            loadingStarted = false;
            var waitRoomScene = GeneralSceneMasterData.Instance().OfflineWaitRoomScene();
            RequestSceneTransition(waitRoomScene, "OfflineLoadingBackToWaitRoom");
        }

        void BackToTitleScene()
        {
            GoToTitleScene();
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
