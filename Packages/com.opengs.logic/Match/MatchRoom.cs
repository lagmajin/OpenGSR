#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace OpenGSCore
{
    public interface IMatchRoom
    {
        string RoomName { get; set; }
    }

    /// <summary>
    /// OpenGS MatchRoom - ゲームマッチの状態管理クラス
    /// 同時実行性（Simultaneous Processing）をサポートするように設計されています。
    /// </summary>
    public partial class MatchRoom : AbstractGameRoom, IMatchRoom, ISyncable
    {
        private readonly MatchRoomEventBus eventBus;

        AbstractMatchRule? rule;

        public AbstractMatchSetting Setting { get; set; } = null!;

        private AbstractMatchSituation? Situation { get; set; } = null;
        private readonly object playerSyncLock = new();
        private readonly Dictionary<string, EPlayerPoseState> playerPoseStates = new();

        public GameScene GameScene { get; set; } = new();

        public WaitRoom? WaitRoomLink { get; set; } = null;
        public string RoomName { get; set; }

        public bool MatchEnd { get; private set; } = false;
        public bool IsSuddenDeathModeNow { get; private set; } = false;

        // 同時処理（Simultaneous Tick）のための入力バッファ
        private readonly ConcurrentQueue<JObject> _inputBuffer = new();

        public int PlayerCount 
        { 
            get 
            { 
                lock (playerSyncLock)
                {
                    return Players.Count;
                }
            } 
        }

        public int Capacity { get; } = 20;

        public bool Playing { get; private set; } = false;
        public bool Finished { get; private set; } = false;

        private Stopwatch sw = new();

        private HighPrecisionGameTimer? timer;

        private FieldItemService itemServiceA = new FieldItemService();
        private AbstractMatchSituation situation;

        /// <summary>
        /// 外部から入力をバッファに追加する（マルチスレッドセーフ）
        /// </summary>
        public void PushInput(JObject input)
        {
            _inputBuffer.Enqueue(input);
        }

        /// <summary>
        /// プレイヤーをルームから削除する
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            lock (playerSyncLock)
            {
                var player = Players.FirstOrDefault(p =>
                    IdEquals(p.Id, playerId));
                if (player == null)
                {
                    return;
                }

                Players.Remove(player);
                playerPoseStates.Remove(player.Id);
            }
        }

        public MatchRoom(int roomNumber, in string roomName, in string roomOwnerId, AbstractMatchSetting setting, MatchRoomEventBus bus) : base(roomNumber, roomOwnerId)
        {
            Setting = setting;
            eventBus = bus;
            Id = Guid.NewGuid().ToString("N");
            RoomName = roomName;

            // ルールと状況の初期化
            rule = MatchRuleFactory.CreateMatchRule(setting);
            
            // モードに応じて適切な Situation を作成
            situation = setting.Mode switch
            {
                EGameMode.TeamDeathMatch => new AbstractTeamMatchSituation(),
                EGameMode.CaptureTheFlag => new CaptureTheFlagMatchSituation(),
                EGameMode.TeamSurvival => new TeamSurvivalMatchSituation(),
                EGameMode.DeathMatch => new AbstractMatchSituation(),
                _ => new AbstractMatchSituation()
            };
            
            situation.mode = setting.Mode;
            situation.RemainingTimeSec = (rule != null) ? rule.MatchTimeMSec() / 1000f : 300f;
        }

        public bool ChangeOwnerRandom()
        {
            lock (playerSyncLock)
            {
                if (Players.Count < 1)
                {
                    return false;
                }

                var random = new Random();
                var randomIndex = random.Next(Players.Count);
                var newOwner = Players[randomIndex];
                
                // 新しいオーナーを設定
                OwnerId = newOwner.Id;
                return true;
            }
        }

        public void AddNewPlayer(PlayerInfo info)
        {
            if (Playing)
            {
                // 途中参加の処理は要検討
                return;
            }

            lock (playerSyncLock)
            {
                if (!Players.Exists(p => IdEquals(p.Id, info.Id)))
                {
                    Players.Add(info);
                }
            }
        }

        public bool TryGetPlayer(string playerId, out PlayerInfo? player)
        {
            lock (playerSyncLock)
            {
                player = null;
                if (string.IsNullOrWhiteSpace(playerId))
                {
                    return false;
                }

                player = Players.FirstOrDefault(p => IdEquals(p.Id, playerId));
                return player != null;
            }
        }

        public bool ContainsPlayer(string playerId)
        {
            lock (playerSyncLock)
            {
                if (string.IsNullOrWhiteSpace(playerId))
                {
                    return false;
                }

                return Players.Any(p => IdEquals(p.Id, playerId));
            }
        }

        /// <summary>
        /// Compares two player ids.
        /// <para>
        /// Player ids are guids produced with the N format, but they also arrive
        /// from clients, and the casing of a guid is not fixed by the format. The
        /// lookups here used a plain string equality while RemovePlayer did not,
        /// so the same player could be found by one call and missed by another,
        /// and a pose was stored under a key the lookup could never match.
        /// </para>
        /// </summary>
        private static bool IdEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public void SetPlayerPoseState(string playerId, EPlayerPoseState poseState)
        {
            lock (playerSyncLock)
            {
                if (string.IsNullOrWhiteSpace(playerId))
                {
                    return;
                }

                // Store under the id the player actually has, not the one the
                // caller passed, so the state can be found again later.
                var actual = Players.FirstOrDefault(p => IdEquals(p.Id, playerId))?.Id;
                if (actual == null)
                {
                    return;
                }

                playerPoseStates[actual] = poseState;
            }
        }

        public EPlayerPoseState GetPlayerPoseState(string playerId)
        {
            lock (playerSyncLock)
            {
                // The dictionary is keyed by the id the player actually has, so
                // resolve the caller's id against the roster before reading it
                // rather than looking the key up verbatim.
                foreach (var entry in playerPoseStates)
                {
                    if (IdEquals(entry.Key, playerId))
                    {
                        return entry.Value;
                    }
                }

                return EPlayerPoseState.Stand;
            }
        }

        public void AddNewPlayers(List<PlayerInfo> list)
        {
            if (list == null || list.Count == 0)
            {
                return;
            }

            foreach (var info in list)
            {
                AddNewPlayer(info);
            }
        }

        /// <summary>
        /// 同時更新処理 (Simultaneous Tick Update)
        /// バッファに蓄積された全入力を一括処理してからシーンを更新する
        /// </summary>
        public override void GameUpdate()
        {
            if (Finished) return;

            // 1. バッファに溜まった全入力を処理
            while (_inputBuffer.TryDequeue(out var input))
            {
                ProcessBufferedInput(input);
            }

            // 2. ゲームシーンのフレーム更新（物理・ロジック）
            GameScene.UpdateFrame();

            // 3. ルールチェック（終了判定など）
            if (Playing && rule != null && rule.IsMatchFinished(situation))
            {
                Finish();
            }
        }

        private void ProcessBufferedInput(JObject input)
        {
            var messageType = input.GetStringOrNull("MessageType");
            var playerId = input.GetStringOrNull("PlayerID");

            if (string.IsNullOrEmpty(messageType) || string.IsNullOrEmpty(playerId)) return;

            switch (messageType)
            {
                case "PlayerMove":
                    var posX = input.Value<float>("PosX");
                    var posY = input.Value<float>("PosY");
                    // GameSceneの状態を更新
                    GameScene.UpdatePlayerPosition(playerId!, posX, posY);
                    break;

                case "PlayerAction":
                    var action = input.GetStringOrNull("ActionType");
                    if (action == "Shoot")
                    {
                        // 射撃イベントの処理
                    }
                    break;
            }
        }

        private System.Timers.Timer? statusUpdateTimer;

        public void StartStatusUpdates()
        {
            statusUpdateTimer = new System.Timers.Timer(1000); // 1秒ごとに更新
            statusUpdateTimer.Elapsed += (sender, e) => SendPeriodicStatusUpdate();
            statusUpdateTimer.Start();
        }

        public void StopStatusUpdates()
        {
            statusUpdateTimer?.Stop();
            statusUpdateTimer?.Dispose();
        }

        public void AddFlagCapture(ETeam team)
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.AddFlagCapture(team);
                return;
            }

            if (situation is AbstractTeamMatchSituation teamSituation)
            {
                switch (team)
                {
                    case ETeam.Red:
                        teamSituation.RedTeamFlagCaptures++;
                        break;
                    case ETeam.Blue:
                        teamSituation.BlueTeamFlagCaptures++;
                        break;
                }
            }
        }

        public void AddFlagReturn(ETeam team)
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.AddFlagReturn(team);
            }
        }

        public void ResetCaptureTheFlagState()
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.Reset();
                return;
            }

            if (situation is AbstractTeamMatchSituation teamSituation)
            {
                teamSituation.RedTeamFlagCaptures = 0;
                teamSituation.BlueTeamFlagCaptures = 0;
                teamSituation.RedTeamKill = 0;
                teamSituation.BlueTeamKill = 0;
            }
        }

        public int GetFlagScore(ETeam team)
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                return team == ETeam.Red ? ctfSituation.RedTeamFlagCaptures : ctfSituation.BlueTeamFlagCaptures;
            }

            if (situation is AbstractTeamMatchSituation teamSituation)
            {
                return team == ETeam.Red ? teamSituation.RedTeamFlagCaptures : teamSituation.BlueTeamFlagCaptures;
            }

            return 0;
        }

        private void SendPeriodicStatusUpdate()
        {
            if (Playing && !Finished)
            {
                float deltaTime = 1.0f; // 1秒周期

                // マッチ状況の更新
                situation.UpdateTime(deltaTime);

                // アイテムAグループの更新
                EFieldItemType? spawnedItem;
                var stateChange = itemServiceA.Update(deltaTime, out spawnedItem);

                if (stateChange == FieldItemService.ESpawnState.Active && spawnedItem.HasValue)
                {
                    eventBus.PublishItemSpawn(spawnedItem.Value, 0);
                }
                else if (stateChange == FieldItemService.ESpawnState.Waiting)
                {
                    eventBus.PublishItemDespawn();
                }
            }
        }

        public void GameStart()
        {
            sw.Start();

            // ステータス更新を開始
            StartStatusUpdates();

            Playing = true;
            eventBus.PublishGameStart();
        }

        public void Finish()
        {
            // ステータス更新を停止
            StopStatusUpdates();

            // 勝敗判定ロジックをファクトリから取得して実行
            var evaluator = MatchResultEvaluatorFactory.CreateEvaluator(Setting.Mode);
            var resultJson = evaluator.Evaluate(situation, Players);
            resultJson["RoomId"] = Id.ToString();

            // マッチ終了イベントを発行
            eventBus.PublishGameEnd();
            eventBus.PublishGameEndWithResult(resultJson);

            Playing = false;
            Finished = true;

            // WaitRoomに終了を通知
            if (WaitRoomLink != null)
            {
                WaitRoomLink.GameIsOver();
                WaitRoomLink = null;
            }
        }

        #region ISyncable Implementation

        private JObject _lastSyncState = new();

        public JObject ToJSon()
        {
            var json = new JObject();
            json["RoomID"] = Id;
            json["RoomName"] = RoomName;
            json["PlayerCount"] = PlayerCount;
            json["IsPlaying"] = Playing;
            json["IsFinished"] = Finished;
            
            // GameSceneのスナップショットを追加
            json["Snapshot"] = GameScene.GetSnapshot();
            
            return json;
        }

        public bool HasChanged()
        {
            var currentState = ToJSon();
            return !JToken.DeepEquals(currentState, _lastSyncState);
        }

        public void SaveSyncState()
        {
            _lastSyncState = ToJSon();
        }

        #endregion

        public void Dispose()
        {
            StopStatusUpdates();
        }
    }
}

