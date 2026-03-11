using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class MissionLobbyScene : AbstractNonBattleScene
    {


        private MissionLobbySceneMediateObject mediateObject;
        // Start is called before the first frame update
        void Start()
        {
            Application.targetFrameRate = 30;
        }

        public override SynchronizationContext MainThread()
        {
            return null;
        }

        // Update is called once per frame
        void Update()
        {

        }

        void FilterRoom()
        {

        }

        void SendChat(in string chat)
        {

        }

        public void CreateNewRoom()
        {

        }

        public void EnterRoom()
        {

        }

        void BackToConnectLobbyScene()
        {
            
        }

    }

}