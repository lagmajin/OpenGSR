#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public interface IAbstractGameRoom
    {

    }
    /// <summary>
    /// A room the server loop ticks.
    /// <para>
    /// It is abstract because a room with nothing to tick and a room whose tick is
    /// missing look the same from outside, and the loop ticks whatever it holds.
    /// </para>
    /// </summary>
    public abstract class AbstractGameRoom
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
        /// <summary>
        /// One tick of the room's simulation.
        /// <para>
        /// This was a virtual method with an empty body, so the server loop could
        /// tick a room that had not overridden it and nothing said so: the room
        /// reported itself as running while simulating nothing. A room that the
        /// loop ticks has to say what one tick of it is.
        /// </para>
        /// </summary>
        public abstract void GameUpdate();

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

