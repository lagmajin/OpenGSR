
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{

    enum eItemSpawnType
    {
        AllRandom,
        PowerAndProtection,
        ItemAGroup, // 新設：パワーアップ/ディフェンスアップ専用

    }



    [DisallowMultipleComponent]
    public class ItemSpawnPoint:AbstractItemSpawnPoint
    {
        public static readonly System.Collections.Generic.Dictionary<int, ItemSpawnPoint> AllSpawnPoints = new();

        [SerializeField]
        public int spawnPointId;

        private void Awake()
        {
            AllSpawnPoints[spawnPointId] = this;
        }

        private void OnDestroy()
        {
            if (AllSpawnPoints.ContainsKey(spawnPointId) && AllSpawnPoints[spawnPointId] == this)
            {
                AllSpawnPoints.Remove(spawnPointId);
            }
        }

        public void SpawnItem(EFieldItemType type)
        {
            GameObject prefab = null;
            switch (type)
            {
                case EFieldItemType.PowerUpItem: prefab = powerUpItemPrefab; break;
                case EFieldItemType.DefenceUpItem: prefab = defenceUpItemPrefab; break;
                // 他のタイプも必要に応じて追加
            }

            if (prefab != null && transform.childCount == 0)
            {
                var item = Instantiate(prefab, transform);
                var pos = transform.position;
                pos.y += heightOffset;
                item.transform.position = pos;
            }
        }

        public void DespawnItem()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        [Button("テストパワーアップアイテム生成")]
        public void TestSpawnPowerUpItem()
        {
            if (gameObject.transform.childCount == 0)
            {

                var item = Instantiate(powerUpItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;

                itemPos.y += heightOffset;

                item.transform.position = itemPos;

            }
            //item.transform.position;

        }

        [Button("テストディフェンスアップアイテム生成")]
        public void TestSpawnDefenceUpItem()
        {
            if (gameObject.transform.childCount == 0)
            {

                var item = Instantiate(defenceUpItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;

                itemPos.y += heightOffset;

                item.transform.position = itemPos;



            }
 







        }

    }
}
