using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGS;
using OpenGSCore;



namespace OpenGS
{
    public class MissionRoomManager
    {
        public static MissionRoomManager Instance { get; } = new();

        private readonly object lockObj = new();
        private MissionRoomManager()
        {

        }

        //OfflineMissionWaitRoom

        public void CreateNewRoom(in string roomName, int capacity = 3)
        {

        }

        public void RemoveRoom()
        {

        }

    }
}
