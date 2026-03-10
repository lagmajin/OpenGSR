using Newtonsoft.Json.Linq;

namespace OpenGS
{
    public interface IMatchNetworkScript
    {
        void OnMessageFromMatchServer(JObject message);
    }
}
