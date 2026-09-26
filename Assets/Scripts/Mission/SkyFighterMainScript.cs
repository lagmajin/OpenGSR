using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0414

namespace OpenGS
{
    public class SkyFighterMainScript : AbstractMatchMainScript
    {
        int diffucluty = 1;

        int needKillCount = 0;
        private bool transitionRequested;

        public GameObject ui;

        private new void Start()
        {
            base.Start();

            if (!CompareTag("MainScript"))
            {
                gameObject.tag = "MainScript";
            }

            GameStart();
        }

        public void GameStart()
        {
            if (isStarted)
            {
                return;
            }

            isStarted = true;
            endFlag = false;
            needKillCount = Mathf.Max(3, diffucluty * 5);
            PlayGameStartVoice();
            Debug.Log($"[SkyFighter] GameStart diff={diffucluty} needKill={needKillCount}");
            SpawnEnemy();
        }

        private void Update()
        {
            if (endFlag)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                MissionClear();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                MissionFail();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                ShowGaveUpDialog();
            }
        }

        public void ShowGaveUpDialog()
        {
            Debug.Log("[SkyFighter] ShowGaveUpDialog");
            GaveUp();
        }

        public void MissionFail()
        {
            Debug.Log("[SkyFighter] MissionFail");
            EndGame();
        }

        public void MissionClear()
        {
            Debug.Log("[SkyFighter] MissionClear");
            EndGame();
        }

        void GaveUp()
        {
            ReturnWaitRoom();
        }

        void SpawnEnemy()
        {
            Debug.Log($"[SkyFighter] SpawnEnemy diff={diffucluty} target={needKillCount}");
            if (ui != null)
            {
                ui.SetActive(true);
            }
        }

        void EndGame()
        {
            if (endFlag || transitionRequested)
            {
                return;
            }

            endFlag = true;
            var nextScene = GeneralSceneMasterData.Instance().MissionResultScene();
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogError("[SkyFighter] Mission result scene is not configured.");
                endFlag = false;
                return;
            }

            transitionRequested = true;
            Debug.Log($"[SkyFighter] EndGame -> {nextScene}");
            if (SceneManager.LoadSceneAsync(nextScene) == null)
            {
                transitionRequested = false;
                endFlag = false;
                Debug.LogError($"[SkyFighter] Failed to load scene: {nextScene}");
            }
        }

        void ReturnWaitRoom()
        {
            if (transitionRequested)
            {
                return;
            }

            var nextScene = GeneralSceneMasterData.Instance().MissionLobbyScene();
            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogError("[SkyFighter] Mission lobby scene is not configured.");
                return;
            }

            transitionRequested = true;
            Debug.Log($"[SkyFighter] ReturnWaitRoom -> {nextScene}");
            if (SceneManager.LoadSceneAsync(nextScene) == null)
            {
                transitionRequested = false;
                Debug.LogError($"[SkyFighter] Failed to load scene: {nextScene}");
            }
        }

        override public void PostEvent(AbstractGameEvent e)
        {
            var eventName = e.EventName;

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

            if (e is PlayerDeadEvent deadEvent)
            {
                var myPlayerId = player != null ? player.GetComponent<AbstractPlayer>()?.UniqueID().ToString() : null;
                if (!string.IsNullOrWhiteSpace(myPlayerId) && deadEvent.PlayerID() == myPlayerId)
                {
                    MissionFail();
                }
                return;
            }

            Debug.Log($"[SkyFighter] PostEvent: {eventName}");
        }
    }
}
