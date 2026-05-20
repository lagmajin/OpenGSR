using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpenGSCore;

namespace OpenGS.Network
{
    /// <summary>
    /// 統一されたイベントハンドラーインターフェース
    /// 本番サーバーとローカルテストサーバーで共有できる
    /// </summary>
    public interface ISharedEventHandler
    {
        /// <summary>
        /// イベントを処理する
        /// </summary>
        /// <param name="json">受信したJSON</param>
        /// <param name="sender">応答送信コールバック</param>
        void HandleEvent(JObject json, Action<JObject> sender);
    }

    /// <summary>
    /// 統一イベントハンドラーのデフォルト実装
    /// OpenGSServerのイベントハンドラーをラップして使用
    /// </summary>
    public class UnifiedEventHandler : ISharedEventHandler
    {
        private sealed class RoomData
        {
            public string RoomId { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "DeathMatch";
            public bool TeamBalance { get; set; } = true;
            public List<string> Players { get; set; } = new();
            public Dictionary<string, bool> PlayerReady { get; set; } = new();
        }

        /// <summary>
        /// メッセージタイプごとの処理
        /// </summary>
        private readonly Dictionary<string, Action<JObject, Action<JObject>>> m_EventHandlers = new();
        private readonly Dictionary<string, RoomData> m_Rooms = new();

        /// <summary>
        /// 応答送信コールバック
        /// </summary>
        private Action<JObject>? m_Sender;

        public UnifiedEventHandler()
        {
            RegisterDefaultHandlers();
            SeedDefaultRooms();
        }

        /// <summary>
        /// デフォルトのハンドラーを登録
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            // ログイン関連
            Register(MessageType.LoginRequest, HandleLoginRequest);
            Register(MessageType.LogoutRequest, HandleLogoutRequest);

            // プレイヤー情報
            Register(MessageType.PlayerInfo, HandlePlayerInfo);

            // ロビー
            Register(MessageType.LobbyEnter, HandleLobbyEnter);
            Register(MessageType.LobbyLeave, HandleLobbyLeave);
            Register(MessageType.LobbyChat, HandleLobbyChat);

            // ルーム
            Register(MessageType.CreateRoomRequest, HandleCreateRoom);
            Register(MessageType.RoomListUpdateRequest, HandleUpdateRoom);
            Register(MessageType.JoinRoomRequest, HandleEnterRoom);
            Register(MessageType.LeaveRoomRequest, HandleLeaveRoom);
            Register(MessageType.WaitRoomChat, HandleRoomChat);
            Register(MessageType.PlayerReadyRequest, HandlePlayerReady);
            Register(MessageType.PlayerUnready, HandlePlayerUnready);
            Register(MessageType.WaitRoomKickPlayer, HandleKickPlayer);
            Register(MessageType.GameStartRequest, HandleGameStart);
            Register(MessageType.WaitRoomCancelCountdown, HandleCancelCountdown);

            // マッチ
            Register(MessageType.LoadingCompletedNotification, HandleLoadingFinished);
            Register(RUDPMessageTypes.PlayerShot, HandlePlayerShot);
            Register("PlayerKilled", HandlePlayerKilled);
            Register("PlayerDamaged", HandlePlayerDamaged);
            Register("GrenadeThrown", HandleGrenadeThrown);
            Register(RUDPMessageTypes.PlayerPositionUpdate, HandlePlayerPositionUpdate);
            Register(RUDPMessageTypes.PlayerRespawn, HandlePlayerRespawn);

            // 装備
            Register("EquipRequest", HandleEquipRequest);
        }

        /// <summary>
        /// ハンドラーを登録
        /// </summary>
        public void Register(string messageType, Action<JObject, Action<JObject>> handler)
        {
            m_EventHandlers[messageType] = handler;
        }

        /// <summary>
        /// イベントを処理する
        /// </summary>
        public void HandleEvent(JObject json, Action<JObject> sender)
        {
            m_Sender = sender;
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());

            if (string.IsNullOrEmpty(messageType))
            {
                return;
            }

            if (m_EventHandlers.TryGetValue(messageType, out var handler))
            {
                handler(json, sender);
            }
            else
            {
                // 未知のメッセージタイプ
                Console.WriteLine($"[UnifiedEventHandler] Unknown message type: {messageType}");
            }
        }

        #region ハンドラー実装

        private void HandleLoginRequest(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.LoginResponse,
                ["UserID"] = json["PlayerID"] ?? json["PlayerLocalId"] ?? "test_user"
            };
            sender(resp);
        }

        private void HandleLogoutRequest(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.LogoutSuccessful
            };
            sender(resp);
        }

        private void HandlePlayerInfo(JObject json, Action<JObject> sender)
        {
            // プレイヤー情報受信の確認
            Console.WriteLine($"[UnifiedEventHandler] PlayerInfo received: {json["PlayerID"] ?? json["PlayerLocalId"]}");
        }

        private void HandleLobbyEnter(JObject json, Action<JObject> sender)
        {
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";
            var resp = new JObject
            {
                ["MessageType"] = MessageType.LobbyEnter,
                ["Success"] = true,
                ["PlayerName"] = playerName
            };
            sender(resp);
        }

        private void HandleLobbyLeave(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.LobbyLeave,
                ["Success"] = true
            };
            sender(resp);
        }

        private void HandleLobbyChat(JObject json, Action<JObject> sender)
        {
            var message = json["Message"]?.ToString() ?? "";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";

            var broadcast = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ChatBroadcast,
                ["Message"] = message,
                ["PlayerName"] = playerName
            };
            sender(broadcast);
        }

        private void HandleCreateRoom(JObject json, Action<JObject> sender)
        {
            var roomName = json["RoomName"]?.ToString() ?? "New Room";
            var capacity = json["Capacity"]?.ToObject<int>() ?? 8;
            var teamBalance = json["TeamBalance"]?.ToObject<bool?>() ?? true;
            var gameMode = json["GameMode"]?.ToString() ?? "DeathMatch";
            var ownerId = json["PlayerID"]?.ToString() ?? "owner";
            var roomId = Guid.NewGuid().ToString("N").Substring(0, 8);

            m_Rooms[roomId] = new RoomData
            {
                RoomId = roomId,
                RoomName = roomName,
                OwnerId = ownerId,
                Capacity = capacity,
                GameMode = gameMode,
                TeamBalance = teamBalance,
                Players = new List<string> { ownerId },
                PlayerReady = new Dictionary<string, bool> { { ownerId, false } }
            };

            var resp = new JObject
            {
                ["MessageType"] = MessageType.CreateRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = roomName,
                ["Capacity"] = capacity,
                ["TeamBalance"] = teamBalance,
                ["GameMode"] = gameMode,
                ["OwnerID"] = ownerId,
                ["PlayerCount"] = 1
            };
            sender(resp);
        }

        private void HandleUpdateRoom(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.RoomListUpdateNotification,
                ["Rooms"] = BuildRoomListSnapshot()
            };
            sender(resp);
        }

        private void HandleEnterRoom(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";

            if (!m_Rooms.TryGetValue(roomId, out var room))
            {
                sender(new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room not found",
                    ["RoomID"] = roomId
                });
                return;
            }

            if (room.Players.Count >= room.Capacity)
            {
                sender(new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room is full",
                    ["RoomID"] = roomId
                });
                return;
            }

            var playerId = json["PlayerID"]?.ToString() ?? Guid.NewGuid().ToString("N");
            if (!room.Players.Contains(playerId))
            {
                room.Players.Add(playerId);
            }
            room.PlayerReady[playerId] = false;

            var resp = new JObject
            {
                ["MessageType"] = MessageType.JoinRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["PlayerName"] = playerName,
                ["Capacity"] = room.Capacity,
                ["GameMode"] = room.GameMode,
                ["OwnerID"] = room.OwnerId,
                ["PlayerCount"] = room.Players.Count
            };
            sender(resp);
        }

        private void HandleLeaveRoom(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.LeaveRoomResponse,
                ["Success"] = true
            };
            sender(resp);
        }

        private void SeedDefaultRooms()
        {
            if (m_Rooms.Count > 0)
            {
                return;
            }

            m_Rooms["room-0001"] = new RoomData
            {
                RoomId = "room-0001",
                RoomName = "Default DM Room",
                OwnerId = "host-001",
                Capacity = 8,
                GameMode = "DeathMatch",
                TeamBalance = true,
                Players = new List<string> { "host-001" },
                PlayerReady = new Dictionary<string, bool> { { "host-001", false } }
            };

            m_Rooms["room-0002"] = new RoomData
            {
                RoomId = "room-0002",
                RoomName = "Default TDM Room",
                OwnerId = "host-002",
                Capacity = 12,
                GameMode = "TeamDeathMatch",
                TeamBalance = true,
                Players = new List<string> { "host-002" },
                PlayerReady = new Dictionary<string, bool> { { "host-002", false } }
            };
        }

        private JArray BuildRoomListSnapshot()
        {
            var rooms = new JArray();
            foreach (var room in m_Rooms.Values)
            {
                rooms.Add(new JObject
                {
                    ["RoomID"] = room.RoomId,
                    ["RoomName"] = room.RoomName,
                    ["OwnerID"] = room.OwnerId,
                    ["Capacity"] = room.Capacity,
                    ["GameMode"] = room.GameMode,
                    ["TeamBalance"] = room.TeamBalance,
                    ["PlayerCount"] = room.Players.Count
                });
            }

            return rooms;
        }

        private void HandleRoomChat(JObject json, Action<JObject> sender)
        {
            var message = json["Message"]?.ToString() ?? "";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";

            var broadcast = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ChatBroadcast,
                ["Message"] = message,
                ["PlayerName"] = playerName
            };
            sender(broadcast);
        }

        private void HandlePlayerReady(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";
            var roomId = json["RoomID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = MessageType.PlayerReadyNotification,
                ["PlayerID"] = playerId,
                ["RoomID"] = roomId
            };
            sender(broadcast);
        }

        private void HandlePlayerUnready(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = MessageType.PlayerUnready,
                ["PlayerID"] = playerId
            };
            sender(broadcast);
        }

        private void HandleKickPlayer(JObject json, Action<JObject> sender)
        {
            var kickedPlayerId = json["KickedPlayerID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = "PlayerKicked",
                ["KickedPlayerID"] = kickedPlayerId
            };
            sender(broadcast);
        }

        private void HandleGameStart(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";

            var countdown = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomStartCountdown,
                ["Countdown"] = 5,
                ["RoomID"] = roomId
            };
            sender(countdown);
        }

        private void HandleCancelCountdown(JObject json, Action<JObject> sender)
        {
            var reason = json["Reason"]?.ToString() ?? "cancelled";

            var cancel = new JObject
            {
                ["MessageType"] = MessageType.WaitRoomCancelCountdown,
                ["Reason"] = reason
            };
            sender(cancel);
        }

        private void HandleLoadingFinished(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";
            var roomId = json["RoomID"]?.ToString() ?? "";

            // ローディング完了をブロードキャスト
            var broadcast = new JObject
            {
                ["MessageType"] = MessageType.LoadingCompletedNotification,
                ["Success"] = true,
                ["PlayerID"] = playerId,
                ["RoomID"] = roomId
            };
            sender(broadcast);
        }

        private void HandlePlayerShot(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";

            // 射撃イベントをブロードキャスト
            var broadcast = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerShot,
                ["PlayerID"] = playerId,
                ["Timestamp"] = DateTime.Now.Ticks
            };
            sender(broadcast);
        }

        private void HandlePlayerKilled(JObject json, Action<JObject> sender)
        {
            var killerId = json["KillerID"]?.ToString() ?? "";
            var victimId = json["VictimID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = "PlayerKilled",
                ["KillerID"] = killerId,
                ["VictimID"] = victimId
            };
            sender(broadcast);
        }

        private void HandlePlayerDamaged(JObject json, Action<JObject> sender)
        {
            var attackerId = json["AttackerID"]?.ToString() ?? "";
            var targetId = json["TargetID"]?.ToString() ?? "";
            var damage = json["Damage"]?.ToObject<int>() ?? 0;

            var broadcast = new JObject
            {
                ["MessageType"] = "PlayerDamaged",
                ["AttackerID"] = attackerId,
                ["TargetID"] = targetId,
                ["Damage"] = damage
            };
            sender(broadcast);
        }

        private void HandleGrenadeThrown(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = "GrenadeThrown",
                ["PlayerID"] = playerId
            };
            sender(broadcast);
        }

        private void HandlePlayerPositionUpdate(JObject json, Action<JObject> sender)
        {
            // 位置更新はACKを返すのみ（ブロードキャストはサーバー側で行う）
            var playerId = json["PlayerID"]?.ToString() ?? "";
            var seq = json["SequenceNumber"]?.ToObject<byte>() ?? 0;

            var ack = new JObject
            {
                ["MessageType"] = "PositionUpdateAck",
                ["PlayerID"] = playerId,
                ["SequenceNumber"] = seq
            };
            sender(ack);
        }

        private void HandlePlayerRespawn(JObject json, Action<JObject> sender)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "";
            var roomId = json["RoomID"]?.ToString() ?? "";

            var broadcast = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerRespawn,
                ["PlayerID"] = playerId,
                ["RoomID"] = roomId
            };
            sender(broadcast);
        }

        private void HandleEquipRequest(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = "PlayerEquipInfo",
                ["PlayerCharacter"] = "Ami",
                ["InstantItemSlot"] = new JArray()
            };
            sender(resp);
        }

        #endregion
    }
}
