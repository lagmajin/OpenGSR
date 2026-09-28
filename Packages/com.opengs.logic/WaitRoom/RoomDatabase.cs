using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace OpenGSCore
{


    public class WaitRoomDatabase
    {
        private List<WaitRoom> rooms=new();

        //private Dictionary<int, WaitRoom> rooms2 = new();


        public void AddRoom(WaitRoom room)
        {
            if (room != null && !rooms.Contains(room))
            {
                rooms.Add(room);
            }
        }
        public List<WaitRoom> AllRooms()
        {
            return new List<WaitRoom>(rooms);
        }

        

        public List<WaitRoom> SearchRoomByGameMode(EGameMode mode)
        {
            var list = new List<WaitRoom>();

            foreach (var room in rooms)
            {
                if (room == null || room.NowPlaying)
                {
                    continue;
                }

                if (mode == EGameMode.Unknown)
                {
                    list.Add(room);
                    continue;
                }

                if (room.MatchRule != null && room.MatchRule.Mode == mode)
                {
                    list.Add(room);
                    continue;
                }

                if (room.setting != null && room.setting.Mode == mode)
                {
                    list.Add(room);
                }
            }

            return list;
        }

        public List<WaitRoom> SearchRoomByRoomName(in string roomName)
        {
            var list = new List<WaitRoom>();
            foreach (var room in rooms)
            {
                if (room.RoomName == roomName && !room.NowPlaying)
                {
                    list.Add(room);
                }
            }
            return list;
        }

        public List<WaitRoom> SearchRoomByRoomNumber(int roomNumber)
        {
            var list = new List<WaitRoom>();
            if (roomNumber >= 0 && roomNumber < rooms.Count)
            {
                list.Add(rooms[roomNumber]);
            }
            return list;
        }

        public void RemoveAllRooms()
        {
            rooms.Clear();
        }

        public bool RemoveRoom(WaitRoom room)
        {
            return rooms.Remove(room);
        }


    }


    public class MatchRoomDatabase
    {
        private Dictionary<int, WaitRoom> rooms = new();
        private int nextId = 0;

        public void AddRoom(WaitRoom room)
        {
            if (room != null)
            {
                rooms[nextId] = room;
                nextId++;
            }
        }

        public WaitRoom GetRoom(int roomId)
        {
            if (rooms.TryGetValue(roomId, out var room))
            {
                return room;
            }
            return null;
        }

        public bool RemoveRoom(int roomId)
        {
            return rooms.Remove(roomId);
        }

        public List<WaitRoom> AllRooms()
        {
            return new List<WaitRoom>(rooms.Values);
        }

        public void RemoveAllRooms()
        {
            rooms.Clear();
            nextId = 0;
        }

    }

}
