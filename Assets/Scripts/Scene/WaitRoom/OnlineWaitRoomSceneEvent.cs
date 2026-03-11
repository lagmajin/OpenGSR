
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
            
        }

        public void OnOtherPlayerEntered()
        {

        }

        public void ExitRoomRequested()
        {
            Debug.Log("ExitRoomRequested");

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
        }


    }
}
