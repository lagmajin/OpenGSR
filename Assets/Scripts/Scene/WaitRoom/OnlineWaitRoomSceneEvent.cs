
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    public partial class OnlineWaitRoomScene : AbstractNonBattleScene, IOnlineWaitRoom, IWaitRoom
    {
        private void OnApplicationQuit()
        {
            if (networkManager != null)
            {
                networkManager.SendWaitRoomLeave(ResolveLocalPlayerId());
            }
        }

        public void OnNewGameStarted()
        {
            LoadGameScene();
        }

        public void OnOtherPlayerEntered()
        {
            RefreshWaitRoomUi();
        }

        public void ExitRoomRequested()
        {
            Debug.Log("ExitRoomRequested");
            ExitWaitRoom();
        }

        [Button("ゲーム開始テスト")]
        public void GameStartRequested()
        {
            Debug.Log("GameStartRequested From Server...");


            LoadGameScene();


        }

        public void ReadyRequested()
        {
            Debug.Log("ReadyRequest");
            Ready(true);
            RefreshWaitRoomUi();
        }


    }
}
