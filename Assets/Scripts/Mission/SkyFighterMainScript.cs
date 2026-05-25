using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0414

namespace OpenGS
{
    public class SkyFighterMainScript : AbstractMatchMainScript
    {
        int diffucluty = 1;

        int needKillCount = 0;

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
            Debug.Log($"[SkyFighter] GameStart diff={diffucluty} needKill={needKillCount}");
            SpawnEnemy();
        }

        private void Update()
        {
            if (endFlag)
            {
                return;
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
            Debug.Log($"[SkyFighter] SpawnEnemy diff={diffucluty}");
        }

        void EndGame()
        {
            if (endFlag)
            {
                return;
            }

            endFlag = true;
            var nextScene = GeneralSceneMasterData.Instance().MissionResultScene();
            Debug.Log($"[SkyFighter] EndGame -> {nextScene}");
            SceneManager.LoadSceneAsync(nextScene);
        }

        void ReturnWaitRoom()
        {
            var nextScene = GeneralSceneMasterData.Instance().MissionLobbyScene();
            Debug.Log($"[SkyFighter] ReturnWaitRoom -> {nextScene}");
            SceneManager.LoadSceneAsync(nextScene);
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
