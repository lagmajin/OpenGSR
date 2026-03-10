using UnityEngine.EventSystems;
using OpenGSCore;

namespace OpenGS
{
    public interface ICTFMatchMainScript : IEventSystemHandler
    {
        void PlayerFlagCaptured(ETeam team);
        void PlayerFlagLost(ETeam team);
    }
}
