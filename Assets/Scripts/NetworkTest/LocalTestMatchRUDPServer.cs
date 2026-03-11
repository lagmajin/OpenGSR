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
            string jsonStr = json.ToString();

            // LiteNetLib用のデータライター
            NetDataWriter writer = new NetDataWriter();

            writer.Put(jsonStr);

            _clientPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void StartServer(int port)
        {
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
                    var messageType = json["MessageType"]?.ToString();
                    PrettyLogger.Bold("RUDP Server", $"Received: {messageType}");

                    // メッセージタイプごとに処理
                    switch (messageType)
                    {
                        case "PlayerInput": // RUDP専用
                            HandlePlayerInput(json);
                            break;
                        case "ShootRequest": // RUDP専用
                            HandleShootRequest(json);
                            break;
                        case RUDPMessageTypes.PlayerShot:
                            HandlePlayerShot(json);
                            break;
                        case RUDPMessageTypes.PlayerDeath:
                            HandlePlayerDeath(json);
                            break;
                        case "ItemUseRequest": // RUDP専用
                            HandleItemUseRequest(json);
                            break;
                        case "ChatMessage": // RUDP専用
                            HandleChatMessage(json);
                            break;
                        default:
                            PrettyLogger.Bold("RUDP Server", $"Unknown message: {messageType}");
                            break;
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON解析エラー: {ex.Message}");
                }
            }
        }

        private void HandlePlayerInput(JObject json)
        {
            // プレイヤー入力を受け取ったら、他のクライアントにブロードキャスト（今は自分に返す）
            PrettyLogger.Bold("RUDP Server", $"PlayerInput received: {json}");
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

        public void StopServer()
        {
            PrettyLogger.Bold("Network", "サーバー終了");

            running = false;
            server.Stop();
        }


    }

}
