using UnityEngine;
using UnityEngine.SceneManagement;



//#pragma warning disable 

namespace OpenGS
{
    public class MissionMainScript : AbstractMatchMainScript
    {
        public GameObject respawnPoints;

        public MissionReSpawnPoints points;

        public MissionAndQuestMediateObject mediateObject;


        public float time = 0.0f;
        private new void Start()
        {

            SpawnPlayer();
        }

        private void Update()
        {

        }

        void SpawnPlayer()
        {
            var prefabPlayer = Resources.Load("Prefabs/Player/Misty") as GameObject;


            //mediateObject.



        }

        void RespawnPlayer()
        {

        }

        void MissionFail()
        {

        }

        void MissionClear()
        {

        }

        public override void PostEvent(AbstractGameEvent e)
        {



        }


    }
}
