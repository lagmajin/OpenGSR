using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// フィールドアイテムのネットワークメッセージ
    /// </summary>
    public static class FieldItemMessages
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
        public static JObject CreatePickupMessage(string itemId, string playerId)
        {
            return new JObject
            {
                ["MessageType"] = ItemPickup,
                ["ItemId"] = itemId,
                ["PlayerId"] = playerId
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
        public static JObject CreateStateSyncMessage(List<FieldItemNetworkManager.FieldItemData> items)
        {
            var itemsArray = new JArray();

            foreach (var item in items)
            {
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
        public static FieldItemNetworkManager.FieldItemData? ParseSpawnMessage(JObject json)
        {
            try
            {
                string itemId = json["ItemId"]?.ToString() ?? "";
                string itemTypeStr = json["ItemType"]?.ToString() ?? "PowerUp";

                if (!System.Enum.TryParse<eFieldItemType>(itemTypeStr, out var itemType))
                {
                    itemType = eFieldItemType.PowerUp;
                }

                float x = json["PositionX"]?.Value<float>() ?? 0;
                float y = json["PositionY"]?.Value<float>() ?? 0;
                float z = json["PositionZ"]?.Value<float>() ?? 0;

                var data = new FieldItemNetworkManager.FieldItemData(itemId, itemType, new Vector3(x, y, z));
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
        public static (string itemId, string playerId)? ParsePickupMessage(JObject json)
        {
            try
            {
                string itemId = json["ItemId"]?.ToString() ?? "";
                string playerId = json["PlayerId"]?.ToString() ?? "";

                return (itemId, playerId);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// クライアント側でフィールドアイテムを NetworkView で同期するためのコンポーネント
    /// </summary>
    public class FieldItemNetworkView : MonoBehaviour
    {
        [Header("Field Item Info")]
        [SerializeField] private string _itemId = "";
        [SerializeField] private eFieldItemType _itemType = eFieldItemType.PowerUp;

        private FieldItemNetworkManager? _manager;
        private bool _isInitialized = false;

        public string ItemId => _itemId;
        public eFieldItemType ItemType => _itemType;

        public void Initialize(string itemId, eFieldItemType itemType)
        {
            _itemId = itemId;
            _itemType = itemType;
            _isInitialized = true;

            _manager = FieldItemNetworkManager.Instance;

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
            if (!_isInitialized) return;

            // TODO: ネットワークマネージャーにメッセージを送信
            var message = FieldItemMessages.CreatePickupMessage(_itemId, playerId);
            // NetworkManager.SendUdpMessage(message);
        }
    }
}
