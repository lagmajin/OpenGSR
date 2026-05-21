
using Sirenix.OdinInspector;
using UnityEngine;

namespace OpenGS
{
    public partial class OnlineWaitRoomScene : AbstractNonBattleScene, IOnlineWaitRoom, IWaitRoom
    {
        private void OnApplicationQuit()
        {
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
