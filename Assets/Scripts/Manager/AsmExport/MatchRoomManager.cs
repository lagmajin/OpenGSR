

using JetBrains.Annotations;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{



    public partial class MatchRoomManager
    {
        [CanBeNull] public WaitRoom OnlineWaitRoom { get; private set; } = null;

        [CanBeNull] public MatchRoom OnlineMatchRoom { get; private set; } = null;

        private readonly object _lockObj=new object();
        public void CreateNewOnlineWaitRoom(in string roomName = "", int capacity = 8)
        {
            lock (_lockObj)
            {

                if (OnlineWaitRoom != null)
                {
                    RemoveOnlineWaitRoom();
                }

                OnlineWaitRoom = new WaitRoom(roomName, "", 0);

            }

        }
        public void RemoveOnlineWaitRoom()
        {
            lock (_lockObj)
            {
                OnlineWaitRoom = null;
            }

            





        }


        public void CreateNewOnlineMatchRoom(in string id)
        {
            lock (_lockObj)
            {

                if (OfflineMatchRoom != null)
                {

                    RemoveOnlineMatchRoom();
                }

                if (OnlineWaitRoom != null)
                {
                    Debug.Log("OnlineMatchRoom created...");

                    OnlineMatchRoom = new MatchRoom("");


                }
                else
                {

                }

            }



        }

        public void CreateNewOnlineMatchRoom()
        {
            lock (_lockObj)
            {

                if (OnlineMatchRoom != null)
                {

                    RemoveOnlineMatchRoom();
                }

                if (OnlineWaitRoom != null)
                {

                    Debug.Log("OnlineMatchRoom created...");

                    OnlineMatchRoom = new MatchRoom("");

                }
                else
                {

                }


            }

        }
        

        public void RemoveOnlineMatchRoom()
        {
            lock (_lockObj)
            {
                OfflineMatchRoom = null;
            }

            

        }

        public bool IsValidOnlineWaitRoom()
        {
            if (OnlineWaitRoom == null)
            {
                return false;
            }

            return true;

        }

        public bool IsValidOnlineMatchRoom()
        {
            if (OnlineWaitRoom == null)
            {
                return false;
            }

            return true;
        }

    }


    public partial class MatchRoomManager : IMatchRoomManager
    {
        //public static MatchRoomManager Instance { get; } = new();

        [CanBeNull] public WaitRoom WaitRoom { get; private set; } = null;

        //public OfflineWaitRoom
        [CanBeNull] public MatchRoom OfflineMatchRoom { get; private set; } = null;

        [CanBeNull] public OfflineWaitRoom OfflineWaitRoom { get; private set; } = null;

        public MatchRoom TestRoom;
        
        //public Stage Stage { get; set; }



        public MapInfo MapInfo { get; set; }
        public MatchRoomManager()
        {

            SetUpDebugMatchRoom();
        }

        private void SetUpDebugMatchRoom()
        {
            TestRoom = new MatchRoom("test");



        }

        public void CreateNewOfflineWaitRoom(in string roomName="")
        {
            if (OfflineWaitRoom == null)
            {

            }

            //OfflineWaitRoom = DependencyInjectionConfig.Resolve<OfflineWaitRoom>();




        }


 

        public void RemoveOfflineWaitRoom()
        {

        }





        public void RemoveOfflineMatchRoom()
        {

        }

        public bool IsValidOfflineWaitRoom()
        {
            return false;

        }

        public bool IsValidOfflineMatchRoom()
        {
            return false;
        }

    }
}
