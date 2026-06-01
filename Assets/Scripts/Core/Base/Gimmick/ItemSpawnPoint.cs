
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
                case EFieldItemType.GranadeLauncher: prefab = randomItemPrefab; break;
                case EFieldItemType.FlameThrower: prefab = randomItemPrefab; break;
                case EFieldItemType.PowerUpItem: prefab = powerUpItemPrefab; break;
                case EFieldItemType.DefenceUpItem: prefab = defenceUpItemPrefab; break;
                case EFieldItemType.SpeedUpItem: prefab = speedUpItemPrefab; break;
                case EFieldItemType.StealthItem: prefab = stealthItemPrefab; break;
                case EFieldItemType.GrenadePack: prefab = grenadePackItemPrefab; break;
                case EFieldItemType.HealItem: prefab = healItemPrefab; break;
            }

            if (prefab != null && transform.childCount == 0)
            {
                Debug.Log($"[ItemSpawnPoint] SpawnItem: {FieldItemVisualResolver.GetDisplayName(type)} ({type})");
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

        [Button("テストスピードアップアイテム生成")]
        public void TestSpawnSpeedUpItem()
        {
            if (gameObject.transform.childCount == 0 && speedUpItemPrefab != null)
            {
                var item = Instantiate(speedUpItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;
                itemPos.y += heightOffset;
                item.transform.position = itemPos;
            }
        }

        [Button("テストステルスアイテム生成")]
        public void TestSpawnStealthItem()
        {
            if (gameObject.transform.childCount == 0 && stealthItemPrefab != null)
            {
                var item = Instantiate(stealthItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;
                itemPos.y += heightOffset;
                item.transform.position = itemPos;
            }
        }

        [Button("テストグレネード補充アイテム生成")]
        public void TestSpawnGrenadePackItem()
        {
            if (gameObject.transform.childCount == 0 && grenadePackItemPrefab != null)
            {
                var item = Instantiate(grenadePackItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;
                itemPos.y += heightOffset;
                item.transform.position = itemPos;
            }
        }

        [Button("テスト回復アイテム生成")]
        public void TestSpawnHealItem()
        {
            if (gameObject.transform.childCount == 0 && healItemPrefab != null)
            {
                var item = Instantiate(healItemPrefab, gameObject.transform);
                var itemPos = gameObject.transform.position;
                itemPos.y += heightOffset;
                item.transform.position = itemPos;
            }
        }

    }
}
