using UnityEngine;
//using KanKikuchi.AudioManager;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    internal interface ISUVMatchMainScript
    {

    }


    [DisallowMultipleComponent]
    public class SUVMatchMainScript : AbstractMatchMainScript, ISUVMatchMainScript
    {


        private void Start()
        {

            SpawnPlayers();
        }

        private void Update()
        {
#if UNITY_EDITOR
            Debug.Log("debug");
#endif



        }

        void SpawnBot()
        {

        }

        void SpawnPlayers()
        {



        }

        void GoToViewerMode()
        {

        }

        void StartObserverMode()
        {

        }


        protected override void OnStart()
        {

        }

        protected override void OnEnd()
        {

        }

        protected override void OnSomeoneDead()
        {

        }


        protected override void OnNetworkDataRecved(JObject obj)
        {

        }

        private void OnlineEventParser(AbstractGameEvent e)
        {
            var eventName = e.EventName;


        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            var eventName = e.EventName;



        }

        public override void PostEvent(AbstractGameEvent e)
        {
  

        }


    }



}
