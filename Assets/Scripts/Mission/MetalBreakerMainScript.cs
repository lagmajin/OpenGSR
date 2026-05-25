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

            time += Time.deltaTime;

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
            SpawnPlayer();
            SpawnEnemy();
        }

        private void SpawnPlayer()
        {
            var prefab = PlayerPrefabStorage != null
                ? PlayerPrefabStorage
                : Resources.Load<GameObject>("Prefabs/Player/Misty");

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
            if (endFlag)
            {
                return;
            }

            endFlag = true;
            var nextScene = "MetalBreakerResultScene";
            SceneManager.LoadSceneAsync(nextScene);
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
            var nextScene = GeneralSceneMasterData.Instance().LobbyScene();
            SceneManager.LoadSceneAsync(nextScene);
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
