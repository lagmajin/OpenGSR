using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TSUVMainScript : AbstractMatchMainScript, ITSuvMainScript
    {
        [SerializeField] private TeamReSpawnPoints redTeamRespawnPoints;
        [SerializeField] private TeamReSpawnPoints blueTeamRespawnPoint;

        private new void Start()
        {
            base.Start();
            Debug.Log("[TSUVMainScript] Start");
            PlayGameStartVoice();
            CreateMyPlayer();
        }

        private void Update()
        {
            if (HandleEscapeToBackScene())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                GameEnd();
            }
        }

        private void CreateMyPlayer()
        {
            Vector3 spawnPoint = GetRandomSpawnPoint(blueTeamRespawnPoint, ETeam.Blue);
            CreateMyPlayer(spawnPoint, ETeam.Blue);
        }

        private void GameEnd()
        {
            GoToResult();
        }

        private void OnlineEventParser(AbstractMatchEvent e)
        {
            var eventName = e.EventName;

            if ("FlagReturnEvent" == eventName)
            {
            }

            if ("FlagLostEvent" == eventName)
            {
                //PlaySound.PlayBGM()
            }
        }

        private void OfflineEventParser(AbstractMatchEvent e)
        {
            //GameGeneralManager.GetInstance
        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (e == null)
            {
                return;
            }

            if (e is GameEndEvent)
            {
                GameEnd();
                return;
            }

            Debug.Log($"[TSUVMainScript] PostEvent: {e.EventName}");
        }
    }
}
