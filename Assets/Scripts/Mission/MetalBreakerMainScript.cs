#pragma warning disable 0108

using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public class MetalBreakerMainScript : AbstractMatchMainScript, IMetalBreakerMainScript
    {
        [SerializeField] public GameObject respawnPoints;
        [SerializeField] public GameObject PlayerPrefabStorage;
        [SerializeField] public float time = 0.0f;
        private bool transitionRequested;

        private void Start()
        {
            base.Start();
            GameStart();
        }

        private void Update()
        {
            if (endFlag)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            time = Mathf.Max(0f, (float.IsFinite(time) ? time : 0f) + deltaTime);

            if (Input.GetKeyDown(KeyCode.F1))
            {
                OnGameFinished();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                OnPlayerDead();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                GaveUp();
            }
        }

        private void GameStart()
        {
            if (isStarted)
            {
                return;
            }

            isStarted = true;
            endFlag = false;
            time = 0f;
            PlayGameStartVoice();
            SpawnPlayer();
            SpawnEnemy();
        }

        private void SpawnPlayer()
        {
            var prefab = PlayerPrefabStorage != null
                ? PlayerPrefabStorage
                : Resources.Load<PlayerPrefabMasterData>("MasterData/Player/PlayerPrefabMasterData")
                    ?.SearchPlayerPrefab(OpenGSCore.EPlayerCharacter.Misty);

            if (prefab == null)
            {
                Debug.LogWarning("[MetalBreakerMainScript] Player prefab not found.");
                return;
            }

            var spawnPosition = respawnPoints != null ? respawnPoints.transform.position : Vector3.zero;
            player = Instantiate(prefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[MetalBreakerMainScript] SpawnPlayer at {spawnPosition}");
        }

        private void RespawnPlayer()
        {
            if (endFlag)
            {
                return;
            }

            if (player != null)
            {
                Destroy(player);
                player = null;
            }

            SpawnPlayer();
        }

        private void GaveUp()
        {
            if (endFlag)
            {
                return;
            }

            endFlag = true;
            BackToLobby();
        }

        private void SpawnEnemy()
        {
            Debug.Log("[MetalBreakerMainScript] SpawnEnemy");
        }

        private void EndGame()
        {
            if (endFlag || transitionRequested)
            {
                return;
            }

            endFlag = true;
            var nextScene = GeneralSceneMasterData.Instance()?.MissionResultScene();
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogError("[MetalBreakerMainScript] Mission result scene is not configured.");
                endFlag = false;
                return;
            }

            transitionRequested = true;
            if (SceneManager.LoadSceneAsync(nextScene) == null)
            {
                transitionRequested = false;
                endFlag = false;
                Debug.LogError($"[MetalBreakerMainScript] Failed to load scene: {nextScene}");
            }
        }

        public void OnPlayerDead()
        {
            RespawnPlayer();
        }

        public void OnGameFinished()
        {
            EndGame();
        }

        private void BackToLobby()
        {
            if (transitionRequested)
            {
                return;
            }

            var nextScene = GeneralSceneMasterData.Instance().LobbyScene();
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogError("[MetalBreakerMainScript] Lobby scene is not configured.");
                return;
            }

            transitionRequested = true;
            if (SceneManager.LoadSceneAsync(nextScene) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[MetalBreakerMainScript] Failed to load scene: {nextScene}");
            }
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (e is GameStartEvent)
            {
                GameStart();
                return;
            }

            if (e is GameEndEvent)
            {
                EndGame();
                return;
            }

            if (e is PlayerDeadEvent)
            {
                OnPlayerDead();
                return;
            }

            Debug.Log($"[MetalBreakerMainScript] PostEvent: {e.EventName}");
        }
    }
}
