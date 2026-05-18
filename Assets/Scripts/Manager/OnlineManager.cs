using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace OpenGS
{
    public class LobbyServerInfo
    {
        public int? Port { get; set; } = null;
        public string IPAddress { get; set; } = null;
    }

    public class MatchServerInfo
    {
        public int? Port { get; set; } = null;
        public string IP { get; set; } = null;
        public int? UdpPort { get; set; } = null;

        public bool HasEndpoint()
        {
            return !string.IsNullOrWhiteSpace(IP) && (UdpPort.HasValue || Port.HasValue);
        }

        public void Clear()
        {
            Port = null;
            IP = null;
            UdpPort = null;
        }
    }
    public class OnlineManager
    {
        public static OnlineManager Instance { get; } = new();
        public LobbyServerInfo LobbyServerInfo { get; set; } = new();
        public MatchServerInfo MatchServerInfo { get; set; } = new();

        private OnlineManager()
        {

        }
    }
}
