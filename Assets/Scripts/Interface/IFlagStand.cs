using UnityEngine.EventSystems;

namespace OpenGS
{
    public interface IFlagStand : IEventSystemHandler
    {
        void SetFlag();
        bool HasFlag();
        void FlagReady();
    }
}
