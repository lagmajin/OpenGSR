using UnityEngine;
//using KanKikuchi.AudioManager;
using Newtonsoft.Json.Linq;

namespace OpenGS
{


    [DisallowMultipleComponent]
    public class TSUVMainScript : AbstractMatchMainScript,ITSuvMainScript
    {
        [SerializeField] private TeamReSpawnPoints redTeamRespawnPoints;
        [SerializeField] private TeamReSpawnPoints blueTeamRespawnPoint;


        void Start()
        {


        }


        void Update()
        {

        }

        private void CreateMyPlayer()
        {



        }

        private void GameEnd()
        {

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


        }
    }


}
