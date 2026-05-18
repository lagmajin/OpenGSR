using System;
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
        private readonly HashSet<string> _joinedPlayers = new();
        private string _roomId = "local-match-room";
        private string _roomName = "Local Match";

        public event Action<JObject> MessageProduced;

        // テスト用：ダミープレイヤーの状態
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
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(port);
            running = true;

            listener.ConnectionRequestEvent += OnConnectionRequest;
            listener.PeerConnectedEvent += OnPeerConnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;

            PrettyLogger.Bold("Network", "LocalServer port:"+port.ToString());

            Task.Run(() => PollLoop());
            Task.Run(() => TestDataBroadcastLoop()); // テストデータを定期送信
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
                case "ClientConnect":
                    HandleClientConnect(json);
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
                case "TeamKill":
                    HandleTeamKill(json);
                    break;
                case "ItemUseRequest":
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
            // プレイヤー入力を受け取ったら、他のクライアントにブロードキャスト（今は自分に返す）
            PrettyLogger.Bold("RUDP Server", $"PlayerInput received: {json}");
        }

        private void HandleClientConnect(JObject json)
        {
            var playerId = json["PlayerID"]?.ToString() ?? "local_player";
            var roomId = json["RoomID"]?.ToString();
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                _roomId = roomId;
            }

            if (_joinedPlayers.Add(playerId))
            {
                PrettyLogger.Bold("RUDP Server", $"ClientConnect: {playerId} joined {_roomId}");
            }

            var joined = new JObject
            {
                ["MessageType"] = "MatchJoined",
                ["RoomID"] = _roomId,
                ["RoomName"] = _roomName,
                ["PlayerID"] = playerId,
                ["Capacity"] = 8
            };
            SendJson(joined);

            var snapshot = new JObject
            {
                ["MessageType"] = "Snapshot",
                ["RoomID"] = _roomId,
                ["RoomName"] = _roomName,
                ["IsPlaying"] = true,
                ["Players"] = BuildPlayersJson(),
                ["Snapshot"] = new JObject()
            };
            SendJson(snapshot);

            var matchStart = new JObject
            {
                ["MessageType"] = MessageType.GameStartNotification,
                ["RoomID"] = _roomId,
                ["MapName"] = "LocalMap",
                ["GameMode"] = "DeathMatch"
            };
            SendJson(matchStart);
        }

        private void HandleShootRequest(JObject json)
        {
            // 射撃リクエストを受け取ったら、射撃イベントを全クライアントに通知
            var playerId = json["PlayerId"]?.ToString() ?? "unknown";
            PrettyLogger.Bold("RUDP Server", $"ShootRequest from {playerId}");

            // テスト：射撃イベントを返す
            var shotMsg = RUDPMessageBuilder.CreatePlayerShot(playerId, new Vector2(0, 0), new Vector2(1, 0), "Pistol");
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

            PrettyLogger.Bold("RUDP Server", $"PlayerShot from {playerId}: {weaponType} at ({posX}, {posY}) dir({dirX}, {dirY})");

            // テスト：他のクライアントにブロードキャスト（自分に返す）
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

            PrettyLogger.Bold("RUDP Server", $"PlayerDeath: {playerId} killed by {killerId}");

            // テスト：死亡イベントをブロードキャスト
            var deathMsg = RUDPMessageBuilder.CreatePlayerDeath(playerId, killerId);
            SendJson(deathMsg);

            // テスト：キルスコア更新を送信
            var killerTeam = "Red";
            var victimTeam = "Blue";
            
            var killScoreMsg = RUDPMessageBuilder.CreateKillScoreUpdate(
                killerId, 
                1, // kills
                0, // deaths
                100, // score
                killerTeam
            );
            SendJson(killScoreMsg);

            // 死亡者のスコアも更新
            var deathScoreMsg = RUDPMessageBuilder.CreateKillScoreUpdate(
                playerId,
                0,
                1,
                0,
                victimTeam
            );
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

            PrettyLogger.Bold("RUDP Server", $"PlayerRespawn: {playerId} at ({posX}, {posY})");

            SendJson(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerRespawn,
                ["PlayerId"] = playerId,
                ["PosX"] = posX,
                ["PosY"] = posY
            });
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
        /// テスト用：定期的にダミーデータを送信
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
                            // ランダムに動くダミープレイヤー
                            testPlayerX += (float)(random.NextDouble() - 0.5) * 2f;
                            testPlayerY += (float)(random.NextDouble() - 0.5) * 2f;
                            testPlayerRotation += (float)(random.NextDouble() - 0.5) * 45f;

                            var posMsg = RUDPMessageBuilder.CreatePlayerPositionUpdate(
                                "TestPlayer", 
                                new Vector2(testPlayerX, testPlayerY), 
                                testPlayerRotation
                            );
                            SendJson(posMsg);
                        }

                        // 120フレームごと（約2秒）にゲーム状態を送信
                        if (frameCount % 120 == 0)
                        {
                            var scores = new JObject();
                            scores["TeamA"] = random.Next(0, 10);
                            scores["TeamB"] = random.Next(0, 10);

                            var stateMsg = RUDPMessageBuilder.CreateGameStateSync(300 - frameCount / 60, scores);
                            SendJson(stateMsg);
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

        private JArray BuildPlayersJson()
        {
            var players = new JArray();
            foreach (var playerId in _joinedPlayers.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
            {
                players.Add(new JObject
                {
                    ["Id"] = playerId,
                    ["Name"] = playerId,
                    ["Team"] = ETeam.NoTeam.ToString(),
                    ["IsReady"] = true,
                    ["IsBot"] = false,
                    ["Kills"] = 0,
                    ["Deaths"] = 0
                });
            }

            return players;
        }

        public void StopServer()
        {
            PrettyLogger.Bold("Network", "サーバー終了");

            running = false;
            server.Stop();
        }


    }

}
