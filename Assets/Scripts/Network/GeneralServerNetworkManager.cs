using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// Manages TCP connection to the general (lobby) server.
    /// </summary>
    public class GeneralServerNetworkManager
    {
        private sealed class GuildMemberRecord
        {
            public string PlayerId { get; set; } = "";
            public string Role { get; set; } = "Member";
            public string JoinedAt { get; set; } = "";
        }

        private sealed class GuildRecord
        {
            public string Id { get; set; } = "";
            public string GuildName { get; set; } = "";
            public string GuildShortName { get; set; } = "";
            public string LeaderId { get; set; } = "";
            public int Level { get; set; } = 1;
            public long Experience { get; set; } = 0;
            public string CreationTime { get; set; } = "";
            public Dictionary<string, GuildMemberRecord> Members { get; } = new Dictionary<string, GuildMemberRecord>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class RoomRecord
        {
            public sealed class RoomPlayerRecord
            {
                public string PlayerId { get; set; } = "";
                public string PlayerName { get; set; } = "";
                public bool IsReady { get; set; } = false;
                public string PlayerCharacter { get; set; } = EPlayerCharacter.Misty.ToString();
            }

            public string RoomId { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "TeamDeathMatch";
            public string Map { get; set; } = "";
            public string Password { get; set; } = "";
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
        private readonly Dictionary<string, GuildRecord> localGuilds = new Dictionary<string, GuildRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadingCompletedPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int localRoomSequence = 1;
        private string currentAccountId = "";
        private string currentRoomId = "";
        private readonly string localMatchServerIp = "127.0.0.1";
        private const int LocalMatchServerPort = 60001;
        private const int LocalMatchServerUdpPort = 63000;
        private const string LocalServerStateFileName = "general_server_state.json";

        public GeneralServerNetworkManager()
        {
            LoadLocalServerState();
            if (localRooms.Count == 0)
            {
                SeedLocalRooms();
            }
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
            _ = ip;
            _ = port;
            _ = pass;

            ResetTransientSessionState();
            Online = true;
            var resolvedAccountName = string.IsNullOrWhiteSpace(AccountManager.Instance.CurrentProfile.DisplayName)
                ? "Player"
                : AccountManager.Instance.CurrentProfile.DisplayName;
            var resolvedAccountId = string.IsNullOrWhiteSpace(id)
                ? AccountManager.Instance.CurrentProfile.GlobalUserId
                : id;

            if (string.IsNullOrWhiteSpace(resolvedAccountId))
            {
                resolvedAccountId = "local-account";
            }

            SetCurrentAccount(resolvedAccountName, resolvedAccountId);
            connectedSubject.OnNext(Unit.Default);
        }

        public void TryConnectToServer(string ip, int port)
        {
            ConnectToGeneralServerSync(ip, port, AccountManager.Instance.CurrentProfile.GlobalUserId, "");
        }

        public void Disconnect()
        {
            ClearCurrentRoom();
            SaveLocalServerState();
            Online = false;
            AccountManager.Instance.Logout();
            disconnectedSubject.OnNext(Unit.Default);
        }

        public void ResetTransientSessionState()
        {
            loadingCompletedPlayers.Clear();
            LastMatchResult = null;
            currentRoomId = string.Empty;
        }

        public void ClearCurrentRoom()
        {
            currentRoomId = string.Empty;
            loadingCompletedPlayers.Clear();
        }

        private string LocalServerStatePath => Path.Combine(Application.persistentDataPath, LocalServerStateFileName);

        private void LoadLocalServerState()
        {
            try
            {
                if (!File.Exists(LocalServerStatePath))
                {
                    return;
                }

                var root = JObject.Parse(File.ReadAllText(LocalServerStatePath));

                localRooms.Clear();
                accounts.Clear();
                localGuilds.Clear();

                localRoomSequence = root.Value<int?>("LocalRoomSequence") ?? 1;
                currentAccountId = root.Value<string>("CurrentAccountId") ?? string.Empty;
                currentRoomId = root.Value<string>("CurrentRoomId") ?? string.Empty;

                if (root["Accounts"] is JArray accountArray)
                {
                    foreach (var token in accountArray.OfType<JObject>())
                    {
                        var account = BuildAccountRecord(token);
                        if (!string.IsNullOrWhiteSpace(account.GlobalUserId))
                        {
                            accounts[account.GlobalUserId] = account;
                        }
                    }
                }

                if (root["Rooms"] is JArray roomArray)
                {
                    foreach (var token in roomArray.OfType<JObject>())
                    {
                        var room = BuildRoomRecord(token);
                        if (!string.IsNullOrWhiteSpace(room.RoomId))
                        {
                            localRooms.Add(room);
                        }
                    }
                }

                if (root["Guilds"] is JArray guildArray)
                {
                    foreach (var token in guildArray.OfType<JObject>())
                    {
                        var guild = BuildGuildRecord(token);
                        if (!string.IsNullOrWhiteSpace(guild.GuildName))
                        {
                            localGuilds[guild.GuildName] = guild;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(currentAccountId) && accounts.TryGetValue(currentAccountId, out var currentAccount))
                {
                    AccountManager.Instance.LoginData(currentAccount.AccountName, "", currentAccount.GlobalUserId);
                    AccountManager.Instance.SetCredits(currentAccount.Credits);
                }
                else if (!string.IsNullOrWhiteSpace(currentAccountId))
                {
                    currentAccountId = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(currentRoomId) &&
                    !localRooms.Any(room => string.Equals(room.RoomId, currentRoomId, StringComparison.OrdinalIgnoreCase)))
                {
                    currentRoomId = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeneralServerNetworkManager] Failed to load local state: {ex.Message}");
            }
        }

        private void SaveLocalServerState()
        {
            try
            {
                var root = new JObject
                {
                    ["LocalRoomSequence"] = localRoomSequence,
                    ["CurrentAccountId"] = currentAccountId ?? string.Empty,
                    ["CurrentRoomId"] = currentRoomId ?? string.Empty,
                    ["Accounts"] = new JArray(accounts.Values.Select(BuildAccountJson)),
                    ["Rooms"] = new JArray(localRooms.Select(BuildRoomJson)),
                    ["Guilds"] = new JArray(localGuilds.Values.Select(BuildGuildJson))
                };

                File.WriteAllText(LocalServerStatePath, JsonConvert.SerializeObject(root, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeneralServerNetworkManager] Failed to save local state: {ex.Message}");
            }
        }

        private static JObject BuildAccountJson(AccountRecord account)
        {
            return new JObject
            {
                ["GlobalUserId"] = account.GlobalUserId,
                ["AccountName"] = account.AccountName,
                ["Credits"] = account.Credits,
                ["PurchasedItems"] = new JArray(account.PurchasedItems),
                ["EquippedItems"] = new JArray(account.EquippedItems.Select(item => new JObject
                {
                    ["Category"] = item.Key.ToString(),
                    ["ItemId"] = item.Value ?? string.Empty
                })),
                ["EquippedInstantItems"] = new JArray(account.EquippedInstantItems.Select(item => new JObject
                {
                    ["Slot"] = item.Key,
                    ["ItemId"] = item.Value ?? string.Empty
                }))
            };
        }

        private static AccountRecord BuildAccountRecord(JObject json)
        {
            var account = new AccountRecord
            {
                GlobalUserId = json["GlobalUserId"]?.ToString() ?? string.Empty,
                AccountName = json["AccountName"]?.ToString() ?? string.Empty,
                Credits = json["Credits"]?.ToObject<long>() ?? 1000
            };

            if (json["PurchasedItems"] is JArray purchasedItems)
            {
                foreach (var item in purchasedItems)
                {
                    var itemId = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        account.PurchasedItems.Add(itemId);
                    }
                }
            }

            if (json["EquippedItems"] is JArray equippedItems)
            {
                foreach (var item in equippedItems.OfType<JObject>())
                {
                    var categoryText = item["Category"]?.ToString() ?? string.Empty;
                    if (Enum.TryParse(categoryText, true, out EShopCategory category))
                    {
                        account.EquippedItems[category] = item["ItemId"]?.ToString() ?? string.Empty;
                    }
                }
            }

            if (json["EquippedInstantItems"] is JArray instantItems)
            {
                foreach (var item in instantItems.OfType<JObject>())
                {
                    var slot = item["Slot"]?.ToObject<int>() ?? 0;
                    account.EquippedInstantItems[slot] = item["ItemId"]?.ToString() ?? string.Empty;
                }
            }

            return account;
        }

        private static JObject BuildRoomJson(RoomRecord room)
        {
            return new JObject
            {
                ["RoomId"] = room.RoomId,
                ["RoomName"] = room.RoomName,
                ["OwnerId"] = room.OwnerId,
                ["Capacity"] = room.Capacity,
                ["GameMode"] = room.GameMode,
                ["Map"] = room.Map,
                ["Password"] = room.Password,
                ["TeamBalance"] = room.TeamBalance,
                ["PlayerCount"] = room.PlayerCount,
                ["Players"] = new JArray(room.Players.Select(player => new JObject
                {
                    ["PlayerId"] = player.PlayerId,
                    ["PlayerName"] = player.PlayerName,
                    ["IsReady"] = player.IsReady,
                    ["PlayerCharacter"] = player.PlayerCharacter
                }))
            };
        }

        private static RoomRecord BuildRoomRecord(JObject json)
        {
            var room = new RoomRecord
            {
                RoomId = json["RoomId"]?.ToString() ?? string.Empty,
                RoomName = json["RoomName"]?.ToString() ?? string.Empty,
                OwnerId = json["OwnerId"]?.ToString() ?? string.Empty,
                Capacity = json["Capacity"]?.ToObject<int>() ?? 8,
                GameMode = json["GameMode"]?.ToString() ?? "TeamDeathMatch",
                Map = json["Map"]?.ToString() ?? string.Empty,
                Password = json["Password"]?.ToString() ?? string.Empty,
                TeamBalance = json["TeamBalance"]?.ToObject<bool>() ?? true,
                PlayerCount = json["PlayerCount"]?.ToObject<int>() ?? 0
            };

            if (json["Players"] is JArray players)
            {
                foreach (var token in players.OfType<JObject>())
                {
                    room.Players.Add(new RoomRecord.RoomPlayerRecord
                    {
                        PlayerId = token["PlayerId"]?.ToString() ?? string.Empty,
                        PlayerName = token["PlayerName"]?.ToString() ?? string.Empty,
                        IsReady = token["IsReady"]?.ToObject<bool>() ?? false,
                        PlayerCharacter = token["PlayerCharacter"]?.ToString() ?? EPlayerCharacter.Misty.ToString()
                    });
                }
            }

            room.PlayerCount = room.Players.Count;
            return room;
        }

        private static JObject BuildGuildJson(GuildRecord guild)
        {
            return new JObject
            {
                ["Id"] = guild.Id,
                ["GuildName"] = guild.GuildName,
                ["GuildShortName"] = guild.GuildShortName,
                ["LeaderId"] = guild.LeaderId,
                ["Level"] = guild.Level,
                ["Experience"] = guild.Experience,
                ["CreationTime"] = guild.CreationTime,
                ["Members"] = new JArray(guild.Members.Values.Select(member => new JObject
                {
                    ["PlayerId"] = member.PlayerId,
                    ["Role"] = member.Role,
                    ["JoinedAt"] = member.JoinedAt
                }))
            };
        }

        private static GuildRecord BuildGuildRecord(JObject json)
        {
            var guild = new GuildRecord
            {
                Id = json["Id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                GuildName = json["GuildName"]?.ToString() ?? string.Empty,
                GuildShortName = json["GuildShortName"]?.ToString() ?? string.Empty,
                LeaderId = json["LeaderId"]?.ToString() ?? string.Empty,
                Level = json["Level"]?.ToObject<int>() ?? 1,
                Experience = json["Experience"]?.ToObject<long>() ?? 0,
                CreationTime = json["CreationTime"]?.ToString() ?? DateTime.UtcNow.ToString("o")
            };

            if (json["Members"] is JArray members)
            {
                foreach (var token in members.OfType<JObject>())
                {
                    var playerId = token["PlayerId"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(playerId))
                    {
                        continue;
                    }

                    guild.Members[playerId] = new GuildMemberRecord
                    {
                        PlayerId = playerId,
                        Role = token["Role"]?.ToString() ?? "Member",
                        JoinedAt = token["JoinedAt"]?.ToString() ?? DateTime.UtcNow.ToString("o")
                    };
                }
            }

            return guild;
        }

        public void SendMessage(JObject json)
        {
            if (json != null)
            {
                NormalizeMessageType(json);

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

        public void SendUpdateRoomRequest(System.Collections.Generic.List<OpenGSCore.EGameMode> modes)
        {
            SendUpdateRoomRequestCore(modes);
        }

        private void SendUpdateRoomRequestCore(System.Collections.Generic.List<OpenGSCore.EGameMode> modes)
        {
            var snapshot = new RoomListSnapshot();
            foreach (var room in localRooms)
            {
                room.PlayerCount = room.Players.Count > 0 ? room.Players.Count : room.PlayerCount;
                snapshot.Rooms.Add(new RoomListEntry
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    OwnerId = room.OwnerId,
                    Capacity = room.Capacity,
                    GameMode = room.GameMode,
                    TeamBalance = room.TeamBalance,
                    PlayerCount = room.PlayerCount
                });
            }

            var json = snapshot.ToJson();
            json["MessageType"] = MessageType.RoomListUpdateRequest;
            json["MatchRoomType"] = (modes == null || modes.Count == 0) ? "All" : string.Join(",", modes);
            json["Options"] = "";
            SendMessage(json);
            EmitToClient(snapshot.ToJson());
        }

        public void SendCreateNewWaitRoomRequest(string roomName, int capacity, string gameMode, bool teamBalance, string password = "")
        {
            var roomId = $"room-{localRoomSequence++:D4}";
            var ownerId = ResolveLocalPlayerId();
            currentRoomId = roomId;
            loadingCompletedPlayers.Clear();
            var room = new RoomRecord
            {
                RoomId = roomId,
                RoomName = string.IsNullOrWhiteSpace(roomName) ? "New Room" : roomName,
                OwnerId = ownerId,
                Capacity = capacity,
                GameMode = string.IsNullOrWhiteSpace(gameMode) ? "TeamDeathMatch" : gameMode,
                Password = password ?? string.Empty,
                TeamBalance = teamBalance
            };
            room.Players.Add(CreateLocalRoomPlayerRecord(ownerId, ResolveLocalPlayerName(), false));
            room.PlayerCount = room.Players.Count;
            localRooms.Add(room);

            EmitToClient(BuildRoomInfoSnapshot(room).ToResponseJson(MessageType.CreateRoomResponse));
            EmitToClient(BuildRoomInfoSnapshot(room).ToNotificationJson(MessageType.RoomCreated));
            SaveLocalServerState();
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
                EmitToClient(BuildRoomInfoSnapshot(room).ToNotificationJson(MessageType.RoomFull));
                return;
            }

            if (!string.IsNullOrEmpty(room.Password) && !string.Equals(room.Password, password ?? string.Empty, StringComparison.Ordinal))
            {
                EmitToClient(new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Incorrect password",
                    ["RoomID"] = roomId
                });
                return;
            }

            var existingPlayer = room.Players.Find(player => string.Equals(player.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (existingPlayer == null)
            {
                room.Players.Add(CreateLocalRoomPlayerRecord(playerId, playerName, false));
            }
            else
            {
                ApplyLocalRoomPlayerState(existingPlayer, playerName, false);
            }

            room.PlayerCount = room.Players.Count;
            currentRoomId = room.RoomId;
            loadingCompletedPlayers.Clear();
            var response = BuildRoomInfoSnapshot(room).ToResponseJson(MessageType.JoinRoomResponse);
            response["PlayerID"] = playerId;
            response["PlayerName"] = playerName;
            EmitToClient(response);
            SaveLocalServerState();
        }

        public JObject GetCurrentWaitRoomPlayerListSnapshot()
        {
            var room = localRooms.Find(candidate => string.Equals(candidate.RoomId, currentRoomId, StringComparison.OrdinalIgnoreCase));
            return room == null ? null : BuildWaitRoomPlayerListMessage(room);
        }

        private void EmitToClient(JObject json)
        {
            NormalizeMessageType(json);
            CacheLastMatchResult(json);
            dataReceivedSubject.OnNext(json);
        }

        private void CacheLastMatchResult(JObject json)
        {
            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            if (messageType == MessageType.MatchResult || messageType == MessageType.MatchEndNotification)
            {
                LastMatchResult = json;
            }
        }

        private static RoomInfoSnapshot BuildRoomInfoSnapshot(RoomRecord room)
        {
            return new RoomInfoSnapshot
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                OwnerId = room.OwnerId,
                Capacity = room.Capacity,
                GameMode = room.GameMode,
                Map = room.Map,
                TeamBalance = room.TeamBalance,
                PlayerCount = room.Players.Count
            };
        }

        private static void NormalizeMessageType(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var normalized = MessageType.Normalize(json["MessageType"]?.ToString());
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                json["MessageType"] = normalized;
            }
        }

        private static bool ShouldForwardToClients(string messageType)
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return false;
            }

            var normalizedMessageType = MessageType.Normalize(messageType);

            if (normalizedMessageType.EndsWith("Response", StringComparison.OrdinalIgnoreCase) ||
                normalizedMessageType.EndsWith("Notification", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedMessageType == MessageType.LoginResponse || normalizedMessageType == MessageType.LogoutSuccessful;
        }

        public void SetCurrentAccount(string accountName, string globalUserId)
        {
            var account = EnsureAccount(globalUserId, accountName);
            currentAccountId = account.GlobalUserId;
            AccountManager.Instance.LoginData(account.AccountName, "", account.GlobalUserId);
            AccountManager.Instance.SetCredits(account.Credits);
            SaveLocalServerState();
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
            SaveLocalServerState();
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

            SaveLocalServerState();
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

            SaveLocalServerState();
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
                case MessageType.SceneTransitionRequest:
                {
                    HandleSceneTransitionRequest(json);
                    break;
                }
                case MessageType.ClientLoadingSceneEntered:
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
                case MessageType.LoadingStarted:
                {
                    loadingCompletedPlayers.Clear();
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.LoadingStartedNotification,
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? "",
                        ["Progress"] = 0f
                    });
                    break;
                }
                case MessageType.LoadingProgress:
                {
                    var progress = json["Progress"]?.ToObject<float>() ?? 0f;
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.LoadingProgressNotification,
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? "",
                        ["Progress"] = Mathf.Clamp01(progress)
                    });
                    break;
                }
                case MessageType.LoadingCompleted:
                {
                    var playerId = json["PlayerID"]?.ToString() ?? "";
                    var normalizedPlayerId = string.IsNullOrWhiteSpace(playerId) ? "local_player" : playerId;
                    loadingCompletedPlayers.Add(normalizedPlayerId);
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.LoadingCompletedNotification,
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
                            ["MessageType"] = MessageType.LoadingCompletedNotification,
                            ["Success"] = true,
                            ["PlayerID"] = mockPlayerId
                        });
                    }

                    if (loadingCompletedPlayers.Count >= expectedPlayers)
                    {
                        EmitToClient(new JObject
                        {
                            ["MessageType"] = MessageType.AllowEnterMap,
                            ["Success"] = true,
                            ["PlayerID"] = normalizedPlayerId
                        });
                    }

                    break;
                }
            }
        }

        private void HandleSceneTransitionRequest(JObject json)
        {
            var fromScene = json["FromScene"]?.ToString() ?? string.Empty;
            var toScene = json["ToScene"]?.ToString() ?? string.Empty;
            var reason = json["Reason"]?.ToString() ?? string.Empty;
            var approved = IsSceneTransitionAllowed(fromScene, toScene, reason);

            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.SceneTransitionResponse,
                ["Approved"] = approved,
                ["FromScene"] = fromScene,
                ["ToScene"] = toScene,
                ["Reason"] = approved ? "Approved" : BuildSceneTransitionDenyReason(fromScene, toScene, reason)
            });
        }

        private JArray BuildGuildListArray()
        {
            var result = new JArray();
            foreach (var guild in localGuilds.Values.OrderBy(g => g.GuildName))
            {
                result.Add(BuildGuildSummaryJson(guild));
            }

            return result;
        }

        private static JObject BuildGuildSummaryJson(GuildRecord guild)
        {
            return new JObject
            {
                ["Id"] = guild.Id,
                ["GuildName"] = guild.GuildName,
                ["GuildShortName"] = guild.GuildShortName,
                ["LeaderId"] = guild.LeaderId,
                ["Level"] = guild.Level,
                ["Experience"] = guild.Experience,
                ["CreationTime"] = guild.CreationTime,
                ["MemberCount"] = guild.Members.Count
            };
        }

        private static JObject BuildGuildDetailJson(GuildRecord guild)
        {
            var memberArray = new JArray();
            foreach (var member in guild.Members.Values
                         .OrderBy(member => string.Equals(member.Role, "Leader", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                         .ThenBy(member => member.PlayerId))
            {
                memberArray.Add(new JObject
                {
                    ["MemberId"] = member.PlayerId,
                    ["Role"] = member.Role,
                    ["JoinedAt"] = member.JoinedAt
                });
            }

            return new JObject
            {
                ["Id"] = guild.Id,
                ["GuildName"] = guild.GuildName,
                ["GuildShortName"] = guild.GuildShortName,
                ["LeaderId"] = guild.LeaderId,
                ["Level"] = guild.Level,
                ["Experience"] = guild.Experience,
                ["CreationTime"] = guild.CreationTime,
                ["MemberCount"] = guild.Members.Count,
                ["Members"] = memberArray
            };
        }

        private bool TryGetGuild(string guildName, out GuildRecord guild)
        {
            guild = null;

            if (string.IsNullOrWhiteSpace(guildName))
            {
                return false;
            }

            return localGuilds.TryGetValue(guildName, out guild);
        }

        private bool TryCreateGuild(string guildName, string shortName, string leaderId, out GuildRecord guild)
        {
            guild = null;

            if (string.IsNullOrWhiteSpace(guildName) || string.IsNullOrWhiteSpace(leaderId))
            {
                return false;
            }

            if (localGuilds.ContainsKey(guildName))
            {
                return false;
            }

            guild = new GuildRecord
            {
                GuildName = guildName,
                GuildShortName = string.IsNullOrWhiteSpace(shortName) ? guildName : shortName,
                LeaderId = leaderId
            };

            guild.Members[leaderId] = new GuildMemberRecord
            {
                PlayerId = leaderId,
                Role = "Leader",
                JoinedAt = DateTime.UtcNow.ToString("o")
            };

            localGuilds[guildName] = guild;
            SaveLocalServerState();
            return true;
        }

        private bool TryJoinGuild(string guildName, string memberId, string role, out GuildRecord guild)
        {
            guild = null;

            if (!TryGetGuild(guildName, out guild) || string.IsNullOrWhiteSpace(memberId))
            {
                return false;
            }

            if (guild.Members.ContainsKey(memberId))
            {
                return false;
            }

            guild.Members[memberId] = new GuildMemberRecord
            {
                PlayerId = memberId,
                Role = string.IsNullOrWhiteSpace(role) ? "Member" : role,
                JoinedAt = DateTime.UtcNow.ToString("o")
            };

            if (string.Equals(role, "Leader", StringComparison.OrdinalIgnoreCase))
            {
                guild.LeaderId = memberId;
            }

            SaveLocalServerState();
            return true;
        }

        private bool TryLeaveGuild(string guildName, string memberId, out GuildRecord guild)
        {
            guild = null;

            if (!TryGetGuild(guildName, out guild) || string.IsNullOrWhiteSpace(memberId))
            {
                return false;
            }

            if (!guild.Members.Remove(memberId))
            {
                return false;
            }

            if (string.Equals(guild.LeaderId, memberId, StringComparison.OrdinalIgnoreCase))
            {
                var nextLeader = guild.Members.Values.FirstOrDefault();
                if (nextLeader != null)
                {
                    nextLeader.Role = "Leader";
                    guild.LeaderId = nextLeader.PlayerId;
                }
                else
                {
                    guild.LeaderId = string.Empty;
                }
            }

            SaveLocalServerState();
            return true;
        }

        private bool TryKickGuildMember(string guildName, string memberId, out GuildRecord guild)
        {
            return TryLeaveGuild(guildName, memberId, out guild);
        }

        private bool IsGuildMember(string guildName, string memberId)
        {
            return TryGetGuild(guildName, out var guild) && guild.Members.ContainsKey(memberId);
        }

        private bool IsSceneTransitionAllowed(string fromScene, string toScene, string reason)
        {
            var generalScenes = GeneralSceneMasterData.Instance();
            var lobbyScene = generalScenes != null ? generalScenes.LobbyScene() : "LobbyScene";
            var titleScene = generalScenes != null ? generalScenes.TitleScene() : "TitleScene";
            var onlineWaitRoomScene = generalScenes != null ? generalScenes.OnlineWaitRoomScene() : "OnlineWaitRoom";
            var offlineWaitRoomScene = generalScenes != null ? generalScenes.OfflineWaitRoomScene() : "OfflineWaitRoom";
            var onlineLoadingScene = generalScenes != null ? generalScenes.OnlineLoadingScene() : "OnlineLoadingScene";
            var resultScene = generalScenes != null ? generalScenes.ResultScene() : "ResultScene";

            if (string.IsNullOrWhiteSpace(toScene))
            {
                return false;
            }

            if (string.Equals(toScene, lobbyScene, StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(fromScene, titleScene, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fromScene, resultScene, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fromScene, "ConnectToServerScene", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fromScene, onlineWaitRoomScene, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fromScene, offlineWaitRoomScene, StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(toScene, onlineWaitRoomScene, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(currentRoomId) || LastMatchResult != null;
            }

            if (string.Equals(toScene, offlineWaitRoomScene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(toScene, onlineLoadingScene, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(currentRoomId)
                    && (string.Equals(fromScene, onlineWaitRoomScene, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(reason, "GameStart", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(reason, "StartCountdown", StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(toScene, resultScene, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(currentRoomId) || LastMatchResult != null;
            }

            if (string.Equals(toScene, titleScene, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return true;
        }

        private static string BuildSceneTransitionDenyReason(string fromScene, string toScene, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return $"Transition denied from {fromScene} to {toScene}";
            }

            return $"Transition denied from {fromScene} to {toScene}: {reason}";
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
            dmRoom.Players.Add(new RoomRecord.RoomPlayerRecord { PlayerId = "host-001", PlayerName = "HostDM", IsReady = false, PlayerCharacter = EPlayerCharacter.Misty.ToString() });
            dmRoom.Players.Add(new RoomRecord.RoomPlayerRecord { PlayerId = "guest-001", PlayerName = "GuestDM", IsReady = false, PlayerCharacter = EPlayerCharacter.Ami.ToString() });
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
                    IsReady = false,
                    PlayerCharacter = i == 0 ? EPlayerCharacter.Misty.ToString() : EPlayerCharacter.Ami.ToString()
                });
            }

            localRooms.Add(tdmRoom);
            SaveLocalServerState();
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
                case MessageType.WaitRoomChat:
                    EmitToClient(new JObject
                    {
                        ["MessageType"] = MessageType.WaitRoomChat,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? ResolveLocalPlayerId(),
                        ["PlayerName"] = json["PlayerName"]?.ToString() ?? ResolveLocalPlayerName(),
                        ["Message"] = json["Message"]?.ToString() ?? "",
                        ["RoomID"] = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString() ?? currentRoomId
                    });
                    return true;

                case MessageType.WaitRoomLeave:
                    return HandleLocalWaitRoomLeave(json);

                case MessageType.WaitRoomPlayerReady:
                    return HandleLocalReadyState(json, true);

                case MessageType.WaitRoomPlayerUnready:
                    return HandleLocalReadyState(json, false);

                case MessageType.WaitRoomSettingsChange:
                    return HandleLocalWaitRoomSettingsChange(json);

                case MessageType.GameStartRequest:
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
                        EmitToClient(new JObject
                        {
                            ["MessageType"] = MessageType.WaitRoomStartCountdown,
                            ["RoomID"] = currentRoomId,
                            ["Countdown"] = 5
                        });
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
            EmitToClient(new JObject
            {
                ["MessageType"] = MessageType.WaitRoomLeave,
                ["PlayerID"] = playerId,
                ["RoomID"] = roomId
            });
            if (room.PlayerCount == 0)
            {
                localRooms.Remove(room);
                EmitToClient(BuildRoomInfoSnapshot(room).ToNotificationJson(MessageType.RoomDeleted));
                SaveLocalServerState();
                return true;
            }
            EmitToClient(BuildWaitRoomPlayerListMessage(room));
            SaveLocalServerState();
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
                player = CreateLocalRoomPlayerRecord(playerId, ResolveLocalPlayerName(), ready);
                room.Players.Add(player);
            }
            else
            {
                ApplyLocalRoomPlayerState(player, null, ready);
            }

            EmitToClient(new JObject
            {
                ["MessageType"] = ready ? MessageType.WaitRoomPlayerReady : MessageType.WaitRoomPlayerUnready,
                ["PlayerID"] = playerId,
                ["RoomID"] = roomId
            });
            EmitToClient(BuildWaitRoomPlayerListMessage(room));

            if (ready && room.Players.Count > 0 && room.Players.TrueForAll(candidate => candidate.IsReady))
            {
                EmitToClient(new JObject
                {
                    ["MessageType"] = MessageType.WaitRoomStartCountdown,
                    ["RoomID"] = roomId,
                    ["Countdown"] = 5
                });
            }

            SaveLocalServerState();
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

            if (settings["Map"] != null && Enum.TryParse(settings["Map"]!.ToString(), out EMap map))
            {
                room.Map = map.ToString();
            }

            if (settings["Capacity"] != null)
            {
                room.Capacity = settings["Capacity"]!.ToObject<int>();
            }

            if (settings["TeamBalance"] != null)
            {
                room.TeamBalance = settings["TeamBalance"]!.ToObject<bool>();
            }

            if (settings["PlayerCharacter"] != null)
            {
                var character = ParsePlayerCharacter(settings["PlayerCharacter"]!.ToString());
                ApplyPlayerCharacter(room, ResolveLocalPlayerId(), character);
            }

            room.PlayerCount = room.Players.Count;
            var response = BuildRoomInfoSnapshot(room).ToResponseJson(MessageType.WaitRoomSettingsChange);
            response["RoomID"] = roomId;
            response["Settings"] = settings;
            EmitToClient(response);
            EmitToClient(BuildWaitRoomPlayerListMessage(room));
            SaveLocalServerState();
            return true;
        }

        private static JObject BuildWaitRoomPlayerListMessage(RoomRecord room)
        {
            var snapshot = new WaitRoomSnapshot
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                Capacity = room.Capacity,
                GameMode = room.GameMode,
                TeamBalance = room.TeamBalance,
                OwnerId = room.OwnerId
            };

            foreach (var player in room.Players)
            {
                var info = new PlayerInfo(player.PlayerId, player.PlayerName)
                {
                    IsReady = player.IsReady
                };

                if (Enum.TryParse(player.PlayerCharacter, true, out EPlayerCharacter playerCharacter))
                {
                    info.playerCharacter = playerCharacter;
                }

                snapshot.Players.Add(info);
            }

            return snapshot.ToNetworkJson(MessageType.WaitRoomPlayerList);
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

        private static string ResolveLocalPlayerCharacter()
        {
            return GamePlayerManager.Instance.SelectedPlayerCharacter().ToString();
        }

        private static RoomRecord.RoomPlayerRecord CreateLocalRoomPlayerRecord(string playerId, string playerName, bool isReady)
        {
            var player = new RoomRecord.RoomPlayerRecord
            {
                PlayerId = playerId
            };
            ApplyLocalRoomPlayerState(player, playerName, isReady);
            return player;
        }

        private static void ApplyLocalRoomPlayerState(RoomRecord.RoomPlayerRecord player, string playerName, bool isReady)
        {
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                player.PlayerName = playerName;
            }

            player.IsReady = isReady;
            player.PlayerCharacter = ResolveLocalPlayerCharacter();
        }

        private static EPlayerCharacter ParsePlayerCharacter(string characterName)
        {
            if (!string.IsNullOrWhiteSpace(characterName) &&
                Enum.TryParse(characterName, true, out EPlayerCharacter parsedCharacter))
            {
                return parsedCharacter;
            }

            return GamePlayerManager.Instance.SelectedPlayerCharacter();
        }

        private static void ApplyPlayerCharacter(RoomRecord room, string playerId, EPlayerCharacter character)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var player = room.Players.Find(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            if (player == null)
            {
                return;
            }

            player.PlayerCharacter = character.ToString();
        }
    }
}
