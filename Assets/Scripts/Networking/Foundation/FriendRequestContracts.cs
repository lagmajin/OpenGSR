using System;

namespace OpenGS
{
    [Serializable]
    public sealed class FriendRequestEnvelopeRequest
    {
        public string PlayerId;
        public string TargetPlayerId;
    }

    [Serializable]
    public sealed class FriendRequestEnvelopeResponse
    {
        public string PlayerId;
        public string TargetPlayerId;
        public bool Success;
        public string Error;
    }
}
