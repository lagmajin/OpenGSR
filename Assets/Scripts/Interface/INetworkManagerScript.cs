using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;

namespace OpenGS
{
    public interface INetworkManagerScript : IEventSystemHandler
    {
        void TestFunc();
        void ParseNetworkMatchMessageFromServer(JObject json);

        void OnConnected();
        void OnDisconnected();
    }
}
