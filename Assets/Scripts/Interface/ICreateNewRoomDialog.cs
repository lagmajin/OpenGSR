using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public interface ICreateNewRoomDialog
    {
        string RoomName();
        int MaxPlayer();
        string Password();
        EGameMode GameMode();
        bool TeamBalance();
        void ShowDialog();
    }
}
