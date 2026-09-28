using System;
using System.Collections.Generic;
using System.Text;


namespace OpenGSCore
{
    enum EGenerateType
    {
    }

    public sealed class PlayerID
    {
        private readonly string playerID;

        private PlayerID(string id)
        {
            playerID = id;
        }

        public static PlayerID New() => new PlayerID(Guid.NewGuid().ToString("N"));
        public static PlayerID FromString(string id) => id == null ? Null : new PlayerID(id);

        public static readonly PlayerID Empty = new PlayerID(string.Empty);
        public static readonly PlayerID Null = new PlayerID(null);

        public bool IsEmpty => playerID == string.Empty;
        public bool IsNull => playerID == null;

        public override string ToString() => playerID ?? "null";

        public override bool Equals(object obj) =>
            obj is PlayerID other && playerID == other.playerID;

        public override int GetHashCode() => playerID?.GetHashCode() ?? 0;
    }
}
