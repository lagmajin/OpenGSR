using System.Collections;
using UnityEngine;

namespace  OpenGS
{
    public interface IOnlineWaitRoom
    {

        bool IsRoomOwner();

        void SendChat(string chat);
    }
}