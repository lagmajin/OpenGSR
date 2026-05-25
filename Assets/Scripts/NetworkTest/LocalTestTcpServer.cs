using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;

using OpenGSCore;

namespace OpenGS
{
    public sealed class LocalTestTcpServer
    {
        private static readonly Lazy<LocalTestTcpServer> _instance = new(() => new LocalTestTcpServer());

        private TcpListener _server;
        private bool _isRunning;
        private bool _isConnected;
        private Task _serverTask;

        private string userId="";

        // Default to 0 to avoid accidental startup on a hardcoded port. Port will be set from debug settings.
        public int port = 0;

        public string id = "test";

        public event Action<string> OnMessageReceived;

        public LocalTestTcpServer()
        {
            Debug.Log($"LocalTestTcpServer: ctor invoked. default port={port}");
            NetworkFoundationBootstrap.RegisterDefaultHandlers(_requestRouter);
            RegisterFoundationHandlers();
            SeedDefaultRooms();
        }
        public bool IsRunning => _isRunning;

        private bool _debugLogEnabled=true;
        
        public event Action<TcpClient> OnClientConnected; // 新しく追加

        private TcpClient _connectedClient;

        // ルーム管理用
        private readonly Dictionary<string, RoomInfo> _rooms = new();
        private readonly Dictionary<string, PlayerLobbyInfo> _lobbyPlayers = new();
        private string _currentPlayerId = "";
        private readonly Dictionary<string, HashSet<string>> _friendsByPlayer = new();
        private readonly Dictionary<string, HashSet<string>> _incomingFriendRequestsByTarget = new();
        private readonly NetworkRequestRouter _requestRouter = new();

        private void SendWelcomeMessage()
        {

        }

        /// <summary>
        /// ルーム情報
        /// </summary>
        private class RoomInfo
        {
            public string RoomId { get; set; }
            public string RoomName { get; set; }
            public string OwnerId { get; set; }
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "DeathMatch";
            public string Map { get; set; } = "";
            public bool TeamBalance { get; set; } = true;
            public string Password { get; set; } = "";
            public List<string> Players { get; set; } = new();
        }

        /// <summary>
        /// プレイヤー情報
        /// </summary>
        private class PlayerLobbyInfo
        {
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }
            public bool IsReady { get; set; }
            public string CurrentRoomId { get; set; }
            public string PlayerCharacter { get; set; } = EPlayerCharacter.Misty.ToString();
        }
        public async Task StartAsync(int port)
        {
            if (_isRunning) return;
            Debug.Log($"LocalTestTcpServer: StartAsync called with port={port}");

            try
            {
                Debug.Log($"LocalTestTcpServer: StartAsync called from:\n{Environment.StackTrace}");
            }
            catch { }

            // Record the active port on the instance for external inspection
            this.port = port;

            _isRunning = true;
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();

            //Debug.Log($"[TcpServer] Started on port {port}");
            PrettyLogger.Bold("LocalServer", $"Started on port {port}" );

            _serverTask = Task.Run(AcceptClientsAsync); // 自律的に動く


            //var obj = new AIXJsonObject();

            //obj["MessageType"] = "LoginResponse";

            //Debug.Log(obj.ToString());
        }

        public void SetID()
        {

        }

        private async Task AcceptClientsAsync()
        {
            try
            {
                while (_isRunning)
                {
                    var client = await _server.AcceptTcpClientAsync();
                    //Debug.Log("[TcpServer] Client connected");
                    PrettyLogger.Bold("LocalServer", "Client connected");

                    _connectedClient = client;

                    OnClientConnected?.Invoke(client);
                    // Send a ConnectServerSuccessful message immediately so clients that expect this
                    // server-generated event (e.g. to set RSA key) receive it before login flow.
                    try
                    {
                        var connectMsg = new AIXJsonObject();
                        connectMsg["MessageType"] = "ConnectServerSuccessful";
                        connectMsg["RSAPublicKey"] = "DUMMY_PUBLIC_KEY";
                        SendJsonToClient(connectMsg);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"LocalTestTcpServer: failed to send ConnectServerSuccessful: {ex.Message}");
                    }

                    _ = HandleClientAsync(client); // fire and forget
                }
            }
            catch (ObjectDisposedException) { }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            if (client == null)
            {
                Debug.LogError("TcpClient is null");
                return;
            }
            var stream = client.GetStream(); // using はやめる
            byte[] buffer = new byte[1600];
            int bytesRead;
            var sb = new StringBuilder();

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    string fullBuffer = sb.ToString();
                    string[] parts = fullBuffer.Split('\x1F');

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        string json = parts[i].Trim();
                        if (!string.IsNullOrEmpty(json))
                            OnReceive(json);
                    }

                    sb.Clear();
                    sb.Append(parts[^1]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Client read error: {ex.Message}");
            }
            finally
            {
                stream.Close();
                client.Close();
                Debug.Log("Client disconnected");
            }
        }


        private void OnReceive(string message)
        {
            try
            {
                // Robustly extract the JSON object from the received message. Some clients prefix
                // the payload with a two-letter identifier (e.g. "JS") and there may be extra
                // whitespace or control characters. Find the first '{' and last '}' and parse that slice.
                string parseTarget = message;
                int firstBrace = parseTarget.IndexOf('{');
                int lastBrace = parseTarget.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    parseTarget = parseTarget.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
                else if (firstBrace >= 0)
                {
                    parseTarget = parseTarget.Substring(firstBrace);
                }

                parseTarget = parseTarget.Trim();

                var json = JObject.Parse(parseTarget);
                PrettyLogger.Bold("LocalServer", $"[TcpServer] Parsed JSON: {json}");

                if (_requestRouter.CanHandle(json))
                {
                    JObject envelopeResponse = _requestRouter.HandleAsync(json).GetAwaiter().GetResult();
                    SendJsonToClient(envelopeResponse);
                    return;
                }

                var messageType = MessageType.Normalize(json["MessageType"]?.ToString());

                if (messageType == MessageType.LoginRequest)
                {
                    // Client requested login — respond with a LoginResponse message to simulate auth success.
                    OnLogin();
                }

                if(messageType == MessageType.LogoutRequest)
                {
                    OnLogout();
                }

                if(messageType == MessageType.PlayerInfoRequest || messageType == MessageType.PlayerInfo)
                {
                    userId = json["PlayerID"]?.ToString();

                    Debug.Log("User Id:"+userId);

                }
                

                if (messageType == MessageType.ClientLoadingSceneEntered)
                {

                }

                if (messageType == MessageType.LoadingStarted)
                {
                    PrettyLogger.Bold("Network", MessageType.LoadingStarted);


                }

                if (messageType == "EquipRequest")
                {
                    // Client requests equip info -> respond with PlayerEquipInfo loaded from persistent storage
                    try
                    {
                        var loader = new PlayerEquipLoader();
                        var equip = loader.Load();

                        var resp = new JObject();
                        resp["MessageType"] = "PlayerEquipInfo";
                        resp["PlayerCharacter"] = equip?.PlayerCharacter.ToString() ?? "Ami";

                        var slots = new JArray();
                        if (equip?.InstantItemSlots != null)
                        {
                            foreach (var s in equip.InstantItemSlots)
                            {
                                slots.Add(s.ToString());
                            }
                        }
                        resp["InstantItemSlot"] = slots;

                        SendJsonToClient(resp);
                        PrettyLogger.Bold("LocalServer", $"Sent PlayerEquipInfo to client: {resp.ToString(Newtonsoft.Json.Formatting.None)}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"LocalTestTcpServer: failed to respond to EquipRequest: {ex.Message}");
                    }
                }

                if (messageType == MessageType.CreateRoomRequest)
                {
                    // Client requests to create a new room
                    try
                    {
                        var roomName = json["RoomName"]?.ToString() ?? "New Room";
                        var capacity = json["Capacity"]?.ToString() ?? "2";
                        var gameMode = json["GameMode"]?.ToString() ?? "DeathMatch";
                        var teamBalance = json["TeamBalance"]?.ToString() ?? "true";
                        var password = json["Password"]?.ToString() ?? "";

                        // Generate a unique room ID
                        var roomId = Guid.NewGuid().ToString("N").Substring(0, 8);
                        var ownerId = !string.IsNullOrWhiteSpace(_currentPlayerId)
                            ? _currentPlayerId
                            : (json["PlayerID"]?.ToString() ?? "owner");

                        _rooms[roomId] = new RoomInfo
                        {
                            RoomId = roomId,
                            RoomName = roomName,
                            OwnerId = ownerId,
                            Capacity = int.TryParse(capacity, out var parsedCapacity) ? parsedCapacity : 8,
                            GameMode = gameMode,
                            TeamBalance = bool.TryParse(teamBalance, out var parsedTeamBalance) ? parsedTeamBalance : true,
                            Password = password,
                            Players = new List<string> { ownerId }
                        };

                        var resp = new JObject();
                        resp["MessageType"] = MessageType.CreateRoomResponse;
                        resp["Success"] = true;
                        resp["RoomID"] = roomId;
                        resp["RoomName"] = roomName;
                        resp["Capacity"] = capacity;
                        resp["GameMode"] = gameMode;
                        resp["TeamBalance"] = teamBalance;
                        resp["OwnerID"] = ownerId;
                        resp["PlayerCount"] = 1;

                        SendJsonToClient(resp);
                        PrettyLogger.Bold("LocalServer", $"Created room: {roomName} (ID: {roomId})");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"LocalTestTcpServer: failed to handle CreateRoomRequest: {ex.Message}");
                        var errorResp = new JObject();
                        errorResp["MessageType"] = MessageType.CreateRoomResponse;
                        errorResp["Success"] = false;
                        errorResp["ErrorMessage"] = ex.Message;
                        SendJsonToClient(errorResp);
                    }
                }

                if (messageType == MessageType.RoomListUpdateRequest)
                {
                    // Client requests room list
                    try
                    {
                        var resp = new JObject();
                        resp["MessageType"] = MessageType.RoomListUpdateNotification;
                        resp["Rooms"] = BuildRoomListSnapshot();
                        SendJsonToClient(resp);
                        PrettyLogger.Bold("LocalServer", "Sent RoomListUpdateNotification");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"LocalTestTcpServer: failed to handle RoomListUpdateRequest: {ex.Message}");
                    }
                }

                if (messageType == MessageType.FriendRequest)
                {
                    HandleFriendRequest(json);
                }

                if (messageType == MessageType.FriendApproveRequest)
                {
                    HandleFriendApproveRequest(json);
                }

                if (messageType == MessageType.FriendListRequest)
                {
                    HandleFriendListRequest(json);
                }

                if (messageType == MessageType.LoadingProgress)
                {
                    var progress = float.Parse(json["Progress"].ToString());

                    var progressResp = new JObject
                    {
                        ["MessageType"] = MessageType.LoadingProgressNotification,
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? "",
                        ["Progress"] = Mathf.Clamp01(progress)
                    };
                    SendJsonToClient(progressResp);

                    if(progress>=1.0f)
                    {
                        var id = json["id"].ToString();

                        var result = new AIXJsonObject();
                        result["MessageType"] = MessageType.AllowEnterMap;


                        SendJsonToClient(result);
                    }

                    PrettyLogger.Bold("Network", MessageType.LoadingProgressNotification);
                }

                if (messageType == MessageType.LoadingCompleted)
                {
                    var completeResp = new JObject
                    {
                        ["MessageType"] = MessageType.LoadingCompletedNotification,
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? ""
                    };
                    SendJsonToClient(completeResp);

                    var result = new JObject
                    {
                        ["MessageType"] = MessageType.AllowEnterMap,
                        ["Success"] = true,
                        ["PlayerID"] = json["PlayerID"]?.ToString() ?? ""
                    };
                    SendJsonToClient(result);

                }

                #region ロビー・待機室系ハンドラー

                if (messageType == MessageType.LobbyEnter)
                {
                    HandleLobbyEnter(json);
                }

                if (messageType == MessageType.LobbyLeave)
                {
                    HandleLobbyLeave(json);
                }

                if (messageType == MessageType.LobbyChat)
                {
                    HandleLobbyChat(json);
                }

                if (messageType == MessageType.WaitRoomEnter)
                {
                    HandleWaitRoomEnter(json);
                }

                if (messageType == MessageType.WaitRoomLeave)
                {
                    HandleWaitRoomLeave(json);
                }

                if (messageType == MessageType.WaitRoomChat)
                {
                    HandleWaitRoomChat(json);
                }

                if (messageType == MessageType.WaitRoomPlayerReady)
                {
                    HandleWaitRoomPlayerReady(json);
                }

                if (messageType == MessageType.WaitRoomPlayerUnready)
                {
                    HandleWaitRoomPlayerUnready(json);
                }

                if (messageType == MessageType.WaitRoomSettingsChange)
                {
                    HandleWaitRoomSettingsChange(json);
                }

                if (messageType == MessageType.WaitRoomKickPlayer)
                {
                    HandleWaitRoomKickPlayer(json);
                }

                if (messageType == MessageType.WaitRoomStartCountdown)
                {
                    HandleWaitRoomStartCountdown(json);
                }

                if (messageType == MessageType.WaitRoomCancelCountdown)
                {
                    HandleWaitRoomCancelCountdown(json);
                }

                if (messageType == MessageType.JoinRoomRequest)
                {
                    HandleSendEnterRoom(json);
                }

                #endregion


                // イベント通知
                OnMessageReceived?.Invoke(message);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[TcpServer] Invalid JSON: {ex.Message} / Data: {message}");

                
            }
        }
        public void SendJsonToClient(JObject obj)
        {
            if (_connectedClient == null || !_connectedClient.Connected)
            {
                Debug.Log("[TcpServer] No connected client");
                return;
            }
            obj["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string json = obj.ToString(Newtonsoft.Json.Formatting.None);

            // Prefix with identifier expected by client ReadLoop2 ("JS") and append unit separator 0x1F
            try
            {
                NetworkStream stream = _connectedClient.GetStream();
                var prefix = Encoding.UTF8.GetBytes("JS");
                var payload = Encoding.UTF8.GetBytes(json);
                var separator = new byte[] { 0x1F };

                byte[] data = new byte[prefix.Length + payload.Length + separator.Length];
                Buffer.BlockCopy(prefix, 0, data, 0, prefix.Length);
                Buffer.BlockCopy(payload, 0, data, prefix.Length, payload.Length);
                Buffer.BlockCopy(separator, 0, data, prefix.Length + payload.Length, separator.Length);

                stream.Write(data, 0, data.Length);
                stream.Flush();
                Debug.Log($"[TcpServer] Sent JSON (JS+payload+sep): {json}");
            }
            catch (Exception ex)
            {
                Debug.Log($"[TcpServer] Failed to send JSON: {ex.Message}");
            }
        }

        public void SendJsonToClient(AIXJsonObject obj)
        {
            if (_connectedClient == null || !_connectedClient.Connected)
            {
                Debug.Log("[TcpServer] No connected client");
                return;
            }

            obj["TimeStamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string json = obj.ToString();

            try
            {
                NetworkStream stream = _connectedClient.GetStream();
                var prefix = Encoding.UTF8.GetBytes("JS");
                var payload = Encoding.UTF8.GetBytes(json);
                var separator = new byte[] { 0x1F };

                byte[] data = new byte[prefix.Length + payload.Length + separator.Length];
                Buffer.BlockCopy(prefix, 0, data, 0, prefix.Length);
                Buffer.BlockCopy(payload, 0, data, prefix.Length, payload.Length);
                Buffer.BlockCopy(separator, 0, data, prefix.Length + payload.Length, separator.Length);

                stream.Write(data, 0, data.Length);
                stream.Flush();
                Debug.Log($"[TcpServer] Sent JSON (JS+payload+sep): {json}");
            }
            catch (Exception ex)
            {
                Debug.Log($"[TcpServer] Failed to send JSON: {ex.Message}");
            }
        }
        private void OnLogin()
        {
            var endpoint = _connectedClient.Client.RemoteEndPoint;

            var clientID = "test";

            _isConnected = true;

            var obj = new AIXJsonObject();

            // Use the message type expected by the client-side parser
            obj["MessageType"] = MessageType.LoginResponse;
            obj["GlobalUserId"] = string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString("N") : userId;

            SendJsonToClient(obj);

            // Optionally request equipment from client to continue flow
            var message = new AIXJsonObject();
            message["MessageType"] = "EquipRequest";
            message[""] = "";
            SendJsonToClient(message);

        }

        private void OnLogout()
        {
            _isConnected = false;

            var obj = new AIXJsonObject();

            obj["MessageType"] = MessageType.LogoutSuccessful;


            SendJsonToClient(obj);

        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _server?.Stop();
            Console.WriteLine("[TcpServer] Stopped");
        }

        public void SetDebugLog(bool enabled)
        {
            _debugLogEnabled = enabled;
        }

        #region ロビー・待機室ハンドラーメソッド

        private void HandleLobbyEnter(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            var playerName = json["PlayerName"]?.ToString() ?? "Player";

            _currentPlayerId = playerId;
            _lobbyPlayers[playerId] = new PlayerLobbyInfo
            {
                PlayerId = playerId,
                PlayerName = playerName,
                IsReady = false,
                CurrentRoomId = null,
                PlayerCharacter = ResolveLocalPlayerCharacter()
            };

            // 入室通知を送信
            var resp = RUDPMessageBuilder.CreateLobbyEnter(playerId, playerName);
            SendJsonToClient(resp);

            // ロビーのプレイヤー一覧を送信
            var playersArray = new JArray();
            foreach (var player in _lobbyPlayers.Values)
            {
                playersArray.Add(new JObject
                {
                    ["PlayerId"] = player.PlayerId,
                    ["PlayerName"] = player.PlayerName,
                    ["IsReady"] = player.IsReady,
                    ["PlayerCharacter"] = player.PlayerCharacter
                });
            }
            var listResp = RUDPMessageBuilder.CreateLobbyPlayerList(playersArray);
            SendJsonToClient(listResp);

            PrettyLogger.Bold("LocalServer", $"Player entered lobby: {playerName} ({playerId})");
        }

        private void HandleLobbyLeave(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();

            if (!string.IsNullOrEmpty(playerId) && _lobbyPlayers.ContainsKey(playerId))
            {
                var player = _lobbyPlayers[playerId];
                _lobbyPlayers.Remove(playerId);

                var resp = RUDPMessageBuilder.CreateLobbyLeave(playerId);
                SendJsonToClient(resp);

                PrettyLogger.Bold("LocalServer", $"Player left lobby: {player.PlayerName} ({playerId})");
            }
        }

        private void HandleLobbyChat(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();

            var resp = RUDPMessageBuilder.CreateLobbyChat(playerId ?? "", playerName ?? "Unknown", message ?? "");
            SendJsonToClient(resp);

            PrettyLogger.Bold("LocalServer", $"[Lobby Chat] {playerName}: {message}");
        }

        private void HandleWaitRoomEnter(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? _currentPlayerId;
            var playerName = json["PlayerName"]?.ToString() ?? "Player";
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();

            if (string.IsNullOrEmpty(roomId))
            {
                PrettyLogger.Bold("LocalServer", "待機室入室: RoomID がありません");
                return;
            }

            // ルームが存在しない場合は作成
            if (!_rooms.ContainsKey(roomId))
            {
                _rooms[roomId] = new RoomInfo
                {
                    RoomId = roomId,
                    RoomName = $"Room {roomId}",
                    OwnerId = playerId,
                    Players = new List<string> { playerId }
                };
            }
            else
            {
                // 既存ルームにプレイヤーを追加
                if (!_rooms[roomId].Players.Contains(playerId))
                {
                    _rooms[roomId].Players.Add(playerId);
                }
            }

            // プレイヤー情報を更新
            if (_lobbyPlayers.ContainsKey(playerId))
            {
                _lobbyPlayers[playerId].CurrentRoomId = roomId;
                _lobbyPlayers[playerId].IsReady = false;
                _lobbyPlayers[playerId].PlayerCharacter = ResolveLocalPlayerCharacter();
            }
            else
            {
                _lobbyPlayers[playerId] = new PlayerLobbyInfo
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    IsReady = false,
                    CurrentRoomId = roomId,
                    PlayerCharacter = ResolveLocalPlayerCharacter()
                };
            }

            // 入室通知を送信
            var resp = RUDPMessageBuilder.CreateWaitRoomEnter(playerId, playerName, roomId);
            resp["RoomName"] = _rooms[roomId].RoomName;
            resp["Capacity"] = _rooms[roomId].Capacity;
            resp["GameMode"] = _rooms[roomId].GameMode;
            resp["OwnerID"] = _rooms[roomId].OwnerId;
            resp["PlayerCount"] = _rooms[roomId].Players.Count;
            SendJsonToClient(resp);

            // ルームのプレイヤー一覧を送信
            SendWaitRoomPlayerList(roomId);

            PrettyLogger.Bold("LocalServer", $"Player entered waitroom: {playerName} in room {roomId}");
        }

        private void SeedDefaultRooms()
        {
            if (_rooms.Count > 0)
            {
                return;
            }

            _rooms["room-0001"] = new RoomInfo
            {
                RoomId = "room-0001",
                RoomName = "Default DM Room",
                OwnerId = "host-001",
                Capacity = 8,
                GameMode = "DeathMatch",
                TeamBalance = true,
                Players = new List<string> { "host-001" }
            };

            _rooms["room-0002"] = new RoomInfo
            {
                RoomId = "room-0002",
                RoomName = "Default TDM Room",
                OwnerId = "host-002",
                Capacity = 12,
                GameMode = "TeamDeathMatch",
                TeamBalance = true,
                Players = new List<string> { "host-002" }
            };

            _lobbyPlayers["host-001"] = new PlayerLobbyInfo
            {
                PlayerId = "host-001",
                PlayerName = "HostDM",
                IsReady = false,
                CurrentRoomId = "room-0001",
                PlayerCharacter = EPlayerCharacter.Misty.ToString()
            };

            _lobbyPlayers["host-002"] = new PlayerLobbyInfo
            {
                PlayerId = "host-002",
                PlayerName = "HostTDM",
                IsReady = false,
                CurrentRoomId = "room-0002",
                PlayerCharacter = EPlayerCharacter.Ami.ToString()
            };
        }

        private JArray BuildRoomListSnapshot()
        {
            var rooms = new JArray();
            foreach (var room in _rooms.Values)
            {
                rooms.Add(new JObject
                {
                    ["RoomID"] = room.RoomId,
                    ["RoomName"] = room.RoomName,
                    ["OwnerID"] = room.OwnerId,
                    ["Capacity"] = room.Capacity,
                    ["GameMode"] = room.GameMode,
                    ["TeamBalance"] = room.TeamBalance,
                    ["PlayerCount"] = room.Players.Count,
                });
            }

            return rooms;
        }

        private void HandleWaitRoomLeave(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? _currentPlayerId;
            var roomId = json["RoomId"]?.ToString();

            if (string.IsNullOrEmpty(roomId) && _lobbyPlayers.ContainsKey(playerId))
            {
                roomId = _lobbyPlayers[playerId].CurrentRoomId;
            }

            if (!string.IsNullOrEmpty(roomId) && _rooms.ContainsKey(roomId))
            {
                _rooms[roomId].Players.Remove(playerId);

                // ルームが空になった場合は削除
                if (_rooms[roomId].Players.Count == 0)
                {
                    _rooms.Remove(roomId);
                    PrettyLogger.Bold("LocalServer", $"Room {roomId} deleted (empty)");
                }
            }

            if (_lobbyPlayers.ContainsKey(playerId))
            {
                _lobbyPlayers[playerId].CurrentRoomId = null;
                _lobbyPlayers[playerId].IsReady = false;
            }

            var resp = RUDPMessageBuilder.CreateWaitRoomLeave(playerId, roomId ?? "");
            SendJsonToClient(resp);

            PrettyLogger.Bold("LocalServer", $"Player left waitroom: {playerId} from room {roomId}");
        }

        private void HandleWaitRoomChat(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();
            var playerName = json["PlayerName"]?.ToString();
            var message = json["Message"]?.ToString();
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();

            var resp = RUDPMessageBuilder.CreateWaitRoomChat(playerId ?? "", playerName ?? "Unknown", message ?? "", roomId ?? "");
            SendJsonToClient(resp);

            PrettyLogger.Bold("LocalServer", $"[待機室チャット] {playerName}: {message}");
        }

        private void HandleWaitRoomPlayerReady(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? _currentPlayerId;
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();

            if (_lobbyPlayers.ContainsKey(playerId))
            {
                _lobbyPlayers[playerId].IsReady = true;
                roomId ??= _lobbyPlayers[playerId].CurrentRoomId;
            }

            var resp = RUDPMessageBuilder.CreateWaitRoomPlayerReady(playerId, roomId ?? "");
            SendJsonToClient(resp);

            if (!string.IsNullOrEmpty(roomId))
            {
                SendWaitRoomPlayerList(roomId);
            }

            PrettyLogger.Bold("LocalServer", $"Player ready: {playerId}");

            // 全員準備完了かチェック
            CheckAllPlayersReady(roomId);
        }

        private void HandleWaitRoomPlayerUnready(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString() ?? _currentPlayerId;
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();

            if (_lobbyPlayers.ContainsKey(playerId))
            {
                _lobbyPlayers[playerId].IsReady = false;
                roomId ??= _lobbyPlayers[playerId].CurrentRoomId;
            }

            var resp = RUDPMessageBuilder.CreateWaitRoomPlayerUnready(playerId, roomId ?? "");
            SendJsonToClient(resp);

            if (!string.IsNullOrEmpty(roomId))
            {
                SendWaitRoomPlayerList(roomId);
            }

            PrettyLogger.Bold("LocalServer", $"Player unready: {playerId}");
        }

        private void HandleWaitRoomSettingsChange(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var settings = json["Settings"] as JObject;
            var playerId = _currentPlayerId;

            if (!string.IsNullOrEmpty(roomId) && _rooms.ContainsKey(roomId))
            {
                // 設定を更新
                if (settings?["GameMode"] != null)
                    _rooms[roomId].GameMode = settings["GameMode"].ToString();
                if (settings?["Map"] != null && Enum.TryParse(settings["Map"].ToString(), out EMap map))
                    _rooms[roomId].Map = map.ToString();
                if (settings?["Capacity"] != null)
                    _rooms[roomId].Capacity = int.Parse(settings["Capacity"].ToString());
                if (settings?["TeamBalance"] != null)
                    _rooms[roomId].TeamBalance = bool.Parse(settings["TeamBalance"].ToString());

                if (!string.IsNullOrWhiteSpace(playerId) && settings?["PlayerCharacter"] != null)
                {
                    var character = ParsePlayerCharacter(settings["PlayerCharacter"]!.ToString());
                    if (_lobbyPlayers.TryGetValue(playerId, out var player))
                    {
                        player.PlayerCharacter = character.ToString();
                    }
                }

                var resp = RUDPMessageBuilder.CreateWaitRoomSettingsChange(roomId, settings ?? new JObject());
                SendJsonToClient(resp);
                SendWaitRoomPlayerList(roomId);

                PrettyLogger.Bold("LocalServer", $"Room settings changed: {roomId}");
            }
        }

        private void HandleWaitRoomKickPlayer(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? json["PlayerId"]?.ToString();  // キック対象のプレイヤーID
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var reason = json["Reason"]?.ToString() ?? "Kicked by host";
            var kickedByPlayerId = json["KickedByPlayerID"]?.ToString() ?? json["KickedByPlayerId"]?.ToString() ?? _currentPlayerId;  // キックを実行したプレイヤーID

            if (string.IsNullOrEmpty(roomId) || !_rooms.ContainsKey(roomId) || string.IsNullOrEmpty(playerId))
            {
                PrettyLogger.Bold("LocalServer", $"Kick failed: invalid parameters (roomId={roomId}, playerId={playerId})");
                return;
            }

            var room = _rooms[roomId];

            // オーナー権限チェック: ルームオーナーのみがキック可能
            if (room.OwnerId != kickedByPlayerId)
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("NOT_OWNER", "Only the room owner can kick players");
                SendJsonToClient(errorResp);
                PrettyLogger.Bold("LocalServer", $"Kick denied: {kickedByPlayerId} is not the room owner");
                return;
            }

            // 自分自身をキックしようとした場合は拒否
            if (playerId == kickedByPlayerId)
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("CANNOT_KICK_SELF", "Cannot kick yourself");
                SendJsonToClient(errorResp);
                PrettyLogger.Bold("LocalServer", "Kick denied: cannot kick yourself");
                return;
            }

            // 対象プレイヤーがルームに存在するかチェック
            if (!room.Players.Contains(playerId))
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("PLAYER_NOT_IN_ROOM", "Target player is not in the room");
                SendJsonToClient(errorResp);
                PrettyLogger.Bold("LocalServer", $"Kick failed: {playerId} is not in room {roomId}");
                return;
            }

            // キック実行
            room.Players.Remove(playerId);

            if (_lobbyPlayers.ContainsKey(playerId))
            {
                _lobbyPlayers[playerId].CurrentRoomId = null;
            }

            var resp = RUDPMessageBuilder.CreateWaitRoomKickPlayer(playerId, roomId, reason);
            SendJsonToClient(resp);

            // ルームのプレイヤー一覧を更新
            SendWaitRoomPlayerList(roomId);

            PrettyLogger.Bold("LocalServer", $"Player kicked: {playerId} from {roomId} by {kickedByPlayerId}");
        }

        private void HandleWaitRoomStartCountdown(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var countdown = json["Countdown"]?.ToObject<int>() ?? 5;

            var resp = RUDPMessageBuilder.CreateWaitRoomStartCountdown(roomId ?? "", countdown);
            SendJsonToClient(resp);

            PrettyLogger.Bold("LocalServer", $"Game countdown started: {countdown} seconds");

            // カウントダウン終了後にマッチ開始を通知（実際の実装では非同期で待つ）
            // ここでは即座にGameStartRequestとして処理
        }

        private void HandleWaitRoomCancelCountdown(JObject json)
        {
            var roomId = json["RoomID"]?.ToString() ?? json["RoomId"]?.ToString();
            var reason = json["Reason"]?.ToString() ?? "Cancelled";

            var resp = RUDPMessageBuilder.CreateWaitRoomCancelCountdown(roomId ?? "", reason);
            SendJsonToClient(resp);

            PrettyLogger.Bold("LocalServer", $"Countdown cancelled: {reason}");
        }

        private void HandleSendEnterRoom(JObject json)
        {
            var roomId = json["RoomId"]?.ToString() ?? json["RoomID"]?.ToString();
            var playerId = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? _currentPlayerId;
            var password = json["Password"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(roomId))
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("INVALID_ROOM", "Room ID is required");
                SendJsonToClient(errorResp);
                return;
            }

            // ルームが存在しない場合
            if (!_rooms.ContainsKey(roomId))
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("ROOM_NOT_FOUND", "Room not found");
                SendJsonToClient(errorResp);
                return;
            }

            var room = _rooms[roomId];

            // パスワードチェック
            if (!string.IsNullOrEmpty(room.Password) && room.Password != password)
            {
                var errorResp = RUDPMessageBuilder.CreateErrorMessage("WRONG_PASSWORD", "Incorrect password");
                SendJsonToClient(errorResp);
                return;
            }

            // 満員チェック
            if (room.Players.Count >= room.Capacity)
            {
                var resp = RUDPMessageBuilder.CreateRoomFull(roomId);
                SendJsonToClient(resp);
                return;
            }

            // ルームに参加
            HandleWaitRoomEnter(new JObject
            {
                ["PlayerId"] = playerId,
                ["RoomId"] = roomId
            });
        }

        private void SendWaitRoomPlayerList(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || !_rooms.ContainsKey(roomId)) return;

            var room = _rooms[roomId];
            var playersArray = new JArray();

            foreach (var playerId in room.Players)
            {
                if (_lobbyPlayers.TryGetValue(playerId, out var player))
                {
                    playersArray.Add(new JObject
                    {
                        ["PlayerId"] = player.PlayerId,
                        ["PlayerName"] = player.PlayerName,
                        ["IsReady"] = player.IsReady,
                        ["PlayerCharacter"] = player.PlayerCharacter,
                        ["IsOwner"] = player.PlayerId == room.OwnerId
                    });
                }
            }

            var resp = RUDPMessageBuilder.CreateWaitRoomPlayerList(roomId, playersArray);
            SendJsonToClient(resp);
        }

        private void CheckAllPlayersReady(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || !_rooms.ContainsKey(roomId)) return;

            var room = _rooms[roomId];
            bool allReady = true;

            foreach (var playerId in room.Players)
            {
                if (_lobbyPlayers.TryGetValue(playerId, out var player))
                {
                    if (!player.IsReady)
                    {
                        allReady = false;
                        break;
                    }
                }
            }

            if (allReady && room.Players.Count > 0)
            {
                // 全員準備完了 - カウントダウン開始
                var countdownResp = RUDPMessageBuilder.CreateWaitRoomStartCountdown(roomId, 5);
                SendJsonToClient(countdownResp);
                PrettyLogger.Bold("LocalServer", $"All players ready in room {roomId}, starting countdown");
            }
        }

        private static string ResolveLocalPlayerCharacter()
        {
            return GamePlayerManager.Instance.SelectedPlayerCharacter().ToString();
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

        private void HandleFriendRequest(JObject json)
        {
            var requesterId = json["PlayerID"]?.ToString() ?? _currentPlayerId;
            var targetPlayerId = json["TargetPlayerID"]?.ToString();

            var response = new JObject
            {
                ["MessageType"] = MessageType.FriendRequestResponse,
                ["PlayerID"] = requesterId,
                ["TargetPlayerID"] = targetPlayerId ?? ""
            };

            bool success = TryProcessFriendRequest(requesterId, targetPlayerId, out string error);
            response["Success"] = success;
            response["Error"] = error ?? string.Empty;
            SendJsonToClient(response);
        }

        private void HandleFriendApproveRequest(JObject json)
        {
            var approverId = json["PlayerID"]?.ToString() ?? _currentPlayerId;
            var requestPlayerId = json["RequestPlayerID"]?.ToString();
            bool approve = json["Approve"]?.ToObject<bool>() ?? true;

            var response = new JObject
            {
                ["MessageType"] = MessageType.FriendApproveResponse,
                ["PlayerID"] = approverId ?? "",
                ["RequestPlayerID"] = requestPlayerId ?? "",
                ["Approved"] = approve
            };

            if (string.IsNullOrWhiteSpace(approverId) || string.IsNullOrWhiteSpace(requestPlayerId))
            {
                response["Success"] = false;
                response["Error"] = "PlayerID and RequestPlayerID are required.";
                SendJsonToClient(response);
                return;
            }

            EnsureFriendStorage(approverId);
            EnsureFriendStorage(requestPlayerId);

            if (!_incomingFriendRequestsByTarget[approverId].Remove(requestPlayerId))
            {
                response["Success"] = false;
                response["Error"] = "No pending friend request.";
                SendJsonToClient(response);
                return;
            }

            if (approve)
            {
                _friendsByPlayer[approverId].Add(requestPlayerId);
                _friendsByPlayer[requestPlayerId].Add(approverId);
            }

            response["Success"] = true;
            SendJsonToClient(response);
            PrettyLogger.Bold("LocalServer", $"Friend request handled: {requestPlayerId} -> {approverId}, approved={approve}");
        }

        private void HandleFriendListRequest(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? _currentPlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                var errorResponse = new JObject
                {
                    ["MessageType"] = MessageType.FriendListResponse,
                    ["PlayerID"] = "",
                    ["Success"] = false,
                    ["Error"] = "PlayerID is required."
                };
                SendJsonToClient(errorResponse);
                return;
            }

            EnsureFriendStorage(playerId);

            var friends = new JArray();
            foreach (var friendId in _friendsByPlayer[playerId])
            {
                friends.Add(new JObject
                {
                    ["PlayerID"] = friendId,
                    ["PlayerName"] = ResolvePlayerName(friendId)
                });
            }

            var pendingRequests = new JArray();
            foreach (var requesterId in _incomingFriendRequestsByTarget[playerId])
            {
                pendingRequests.Add(new JObject
                {
                    ["FromPlayerID"] = requesterId,
                    ["FromPlayerName"] = ResolvePlayerName(requesterId)
                });
            }

            var response = new JObject
            {
                ["MessageType"] = MessageType.FriendListResponse,
                ["PlayerID"] = playerId,
                ["Success"] = true,
                ["Friends"] = friends,
                ["PendingRequests"] = pendingRequests
            };

            SendJsonToClient(response);
            PrettyLogger.Bold("LocalServer", $"Friend list sent for player {playerId}");
        }

        private void EnsureFriendStorage(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (!_friendsByPlayer.ContainsKey(playerId))
            {
                _friendsByPlayer[playerId] = new HashSet<string>();
            }

            if (!_incomingFriendRequestsByTarget.ContainsKey(playerId))
            {
                _incomingFriendRequestsByTarget[playerId] = new HashSet<string>();
            }
        }

        private string ResolvePlayerName(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId) && _lobbyPlayers.TryGetValue(playerId, out var player))
            {
                return player.PlayerName;
            }

            return playerId ?? "Unknown";
        }

        private void RegisterFoundationHandlers()
        {
            _requestRouter.RegisterHandler<FriendRequestEnvelopeRequest, FriendRequestEnvelopeResponse>(
                NetworkFoundationRoutes.FriendRequest,
                (request, _) =>
                {
                    request ??= new FriendRequestEnvelopeRequest();
                    string requesterId = request.PlayerId ?? _currentPlayerId;
                    string targetPlayerId = request.TargetPlayerId;

                    bool success = TryProcessFriendRequest(requesterId, targetPlayerId, out string error);
                    var response = new FriendRequestEnvelopeResponse
                    {
                        PlayerId = requesterId,
                        TargetPlayerId = targetPlayerId ?? string.Empty,
                        Success = success,
                        Error = error ?? string.Empty
                    };
                    return Task.FromResult(response);
                });
        }

        private bool TryProcessFriendRequest(string requesterId, string targetPlayerId, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(requesterId) || string.IsNullOrWhiteSpace(targetPlayerId))
            {
                error = "PlayerID and TargetPlayerID are required.";
                return false;
            }

            EnsureFriendStorage(requesterId);
            EnsureFriendStorage(targetPlayerId);

            if (requesterId == targetPlayerId)
            {
                error = "Cannot send a friend request to yourself.";
                return false;
            }

            if (_friendsByPlayer[requesterId].Contains(targetPlayerId))
            {
                error = "Already friends.";
                return false;
            }

            if (!_incomingFriendRequestsByTarget[targetPlayerId].Add(requesterId))
            {
                error = "Friend request already sent.";
                return false;
            }

            var notification = new JObject
            {
                ["MessageType"] = MessageType.FriendRequestNotification,
                ["TargetPlayerID"] = targetPlayerId,
                ["FromPlayerID"] = requesterId,
                ["FromPlayerName"] = ResolvePlayerName(requesterId)
            };
            SendJsonToClient(notification);
            PrettyLogger.Bold("LocalServer", $"Friend request: {requesterId} -> {targetPlayerId}");
            return true;
        }

        #endregion
    }


}

