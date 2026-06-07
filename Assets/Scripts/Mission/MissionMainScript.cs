using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public class MissionMainScript : AbstractMatchMainScript
    {
        public MissionReSpawnPoints respawnPoints;
        public MissionReSpawnPoints points;
        public MissionAndQuestMediateObject mediateObject;
        public float time = 0.0f;

        private new void Start()
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
                MissionClear();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                MissionFail();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RespawnPlayer();
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
            Debug.Log("[MissionMainScript] GameStart");
        }

        private void SpawnPlayer()
        {
            var spawnPosition = ResolveSpawnPosition();
            if (player != null)
            {
                Destroy(player);
                player = null;
            }

            player = CreateMyPlayer(spawnPosition, ETeam.NoTeam);
            Debug.Log($"[MissionMainScript] SpawnPlayer at {spawnPosition}");
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (respawnPoints != null)
            {
                return GetRandomSpawnPoint(respawnPoints);
            }

            if (points != null)
            {
                return GetRandomSpawnPoint(points);
            }

            return Vector3.zero;
        }

        private void RespawnPlayer()
        {
            if (endFlag)
            {
                return;
            }

            SpawnPlayer();
        }

        private void MissionFail()
        {
            if (endFlag)
            {
                return;
            }

            endFlag = true;
            Debug.Log("[MissionMainScript] MissionFail");
            GoToMissionResult();
        }

        private void MissionClear()
        {
            if (endFlag)
            {
                return;
            }

            endFlag = true;
            Debug.Log("[MissionMainScript] MissionClear");
            GoToMissionResult();
        }

        private void GoToMissionResult()
        {
            var nextScene = mediateObject != null && mediateObject.GeneralSceneMasterData() != null
                ? mediateObject.GeneralSceneMasterData().MissionResultScene()
                : GeneralSceneMasterData.Instance().MissionResultScene();

            RequestSceneTransition(nextScene, "MissionMainToResult");
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
                MissionClear();
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

            Debug.Log($"[MissionMainScript] PostEvent: {e.EventName}");
        }
    }
}
