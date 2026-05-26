using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// フィールドアイテムのネットワーク同期管理
    /// サーバー&クライアント両方で使用
    /// </summary>
    public class FieldItemNetworkManager : MonoBehaviour
    {
        /// <summary>
        /// フィールドアイテムの状態
        /// </summary>
        public enum ItemState
        {
            Spawned,      // 出現中
            PickedUp,     // 拾われた
            Despawned    // 消滅
        }

        /// <summary>
        /// フィールドアイテムのデータ
        /// </summary>
        [Serializable]
        public class FieldItemData
        {
            public string ItemId;
            public eFieldItemType ItemType;
            public Vector3 Position;
            public ItemState State;
            public string PickedUpByPlayerId;
            public float SpawnTime;
            public float RespawnTime;
            public bool IsActive;

            public FieldItemData(string itemId, eFieldItemType type, Vector3 position)
            {
                ItemId = itemId;
                ItemType = type;
                Position = position;
                State = ItemState.Spawned;
                SpawnTime = Time.time;
                RespawnTime = 0;
                IsActive = true;
            }
        }

        /// <summary>
        /// シングルトン
        /// </summary>
        public static FieldItemNetworkManager? Instance { get; private set; }

        /// <summary>
        /// フィールドアイテムの辞書
        /// </summary>
        private readonly Dictionary<string, FieldItemData> _fieldItems = new();

        /// <summary>
        /// アイテム取得イベント
        /// </summary>
        public event Action<string, string, eFieldItemType>? OnItemPickedUp; // (itemId, playerId, itemType)

        /// <summary>
        /// アイテムスポーンイベント
        /// </summary>
        public event Action<string, eFieldItemType, Vector3>? OnItemSpawned; // (itemId, itemType, position)

        /// <summary>
        /// アイテム消滅イベント
        /// </summary>
        public event Action<string>? OnItemDespawned; // (itemId)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// アイテムを出現させる
        /// </summary>
        public string SpawnItem(eFieldItemType itemType, Vector3 position)
        {
            string itemId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var itemData = new FieldItemData(itemId, itemType, position);
            _fieldItems[itemId] = itemData;

            OnItemSpawned?.Invoke(itemId, itemType, position);

            return itemId;
        }

        /// <summary>
        /// アイテムを拾う
        /// </summary>
        public void PickupItem(string itemId, string playerId)
        {
            if (_fieldItems.TryGetValue(itemId, out var itemData))
            {
                if (itemData.State == ItemState.Spawned && itemData.IsActive)
                {
                    itemData.State = ItemState.PickedUp;
                    itemData.PickedUpByPlayerId = playerId;
                    itemData.IsActive = false;

                    OnItemPickedUp?.Invoke(itemId, playerId, itemData.ItemType);

                    Debug.Log($"[FieldItem] Picked up: {itemId} by {playerId} ({itemData.ItemType})");
                }
            }
        }

        /// <summary>
        /// アイテムを消滅させる
        /// </summary>
        public void DespawnItem(string itemId)
        {
            if (_fieldItems.TryGetValue(itemId, out var itemData))
            {
                itemData.State = ItemState.Despawned;
                itemData.IsActive = false;

                OnItemDespawned?.Invoke(itemId);
            }
        }

        /// <summary>
        /// アイテムをリスポーンさせる
        /// </summary>
        public void RespawnItem(string itemId, Vector3 newPosition)
        {
            if (_fieldItems.TryGetValue(itemId, out var itemData))
            {
                itemData.Position = newPosition;
                itemData.State = ItemState.Spawned;
                itemData.IsActive = true;
                itemData.SpawnTime = Time.time;
                itemData.PickedUpByPlayerId = "";

                OnItemSpawned?.Invoke(itemId, itemData.ItemType, newPosition);
            }
        }

        /// <summary>
        /// アイテムデータを取得
        /// </summary>
        public FieldItemData? GetItemData(string itemId)
        {
            return _fieldItems.TryGetValue(itemId, out var data) ? data : null;
        }

        /// <summary>
        /// 全てのアクティブなアイテムを取得
        /// </summary>
        public List<FieldItemData> GetActiveItems()
        {
            var activeItems = new List<FieldItemData>();
            foreach (var kvp in _fieldItems)
            {
                if (kvp.Value.IsActive && kvp.Value.State == ItemState.Spawned)
                {
                    activeItems.Add(kvp.Value);
                }
            }
            return activeItems;
        }

        /// <summary>
        /// アイテムを削除
        /// </summary>
        public void RemoveItem(string itemId)
        {
            _fieldItems.Remove(itemId);
        }

        /// <summary>
        /// 全アイテムをクリア
        /// </summary>
        public void ClearAll()
        {
            _fieldItems.Clear();
        }

        /// <summary>
        /// アイテムをJSONから復元
        /// </summary>
        public void LoadFromJson(JArray itemsArray)
        {
            _fieldItems.Clear();

            foreach (var itemToken in itemsArray)
            {
                var item = itemToken as JObject;
                if (item == null) continue;

                var data = new FieldItemData(
                    item["ItemId"]?.ToString() ?? "",
                    Enum.Parse<eFieldItemType>(item["ItemType"]?.ToString() ?? "PowerUp"),
                    new Vector3(
                        item["PositionX"]?.Value<float>() ?? 0,
                        item["PositionY"]?.Value<float>() ?? 0,
                        item["PositionZ"]?.Value<float>() ?? 0
                    )
                );

                data.State = Enum.Parse<ItemState>(item["State"]?.ToString() ?? "Spawned");
                data.PickedUpByPlayerId = item["PickedUpByPlayerId"]?.ToString() ?? "";
                data.IsActive = item["IsActive"]?.Value<bool>() ?? true;

                _fieldItems[data.ItemId] = data;
            }
        }

        /// <summary>
        /// アイテムをJSONに変換
        /// </summary>
        public JArray ToJson()
        {
            var array = new JArray();

            foreach (var kvp in _fieldItems)
            {
                var item = new JObject
                {
                    ["ItemId"] = kvp.Value.ItemId,
                    ["ItemType"] = kvp.Value.ItemType.ToString(),
                    ["PositionX"] = kvp.Value.Position.x,
                    ["PositionY"] = kvp.Value.Position.y,
                    ["PositionZ"] = kvp.Value.Position.z,
                    ["State"] = kvp.Value.State.ToString(),
                    ["PickedUpByPlayerId"] = kvp.Value.PickedUpByPlayerId,
                    ["IsActive"] = kvp.Value.IsActive
                };

                array.Add(item);
            }

            return array;
        }
    }
}
