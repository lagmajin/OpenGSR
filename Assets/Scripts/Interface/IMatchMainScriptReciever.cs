using UnityEngine.EventSystems;

namespace OpenGS
{
    public interface IMatchMainScriptReciever : IEventSystemHandler
    {
        void SendEvent();
        void PlayerKilled(string id);
    }
}
