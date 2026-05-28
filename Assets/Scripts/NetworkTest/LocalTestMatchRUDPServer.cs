using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public class LocalTestMatchRUDPServer
    {
        private static LocalTestMatchRUDPServer _instance;
        //public static LocalTestMatchRUDPServer Instance => _instance ??= new LocalTestMatchRUDPServer();

        private NetManager server;
        private EventBasedNetListener listener;
        private NetPeer _clientPeer;
        private volatile bool running;
        private bool _loopbackMode = true;
        private bool _matchEnded;
        private int _totalDeathEvents;
        private readonly Dictionary<string, int> _teamKills = new();
        private readonly ConcurrentDictionary<string, LocalPlayerState> _players = new(StringComparer.OrdinalIgnoreCase);

        public event Action<JObject> MessageProduced;

        private sealed class LocalPlayerState
        {
            public string PlayerId { get; set; } = string.Empty;
            public string PlayerName { get; set; } = string.Empty;
            public string Team { get; set; } = "Blue";
            public bool IsAlive { get; set; } = true;
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float Rotation { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Score { get; set; }
            public string LastWeaponType { get; set; } = "Unknown";
            public string LastStateReason { get; set; } = "Spawn";
        }

        // 送信テスト用プレイヤー状態
        private float testPlayerX = 0f;
        private float testPlayerY = 0f;
        private float testPlayerRotation = 0f;
        private System.Random random = new System.Random();

        public LocalTestMatchRUDPServer()
        { 
            
        }

        public bool IsRunning()
        {

            return server!=null&&server.IsRunning;
        }

        private void SendJson(in JObject json)
        {
            MessageProduced?.Invoke(json);

            if (_loopbackMode)
            {
                return;
            }

            string jsonStr = json.ToString();

            // LiteNetLib用のデータライター
            NetDataWriter writer = new NetDataWriter();

            writer.Put(jsonStr);

            _clientPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void StartServer(int port)
        {
            ResetMatchState();
            ResetDummyPlayerState();
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(port);
            running = true;

            listener.ConnectionRequestEvent += OnConnectionRequest;
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;

            PrettyLogger.Bold("Network", "LocalServer port:"+port.ToString());

            Task.Run(() => PollLoop());
            Task.Run(() => TestDataBroadcastLoop()); // 定期送信用のバックグラウンドループ
        }

        public void StopLoopback()
        {
            _loopbackMode = false;
        }

        public void StartLoopback()
        {
            _loopbackMode = true;
        }

        private void ResetMatchState()
        {
            _matchEnded = false;
            _totalDeathEvents = 0;
            _teamKills.Clear();
            _teamKills["Red"] = 0;
            _teamKills["Blue"] = 0;
            _players.Clear();
        }

        private void ResetDummyPlayerState()
        {
            testPlayerX = 0f;
            testPlayerY = 0f;
            testPlayerRotation = 0f;
        }


        private void OnConnectionRequest(ConnectionRequest request)
        {
            PrettyLogger.Bold("Network", "OnConnectionRequest");

            if (server.ConnectedPeersCount < 10)
                request.Accept();
            else
                request.Reject();
        }

        // ピア接続時に呼ばれるメンバ関数
        private void OnPeerConnected(NetPeer peer)
        {
            // ピアが接続された時の処理
            PrettyLogger.Bold("Network", "ピアが接続されました");

            _clientPeer = peer;

            var json = new JObject();
            json["MessageType"] = MessageType.WelcomeMessage;

            json["Message"] = "This is test RUDP local server";

            SendJson(json);

        }

        // ネットワーク受信イベントを処理するメンバ関数
        private void OnNetworkReceive(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            string rawData = reader.GetString();

            // 2. ASCIIコード31（0x1F）で区切る
            string[] messages = rawData.Split(new[] { (char)31 }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var msg in messages)
            {
                try
                {
                    var json = JObject.Parse(msg);
                    ProcessIncomingMessage(json);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON解析エラー: {ex.Message}");
                }
            }
        }

        public void ProcessIncomingMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            PrettyLogger.Bold("RUDP Server", $"Received: {messageType}");

            switch (messageType)
            {
                case "PlayerInput":
                    HandlePlayerInput(json);
                    break;
                case "ShootRequest":
                    HandleShootRequest(json);
                    break;
                case RUDPMessageTypes.PlayerShot:
                    HandlePlayerShot(json);
                    break;
                case RUDPMessageTypes.PlayerDeath:
                    HandlePlayerDeath(json);
                    break;
                case RUDPMessageTypes.TeamKill:
                    HandleTeamKill(json);
                    break;
                case "ItemUseRequest":
                case RUDPMessageTypes.ItemUse:
                    HandleItemUseRequest(json);
                    break;
                case "ChatMessage":
                    HandleChatMessage(json);
                    break;
                case RUDPMessageTypes.PlayerKill:
                    HandlePlayerKill(json);
                    break;
                case RUDPMessageTypes.KillScoreUpdate:
                    HandleKillScoreUpdate(json);
                    break;
                case RUDPMessageTypes.PlayerRespawn:
                    HandlePlayerRespawn(json);
                    break;
                default:
                    PrettyLogger.Bold("RUDP Server", $"Unknown message: {messageType}");
                    break;
            }
        }

        private void HandlePlayerInput(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? json["PlayerID"]?.ToString() ?? "unknown";
            var state = EnsurePlayerState(playerId, json["PlayerName"]?.ToString());
            state.PosX = json["PosX"]?.ToObject<float>() ?? state.PosX;
            state.PosY = json["PosY"]?.ToObject<float>() ?? state.PosY;
            state.Rotation = json["Rotation"]?.ToObject<float>() ?? state.Rotation;
            state.LastStateReason = "Input";
            PrettyLogger.Bold("RUDP Server", $"PlayerInput received: {playerId} -> ({state.PosX}, {state.PosY}) rot={state.Rotation}");

            SendJson(RUDPMessageBuilder.CreatePlayerPositionUpdate(playerId, new Vector2(state.PosX, state.PosY), state.Rotation));
        }

        private void HandleShootRequest(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var weaponType = json["WeaponType"]?.ToString() ?? "Pistol";
            var state = EnsurePlayerState(playerId, json["PlayerName"]?.ToString());
            state.LastWeaponType = weaponType;
            PrettyLogger.Bold("RUDP Server", $"ShootRequest from {playerId}");

            var shotMsg = RUDPMessageBuilder.CreatePlayerShot(
                playerId,
                new Vector2(state.PosX, state.PosY),
                ResolveDirection(json, state),
                weaponType);
            SendJson(shotMsg);
        }

        /// <summary>
        /// 射撃メッセージを処理（クライアント→サーバー）
        /// </summary>
        private void HandlePlayerShot(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var posX = json["PosX"]?.ToObject<float>() ?? 0f;
            var posY = json["PosY"]?.ToObject<float>() ?? 0f;
            var dirX = json["DirX"]?.ToObject<float>() ?? 0f;
            var dirY = json["DirY"]?.ToObject<float>() ?? 0f;
            var weaponType = json["WeaponType"]?.ToString() ?? "Unknown";
            var state = EnsurePlayerState(playerId, json["PlayerName"]?.ToString());
            state.PosX = posX;
            state.PosY = posY;
            state.LastWeaponType = weaponType;
            state.LastStateReason = "Shot";

            PrettyLogger.Bold("RUDP Server", $"PlayerShot from {playerId}: {weaponType} at ({posX}, {posY}) dir({dirX}, {dirY})");

            var broadcastMsg = RUDPMessageBuilder.CreatePlayerShot(playerId, new Vector2(posX, posY), new Vector2(dirX, dirY), weaponType);
            SendJson(broadcastMsg);
        }

        /// <summary>
        /// 死亡メッセージを処理（クライアント→サーバー）
        /// </summary>
        private void HandlePlayerDeath(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var killerId = json["KillerId"]?.ToString() ?? "";
            var victim = EnsurePlayerState(playerId, json["PlayerName"]?.ToString());
            victim.IsAlive = false;
            victim.Deaths++;
            victim.Score = Math.Max(0, victim.Score - 50);
            victim.LastStateReason = "Death";

            LocalPlayerState? killer = null;
            if (!string.IsNullOrWhiteSpace(killerId))
            {
                killer = EnsurePlayerState(killerId, json["KillerName"]?.ToString());
                killer.Kills++;
                killer.Score += 100;
                killer.LastStateReason = "Kill";
            }

            PrettyLogger.Bold("RUDP Server", $"PlayerDeath: {playerId} killed by {killerId}");

            var deathMsg = RUDPMessageBuilder.CreatePlayerDeath(playerId, killerId);
            SendJson(deathMsg);

            if (killer != null)
            {
                var killerScoreMsg = RUDPMessageBuilder.CreateKillScoreUpdate(
                    killer.PlayerId,
                    killer.Kills,
                    killer.Deaths,
                    killer.Score,
                    killer.Team);
                SendJson(killerScoreMsg);
            }

            var deathScoreMsg = RUDPMessageBuilder.CreateKillScoreUpdate(
                victim.PlayerId,
                victim.Kills,
                victim.Deaths,
                victim.Score,
                victim.Team);
            SendJson(deathScoreMsg);

            _totalDeathEvents++;
            TryBroadcastMatchEnd();
        }

        private void HandleTeamKill(JObject json)
        {
            var killerTeam = json["KillerTeam"]?.ToString() ?? "Red";
            var victimTeam = json["VictimTeam"]?.ToString() ?? "Blue";

            if (!_teamKills.ContainsKey(killerTeam))
            {
                _teamKills[killerTeam] = 0;
            }

            _teamKills[killerTeam]++;
            if (killerTeam.Equals("Red", StringComparison.OrdinalIgnoreCase) || killerTeam.Equals("Blue", StringComparison.OrdinalIgnoreCase))
            {
                var teamPlayers = _players.Values.Where(player => string.Equals(player.Team, killerTeam, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var player in teamPlayers)
                {
                    player.Kills++;
                    player.Score += 100;
                }
            }

            var killScoreMsg = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.KillScoreUpdate,
                ["PlayerId"] = killerTeam,
                ["Kills"] = _teamKills[killerTeam],
                ["Deaths"] = 0,
                ["Score"] = _teamKills[killerTeam] * 100,
                ["Team"] = killerTeam
            };
            SendJson(killScoreMsg);

            PrettyLogger.Bold("RUDP Server", $"TeamKill: {killerTeam} -> {victimTeam} (score={_teamKills[killerTeam]})");
            TryBroadcastMatchEnd();
        }

        private void HandlePlayerKill(JObject json)
        {
            var killerId = json["KillerId"]?.ToString() ?? "unknown";
            var victimId = json["VictimId"]?.ToString() ?? "unknown";
            var weaponType = json["WeaponType"]?.ToString() ?? "Unknown";
            var headshot = json["Headshot"]?.ToObject<bool>() ?? false;

            PrettyLogger.Bold("RUDP Server", $"PlayerKill: {killerId} -> {victimId} ({weaponType}, headshot={headshot})");

            var killMsg = new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerKill,
                ["KillerId"] = killerId,
                ["VictimId"] = victimId,
                ["WeaponType"] = weaponType,
                ["Headshot"] = headshot
            };
            SendJson(killMsg);
        }

        private void HandleKillScoreUpdate(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var kills = json["Kills"]?.ToObject<int>() ?? 0;
            var deaths = json["Deaths"]?.ToObject<int>() ?? 0;
            var score = json["Score"]?.ToObject<int>() ?? 0;
            var team = json["Team"]?.ToString() ?? "Unknown";

            PrettyLogger.Bold("RUDP Server", $"KillScoreUpdate: {playerId} K={kills} D={deaths} S={score} Team={team}");

            SendJson(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.KillScoreUpdate,
                ["PlayerId"] = playerId,
                ["Kills"] = kills,
                ["Deaths"] = deaths,
                ["Score"] = score,
                ["Team"] = team
            });
        }

        private void HandlePlayerRespawn(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var posX = json["PosX"]?.ToObject<float>() ?? 0f;
            var posY = json["PosY"]?.ToObject<float>() ?? 0f;
            var state = EnsurePlayerState(playerId, json["PlayerName"]?.ToString());
            state.PosX = posX;
            state.PosY = posY;
            state.IsAlive = true;
            state.LastStateReason = "Respawn";

            PrettyLogger.Bold("RUDP Server", $"PlayerRespawn: {playerId} at ({posX}, {posY})");

            SendJson(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerRespawn,
                ["PlayerId"] = playerId,
                ["PosX"] = posX,
                ["PosY"] = posY
            });
        }

        private LocalPlayerState EnsurePlayerState(string playerId, string? playerName = null)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = "unknown";
            }

            return _players.GetOrAdd(playerId, id => new LocalPlayerState
            {
                PlayerId = id,
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? id : playerName,
                Team = InferTeam(id)
            });
        }

        private static string InferTeam(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "Blue";
            }

            var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(playerId);
            return (hash & 1) == 0 ? "Red" : "Blue";
        }

        private static Vector2 ResolveDirection(JObject json, LocalPlayerState state)
        {
            var dirX = json["DirX"]?.ToObject<float>() ?? 0f;
            var dirY = json["DirY"]?.ToObject<float>() ?? 0f;
            if (Math.Abs(dirX) < 0.0001f && Math.Abs(dirY) < 0.0001f)
            {
                var rad = state.Rotation * Mathf.Deg2Rad;
                dirX = Mathf.Cos(rad);
                dirY = Mathf.Sin(rad);
            }

            return new Vector2(dirX, dirY);
        }

        private JObject BuildScoreSnapshot()
        {
            var players = new JArray();
            foreach (var player in _players.Values.OrderByDescending(p => p.Score).ThenBy(p => p.PlayerId))
            {
                players.Add(new JObject
                {
                    ["PlayerId"] = player.PlayerId,
                    ["PlayerName"] = player.PlayerName,
                    ["Team"] = player.Team,
                    ["IsAlive"] = player.IsAlive,
                    ["PosX"] = player.PosX,
                    ["PosY"] = player.PosY,
                    ["Rotation"] = player.Rotation,
                    ["Kills"] = player.Kills,
                    ["Deaths"] = player.Deaths,
                    ["Score"] = player.Score,
                    ["LastStateReason"] = player.LastStateReason
                });
            }

            return new JObject
            {
                ["Red"] = _teamKills.TryGetValue("Red", out var red) ? red : 0,
                ["Blue"] = _teamKills.TryGetValue("Blue", out var blue) ? blue : 0,
                ["Players"] = players
            };
        }

        private void TryBroadcastMatchEnd()
        {
            if (_matchEnded)
            {
                return;
            }

            var redKills = _teamKills.TryGetValue("Red", out var red) ? red : 0;
            var blueKills = _teamKills.TryGetValue("Blue", out var blue) ? blue : 0;

            if (redKills < 3 && blueKills < 3 && _totalDeathEvents < 3)
            {
                return;
            }

            _matchEnded = true;

            var winningTeam = redKills == blueKills
                ? "Draw"
                : (redKills > blueKills ? "Red" : "Blue");

            var result = new JObject
            {
                ["MessageType"] = MessageType.MatchEndNotification,
                ["WinningTeam"] = winningTeam,
                ["MyTeam"] = "Blue",
                ["RedTeamKills"] = redKills,
                ["BlueTeamKills"] = blueKills,
                ["TotalDeaths"] = _totalDeathEvents,
                ["Players"] = new JArray()
            };

            SendJson(result);
            PrettyLogger.Bold("RUDP Server", $"Match ended. winner={winningTeam}, red={redKills}, blue={blueKills}, deaths={_totalDeathEvents}");
        }

        private void HandleItemUseRequest(JObject json)
        {
            PrettyLogger.Bold("RUDP Server", $"ItemUseRequest received: {json}");

            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var itemId = json["ItemId"]?.ToString() ?? "";
            var itemType = json["ItemType"]?.ToString() ?? "";
            var effect = json["Effect"]?.ToString() ?? "";

            var itemUseMsg = RUDPMessageBuilder.CreateItemUse(playerId, itemId, itemType, effect);
            SendJson(itemUseMsg);

            if (!Enum.TryParse<EInstantItemType>(itemType, true, out var parsedType))
            {
                PrettyLogger.Bold("RUDP Server", $"ItemUseRequest parsed as unknown item type: {itemType}");
                return;
            }

            foreach (var response in BuildItemUseResponses(playerId, parsedType))
            {
                SendJson(response);
            }
        }

        private IEnumerable<JObject> BuildItemUseResponses(string playerId, EInstantItemType itemType)
        {
            switch (itemType)
            {
                case EInstantItemType.HealthKit:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, "HpRecovery", 0, 100f);
                    yield break;

                case EInstantItemType.FireBullet:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, "BulletEnhance", 30, 30f);
                    yield break;

                case EInstantItemType.PoisonBullet:
                    yield return RUDPMessageBuilder.CreatePlayerDebuff(playerId, "PoisonBullet", 30, 30f);
                    yield break;

                case EInstantItemType.PowerGrenadePack:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, EInstantItemType.PowerGrenadePack.ToString(), 0, 1f);
                    yield break;

                case EInstantItemType.ClusterGrenadePack:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, EInstantItemType.ClusterGrenadePack.ToString(), 0, 1f);
                    yield break;

                case EInstantItemType.MagnetGrenadePack:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, EInstantItemType.MagnetGrenadePack.ToString(), 0, 1f);
                    yield break;

                case EInstantItemType.MineGrenadePack:
                    yield return RUDPMessageBuilder.CreatePlayerBuff(playerId, EInstantItemType.MineGrenadePack.ToString(), 0, 1f);
                    yield break;

                default:
                    yield break;
            }
        }

        /// <summary>
        /// チャットメッセージを処理
        /// </summary>
        private void HandleChatMessage(JObject json)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            var playerName = json["PlayerName"]?.ToString() ?? "Unknown";
            var message = json["Message"]?.ToString() ?? "";
            var teamOnly = json["TeamOnly"]?.ToObject<bool>() ?? false;

            PrettyLogger.Bold("RUDP Server", $"Chat from {playerName}({playerId}): {message} [TeamOnly:{teamOnly}]");

            // エコーバックとして同じメッセージを返す
            var echoMsg = RUDPMessageBuilder.CreateChatMessage(playerId, playerName, message, teamOnly);
            SendJson(echoMsg);

            // システムブロードキャストも送信
            var broadcastMsg = RUDPMessageBuilder.CreateChatBroadcast($"Player {playerName} sent: {message}", "notice");
            SendJson(broadcastMsg);
        }

        private void PollLoop()
        {
            while (running)
            {
                try
                {
                    server.PollEvents();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ポーリング中のエラー: {ex.Message}");
                }
                Thread.Sleep(15);
            }
        }

        /// <summary>
        /// 定期的にサンプルデータを送信
        /// </summary>
        private void TestDataBroadcastLoop()
        {
            int frameCount = 0;
            while (running)
            {
                try
                {
                    if (_clientPeer != null && _clientPeer.ConnectionState == ConnectionState.Connected)
                    {
                        frameCount++;

                        // 60フレームごと（約1秒）にプレイヤー位置を送信
                        if (frameCount % 60 == 0)
                        {
                            AdvanceDummyPlayerState();
                            SendJson(BuildDummyPlayerPositionUpdate());
                        }

                        // 120フレームごと（約2秒）にゲーム状態を送信
                        if (frameCount % 120 == 0)
                        {
                            SendJson(BuildDummyGameStateSync(frameCount));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TestDataBroadcast中のエラー: {ex.Message}");
                }
                Thread.Sleep(16); // 約60FPS
            }
        }

        public void StopServer()
        {
            PrettyLogger.Bold("Network", "サーバー終了");

            running = false;
            server?.Stop();
        }

        private void AdvanceDummyPlayerState()
        {
            // 送信テスト用に少し動かす
            testPlayerX += (float)(random.NextDouble() - 0.5) * 2f;
            testPlayerY += (float)(random.NextDouble() - 0.5) * 2f;
            testPlayerRotation += (float)(random.NextDouble() - 0.5) * 45f;
        }

        private JObject BuildDummyPlayerPositionUpdate()
        {
            return RUDPMessageBuilder.CreatePlayerPositionUpdate(
                "TestPlayer",
                new Vector2(testPlayerX, testPlayerY),
                testPlayerRotation
            );
        }

        private JObject BuildDummyGameStateSync(int frameCount)
        {
            return RUDPMessageBuilder.CreateGameStateSync(Math.Max(0, 300 - frameCount / 60), BuildScoreSnapshot());
        }


    }

}
