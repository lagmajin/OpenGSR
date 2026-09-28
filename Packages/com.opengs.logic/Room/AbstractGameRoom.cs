#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public interface IAbstractGameRoom
    {

    }
    public class AbstractGameRoom
    {
        private int roomNumber_ = 0;

        private List<PlayerInfo> players = new List<PlayerInfo>();

        private bool isFinished = false;
        private AbstractMatchResult? gameResult = null;

        private string? ownerID_ = null;
        public string? OwnerId { get => ownerID_; set => ownerID_ = value; }
        public int RoomNumber { get => roomNumber_; }

        private string id = string.Empty;

        public List<PlayerInfo> Players { get => players; set => players = value; }
        public string Id { get => id; set => id = value; }
        public AbstractGameRoom(int roomNumber, in string roomOwnerID)
        {
            roomNumber_ = roomNumber;
            if (string.IsNullOrEmpty(roomOwnerID))
            {
                ownerID_ = null;
            }
            else
            {
                ownerID_ = roomOwnerID;
            }

            id = Guid.NewGuid().ToString("N");

        }
        public virtual void GameUpdate()
        {




        }

        public virtual AbstractMatchResult? GameResult()
        {
            return gameResult;
        }

        public void SetGameResult(AbstractMatchResult result)
        {
            gameResult = result;
        }

        public bool IsFinished()
        {
            return isFinished;
        }

        public void SetFinished(bool finished)
        {
            isFinished = finished;
        }
    }
}

