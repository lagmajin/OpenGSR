using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// オンライン（サーバー通信）用のショップサービス。
    /// <para>
    /// The server keeps the credits, what has been bought and what is equipped.
    /// This asked the client's own embedded server for all of it, so an online
    /// shop was running against a simulation of itself and two copies of the
    /// truth existed on one machine. The embedded server is what the offline
    /// fallback uses, which is what it is for.
    /// </para>
    /// </summary>
    public class OnlineShopService : IShopService
    {
        private ClientNetworkManager lobby;
        private GeneralServerNetworkManager localServer;
        private readonly ShopMasterData shopMasterData;

        /// <summary>
        /// Answers from the server's shop that have arrived and not been taken.
        /// <para>
        /// Held rather than awaited, because the network layer raises what arrives
        /// and a shop asks questions it needs answers to. A caller takes the
        /// answer to the question it asked, so two things asking at once cannot
        /// read each other's replies.
        /// </para>
        /// </summary>
        private readonly Queue<JObject> pending = new Queue<JObject>();

        public Action OnDataChanged { get; set; }

        public OnlineShopService()
            : this(null)
        {
        }

        public OnlineShopService(ShopMasterData shopMasterData)
        {
            this.shopMasterData = shopMasterData != null
                ? shopMasterData
                : Resources.Load<ShopMasterData>("MasterData/ShopMasterData");

            try
            {
                lobby = DependencyInjectionConfig.Resolve<ClientNetworkManager>();
            }
            catch
            {
                lobby = null;
            }

            if (lobby != null)
            {
                lobby.ShopResponseReceived += OnShopResponseReceived;
            }

            try
            {
                localServer = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            }
            catch
            {
                localServer = null;
            }
        }

        private void OnShopResponseReceived(JObject message)
        {
            if (message == null)
            {
                return;
            }

            lock (pending)
            {
                pending.Enqueue(message);
            }
        }

        /// <summary>
        /// Takes the next answer from the server's shop, if there is one.
        /// </summary>
        private JObject TakeShopResponse()
        {
            lock (pending)
            {
                return pending.Count > 0 ? pending.Dequeue() : null;
            }
        }

        /// <summary>
        /// Sends a question and waits for the answer, giving up rather than
        /// waiting for ever.
        /// <para>
        /// The wait is bounded because the connection can be down, and a shop that
        /// waits for ever on a socket that will never answer is a shop that
        /// hangs rather than one that says it could not.
        /// </para>
        /// </summary>
        private async UniTask<JObject> AskShop(Action send, int timeoutMs = 3000)
        {
            send();

            var deadline = Time.realtimeSinceStartup + (timeoutMs / 1000f);
            while (Time.realtimeSinceStartup < deadline)
            {
                var answer = TakeShopResponse();
                if (answer != null)
                {
                    return answer;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            return null;
        }

        /// <summary>
        /// Whether the server's shop is the one to ask.
        /// <summary>
        private bool HasLobby
        {
            get
            {
                if (lobby == null)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(lobby.ClientPlayerId);
            }
        }

        public async UniTask<List<ShopItemData>> GetItemsAsync(EShopCategory category)
        {
            Debug.Log("[OnlineShop] Requesting items from server...");
            await UniTask.Yield();

            if (shopMasterData != null)
            {
                return shopMasterData.GetItemsByCategory(category);
            }

            return ShopCatalogFactory.GetDefaultItems(category);
        }

        public async UniTask<bool> PurchaseItemAsync(string itemId, int price)
        {
            if (string.IsNullOrWhiteSpace(itemId) || price < 0)
            {
                Debug.LogWarning($"[OnlineShop] Purchase rejected: itemId='{itemId}', price={price}.");
                return false;
            }

            if (HasLobby)
            {
                var answer = await AskShop(() => lobby.RequestShopPurchase(itemId, price));
                if (answer == null)
                {
                    Debug.LogWarning("[OnlineShop] The server did not answer the purchase, so it was not made.");
                    return false;
                }

                var bought = answer["Success"]?.ToObject<bool>() ?? false;
                if (bought)
                {
                    // サーバー購入成功時も、次のUI更新・再起動で状態を失わないよう
                    // クライアントの購入済みキャッシュを同期する。
                    UserSaveManager.SetPurchased(itemId);
                }

                NotifyDataChanged();
                return bought;
            }

            // No lobby to ask, so the embedded server answers, which is what it is
            // for. It is a stand-in for a server that is not there, not a second
            // authority beside one that is.
            Debug.Log($"[OnlineShop] Purchasing item {itemId} on the embedded server...");
            await UniTask.Yield();

            var success = localServer != null
                ? localServer.PurchaseItem(itemId, price)
                : EconomyManager.SpendCredits(price);

            if (success)
            {
                UserSaveManager.SetPurchased(itemId);
            }

            NotifyDataChanged();
            return success;
        }

        public async UniTask<bool> EquipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            itemId = itemId?.Trim();
            if (string.IsNullOrWhiteSpace(itemId) || (category == EShopCategory.InstantItem && !IsValidSlot(slot)))
            {
                Debug.LogWarning($"[OnlineShop] Equip rejected: itemId='{itemId}', category={category}, slot={slot}.");
                return false;
            }

            if (HasLobby)
            {
                var answer = await AskShop(() =>
                    lobby.RequestShopEquip(itemId, category.ToString(), slot));
                if (answer == null)
                {
                    Debug.LogWarning("[OnlineShop] The server did not answer the equip, so it was not made.");
                    return false;
                }

                var equipped = answer["Success"]?.ToObject<bool>() ?? false;
                NotifyDataChanged();
                return equipped;
            }

            await UniTask.Yield();

            var success = true;
            if (localServer != null)
            {
                success = localServer.EquipItem(itemId, category, slot);
            }
            else if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot(itemId, category, slot);
            }
            else
            {
                UserSaveManager.EquipItem(itemId, category);
            }

            NotifyDataChanged();
            return success;
        }

        public async UniTask<bool> UnequipItemAsync(string itemId, EShopCategory category, int slot = 0)
        {
            if (category == EShopCategory.InstantItem && !IsValidSlot(slot))
            {
                Debug.LogWarning($"[OnlineShop] Unequip rejected: category={category}, slot={slot}.");
                return false;
            }

            if (HasLobby)
            {
                var answer = await AskShop(() =>
                    lobby.RequestShopEquip(string.Empty, category.ToString(), slot));
                if (answer == null)
                {
                    Debug.LogWarning("[OnlineShop] The server did not answer the unequip, so it was not made.");
                    return false;
                }

                var success = answer["Success"]?.ToObject<bool>() ?? false;
                NotifyDataChanged();
                return success;
            }

            await UniTask.Yield();

            var done = true;
            if (localServer != null)
            {
                done = localServer.UnequipItem(category, slot);
            }
            else if (category == EShopCategory.InstantItem)
            {
                UserSaveManager.EquipToSlot("", category, slot);
            }
            else
            {
                UserSaveManager.EquipItem("", category);
            }

            NotifyDataChanged();
            return done;
        }

        private void NotifyDataChanged()
        {
            if (OnDataChanged == null)
            {
                return;
            }

            foreach (Action handler in OnDataChanged.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[OnlineShopService] OnDataChanged subscriber failed: {ex}");
                }
            }
        }

        public long GetCredits()
        {
            if (HasLobby)
            {
                var state = LatestShopState();
                if (state != null)
                {
                    return state["Credits"]?.ToObject<long>() ?? 0L;
                }
            }

            return localServer != null ? localServer.GetCredits() : EconomyManager.GetCredits();
        }

        public bool IsPurchased(string itemId)
        {
            if (HasLobby)
            {
                var state = LatestShopState();
                var purchased = state?["PurchasedItems"] as JArray;
                if (purchased != null)
                {
                    foreach (var entry in purchased)
                    {
                        var id = entry?.ToString() ?? (entry as JObject)?["ItemId"]?.ToString();
                        if (string.Equals(id, itemId, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return localServer != null ? localServer.IsPurchased(itemId) : UserSaveManager.IsPurchased(itemId);
        }

        /// <summary>
        /// The last state the server reported, asked for if there is not one.
        /// <para>
        /// The read side of the shop asks on demand rather than being told, so a
        /// shop opened for the first time has something to show. The write side
        /// waits for the answer to its own question, because a purchase that was
        /// never confirmed has to be reported as not made.
        /// </para>
        /// </summary>
        private JObject LatestShopState()
        {
            var answer = TakeShopResponse();
            if (answer != null)
            {
                return answer;
            }

            lobby.RequestShopState();
            return TakeShopResponse();
        }

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            if (HasLobby)
            {
                var state = LatestShopState();
                if (state != null)
                {
                    return IsInServerEquipped(state, itemId, category, slot);
                }
            }

            return localServer != null
                ? localServer.IsEquipped(itemId, category, slot)
                : (category == EShopCategory.InstantItem
                    ? UserSaveManager.GetEquippedInSlot(category, slot) == itemId
                    : category == EShopCategory.Weapon
                        ? UserSaveManager.IsFavoriteWeapon(itemId)
                        : UserSaveManager.GetEquippedId(category) == itemId);
        }

        public string GetEquippedItemId(EShopCategory category, int slot = 0)
        {
            if (HasLobby)
            {
                var fromServer = EquippedItemIdFromServer(LatestShopState(), category, slot);
                if (!string.IsNullOrEmpty(fromServer))
                {
                    return fromServer;
                }
            }

            if (localServer != null)
            {
                return localServer.GetEquippedItemId(category, slot);
            }

            if (category == EShopCategory.InstantItem)
            {
                var items = UserSaveManager.GetEquippedInstantItems();
                return items != null && slot >= 0 && slot < items.Length ? items[slot] : string.Empty;
            }

            return UserSaveManager.GetEquippedId(category) ?? string.Empty;
        }

        private static bool IsInServerEquipped(JObject state, string itemId, EShopCategory category, int slot)
        {
            var bySlot = state?["EquippedInstantItems"] as JArray;
            if (bySlot != null)
            {
                foreach (var entry in bySlot.OfType<JObject>())
                {
                    if ((entry["Slot"]?.ToObject<int>() ?? -1) == slot &&
                        string.Equals(entry["ItemId"]?.ToString(), itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            var byName = state?["EquippedItems"] as JArray;
            if (byName != null)
            {
                foreach (var entry in byName.OfType<JObject>())
                {
                    if (string.Equals(
                            entry["Category"]?.ToString(),
                            category.ToString(),
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(entry["ItemId"]?.ToString(), itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string EquippedItemIdFromServer(JObject state, EShopCategory category, int slot)
        {
            var bySlot = state?["EquippedInstantItems"] as JArray;
            if (bySlot != null && category == EShopCategory.InstantItem)
            {
                foreach (var entry in bySlot.OfType<JObject>())
                {
                    if ((entry["Slot"]?.ToObject<int>() ?? -1) == slot)
                    {
                        return entry["ItemId"]?.ToString() ?? string.Empty;
                    }
                }
            }

            var byName = state?["EquippedItems"] as JArray;
            if (byName != null)
            {
                foreach (var entry in byName.OfType<JObject>())
                {
                    if (string.Equals(
                            entry["Category"]?.ToString(),
                            category.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return entry["ItemId"]?.ToString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        private static bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < 3;
        }
    }
}
