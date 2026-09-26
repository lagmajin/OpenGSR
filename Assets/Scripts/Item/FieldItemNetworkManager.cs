#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// フィールドアイテムのネットワーク同期管理
    /// サーバー&クライアント両方で使用
    /// </summary>
    public class WorldItemNetworkManager : MonoBehaviour
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
        public class WorldItemData
        {
            public string ItemId;
            public eFieldItemType ItemType;
            public Vector3 Position;
            public ItemState State;
            public string PickedUpByPlayerId = string.Empty;
            public float SpawnTime;
            public float RespawnTime;
            public bool IsActive;

            public WorldItemData(string itemId, eFieldItemType type, Vector3 position)
            {
                ItemId = itemId;
                ItemType = type;
                Position = position;
                State = ItemState.Spawned;
                SpawnTime = GetSafeTime();
                RespawnTime = 0;
                IsActive = true;
            }
        }

        /// <summary>
        /// シングルトン
        /// </summary>
        public static WorldItemNetworkManager? Instance { get; private set; }

        /// <summary>
        /// フィールドアイテムの辞書
        /// </summary>
        private readonly Dictionary<string, WorldItemData> _fieldItems = new();

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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearAll();
        }

        /// <summary>
        /// アイテムを出現させる
        /// </summary>
        public string SpawnItem(eFieldItemType itemType, Vector3 position)
        {
            string itemId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var itemData = new WorldItemData(itemId, itemType, position);
            _fieldItems[itemId] = itemData;

            InvokeSafely(OnItemSpawned, itemId, itemType, position, nameof(OnItemSpawned));

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

                    InvokeSafely(OnItemPickedUp, itemId, playerId, itemData.ItemType, nameof(OnItemPickedUp));

                    Debug.Log($"[FieldItem] Picked up: {itemId} by {playerId} ({itemData.ItemType})");
                }
            }
        }

        /// <summary>
        /// アイテムを消滅させる
        /// </summary>
        public void DespawnItem(string itemId)
        {
            if (_fieldItems.TryGetValue(itemId, out var itemData)
                && itemData.IsActive
                && itemData.State != ItemState.Despawned)
            {
                itemData.State = ItemState.Despawned;
                itemData.IsActive = false;

                InvokeSafely(OnItemDespawned, itemId, nameof(OnItemDespawned));
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
                itemData.SpawnTime = GetSafeTime();
                itemData.PickedUpByPlayerId = "";

                InvokeSafely(OnItemSpawned, itemId, itemData.ItemType, newPosition, nameof(OnItemSpawned));
            }
        }

        /// <summary>
        /// アイテムデータを取得
        /// </summary>
        public WorldItemData? GetItemData(string itemId)
        {
            return _fieldItems.TryGetValue(itemId, out var data) ? data : null;
        }

        /// <summary>
        /// 全てのアクティブなアイテムを取得
        /// </summary>
        public List<WorldItemData> GetActiveItems()
        {
            var activeItems = new List<WorldItemData>();
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

        private static float GetSafeTime()
        {
            var now = Time.time;
            return float.IsFinite(now) && now >= 0f ? now : 0f;
        }

        /// <summary>
        /// アイテムをJSONから復元
        /// </summary>
        public void LoadFromJson(JArray itemsArray)
        {
            _fieldItems.Clear();
            if (itemsArray == null)
            {
                return;
            }

            foreach (var itemToken in itemsArray)
            {
                var item = itemToken as JObject;
                if (item == null) continue;

                var itemId = item["ItemId"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    Debug.LogWarning("[WorldItemNetworkManager] Ignoring item data without an ItemId.");
                    continue;
                }

                var stateText = item["State"]?.ToString() ?? "Spawned";
                if (!Enum.TryParse<ItemState>(stateText, true, out var parsedState))
                {
                    Debug.LogWarning($"[WorldItemNetworkManager] Ignoring item with invalid state: {stateText}");
                    continue;
                }

                var positionX = ReadFloat(item["PositionX"]);
                var positionY = ReadFloat(item["PositionY"]);
                var positionZ = ReadFloat(item["PositionZ"]);
                if (!IsFinite(positionX) || !IsFinite(positionY) || !IsFinite(positionZ))
                {
                    Debug.LogWarning($"[WorldItemNetworkManager] Ignoring item with invalid position: {itemId}");
                    continue;
                }

                var data = new WorldItemData(
                    itemId,
                    WorldItemVisualResolver.TryParseLegacy(item["ItemType"]?.ToString() ?? "PowerUp", out var parsedType)
                        ? parsedType
                        : eFieldItemType.PowerUpItem,
                    new Vector3(positionX, positionY, positionZ)
                );

                data.State = parsedState;
                data.PickedUpByPlayerId = item["PickedUpByPlayerId"]?.ToString() ?? "";
                data.IsActive = ReadBool(item["IsActive"], true);

                _fieldItems[data.ItemId] = data;
            }
        }

        private static void InvokeSafely(Action<string, string, eFieldItemType> handlers, string itemId, string playerId, eFieldItemType itemType, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, string, eFieldItemType> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(itemId, playerId, itemType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldItemNetworkManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<string, eFieldItemType, Vector3> handlers, string itemId, eFieldItemType itemType, Vector3 position, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, eFieldItemType, Vector3> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(itemId, itemType, position);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldItemNetworkManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<string> handlers, string itemId, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(itemId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WorldItemNetworkManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float ReadFloat(JToken token)
        {
            if (token == null)
            {
                return 0f;
            }

            try
            {
                return token.ToObject<float>();
            }
            catch
            {
                return float.NaN;
            }
        }

        private static bool ReadBool(JToken token, bool fallback)
        {
            if (token == null)
            {
                return fallback;
            }

            try
            {
                return token.ToObject<bool>();
            }
            catch
            {
                return fallback;
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

    [System.Obsolete("Use WorldItemNetworkManager instead.")]
    public class FieldItemNetworkManager : WorldItemNetworkManager
    {
    }
}
