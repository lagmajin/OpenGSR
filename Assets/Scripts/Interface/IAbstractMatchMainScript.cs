using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    public interface IAbstractMatchMainScript
    {
        EGameMode GameMode();
        void PostEvent(AbstractGameEvent e);
        List<GameObject> AllPlayers();
        void OnMyPlayerDead();
    }
}
