using UnityEngine.EventSystems;
using OpenGSCore;

namespace OpenGS
{
    public interface IWaitRoom : IEventSystemHandler
    {
        void Plus();
        void Minus();

        void SendChat(string chat);

        void ChangeGameMode(EGameMode mode);
        void ChangeMap(EMap map);

        void ChangeTeamBalance(bool balance);
    }
}
