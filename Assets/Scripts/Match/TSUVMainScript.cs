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
            CreateMyPlayer();
        }

        private void Update()
        {
        }

        private void CreateMyPlayer()
        {
            var spawnPoint = blueTeamRespawnPoint != null
                ? blueTeamRespawnPoint.RandomBlueTeam()
                : Vector2.zero;

            CreateMyPlayer(new Vector3(spawnPoint.x, spawnPoint.y, 0), ETeam.Blue);
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
