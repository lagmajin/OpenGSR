using UnityEngine;

namespace OpenGS
{
    public interface IRespawnPoints
    {
        Vector2 GetRandomSpawnPoint(ETeam team = ETeam.NoTeam);
        int Count(ETeam team = ETeam.NoTeam);
    }

    public interface IReSpawnPoints : IRespawnPoints
    {
        Vector2 random();
    }
}
