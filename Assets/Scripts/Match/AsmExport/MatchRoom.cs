using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using OpenGSCore;
using Newtonsoft.Json.Linq;
using UnityEngine; // Unity固有の機能のために追加

namespace OpenGS
{
    public enum EMatchOverReason
    {
        TimeOver,
        FlagReturn,
        Kill,
    }

    public enum EMatchResult
    {
        BeforeMatch,
        DuringTheGame,
        BlueTeamWon,
        RedTeamWon,
        Draw,
        WinSoloPlayer,
    }

    /// <summary>
    /// クライアント側 MatchRoom 実装
    /// OpenGSCore.IMatchRoom を継承し、同時更新（Simultaneous Tick）に対応
    /// </summary>
    public class MatchRoom : OpenGSCore.IMatchRoom
    {
        private readonly object _lockObj = new();
        
        // OpenGSCore.MatchRoom と同様のバッファ（クライアント側の予測や補間用）
        private readonly ConcurrentQueue<JObject> _inputBuffer = new();

        public MatchData MatchData { get; private set; } = new();

        public OpenGS.Stage Stage { get; private set; } // Unityシーン内のステージ管理クラスへの参照を想定

        public string Id { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; } = 0;
        public EGameMode GameMode { get; set; } = EGameMode.Unknown;
        public IMatchRule Rule { get; set; }

        public bool IsStarted { get; private set; } = false;
        public bool Playing { get; private set; } = false;

        public EMatchResult Result { get; set; }

        public PlayerDatabase database = new(); // プレイヤー情報データベース
        public PlayerMatchManager PlayerManager { get; set; } = new(); // プレイヤーマッチ管理

        private ClientNetworkManager _networkManager; // ネットワークマネージャーへの参照

        public MatchRoom(string id)
        {
            Id = id;
            RoomName = "New Match Room";
            Stage = new Stage();
        }

        /// <summary>
        /// サーバーまたはローカルからの入力をバッファに追加
        /// </summary>
        public void PushInput(JObject input)
        {
            _inputBuffer.Enqueue(input);
        }

        public bool IsEnd()
        {
            return !Playing;
        }

        public void PrepareMatch()
        {
            // 準備ロジック
            Debug.Log("[MatchRoom] Preparing match...");
        }

        public void StartMatch()
        {
            IsStarted = true;
            Playing = true;
            Debug.Log("[MatchRoom] Match started!");
        }

        public void FinishMatch()
        {
            Playing = false;
            Debug.Log("[MatchRoom] Match finished!");
        }

        public void ResetCaptureTheFlagState()
        {
            MatchData?.ResetCaptureTheFlagState();
        }

        public bool TryRegisterFlagEvent(string eventKey)
        {
            return MatchData?.TryRegisterFlagEvent(eventKey) ?? true;
        }

        public (int RedScore, int BlueScore) AddFlagScore(ETeam scoringTeam, int delta = 1)
        {
            if (MatchData == null)
            {
                return (0, 0);
            }

            return MatchData.AddFlagScoreAndGet(scoringTeam, delta);
        }

        /// <summary>
        /// クライアント側のゲーム更新ループ
        /// </summary>
        public void GameUpdate()
        {
            if (!Playing) return;

            // バッファに蓄積された入力を処理（サーバーからのスナップショットや、クライアント自身の予測入力など）
            while (_inputBuffer.TryDequeue(out var input))
            {
                ProcessBufferedInput(input);
            }

            // ここにクライアント側のゲームロジック（クライアント予測、アニメーション、UI更新など）を追加
            // 例: Stage?.UpdateClientVisuals();
        }

        /// <summary>
        /// バッファされた入力（スナップショット含む）を処理
        /// </summary>
        private void ProcessBufferedInput(JObject input)
        {
            string messageType = input.GetStringOrNull("MessageType");
            string playerId = input.GetStringOrNull("PlayerID");

            switch (messageType)
            {
                case RUDPMessageTypes.Snapshot:
                    // サーバーからのスナップショットを適用
                    ApplyServerSnapshot(input);
                    break;
                case "PlayerMove":
                    // クライアント自身の予測入力処理、または遅延補間
                    // 例: GameScene.UpdatePlayerPosition(playerId, input.Value<float>("PosX"), input.Value<float>("PosY"));
                    break;
                // 他のクライアント側で処理すべき入力タイプがあればここに追加
                default:
                    Debug.Log($"[MatchRoom] Received unhandled buffered input type: {messageType}");
                    break;
            }
        }

        /// <summary>
        /// サーバーからのスナップショットをクライアントの状態に適用
        /// </summary>
        private void ApplyServerSnapshot(JObject snapshot)
        {
            // ルームの基本情報を更新
            Id = snapshot.GetStringOrNull("RoomID") ?? Id;
            RoomName = snapshot.GetStringOrNull("RoomName") ?? RoomName;
            Playing = snapshot.Value<bool>("IsPlaying");
            // Finished = snapshot.Value<bool>("IsFinished"); // クライアント側で直接Finishを呼ばない

            Debug.Log($"[MatchRoom] Applied Snapshot for Room {RoomName} ({Id}). Playing: {Playing}");

            // GameSceneのスナップショットを適用
            JObject gameSceneSnapshot = snapshot.Value<JObject>("Snapshot");
            if (gameSceneSnapshot != null)
            {
                Stage?.ApplySnapshot(gameSceneSnapshot);
            }

            // プレイヤー情報の更新（もしスナップショットに含まれていれば）
            // 例: JArray playersArray = snapshot.Value<JArray>("Players");
            // if (playersArray != null) { /* プレイヤーリストを更新 */ }
        }

        public void AddPlayer(OpenGSCore.PlayerInfo info)
        {
            lock (_lockObj)
            {
                if (info == null)
                {
                    Debug.LogWarning("[MatchRoom] AddPlayer called with null info.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(info.Id))
                {
                    info.Id = Guid.NewGuid().ToString("N");
                }

                var coreStatus = new OpenGSCore.PlayerStatus
                {
                    MaxHp = Math.Max(1, info.MaxHealth),
                    Hp = info.Health,
                    AttackPower = info.AttackPower,
                    DefensePower = info.DefensePower,
                    MaxBooster = 100f,
                    Booster = 100f
                };

                ApplyModeScaling(info, coreStatus);

                var playerData = new PlayerData(info, coreStatus);
                database.AddPlayer(playerData);
                PlayerManager.AddPlayer(info, coreStatus);
                Debug.Log($"[MatchRoom] Player {info.Name} ({info.Id}) added locally.");
            }
        }

        public void AddPlayers(List<OpenGSCore.PlayerInfo> info)
        {
            foreach (var player in info)
            {
                AddPlayer(player);
            }
        }

        public EMatchResult WinTeam()
        {
            return EMatchResult.DuringTheGame;
        }

        public PlayerData MyPlayer()
        {
            var localPlayerId = ResolveLocalPlayerId();
            if (!string.IsNullOrWhiteSpace(localPlayerId) && database.TryGetPlayer(localPlayerId, out var databasePlayer))
            {
                return databasePlayer;
            }

            var myPlayer = PlayerManager.MyPlayer();
            if (myPlayer != null)
            {
                return new PlayerData(myPlayer.PlayerInfo, myPlayer.Status);
            }

            if (database.AllPlayer().Count > 0)
            {
                return database.AllPlayer()[database.AllPlayer().Count - 1];
            }

            Debug.LogWarning("[MatchRoom] MyPlayer was requested but no local player data is available.");
            return null;
        }

        private string ResolveLocalPlayerId()
        {
            if (_networkManager == null)
            {
                _networkManager = GameObject.FindFirstObjectByType<ClientNetworkManager>();
            }

            return _networkManager != null ? _networkManager.ClientPlayerId : string.Empty;
        }

        private void ApplyModeScaling(OpenGSCore.PlayerInfo info, OpenGSCore.PlayerStatus status)
        {
            if (info == null || status == null)
            {
                return;
            }

            if (!MatchModeResolver.IsSurvivalLike(GameMode))
            {
                return;
            }

            var multiplier = MatchModeResolver.ResolveHealthMultiplier(GameMode);
            var maxHp = Mathf.Max(1, Mathf.RoundToInt(info.MaxHealth * multiplier));
            info.MaxHealth = maxHp;
            info.Health = maxHp;
            status.MaxHp = maxHp;
            status.Hp = maxHp;
        }
    }
}
