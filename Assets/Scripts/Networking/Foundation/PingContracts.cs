using System;

namespace OpenGS
{
    [Serializable]
    public sealed class PingRequest
    {
        public string Nonce;
        public string ClientSentAtUtc;
    }

    [Serializable]
    public sealed class PingResponse
    {
        public string Nonce;
        public string ServerSentAtUtc;
        public string EchoClientSentAtUtc;
    }
}
