using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    [Serializable]
    public sealed class NetworkReplayFrame
    {
        public long elapsedMs;
        public string direction = string.Empty;
        public string messageType = string.Empty;
        public JObject message = new JObject();
    }

    [Serializable]
    public sealed class NetworkReplayRecording
    {
        public int formatVersion = 1;
        public string source = "client";
        public string gameVersion = string.Empty;
        public string sceneName = string.Empty;
        public string recordedAtUtc = string.Empty;
        public List<NetworkReplayFrame> frames = new List<NetworkReplayFrame>();
    }
}
