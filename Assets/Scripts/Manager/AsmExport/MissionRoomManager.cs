using System;

namespace OpenGS
{
    public class MissionRoomManager
    {
        public static MissionRoomManager Instance { get; } = new();

        private readonly object lockObj = new();
        private string currentRoomName = string.Empty;
        private int currentCapacity = 3;
        private int selectedMissionIndex = 1;
        private int selectedQuestIndex = 1;
        private bool isQuestMode;
        private bool isRoomOwner;

        private MissionRoomManager()
        {
        }

        public void CreateNewRoom(in string roomName, int capacity = 3)
        {
            lock (lockObj)
            {
                currentRoomName = string.IsNullOrWhiteSpace(roomName) ? "MissionRoom" : roomName;
                currentCapacity = Math.Max(1, capacity);
                isQuestMode = false;
                isRoomOwner = true;
            }
        }

        public void RemoveRoom()
        {
            lock (lockObj)
            {
                currentRoomName = string.Empty;
                currentCapacity = 3;
                selectedMissionIndex = 1;
                selectedQuestIndex = 1;
                isQuestMode = false;
                isRoomOwner = false;
            }
        }

        public void SetMissionIndex(int missionIndex)
        {
            lock (lockObj)
            {
                selectedMissionIndex = Math.Max(1, missionIndex);
                selectedQuestIndex = 1;
                isQuestMode = false;
            }
        }

        public void SetQuestIndex(int questIndex)
        {
            lock (lockObj)
            {
                selectedQuestIndex = Math.Max(1, questIndex);
                selectedMissionIndex = 1;
                isQuestMode = true;
            }
        }

        public string RoomName()
        {
            lock (lockObj)
            {
                return currentRoomName;
            }
        }

        public int Capacity()
        {
            lock (lockObj)
            {
                return currentCapacity;
            }
        }

        public int MissionIndex()
        {
            lock (lockObj)
            {
                return selectedMissionIndex;
            }
        }

        public int QuestIndex()
        {
            lock (lockObj)
            {
                return selectedQuestIndex;
            }
        }

        public bool IsQuestMode()
        {
            lock (lockObj)
            {
                return isQuestMode;
            }
        }

        public bool IsRoomOwner()
        {
            lock (lockObj)
            {
                return isRoomOwner;
            }
        }
    }
}
