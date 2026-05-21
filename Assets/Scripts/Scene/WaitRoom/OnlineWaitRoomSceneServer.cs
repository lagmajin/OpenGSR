using UnityEngine;


namespace OpenGS
{
    public partial class OnlineWaitRoomScene
    {
        void SendGameStartRequest()
        {
            if (networkManager == null || !roomOwner)
            {
                return;
            }

            networkManager.SendGameStart();
        }

        void SendReadyRequest()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.SendReady();
            UpdateReadyButtonVisual();
        }

        void SendUnReadyRequest()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.SendUnready();
            UpdateReadyButtonVisual();
        }

        void GameStarted()
        {
            LoadGameScene();
        }

    }
}
