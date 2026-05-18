using System.Collections.Generic;
using System;
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
            public sealed class RoomPlayerRecord
            {
                public string PlayerId { get; set; } = "";
                public string PlayerName { get; set; } = "";
                public bool IsReady { get; set; } = false;
            }

            public string RoomId { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "TeamDeathMatch";
            public bool TeamBalance { get; set; } = true;
            public int PlayerCount { get; set; } = 0;
            public List<RoomPlayerRecord> Players { get; } = new List<RoomPlayerRecord>();
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
        private readonly HashSet<string> loadingCompletedPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int localRoomSequence = 1;
        private string currentAccountId = "";
        private string currentRoomId = "";
        private readonly string localMatchServerIp = "127.0.0.1";
        private const int LocalMatchServerPort = 60001;
        private const int LocalMatchServerUdpPort = 63000;

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
                if (HandleLocalWaitRoomMessage(json))
                {
                    return;
                }

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
                room.PlayerCount = room.Players.Count > 0 ? room.Players.Count : room.PlayerCount;
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
            var ownerId = ResolveLocalPlayerId();
            var ownerName = ResolveLocalPlayerName();
            currentRoomId = roomId;
            loadingCompletedPlayers.Clear();
            var room = new RoomRecord
            {
                RoomId = roomId,
                RoomName = string.IsNullOrWhiteSpace(roomName) ? "New Room" : roomName,
                OwnerId = ownerId,
                Capacity = capacity,
                GameMode = string.IsNullOrWhiteSpace(gameMode) ? "TeamDeathMatch" : gameMode,
                TeamBalance = teamBalance,
                PlayerCount = 1
            };
            room.Players.Add(new RoomRecord.RoomPlayerRecord
            {
                PlayerId = ownerId,
                PlayerName = ownerName,
                IsReady = false
            });
            localRooms.Add(room);

            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.CreateRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = roomName,
                ["Capacity"] = capacity,
                ["GameMode"] = gameMode,
                ["TeamBalance"] = teamBalance ? "True" : "False",
                ["OwnerID"] = ownerId
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

            var existingPlayer = room.Players.Find(player => string.Equals(player.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (existingPlayer == null)
            {
                room.Players.Add(new RoomRecord.RoomPlayerRecord
                {
                    PlayerId = playerId,
                    PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
                    IsReady = false
                });
            }
            else
            {
                existingPlayer.PlayerName = string.IsNullOrWhiteSpace(playerName) ? existingPlayer.PlayerName : playerName;
                existingPlayer.IsReady = false;
            }

            room.PlayerCount = room.Players.Count;
            currentRoomId = room.RoomId;
            loadingCompletedPlayers.Clear();
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

        public JObject GetCurrentWaitRoomPlayerListSnapshot()
        {
            var room = localRooms.Find(candidate => string.Equals(candidate.RoomId, currentRoomId, StringComparison.OrdinalIgnoreCase));
            return room == null ? null : BuildWaitRoomPlayerListMessage(room);
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
                case MessageType.MatchServerInfoRequest:
                {
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.MatchServerInfoResponse,
                        ["Success"] = true,
                        ["IP"] = localMatchServerIp,
                        ["Port"] = LocalMatchServerPort,
                        ["UdpPort"] = LocalMatchServerUdpPort,
                        ["RoomID"] = currentRoomId
                    });
                    break;
                }
                case "ClientLoadingSceneEntered":
                {
                    var playerId = json["PlayerID"]?.ToString() ?? ResolveLocalPlayerId();
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.MatchServerInfoResponse,
                        ["Success"] = true,
                        ["IP"] = localMatchServerIp,
                        ["Port"] = LocalMatchServerPort,
                        ["UdpPort"] = LocalMatchServerUdpPort,
                        ["RoomID"] = currentRoomId,
                        ["PlayerID"] = playerId
                    });
                    break;
                }
                case "LoadingStarted":
                {
                    loadingCompletedPlayers.Clear();
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
                    var normalizedPlayerId = string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
                    loadingCompletedPlayers.Add(normalizedPlayerId);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = "LoadingCompletedNotification",
                        ["Success"] = true,
                        ["PlayerID"] = normalizedPlayerId
                    });

                    var room = localRooms.Find(r => string.Equals(r.RoomId, currentRoomId, StringComparison.OrdinalIgnoreCase));
                    var expectedPlayers = Mathf.Max(1, room?.PlayerCount ?? 1);
                    for (var index = loadingCompletedPlayers.Count; index < expectedPlayers; index++)
                    {
                        var mockPlayerId = $"mock-player-{index + 1:D2}";
                        if (!loadingCompletedPlayers.Add(mockPlayerId))
                        {
                            continue;
                        }

                        EmitToClient(new JObject
                        {
                            ["MessageType"] = "LoadingCompletedNotification",
                            ["Success"] = true,
                            ["PlayerID"] = mockPlayerId
                        });
                    }

                    if (loadingCompletedPlayers.Count >= expectedPlayers)
                    {
                        EmitToClient(new JObject
                        {
                            ["MessageType"] = "AllowEnterMap",
                            ["Success"] = true,
                            ["PlayerID"] = normalizedPlayerId
                        });
                    }

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

            var dmRoom = new RoomRecord
            {
                RoomId = $"room-{localRoomSequence++:D4}",
                RoomName = "Default DM Room",
                OwnerId = "host-001",
                Capacity = 8,
                GameMode = "DeathMatch",
                TeamBalance = false,
                PlayerCount = 2
            };
            dmRoom.Players.Add(new RoomRecord.RoomPlayerRecord { PlayerId = "host-001", PlayerName = "HostDM", IsReady = false });
            dmRoom.Players.Add(new RoomRecord.RoomPlayerRecord { PlayerId = "guest-001", PlayerName = "GuestDM", IsReady = false });
            localRooms.Add(dmRoom);

            var tdmRoom = new RoomRecord
            {
                RoomId = $"room-{localRoomSequence++:D4}",
                RoomName = "Default TDM Room",
                OwnerId = "host-002",
                Capacity = 12,
                GameMode = "TeamDeathMatch",
                TeamBalance = true,
                PlayerCount = 6
            };
            for (var i = 0; i < 6; i++)
            {
                tdmRoom.Players.Add(new RoomRecord.RoomPlayerRecord
                {
                    PlayerId = i == 0 ? "host-002" : $"guest-tdm-{i:D3}",
                    PlayerName = i == 0 ? "HostTDM" : $"Guest{i}",
                    IsReady = false
                });
            }

            localRooms.Add(tdmRoom);
        }

        private bool HandleLocalWaitRoomMessage(JObject json)
        {
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return false;
            }

            switch (messageType)
            {
                case RUDPMessageTypes.WaitRoomChat:
                    EmitToClient(RUDPMessageBuilder.CreateWaitRoomChat(
                        json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? ResolveLocalPlayerId(),
                        json["PlayerName"]?.ToString() ?? ResolveLocalPlayerName(),
                        json["Message"]?.ToString() ?? "",
                        json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? currentRoomId));
                    return true;

                case RUDPMessageTypes.WaitRoomLeave:
                    return HandleLocalWaitRoomLeave(json);

                case RUDPMessageTypes.WaitRoomPlayerReady:
                    return HandleLocalReadyState(json, true);

                case RUDPMessageTypes.WaitRoomPlayerUnready:
                    return HandleLocalReadyState(json, false);

                case RUDPMessageTypes.WaitRoomSettingsChange:
                    return HandleLocalWaitRoomSettingsChange(json);

                case "GameStartRequest":
                    if (!string.IsNullOrWhiteSpace(currentRoomId))
                    {
                        EmitToClient(new JObject
                        {
                            ["MessageType"] = MessageType.MatchServerInfoResponse,
                            ["Success"] = true,
                            ["IP"] = localMatchServerIp,
                            ["Port"] = LocalMatchServerPort,
                            ["UdpPort"] = LocalMatchServerUdpPort,
                            ["RoomID"] = currentRoomId
                        });
                        EmitToClient(RUDPMessageBuilder.CreateWaitRoomStartCountdown(currentRoomId, 5));
                        return true;
                    }

                    return false;
            }

            return false;
        }

        private bool HandleLocalWaitRoomLeave(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? currentRoomId;
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? ResolveLocalPlayerId();
            var room = localRooms.Find(candidate => string.Equals(candidate.RoomId, roomId, StringComparison.OrdinalIgnoreCase));
            if (room == null)
            {
                return false;
            }

            room.Players.RemoveAll(player => string.Equals(player.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            room.PlayerCount = room.Players.Count;
            if (string.Equals(currentRoomId, roomId, StringComparison.OrdinalIgnoreCase))
            {
                currentRoomId = "";
            }
            EmitToClient(RUDPMessageBuilder.CreateWaitRoomLeave(playerId, roomId));
            EmitToClient(BuildWaitRoomPlayerListMessage(room));
            return true;
        }

        private bool HandleLocalReadyState(JObject json, bool ready)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? currentRoomId;
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? ResolveLocalPlayerId();
            var room = localRooms.Find(candidate => string.Equals(candidate.RoomId, roomId, StringComparison.OrdinalIgnoreCase));
            if (room == null)
            {
                return false;
            }

            var player = room.Players.Find(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (player == null)
            {
                player = new RoomRecord.RoomPlayerRecord
                {
                    PlayerId = playerId,
                    PlayerName = ResolveLocalPlayerName(),
                    IsReady = ready
                };
                room.Players.Add(player);
            }
            else
            {
                player.IsReady = ready;
            }

            EmitToClient(ready
                ? RUDPMessageBuilder.CreateWaitRoomPlayerReady(playerId, roomId)
                : RUDPMessageBuilder.CreateWaitRoomPlayerUnready(playerId, roomId));
            EmitToClient(BuildWaitRoomPlayerListMessage(room));

            if (ready && room.Players.Count > 0 && room.Players.TrueForAll(candidate => candidate.IsReady))
            {
                EmitToClient(RUDPMessageBuilder.CreateWaitRoomStartCountdown(roomId, 5));
            }

            return true;
        }

        private bool HandleLocalWaitRoomSettingsChange(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? currentRoomId;
            var room = localRooms.Find(candidate => string.Equals(candidate.RoomId, roomId, StringComparison.OrdinalIgnoreCase));
            var settings = json["Settings"] as JObject;
            if (room == null || settings == null)
            {
                return false;
            }

            if (settings["GameMode"] != null)
            {
                room.GameMode = settings["GameMode"]!.ToString();
            }

            if (settings["Capacity"] != null)
            {
                room.Capacity = settings["Capacity"]!.ToObject<int>();
            }

            if (settings["TeamBalance"] != null)
            {
                room.TeamBalance = settings["TeamBalance"]!.ToObject<bool>();
            }

            room.PlayerCount = room.Players.Count;
            EmitToClient(RUDPMessageBuilder.CreateWaitRoomSettingsChange(roomId, settings));
            return true;
        }

        private static JObject BuildWaitRoomPlayerListMessage(RoomRecord room)
        {
            var players = new JArray();
            foreach (var player in room.Players)
            {
                players.Add(new JObject
                {
                    ["PlayerId"] = player.PlayerId,
                    ["PlayerName"] = player.PlayerName,
                    ["IsReady"] = player.IsReady,
                    ["IsOwner"] = string.Equals(player.PlayerId, room.OwnerId, StringComparison.OrdinalIgnoreCase)
                });
            }

            return RUDPMessageBuilder.CreateWaitRoomPlayerList(room.RoomId, players);
        }

        private string ResolveLocalPlayerId()
        {
            if (!string.IsNullOrWhiteSpace(currentAccountId))
            {
                return currentAccountId;
            }

            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            return string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
        }

        private static string ResolveLocalPlayerName()
        {
            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            return string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        }
    }
}
