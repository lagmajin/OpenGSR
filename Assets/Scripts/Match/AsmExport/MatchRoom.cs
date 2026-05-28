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
            // ClientNetworkManagerのインスタンスを検索（Awake/Start時が望ましい）
            _networkManager = GameObject.FindFirstObjectByType<ClientNetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("[MatchRoom] ClientNetworkManager not found in scene!");
            }
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
            if (snapshot == null)
            {
                return;
            }

            // ルームの基本情報を更新
            Id = snapshot.GetStringOrNull("RoomID") ?? Id;
            RoomName = snapshot.GetStringOrNull("RoomName") ?? RoomName;
            Capacity = snapshot.Value<int?>("Capacity") ?? snapshot.Value<int?>("MaxCapacity") ?? Capacity;
            Playing = snapshot.Value<bool?>("IsPlaying") ?? snapshot.Value<bool?>("Playing") ?? Playing;
            IsStarted = Playing || snapshot.Value<bool?>("IsStarted") == true;
            Result = ResolveMatchResult(snapshot);

            Debug.Log($"[MatchRoom] Applied Snapshot for Room {RoomName} ({Id}). Playing: {Playing}");

            var mapText = snapshot.GetStringOrNull("Map");
            if (Stage != null && !string.IsNullOrWhiteSpace(mapText) && Enum.TryParse(mapText, true, out EMap map))
            {
                Stage.MapName = mapText;
                Stage.Mode = snapshot.GetStringOrNull("GameMode") != null && Enum.TryParse(snapshot.GetStringOrNull("GameMode"), true, out EGameMode mode)
                    ? mode
                    : Stage.Mode;
            }

            // GameSceneのスナップショットを適用
            JObject gameSceneSnapshot = snapshot.Value<JObject>("Snapshot");
            if (gameSceneSnapshot != null)
            {
                GameScene.ApplySnapshot(gameSceneSnapshot);
            }

            var playersArray = snapshot.Value<JArray>("Players");
            if (playersArray != null)
            {
                SyncPlayersFromSnapshot(playersArray);
            }

            var accountId = AccountManager.Instance?.CurrentProfile?.GlobalUserId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                PlayerManager.SetMyPlayerId(accountId);
                MatchData.SetMyPlayerId(accountId);
            }
        }

        public void AddPlayer(OpenGSCore.PlayerInfo info)
        {
            lock (_lockObj)
            {
                if (info == null)
                {
                    return;
                }

                var status = new PlayerStatus(info.Team, EPlayerType.OtherPlayer, Math.Max(1, info.MaxHealth), 100f)
                {
                    Hp = info.Health,
                    Booster = 100f,
                    AttackPower = info.AttackPower,
                    DefensePower = info.DefensePower
                };

                PlayerManager.AddPlayer(info, status);
                MatchData.UpdatePlayerStatus(info.Id, status);
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
            if (!IsStarted)
            {
                return EMatchResult.BeforeMatch;
            }

            if (Playing)
            {
                return EMatchResult.DuringTheGame;
            }

            return Result;
        }

        public PlayerData MyPlayer()
        {
            var player = PlayerManager.MyPlayer();
            if (player == null)
            {
                Debug.LogWarning("[MatchRoom] MyPlayer was requested but no local player data is available.");
                return null;
            }

            return new PlayerData(player.PlayerInfo, player.Status);
        }

        private void SyncPlayersFromSnapshot(JArray playersArray)
        {
            PlayerManager.RemoveAll();

            foreach (var token in playersArray)
            {
                if (token is not JObject playerJson)
                {
                    continue;
                }

                var playerInfo = BuildPlayerInfo(playerJson);
                var playerStatus = BuildPlayerStatus(playerJson, playerInfo);
                PlayerManager.AddPlayer(playerInfo, playerStatus);
                MatchData.UpdatePlayerStatus(playerInfo.Id, playerStatus);
            }
        }

        private static PlayerInfo BuildPlayerInfo(JObject playerJson)
        {
            var id = playerJson.GetStringOrNull("PlayerID")
                ?? playerJson.GetStringOrNull("PlayerId")
                ?? playerJson.GetStringOrNull("Id")
                ?? string.Empty;
            var name = playerJson.GetStringOrNull("PlayerName")
                ?? playerJson.GetStringOrNull("DisplayName")
                ?? playerJson.GetStringOrNull("Name")
                ?? id;

            var playerInfo = new PlayerInfo(id, name)
            {
                IsBot = playerJson.Value<bool?>("IsBot") ?? false,
                IsReady = playerJson.Value<bool?>("IsReady") ?? false,
                Kills = playerJson.Value<int?>("Kills") ?? playerJson.Value<int?>("KillCount") ?? 0,
                Deaths = playerJson.Value<int?>("Deaths") ?? playerJson.Value<int?>("DeathCount") ?? 0,
                Team = TryParseEnum(playerJson.GetStringOrNull("Team") ?? playerJson.GetStringOrNull("TeamName"), ETeam.NoTeam),
                Health = playerJson.Value<int?>("Health") ?? playerJson.Value<int?>("Hp") ?? 100,
                MaxHealth = playerJson.Value<int?>("MaxHealth") ?? playerJson.Value<int?>("MaxHp") ?? 100,
                AttackPower = playerJson.Value<int?>("AttackPower") ?? 10,
                DefensePower = playerJson.Value<int?>("DefensePower") ?? 5,
                playerCharacter = TryParseEnum(playerJson.GetStringOrNull("PlayerCharacter"), EPlayerCharacter.Misty)
            };

            if (playerJson["EquipInstantItems"] is JArray equipInstantItems)
            {
                playerInfo.EquipInstantItems.Clear();
                foreach (var item in equipInstantItems)
                {
                    if (Enum.TryParse(item?.ToString(), true, out EInstantItemType instantItem))
                    {
                        playerInfo.EquipInstantItems.Add(instantItem);
                    }
                }
            }

            return playerInfo;
        }

        private static PlayerStatus BuildPlayerStatus(JObject playerJson, PlayerInfo info)
        {
            var maxHp = playerJson.Value<float?>("MaxHealth") ?? playerJson.Value<float?>("MaxHp") ?? info.MaxHealth;
            var maxBooster = playerJson.Value<float?>("MaxBooster") ?? 100f;
            var status = new PlayerStatus(info.Team, EPlayerType.OtherPlayer, Math.Max(1, (int)maxHp), maxBooster)
            {
                Hp = playerJson.Value<float?>("Health") ?? playerJson.Value<float?>("Hp") ?? maxHp,
                Booster = playerJson.Value<float?>("Booster") ?? maxBooster,
                AttackPower = playerJson.Value<int?>("AttackPower") ?? info.AttackPower,
                DefensePower = playerJson.Value<int?>("DefensePower") ?? info.DefensePower
            };

            return status;
        }

        private static TEnum TryParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, true, out var parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private EMatchResult ResolveMatchResult(JObject snapshot)
        {
            var winner = snapshot.GetStringOrNull("WinningTeam")
                ?? snapshot.GetStringOrNull("WinnerTeam")
                ?? snapshot.GetStringOrNull("WinningSide");

            if (!string.IsNullOrWhiteSpace(winner))
            {
                if (Enum.TryParse(winner, true, out ETeam team))
                {
                    return team switch
                    {
                        ETeam.Red => EMatchResult.RedTeamWon,
                        ETeam.Blue => EMatchResult.BlueTeamWon,
                        _ => EMatchResult.Draw
                    };
                }

                if (string.Equals(winner, "Draw", StringComparison.OrdinalIgnoreCase))
                {
                    return EMatchResult.Draw;
                }
            }

            if (snapshot.Value<bool?>("IsFinished") == true || snapshot.Value<bool?>("Finished") == true)
            {
                return EMatchResult.Draw;
            }

            return Playing ? EMatchResult.DuringTheGame : EMatchResult.BeforeMatch;
        }
    }
}
