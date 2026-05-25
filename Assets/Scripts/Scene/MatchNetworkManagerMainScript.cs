using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class MatchNetworkManagerScript : MonoBehaviour, INetworkManagerScript
    {
        public void TestFunc()
        {
            Debug.Log("[MatchNetworkManager] TestFunc");
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
            Debug.Log($"[MatchNetworkManager] Received: {json}");
        }

        public void OnConnected()
        {
            Debug.Log("[MatchNetworkManager] Connected");
        }

        public void OnDisconnected()
        {
            Debug.Log("[MatchNetworkManager] Disconnected");
        }
    }

    public class MatchNetworkManagerMainScript : MatchNetworkManagerScript
    {
    }
}
