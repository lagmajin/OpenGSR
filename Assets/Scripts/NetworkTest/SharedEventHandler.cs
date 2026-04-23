using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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
        /// <summary>
        /// メッセージタイプごとの処理
        /// </summary>
        private readonly Dictionary<string, Action<JObject, Action<JObject>>> m_EventHandlers = new();

        /// <summary>
        /// 応答送信コールバック
        /// </summary>
        private Action<JObject>? m_Sender;

        public UnifiedEventHandler()
        {
            RegisterDefaultHandlers();
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
            Register(RUDPMessageTypes.LobbyEnter, HandleLobbyEnter);
            Register(RUDPMessageTypes.LobbyLeave, HandleLobbyLeave);
            Register(RUDPMessageTypes.LobbyChat, HandleLobbyChat);

            // ルーム
            Register(MessageType.CreateRoomRequest, HandleCreateRoom);
            Register(MessageType.RoomListUpdateRequest, HandleUpdateRoom);
            Register(MessageType.JoinRoomRequest, HandleEnterRoom);
            Register(MessageType.LeaveRoomRequest, HandleLeaveRoom);
            Register("RoomChat", HandleRoomChat);
            Register(MessageType.PlayerReadyRequest, HandlePlayerReady);
            Register(MessageType.PlayerUnready, HandlePlayerUnready);
            Register("KickPlayer", HandleKickPlayer);
            Register(MessageType.GameStartRequest, HandleGameStart);
            Register("CancelCountdown", HandleCancelCountdown);

            // マッチ
            Register("LoadingFinished", HandleLoadingFinished);
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
                ["MessageType"] = RUDPMessageTypes.LobbyEnter,
                ["Success"] = true,
                ["PlayerName"] = playerName
            };
            sender(resp);
        }

        private void HandleLobbyLeave(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.LobbyLeave,
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
            var roomId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var resp = new JObject
            {
                ["MessageType"] = MessageType.CreateRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["RoomName"] = roomName
            };
            sender(resp);
        }

        private void HandleUpdateRoom(JObject json, Action<JObject> sender)
        {
            var resp = new JObject
            {
                ["MessageType"] = MessageType.RoomListUpdateNotification,
                ["Rooms"] = new JArray()
            };
            sender(resp);
        }

        private void HandleEnterRoom(JObject json, Action<JObject> sender)
        {
            var roomId = json["RoomID"]?.ToString() ?? "";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";

            var resp = new JObject
            {
                ["MessageType"] = MessageType.JoinRoomResponse,
                ["Success"] = true,
                ["RoomID"] = roomId,
                ["PlayerName"] = playerName
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
                ["MessageType"] = RUDPMessageTypes.WaitRoomStartCountdown,
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
                ["MessageType"] = RUDPMessageTypes.WaitRoomCancelCountdown,
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
                ["MessageType"] = "LoadingFinished",
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
