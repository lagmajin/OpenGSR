#nullable enable
using System;
using OpenGSCore;
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
            public EFieldItemType ItemType;
            public Vector3 Position;
            public ItemState State;
            public string PickedUpByPlayerId = string.Empty;
            public float SpawnTime;
            public float RespawnTime;
            public bool IsActive;

            public WorldItemData(string itemId, EFieldItemType type, Vector3 position)
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
        public event Action<string, string, EFieldItemType>? OnItemPickedUp; // (itemId, playerId, itemType)

        /// <summary>
        /// アイテムスポーンイベント
        /// </summary>
        public event Action<string, EFieldItemType, Vector3>? OnItemSpawned; // (itemId, itemType, position)

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

        private void Update()
        {
            // A pickup the server never answers must not leave the effect
            // applied forever, so give up on the ones that time out and let
            // them run the revert they registered.
            if (_pendingPickups.Count > 0)
            {
                ExpirePendingPickups();
            }
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
        public string SpawnItem(EFieldItemType itemType, Vector3 position)
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
        private readonly Dictionary<string, EFieldItemType> _pendingTypes =
            new Dictionary<string, EFieldItemType>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action> _pendingReverts =
            new Dictionary<string, Action>(StringComparer.Ordinal);
        private readonly Dictionary<string, PendingPickup> _pendingPickups =
            new Dictionary<string, PendingPickup>(StringComparer.Ordinal);

        /// <summary>
        /// A pickup the client asked for and the server has not answered yet.
        /// </summary>
        public sealed class PendingPickup
        {
            public PendingPickup(string itemId, string playerId)
            {
                ItemId = itemId;
                PlayerId = playerId;
            }

            public string ItemId { get; }
            public string PlayerId { get; }
            public float RequestedAt { get; set; }
        }

        /// <summary>
        /// How long a pickup may wait for the server before the client gives up
        /// and does not keep the effect. Without this an unanswered request
        /// would leave the effect in limbo forever.
        /// </summary>
        public const float PickupTimeoutSeconds = 2.0f;

        /// <summary>
        /// Raised once the server grants a pickup. The argument is the duration
        /// the server decided, which is not necessarily the one the client
        /// asked for.
        /// </summary>
        public event Action<string, string, EFieldItemType, float>? OnPickupGranted;

        /// <summary>
        /// Raised when the server refuses a pickup, so the client can undo
        /// anything it applied optimistically.
        /// </summary>
        public event Action<string, string, EFieldItemType>? OnPickupRefused;

        /// <summary>
        /// Records a pickup request and applies the effect optimistically so the
        /// pickup feels instant. The revert is kept so a refusal can take it
        /// back, and the server still decides the duration.
        /// </summary>
        public bool BeginPendingPickup(
            string itemId,
            string playerId,
            EFieldItemType type,
            Action revert)
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(playerId))
            {
                return false;
            }

            if (_pendingPickups.ContainsKey(itemId))
            {
                return false;
            }

            _pendingPickups[itemId] = new PendingPickup(itemId, playerId)
            {
                RequestedAt = Time.time
            };
            _pendingTypes[itemId] = type;
            _pendingReverts[itemId] = revert;
            return true;
        }

        /// <summary>
        /// Applies the server's ruling to a pending pickup: granted drops the
        /// revert, refused runs it.
        /// </summary>
        public bool ResolvePickup(string itemId, bool granted, EFieldItemType type, float serverDuration)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            if (!_pendingPickups.TryGetValue(itemId, out var pending))
            {
                return false;
            }

            _pendingPickups.Remove(itemId);
            _pendingTypes.Remove(itemId);

            if (granted)
            {
                _pendingReverts.Remove(itemId);
                InvokeSafely(OnPickupGranted, itemId, pending.PlayerId, type, serverDuration, nameof(OnPickupGranted));
            }
            else
            {
                if (_pendingReverts.TryGetValue(itemId, out var revert) && revert != null)
                {
                    revert();
                }

                _pendingReverts.Remove(itemId);
                InvokeSafely(OnPickupRefused, itemId, pending.PlayerId, type, nameof(OnPickupRefused));
            }

            return true;
        }

        /// <summary>
        /// Abandons pickups the server never answered. Returns how many timed
        /// out so a caller can log it.
        /// </summary>
        public int ExpirePendingPickups()
        {
            var now = Time.time;
            var expired = new List<string>();

            foreach (var entry in _pendingPickups)
            {
                if (now - entry.Value.RequestedAt > PickupTimeoutSeconds)
                {
                    expired.Add(entry.Key);
                }
            }

            foreach (var itemId in expired)
            {
                var type = _pendingTypes.TryGetValue(itemId, out var known)
                    ? known
                    : EFieldItemType.PowerUpItem;
                ResolvePickup(itemId, granted: false, type, 0f);
            }

            return expired.Count;
        }
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
            _pendingPickups.Clear();
            _pendingTypes.Clear();
            _pendingReverts.Clear();
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
            _pendingPickups.Clear();
            _pendingTypes.Clear();
            _pendingReverts.Clear();
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
                    OpenGSCore.FieldItemTypeNames.TryParse(item["ItemType"]?.ToString(), out var parsedType)
                        ? parsedType
                        : EFieldItemType.PowerUpItem,
                    new Vector3(positionX, positionY, positionZ)
                );

                data.State = parsedState;
                data.PickedUpByPlayerId = item["PickedUpByPlayerId"]?.ToString() ?? "";
                data.IsActive = ReadBool(item["IsActive"], true);

                _fieldItems[data.ItemId] = data;
            }
        }

        private static void InvokeSafely(
            Action<string, string, EFieldItemType, float> handlers,
            string itemId,
            string playerId,
            EFieldItemType itemType,
            float duration,
            string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, string, EFieldItemType, float> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(itemId, playerId, itemType, duration);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FieldItem] {eventName} handler failed: {ex.Message}");
                }
            }
        }
        private static void InvokeSafely(Action<string, string, EFieldItemType> handlers, string itemId, string playerId, EFieldItemType itemType, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, string, EFieldItemType> handler in handlers.GetInvocationList())
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

        private static void InvokeSafely(Action<string, EFieldItemType, Vector3> handlers, string itemId, EFieldItemType itemType, Vector3 position, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, EFieldItemType, Vector3> handler in handlers.GetInvocationList())
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
