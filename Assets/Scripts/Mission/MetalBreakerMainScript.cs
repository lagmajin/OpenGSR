#pragma warning disable 0108

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;



namespace OpenGS
{
    //[RequireComponent(MultipleTags)]
    public class MetalBreakerMainScript : AbstractMatchMainScript
    {
        [SerializeField]
        public GameObject respawnPoints;

        [SerializeField]
        public GameObject PlayerPrefabStorage;

        [SerializeField]
        public float time = 0.0f;

        private void Start()
        {


            Invoke("SpawnPlayer", 3.0f);

        }

        private void Update()
        {



        }

        void SpwanPlayer()
        {

        }

        void RespawnPlayer()
        {

        }

        void GaveUp()
        {


        }

        void SpawnEnemy()
        {

        }

        void EndGame()
        {

        }


        public void OnPlayerDead()
        {

        }
        public void OnGameFinished()
        {

        }
        void BackToLobby()
        {

        }

        override public void PostEvent(AbstractGameEvent e)
        {
            if (e.GetType() == typeof(PlayerDeadEvent))
            {

            }



        }
    }
}
