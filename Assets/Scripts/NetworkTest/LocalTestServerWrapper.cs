#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using OpenGSCore;

namespace OpenGS.Network
{
    /// <summary>
    /// ローカルテストサーバー用のイベントハンドラーラッパー
    /// 本番サーバーのイベント処理をローカルでシミュレート
    /// </summary>
    public class LocalTestServerWrapper
    {
        /// <summary>
        /// 共有イベントハンドラー
        /// </summary>
        private readonly UnifiedEventHandler m_EventHandler;

        /// <summary>
        /// ルーム管理
        /// </summary>
        private readonly Dictionary<string, RoomData> m_Rooms = new();

        /// <summary>
        /// ロビープレイヤー管理
        /// </summary>
        private readonly Dictionary<string, PlayerData> m_LobbyPlayers = new();

        /// <summary>
        /// ルームデータ
        /// </summary>
        private class RoomData
        {
            public string RoomId { get; set; } = "";
            public string RoomName { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public int Capacity { get; set; } = 8;
            public string GameMode { get; set; } = "DeathMatch";
            public List<string> Players { get; set; } = new();
            public Dictionary<string, bool> PlayerReady { get; set; } = new();
        }

        /// <summary>
        /// プレイヤーデータ
        /// </summary>
        private class PlayerData
        {
            public string PlayerId { get; set; } = "";
            public string PlayerName { get; set; } = "";
            public string CurrentRoomId { get; set; } = "";
        }

        /// <summary>
        /// ログ出力をカスタマイズ
        /// </summary>
        public Action<string>? OnLog { get; set; }

        public LocalTestServerWrapper()
        {
            m_EventHandler = new UnifiedEventHandler();
            RegisterCustomHandlers();
            SeedDefaultRooms();
        }

        /// <summary>
        /// カスタムハンドラーを登録（オーバーライド用）
        /// </summary>
        private void RegisterCustomHandlers()
        {
            // ルーム作成のカスタム実装
            m_EventHandler.Register(MessageType.CreateRoomRequest, HandleCreateRoom);

            // ルーム一覧のカスタム実装
            m_EventHandler.Register(MessageType.RoomListUpdateRequest, HandleUpdateRoom);

            // ルーム参加のカスタム実装
            m_EventHandler.Register(MessageType.JoinRoomRequest, HandleEnterRoom);

            // 準備完了のカスタム実装
            m_EventHandler.Register(MessageType.PlayerReadyRequest, HandlePlayerReady);

            // ゲームスタートのカスタム実装
            m_EventHandler.Register(MessageType.GameStartRequest, HandleGameStart);
        }

        /// <summary>
        /// イベントを処理する
        /// </summary>
        public void ProcessEvent(JObject json, Action<JObject> sendResponse)
        {
            var messageType = json["MessageType"]?.ToString() ?? "";
            Log($"Process: {messageType}");
            m_EventHandler.HandleEvent(json, sendResponse);
        }

        #region カスタムハンドラー実装

        private void HandleCreateRoom(JObject json, Action<JObject> sender)
        {
            var roomName = json["RoomName"]?.ToString() ?? "New Room";
            var ownerId = json["PlayerID"]?.ToString() ?? "owner";
            var gameMode = json["GameMode"]?.ToString() ?? "DeathMatch";
            var capacity = json["Capacity"]?.ToObject<int>() ?? 8;

            var roomId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var room = new RoomData
            {
                RoomId = roomId,
                RoomName = roomName,
                OwnerId = ownerId,
                Capacity = capacity,
                GameMode = gameMode,
                Players = new List<string> { ownerId },
                PlayerReady = new Dictionary<string, bool> { { ownerId, false } }
            };

            m_Rooms[roomId] = room;

            var resp = new JObject
            {
                ["MessageType"] = MessageType.CreateRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = roomName,
                ["GameMode"] = gameMode,
                ["OwnerID"] = ownerId,
                ["Capacity"] = capacity,
                ["PlayerCount"] = 1
            };

            sender(resp);
            Log($"Room created: {roomName} (ID: {roomId})");
        }

        private void HandleUpdateRoom(JObject json, Action<JObject> sender)
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
                    ["PlayerCount"] = room.Players.Count
                });
            }

            sender(new JObject
            {
                ["MessageType"] = MessageType.RoomListUpdateNotification,
                ["Rooms"] = rooms
            });
        }

        private void HandleEnterRoom(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";
            var playerId = json["PlayerID"]?.ToString() ?? "";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";

            if (!m_Rooms.TryGetValue(roomId, out var room))
            {
                var errorResp = new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room not found"
                };
                sender(errorResp);
                return;
            }

            if (room.Players.Count >= room.Capacity)
            {
                var errorResp = new JObject
                {
                    ["MessageType"] = MessageType.JoinRoomResponse,
                    ["Success"] = false,
                    ["ErrorMessage"] = "Room is full"
                };
                sender(errorResp);
                return;
            }

            room.Players.Add(playerId);
            room.PlayerReady[playerId] = false;

            var resp = new JObject
            {
                ["MessageType"] = MessageType.JoinRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = room.RoomName,
                ["PlayerID"] = playerId,
                ["Capacity"] = room.Capacity,
                ["GameMode"] = room.GameMode,
                ["OwnerID"] = room.OwnerId,
                ["Players"] = JArray.FromObject(room.Players)
            };

            sender(resp);
            Log($"Player {playerName} entered room {room.RoomName}");
        }

        private void HandlePlayerReady(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";
            var playerId = json["PlayerID"]?.ToString() ?? "";

            if (m_Rooms.TryGetValue(roomId, out var room))
            {
                room.PlayerReady[playerId] = true;

                var resp = new JObject
                {
                    ["MessageType"] = MessageType.PlayerReadyNotification,
                    ["PlayerID"] = playerId,
                    ["RoomID"] = roomId
                };
                sender(resp);

                // 全員が準備完了かチェック
                CheckAllReady(room);
            }
        }

        private void HandleGameStart(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";

            if (m_Rooms.TryGetValue(roomId, out var room))
            {
                var countdown = new JObject
                {
                    ["MessageType"] = MessageType.WaitRoomStartCountdown,
                    ["Countdown"] = 5,
                    ["RoomID"] = roomId
                };
                sender(countdown);
            }
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
                Players = new List<string> { "host-002" },
                PlayerReady = new Dictionary<string, bool> { { "host-002", false } }
            };
        }

        private void CheckAllReady(RoomData room)
        {
            bool allReady = true;
            foreach (var playerId in room.Players)
            {
                if (!room.PlayerReady.GetValueOrDefault(playerId, false))
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady && room.Players.Count > 0)
            {
                Log($"All players ready in room {room.RoomName}, starting countdown...");
            }
        }

        #endregion

        /// <summary>
        /// ログ出力
        /// </summary>
        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Debug.Log($"[LocalTestServerWrapper] {message}");
        }

        /// <summary>
        /// ルーム情報を取得（デバッグ用）
        /// </summary>
        public string GetDebugInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[LocalTestServerWrapper] Rooms: {m_Rooms.Count}, Players: {m_LobbyPlayers.Count}");
            foreach (var room in m_Rooms.Values)
            {
                sb.AppendLine($"  Room: {room.RoomName} ({room.RoomId}) - {room.Players.Count}/{room.Capacity}");
            }
            return sb.ToString();
        }
    }
}

