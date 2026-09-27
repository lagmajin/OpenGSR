using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// フィールドアイテムのネットワークメッセージ
    /// </summary>
    public static class WorldItemMessages
    {
        #region Message Types

        public const string ItemSpawn = "FieldItemSpawn";
        public const string ItemPickup = "FieldItemPickup";
        public const string ItemDespawn = "FieldItemDespawn";
        public const string ItemStateSync = "FieldItemStateSync";
        public const string ItemSpawnBatch = "FieldItemSpawnBatch";

        #endregion

        #region Serialization

        /// <summary>
        /// アイテムスポーンメッセージを作成
        /// </summary>
        public static JObject CreateSpawnMessage(string itemId, string itemType, float x, float y, float z)
        {
            return new JObject
            {
                ["MessageType"] = ItemSpawn,
                ["ItemId"] = itemId,
                ["ItemType"] = itemType,
                ["PositionX"] = x,
                ["PositionY"] = y,
                ["PositionZ"] = z
            };
        }

        /// <summary>
        /// アイテムピックアメッセージを作成
        /// </summary>
        public static JObject CreatePickupMessage(string itemId, string playerId, int spawnPointId = -1, Vector3? position = null)
        {
            var pos = position ?? Vector3.zero;
            return new JObject
            {
                ["MessageType"] = ItemPickup,
                ["ItemId"] = itemId,
                ["PlayerID"] = playerId,
                ["SpawnPointId"] = spawnPointId,
                ["PositionX"] = pos.x,
                ["PositionY"] = pos.y,
                ["PositionZ"] = pos.z
            };
        }

        /// <summary>
        /// アイテム消滅メッセージを作成
        /// </summary>
        public static JObject CreateDespawnMessage(string itemId)
        {
            return new JObject
            {
                ["MessageType"] = ItemDespawn,
                ["ItemId"] = itemId
            };
        }

        /// <summary>
        /// アイテム状態同期メッセージを作成
        /// </summary>
        public static JObject CreateStateSyncMessage(List<WorldItemNetworkManager.WorldItemData> items)
        {
            var itemsArray = new JArray();

            if (items == null)
            {
                return new JObject
                {
                    ["MessageType"] = ItemStateSync,
                    ["Items"] = itemsArray
                };
            }

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                itemsArray.Add(new JObject
                {
                    ["ItemId"] = item.ItemId,
                    ["ItemType"] = item.ItemType.ToString(),
                    ["PositionX"] = item.Position.x,
                    ["PositionY"] = item.Position.y,
                    ["PositionZ"] = item.Position.z,
                    ["State"] = item.State.ToString(),
                    ["IsActive"] = item.IsActive
                });
            }

            return new JObject
            {
                ["MessageType"] = ItemStateSync,
                ["Items"] = itemsArray
            };
        }

        /// <summary>
        /// アイテムをJSONからパース
        /// </summary>
        public static WorldItemNetworkManager.WorldItemData ParseSpawnMessage(JObject json)
        {
            try
            {
                string itemId = json["ItemId"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    return null;
                }

                string itemTypeStr = json["ItemType"]?.ToString() ?? string.Empty;

                if (!OpenGSCore.FieldItemTypeNames.TryParse(itemTypeStr, out var itemType))
                {
                    itemType = EFieldItemType.PowerUpItem;
                }

                float x = json["PositionX"]?.Value<float>() ?? 0;
                float y = json["PositionY"]?.Value<float>() ?? 0;
                float z = json["PositionZ"]?.Value<float>() ?? 0;
                if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
                {
                    return null;
                }

                var data = new WorldItemNetworkManager.WorldItemData(itemId, itemType, new Vector3(x, y, z));
                return data;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// アイテム pickupをJSONからパース
        /// </summary>
        public static (string itemId, string playerId) ParsePickupMessage(JObject json)
        {
            try
            {
                string itemId = json["ItemId"]?.ToString() ?? "";
                string playerId = json["PlayerId"]?.ToString()
                    ?? json["PlayerID"]?.ToString()
                    ?? "";

                if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(playerId))
                {
                    return default;
                }

                return (itemId, playerId);
            }
            catch
            {
                return default;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion
    }

    /// <summary>
    /// クライアント側でフィールドアイテムを NetworkView で同期するためのコンポーネント
    /// </summary>
    public class WorldItemNetworkView : MonoBehaviour
    {
        [Header("Field Item Info")]
        [SerializeField] private string _itemId = "";
        [SerializeField] private EFieldItemType _itemType = EFieldItemType.PowerUpItem;
        [SerializeField] private int _spawnPointId = -1;

        private WorldItemNetworkManager _manager;
        private bool _isInitialized = false;
        private bool _pickupSent;

        public string ItemId => _itemId;
        public EFieldItemType ItemType => _itemType;

        public void Initialize(string itemId, EFieldItemType itemType, int spawnPointId = -1)
        {
            if (_manager != null)
            {
                _manager.OnItemDespawned -= OnItemDespawned;
            }

            _itemId = itemId;
            _itemType = itemType;
            _spawnPointId = spawnPointId;
            _isInitialized = true;
            _pickupSent = false;

            _manager = WorldItemNetworkManager.Instance;

            // スポーンイベント的通知
            if (_manager != null)
            {
                _manager.OnItemDespawned += OnItemDespawned;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnItemDespawned -= OnItemDespawned;
            }
        }

        private void OnItemDespawned(string itemId)
        {
            if (itemId == _itemId)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// アイテムを拾ったことをネットワークに通知
        /// </summary>
        public void SendPickupToServer(string playerId)
        {
            if (!_isInitialized || _pickupSent || string.IsNullOrWhiteSpace(playerId)) return;

            _pickupSent = true;

            var message = WorldItemMessages.CreatePickupMessage(_itemId, playerId, _spawnPointId, transform.position);

            try
            {
                var networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                networkManager?.SendMessage(message);
            }
            catch
            {
                // Keep the local item state usable even if the network layer is unavailable.
            }

            _manager?.PickupItem(_itemId, playerId);
        }
    }

    [System.Obsolete("Use WorldItemMessages instead.")]
    public static class FieldItemMessages
    {
        public const string ItemSpawn = WorldItemMessages.ItemSpawn;
        public const string ItemPickup = WorldItemMessages.ItemPickup;
        public const string ItemDespawn = WorldItemMessages.ItemDespawn;
        public const string ItemStateSync = WorldItemMessages.ItemStateSync;
        public const string ItemSpawnBatch = WorldItemMessages.ItemSpawnBatch;

        public static JObject CreateSpawnMessage(string itemId, string itemType, float x, float y, float z) => WorldItemMessages.CreateSpawnMessage(itemId, itemType, x, y, z);
        public static JObject CreatePickupMessage(string itemId, string playerId, int spawnPointId = -1, Vector3? position = null) => WorldItemMessages.CreatePickupMessage(itemId, playerId, spawnPointId, position);
        public static JObject CreateDespawnMessage(string itemId) => WorldItemMessages.CreateDespawnMessage(itemId);
        public static JObject CreateStateSyncMessage(List<WorldItemNetworkManager.WorldItemData> items) => WorldItemMessages.CreateStateSyncMessage(items);
        public static WorldItemNetworkManager.WorldItemData ParseSpawnMessage(JObject json) => WorldItemMessages.ParseSpawnMessage(json);
        public static (string itemId, string playerId) ParsePickupMessage(JObject json) => WorldItemMessages.ParsePickupMessage(json);
    }
}
