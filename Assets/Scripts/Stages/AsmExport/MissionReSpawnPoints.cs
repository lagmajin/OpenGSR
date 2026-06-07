using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenGS
{
    public interface IMissionReSpawnPoints : IRespawnPoints
    {
    }

    [DisallowMultipleComponent]
    public class MissionReSpawnPoints : MonoBehaviour, IMissionReSpawnPoints
    {
        [SerializeField] private Transform spawnPoint;

        public Vector2 GetRandomSpawnPoint(ETeam team = ETeam.NoTeam)
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            if (transform.childCount > 0)
            {
                return transform.GetChild(0).position;
            }

            return transform.position;
        }

        public int Count(ETeam team = ETeam.NoTeam)
        {
            return 1;
        }
    }
}
