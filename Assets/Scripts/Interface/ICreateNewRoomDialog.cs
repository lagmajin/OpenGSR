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
        EMap Map();
        bool TeamBalance();
        void ShowDialog();
    }
}
