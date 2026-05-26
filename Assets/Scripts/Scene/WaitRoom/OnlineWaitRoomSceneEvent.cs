
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
            GameStarted();
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
            RequestGameStart();
        }

        public void ReadyRequested()
        {
            ToggleReadyState();
        }


    }
}
