using UnityEngine;
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





        }

        public void GameStart()
        {

        }

        private void Update()
        {



        }

        public void ShowGaveUpDialog()
        {

        }

        public void MissionFail()
        {

        }

        public void MissionClear()
        {

        }

        void GaveUp()
        {

            ReturnWaitRoom();


        }

        void SpawnEnemy()
        {

        }

        void EndGame()
        {

        }

        void ReturnWaitRoom()
        {

        }

        override public void PostEvent(AbstractGameEvent e)
        {
            var eventName = e.EventName;



        }


    }

}
