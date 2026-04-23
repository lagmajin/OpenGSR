using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using OpenGS;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Manages TCP connection to the general (lobby) server.
    /// </summary>
    public class GeneralServerNetworkManager
    {
        private sealed class RoomRecord
        {
            public string RoomId { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "TeamDeathMatch";
            public bool TeamBalance { get; set; } = true;
            public int PlayerCount { get; set; } = 0;
        }

        private sealed class AccountRecord
        {
            public string GlobalUserId { get; set; } = "";
            public string AccountName { get; set; } = "";
            public long Credits { get; set; } = 1000;
            public HashSet<string> PurchasedItems { get; } = new HashSet<string>();
            public Dictionary<EShopCategory, string> EquippedItems { get; } = new Dictionary<EShopCategory, string>();
            public Dictionary<int, string> EquippedInstantItems { get; } = new Dictionary<int, string>();
        }

        private readonly Subject<JObject> dataReceivedSubject = new Subject<JObject>();
        private readonly Subject<Unit> connectedSubject = new Subject<Unit>();
        private readonly Subject<Unit> disconnectedSubject = new Subject<Unit>();

        private readonly List<INetworkManagerScript> scripts = new List<INetworkManagerScript>();
        private readonly List<RoomRecord> localRooms = new List<RoomRecord>();
        private readonly Dictionary<string, AccountRecord> accounts = new Dictionary<string, AccountRecord>();
        private int localRoomSequence = 1;
        private string currentAccountId = "";

        public GeneralServerNetworkManager()
        {
            SeedLocalRooms();
        }

        public System.IObservable<JObject> DataReceivedStream => dataReceivedSubject.AsObservable();
        public System.IObservable<Unit> ConnectedStream => connectedSubject.AsObservable();
        public System.IObservable<Unit> DisconnectedStream => disconnectedSubject.AsObservable();

        public bool Online { get; private set; } = false;
        public JObject LastMatchResult { get; private set; }

        public void ClearLastMatchResult() => LastMatchResult = null;

        public void Subscribe(INetworkManagerScript script)
        {
            if (!scripts.Contains(script)) scripts.Add(script);
        }

        public void UnSubscribe(INetworkManagerScript script) => scripts.Remove(script);

        public void ConnectToGeneralServerSync(string ip, int port, string id, string pass)
        {
            Debug.Log($"[GeneralServerNetworkManager] ConnectToGeneralServerSync {ip}:{port}");
        }

        public void TryConnectToServer(string ip, int port)
        {
            Debug.Log($"[GeneralServerNetworkManager] TryConnectToServer {ip}:{port}");
        }

        public void Disconnect()
        {
            Online = false;
            Debug.Log("[GeneralServerNetworkManager] Disconnect");
        }

        public void SendMessage(in JObject json)
        {
            Debug.Log($"[GeneralServerNetworkManager] SendMessage: {json?["MessageType"]}");
        }

        public void SendMessage(JObject json)
        {
            Debug.Log($"[GeneralServerNetworkManager] SendMessage: {json?["MessageType"]}");
            if (json != null)
            {
                HandleAccountAndShopMessage(json);
                CacheLastMatchResult(json);

                var messageType = json["MessageType"]?.ToString();
                if (ShouldForwardToClients(messageType))
                {
                    dataReceivedSubject.OnNext(json);
                }
            }
        }

        public void SendUpdateRoomRequest()
        {
            SendUpdateRoomRequestCore(new List<OpenGSCore.EGameMode>());
        }

        public void SendUpdateRoomRequest(in System.Collections.Generic.List<OpenGSCore.EGameMode> modeList)
        {
            SendUpdateRoomRequestCore(modeList);
        }

        public void SendUpdateRoomRequest(System.Collections.Generic.List<OpenGSCore.EGameMode> modes)
        {
            SendUpdateRoomRequestCore(modes);
        }

        private void SendUpdateRoomRequestCore(System.Collections.Generic.List<OpenGSCore.EGameMode> modes)
        {
            var rooms = new JArray();
            foreach (var room in localRooms)
            {
                rooms.Add(new JObject
                {
                    ["RoomID"] = room.RoomId,
                    ["RoomName"] = room.RoomName,
                    ["OwnerID"] = room.OwnerId,
                    ["Capacity"] = room.Capacity,
                    ["GameMode"] = room.GameMode,
                    ["TeamBalance"] = room.TeamBalance ? "True" : "False",
                    ["PlayerCount"] = room.PlayerCount
                });
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.RoomListUpdateRequest,
                ["MatchRoomType"] = (modes == null || modes.Count == 0) ? "All" : string.Join(",", modes),
                ["Options"] = ""
            };
            SendMessage(json);
            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.RoomListUpdateNotification,
                ["Rooms"] = rooms
            });
        }

        public void SendCreateNewWaitRoomRequest(string roomName, int capacity, string gameMode, bool teamBalance, string password = "")
        {
            var roomId = $"room-{localRoomSequence++:D4}";
            localRooms.Add(new RoomRecord
            {
                RoomId = roomId,
                RoomName = string.IsNullOrWhiteSpace(roomName) ? "New Room" : roomName,
                OwnerId = "local_player",
                Capacity = capacity,
                GameMode = string.IsNullOrWhiteSpace(gameMode) ? "TeamDeathMatch" : gameMode,
                TeamBalance = teamBalance,
                PlayerCount = 1
            });

            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.CreateRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = roomName,
                ["Capacity"] = capacity,
                ["GameMode"] = gameMode,
                ["TeamBalance"] = teamBalance ? "True" : "False",
                ["OwnerID"] = "local_player"
            });
        }

        public void SendEnterWaitRoomRequest(string roomId, string playerId, string playerName, string password = "")
        {
            var room = localRooms.Find(r => string.Equals(r.RoomId, roomId, StringComparison.OrdinalIgnoreCase));
            if (room == null)
            {
                EmitToClient(new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room not found",
                    ["RoomID"] = roomId
                });
                return;
            }

            if (room.PlayerCount >= room.Capacity)
            {
                EmitToClient(new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room is full",
                    ["RoomID"] = roomId
                });
                return;
            }

            room.PlayerCount = Math.Min(room.Capacity, room.PlayerCount + 1);
            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.JoinRoomResponse,
                ["Success"] = true,
                ["RoomID"] = room.RoomId,
                ["RoomName"] = room.RoomName,
                ["Capacity"] = room.Capacity,
                ["PlayerID"] = playerId,
                ["PlayerName"] = playerName,
                ["Players"] = room.PlayerCount
            });
        }

        private void EmitToClient(JObject json)
        {
            CacheLastMatchResult(json);
            dataReceivedSubject.OnNext(json);
        }

        private void CacheLastMatchResult(JObject json)
        {
            var messageType = json?["MessageType"]?.ToString();
            if (messageType == MessageType.MatchResult || messageType == MessageType.MatchEndNotification)
            {
                LastMatchResult = json;
            }
        }

        private static bool ShouldForwardToClients(string messageType)
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return false;
            }

            if (string.Equals(messageType, "LoginSuccessful", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(messageType, "LogoutSuccess", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (messageType.EndsWith("Response", StringComparison.OrdinalIgnoreCase) ||
                messageType.EndsWith("Notification", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return messageType == MessageType.LoginResponse || messageType == MessageType.LogoutSuccessful;
        }

        public void SetCurrentAccount(string accountName, string globalUserId)
        {
            var account = EnsureAccount(globalUserId, accountName);
            currentAccountId = account.GlobalUserId;
            AccountManager.Instance.LoginData(account.AccountName, "", account.GlobalUserId);
            AccountManager.Instance.SetCredits(account.Credits);
        }

        public long GetCredits()
        {
            var account = EnsureCurrentAccount();
            return account.Credits;
        }

        public bool PurchaseItem(string itemId, int price)
        {
            var account = EnsureCurrentAccount();
            if (string.IsNullOrWhiteSpace(itemId) || price < 0)
            {
                return false;
            }

            if (account.Credits < price)
            {
                return false;
            }

            account.Credits -= price;
            account.PurchasedItems.Add(itemId);
            AccountManager.Instance.SetCredits(account.Credits);
            UserSaveManager.SetPurchased(itemId, true);
            EconomyManager.SetCredits((int)account.Credits);
            return true;
        }

        public bool EquipItem(string itemId, EShopCategory category, int slot = 0)
        {
            var account = EnsureCurrentAccount();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (category == EShopCategory.InstantItem)
            {
                account.EquippedInstantItems[slot] = itemId;
                UserSaveManager.EquipToSlot(itemId, category, slot);
            }
            else
            {
                account.EquippedItems[category] = itemId;
                UserSaveManager.EquipItem(itemId, category);
            }

            return true;
        }

        public bool UnequipItem(EShopCategory category, int slot = 0)
        {
            var account = EnsureCurrentAccount();
            if (category == EShopCategory.InstantItem)
            {
                account.EquippedInstantItems[slot] = "";
                UserSaveManager.EquipToSlot("", category, slot);
            }
            else
            {
                account.EquippedItems[category] = "";
                UserSaveManager.EquipItem("", category);
            }

            return true;
        }

        public bool IsPurchased(string itemId)
        {
            var account = EnsureCurrentAccount();
            return account.PurchasedItems.Contains(itemId) || UserSaveManager.IsPurchased(itemId);
        }

        public bool IsEquipped(string itemId, EShopCategory category, int slot = 0)
        {
            var account = EnsureCurrentAccount();
            if (category == EShopCategory.InstantItem)
            {
                return account.EquippedInstantItems.TryGetValue(slot, out var equipped) && equipped == itemId;
            }

            return account.EquippedItems.TryGetValue(category, out var equippedItem) && equippedItem == itemId;
        }

        private AccountRecord EnsureCurrentAccount()
        {
            if (string.IsNullOrWhiteSpace(currentAccountId))
            {
                currentAccountId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            }

            if (string.IsNullOrWhiteSpace(currentAccountId))
            {
                currentAccountId = "local-account";
            }

            return EnsureAccount(currentAccountId, AccountManager.Instance.CurrentProfile.DisplayName);
        }

        private AccountRecord EnsureAccount(string globalUserId, string accountName)
        {
            if (string.IsNullOrWhiteSpace(globalUserId))
            {
                globalUserId = "local-account";
            }

            if (!accounts.TryGetValue(globalUserId, out var account))
            {
                account = new AccountRecord
                {
                    GlobalUserId = globalUserId,
                    AccountName = string.IsNullOrWhiteSpace(accountName) ? "Player" : accountName,
                    Credits = Math.Max(0, AccountManager.Instance.GetCredits())
                };
                accounts[globalUserId] = account;
            }
            else if (!string.IsNullOrWhiteSpace(accountName))
            {
                account.AccountName = accountName;
            }

            return account;
        }

        private void HandleAccountAndShopMessage(JObject json)
        {
            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            switch (messageType)
            {
                case MessageType.LoginRequest:
                case MessageType.LoginResponse:
                {
                    var accountName = json["AccountName"]?.ToString() ?? json["PlayerName"]?.ToString() ?? "Player";
                    var globalUserId = json["GlobalUserId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? Guid.NewGuid().ToString("N");
                    SetCurrentAccount(accountName, globalUserId);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.LoginResponse,
                        ["Success"] = true,
                        ["AccountName"] = accountName,
                        ["GlobalUserId"] = globalUserId,
                        ["Credits"] = GetCredits()
                    });
                    break;
                }
                case MessageType.CreateAccountRequest:
                {
                    var accountName = json["AccountName"]?.ToString() ?? "Player";
                    var globalUserId = json["GlobalUserId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(globalUserId))
                    {
                        globalUserId = Guid.NewGuid().ToString("N");
                    }

                    SetCurrentAccount(accountName, globalUserId);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.CreateAccountResponse,
                        ["Success"] = true,
                        ["AccountName"] = accountName,
                        ["GlobalUserId"] = globalUserId
                    });
                    break;
                }
                case MessageType.ShopStateRequest:
                {
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.ShopStateResponse,
                        ["Credits"] = GetCredits(),
                        ["PurchasedItems"] = new JArray(EnsureCurrentAccount().PurchasedItems)
                    });
                    break;
                }
                case MessageType.ShopPurchaseRequest:
                {
                    var itemId = json["ItemId"]?.ToString() ?? "";
                    var price = json["Price"]?.ToObject<int>() ?? 0;
                    var success = PurchaseItem(itemId, price);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.ShopPurchaseResponse,
                        ["Success"] = success,
                        ["ItemId"] = itemId,
                        ["Credits"] = GetCredits()
                    });
                    break;
                }
                case MessageType.ShopEquipRequest:
                {
                    var itemId = json["ItemId"]?.ToString() ?? "";
                    var categoryText = json["Category"]?.ToString() ?? EShopCategory.Weapon.ToString();
                    Enum.TryParse(categoryText, true, out EShopCategory category);
                    var slot = json["Slot"]?.ToObject<int>() ?? 0;
                    var success = EquipItem(itemId, category, slot);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.ShopEquipResponse,
                        ["Success"] = success,
                        ["ItemId"] = itemId,
                        ["Category"] = category.ToString(),
                        ["Slot"] = slot
                    });
                    break;
                }
                case MessageType.ShopUnequipRequest:
                {
                    var categoryText = json["Category"]?.ToString() ?? EShopCategory.Weapon.ToString();
                    Enum.TryParse(categoryText, true, out EShopCategory category);
                    var slot = json["Slot"]?.ToObject<int>() ?? 0;
                    var success = UnequipItem(category, slot);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.ShopEquipResponse,
                        ["Success"] = success,
                        ["Category"] = category.ToString(),
                        ["Slot"] = slot
                    });
                    break;
                }
                case "LoadingStarted":
                {
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = "LoadingStartedNotification",
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? "",
                        ["Progress"] = 0f
                    });
                    break;
                }
                case "LoadingProgress":
                {
                    var progress = json["Progress"]?.ToObject<float>() ?? 0f;
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = "LoadingProgressNotification",
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? "",
                        ["Progress"] = Mathf.Clamp01(progress)
                    });
                    break;
                }
                case "LoadingCompleted":
                {
                    var playerId = json["PlayerID"]?.ToString() ?? "";
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = "LoadingCompletedNotification",
                        ["Success"] = true,
                        ["PlayerID"] = playerId
                    });
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = "AllowEnterMap",
                        ["Success"] = true,
                        ["PlayerID"] = playerId
                    });
                    break;
                }
            }
        }

        private void SeedLocalRooms()
        {
            if (localRooms.Count > 0)
            {
                return;
            }

            localRooms.Add(new RoomRecord
            {
                RoomId = $"room-{localRoomSequence++:D4}",
                RoomName = "Default DM Room",
                OwnerId = "host-001",
                Capacity = 8,
                GameMode = "DeathMatch",
                TeamBalance = false,
                PlayerCount = 2
            });

            localRooms.Add(new RoomRecord
            {
                RoomId = $"room-{localRoomSequence++:D4}",
                RoomName = "Default TDM Room",
                OwnerId = "host-002",
                Capacity = 12,
                GameMode = "TeamDeathMatch",
                TeamBalance = true,
                PlayerCount = 6
            });
        }
    }
}
