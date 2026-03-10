
using UnityEngine.EventSystems;

namespace OpenGS
{
    public interface IBGMAndBGNManager : IEventSystemHandler
    {
        public void PlayBGM();
        public void StopBGM();

        public void PlayBGN();


    }
}
