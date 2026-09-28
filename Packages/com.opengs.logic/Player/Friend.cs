using System;
using System.Collections.Generic;

namespace OpenGSCore
{
    public enum EFriendStatus
    {
        Offline,
        Online,
        InGame,
        Away
    }

    public enum EFriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class FriendEntry
    {
        public string PlayerId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public EFriendStatus Status { get; set; } = EFriendStatus.Offline;
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastOnline { get; set; } = DateTime.UtcNow;

        public FriendEntry()
        {
        }

        public FriendEntry(string playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName;
        }
    }

    public class FriendRequest
    {
        public string RequestId { get; set; } = "";
        public string FromPlayerId { get; set; } = "";
        public string FromPlayerName { get; set; } = "";
        public string ToPlayerId { get; set; } = "";
        public EFriendRequestStatus Status { get; set; } = EFriendRequestStatus.Pending;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public FriendRequest()
        {
        }

        public FriendRequest(string fromPlayerId, string fromPlayerName, string toPlayerId)
        {
            RequestId = Guid.NewGuid().ToString("N");
            FromPlayerId = fromPlayerId;
            FromPlayerName = fromPlayerName;
            ToPlayerId = toPlayerId;
        }
    }

    public class FriendList
    {
        public List<FriendEntry> Friends { get; set; } = new();
        public List<FriendRequest> IncomingRequests { get; set; } = new();
        public List<FriendRequest> OutgoingRequests { get; set; } = new();

        public bool IsFriend(string playerId)
        {
            return Friends.Exists(f => f.PlayerId == playerId);
        }

        public bool HasPendingRequest(string fromPlayerId, string toPlayerId)
        {
            return IncomingRequests.Exists(r =>
                r.FromPlayerId == fromPlayerId && r.ToPlayerId == toPlayerId && r.Status == EFriendRequestStatus.Pending)
                || OutgoingRequests.Exists(r =>
                r.FromPlayerId == fromPlayerId && r.ToPlayerId == toPlayerId && r.Status == EFriendRequestStatus.Pending);
        }

        public void AddFriend(FriendEntry entry)
        {
            if (!IsFriend(entry.PlayerId))
            {
                Friends.Add(entry);
            }
        }

        public bool RemoveFriend(string playerId)
        {
            return Friends.RemoveAll(f => f.PlayerId == playerId) > 0;
        }

        public List<FriendEntry> GetOnlineFriends()
        {
            return Friends.FindAll(f => f.Status != EFriendStatus.Offline);
        }
    }

    public class FriendOperationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = "";

        public static FriendOperationResult Ok() => new() { Success = true };
        public static FriendOperationResult Fail(string error) => new() { Success = false, Error = error };
    }
}
