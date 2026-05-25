using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MatchNetworkManagerScript : MonoBehaviour, INetworkManagerScript
    {
        public JObject LastReceivedMessage { get; private set; }
        public bool IsConnectedState { get; private set; }

        public void TestFunc()
        {
            Debug.Log("[MatchNetworkManager] TestFunc");
        }

        public virtual void ParseNetworkMatchMessageFromServer(JObject json)
        {
            LastReceivedMessage = json;
            Debug.Log($"[MatchNetworkManager] Received: {json}");
        }

        public virtual void OnConnected()
        {
            IsConnectedState = true;
            Debug.Log("[MatchNetworkManager] Connected");
        }

        public virtual void OnDisconnected()
        {
            IsConnectedState = false;
            Debug.Log("[MatchNetworkManager] Disconnected");
        }
    }

    public class MatchNetworkManagerMainScript : MatchNetworkManagerScript
    {
        public string LastMessageType => LastReceivedMessage?["MessageType"]?.ToString() ?? string.Empty;

        public override void ParseNetworkMatchMessageFromServer(JObject json)
        {
            base.ParseNetworkMatchMessageFromServer(json);

            if (json == null)
            {
                return;
            }

            Debug.Log($"[MatchNetworkManagerMainScript] MessageType={LastMessageType}");
        }
    }
}
