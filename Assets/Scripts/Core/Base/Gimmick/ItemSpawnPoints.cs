using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    interface IItemSpawnPoints
    {
    }

    [DisallowMultipleComponent]
    class ItemSpawnPoints : MonoBehaviour, IItemSpawnPoints
    {
        [SerializeField] private bool registerAsDefault = true;

        private static readonly List<ItemSpawnPoints> Instances = new();

        public static ItemSpawnPoints Default { get; private set; }

        public static IReadOnlyCollection<ItemSpawnPoint> AllSpawnPoints =>
            Default != null ? Default.spawnPoints.Values : ItemSpawnPoint.AllSpawnPoints.Values;

        private readonly Dictionary<int, ItemSpawnPoint> spawnPoints = new();

        private void Awake()
        {
            Instances.Add(this);
            if (registerAsDefault || Default == null)
            {
                Default = this;
            }
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
            if (Default == this)
            {
                Default = Instances.Count > 0 ? Instances[0] : null;
            }
        }

        public static bool TryGetPoint(int spawnPointId, out ItemSpawnPoint point)
        {
            if (Default != null)
            {
                return Default.spawnPoints.TryGetValue(spawnPointId, out point);
            }

            return ItemSpawnPoint.AllSpawnPoints.TryGetValue(spawnPointId, out point);
        }

        public void Register(ItemSpawnPoint point)
        {
            if (point == null)
            {
                return;
            }

            spawnPoints[point.SpawnPointId] = point;
        }

        public void Unregister(ItemSpawnPoint point)
        {
            if (point == null)
            {
                return;
            }

            if (spawnPoints.TryGetValue(point.SpawnPointId, out var current) && current == point)
            {
                spawnPoints.Remove(point.SpawnPointId);
            }
        }

        public void SpawnItem(int spawnPointId, EFieldItemType type)
        {
            if (TryGetPoint(spawnPointId, out var point))
            {
                point.SpawnItem(type);
            }
        }

        public void DespawnItem(int spawnPointId)
        {
            if (TryGetPoint(spawnPointId, out var point))
            {
                point.DespawnItem();
            }
        }

        public void DespawnAllItems()
        {
            foreach (var point in AllSpawnPoints)
            {
                point?.DespawnItem();
            }
        }
    }
}
