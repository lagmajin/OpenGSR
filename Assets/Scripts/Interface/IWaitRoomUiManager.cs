using OpenGSCore;

namespace OpenGS
{
    public interface IWaitRoomUiManager
    {
        void ChangeRoomTitle(string roomTitle);
        void ChangeRoomCapacity(int capacity);
        void ChangeGameMode(EGameMode gameMode);
    }
}
