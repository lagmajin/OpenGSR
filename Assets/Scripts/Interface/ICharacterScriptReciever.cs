using UnityEngine.EventSystems;

namespace OpenGS
{
    public interface ICharacterScriptReciever : IEventSystemHandler
    {
        void SendEvent();
    }
}
