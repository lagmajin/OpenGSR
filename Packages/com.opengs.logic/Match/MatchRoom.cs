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

        /// <summary>
        /// The rule this room is playing to, or null before one exists.
        /// <para>
        /// Read by anything that has to describe the match: the room state
        /// publishes the rule's own limits, and a caller asking what the room is
        /// actually playing is asking this rather than the setting the rule was
        /// built from, because the two can disagree.
        /// </para>
        /// </summary>
        public AbstractMatchRule? Rule => rule;

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
        /// <summary>
        /// Takes a player out of the room and brings the match's view of who is
        /// still in it up to date.
        /// <para>
        /// Removing them and counting them are one thing. A player who has gone
        /// is not on a team any more, and a room that still counted them would
        /// keep a team alive that has nobody standing in it, so the wipe that
        /// ends a team survival match would never arrive.
        /// </para>
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

            RefreshAliveCounts();
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

        /// <summary>
        /// Records a capture and gives the player who made it something for it.
        /// <para>
        /// The score is what a player earns, so a capture that only moved a team's
        /// tally earned nothing: a match won entirely on captures persisted a score
        /// of zero, and the experience that comes with it. The score is a room
        /// state and the tally is the same event seen from the other side, so one
        /// call now does both and a caller cannot do one without the other.
        /// </para>
        /// </summary>
        /// <param name="team">The team that scored.</param>
        /// <param name="capturingPlayerId">
        /// Who carried the flag in, or null when it is not known. A team score is
        /// not a player's score, so without this nothing is credited to anybody.
        /// </param>
        public void AddFlagCapture(ETeam team, string? capturingPlayerId = null)
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.AddFlagCapture(team);
            }
            else if (situation is AbstractTeamMatchSituation teamSituation)
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
            else
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(capturingPlayerId))
            {
                return;
            }

            var scorer = Players.FirstOrDefault(player =>
                string.Equals(player.Id, capturingPlayerId, StringComparison.OrdinalIgnoreCase));

            if (scorer == null)
            {
                return;
            }

            // A kill is worth a hundred, so a capture is worth the same: it is the
            // same kind of thing, done for a different reason.
            scorer.Score += CaptureScore;
        }

        /// <summary>
        /// What one capture is worth to the player who made it.
        /// </summary>
        public const int CaptureScore = 100;

        public void AddFlagReturn(ETeam team)
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.AddFlagReturn(team);
            }
        }

        /// <summary>
        /// Clears the capture the flag state of a room that is playing that mode.
        /// <para>
        /// This used to reach for the team scores of any team mode, which meant a
        /// method named for one mode could clear the kill totals a team death
        /// match is won and ended on. A room is only reset for the mode it is
        /// playing, so the other modes' numbers are left alone.
        /// </para>
        /// </summary>
        public void ResetCaptureTheFlagState()
        {
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                ctfSituation.Reset();
            }
        }

        /// <summary>
        /// Records a kill on both the player and the match.
        /// <para>
        /// The rules end a match on the best kill count, and the best kill count
        /// is state on the match rather than a thing to be recomputed from a
        /// player list at the moment somebody asks. Nothing used to write it, so
        /// the kill condition in the death match and survival rules could not
        /// fire and those matches could only ever end on the clock.
        /// </para>
        /// </summary>
        public void RecordKill(string killerId)
        {
            var killer = Players.FirstOrDefault(player =>
                string.Equals(player.Id, killerId, StringComparison.OrdinalIgnoreCase));

            if (killer == null)
            {
                return;
            }

            killer.Kills++;
            situation?.RecordKill(killer.Kills);

            // A kill belongs to the team that scored it, and a team death match
            // is won and ended on the team totals. Nobody was writing them, so
            // that mode compared two zeroes and reported a draw whatever had
            // happened in it.
            (situation as AbstractTeamMatchSituation)?.AddKill(killer.Team);
        }

        /// <summary>
        /// Records a death on both the player and the match.
        /// </summary>
        public void RecordDeath(string playerId)
        {
            var player = Players.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, playerId, StringComparison.OrdinalIgnoreCase));

            if (player == null)
            {
                return;
            }

            player.Deaths++;
            situation?.RecordDeath();
            RefreshAliveCounts();
        }

        /// <summary>
        /// Brings the per team alive counts up to date with who is in the room.
        /// <para>
        /// The team survival rule ends a match when one side is wiped, and it read
        /// these counts from a situation that nothing ever wrote, so the wipe could
        /// never happen and that mode could only ever end on the clock. The counts
        /// are derived from the players rather than kept beside them, because a
        /// second copy of who is alive is a thing that can disagree with who is.
        /// </para>
        /// </summary>
        private void RefreshAliveCounts()
        {
            if (situation is not TeamSurvivalMatchSituation teamSuv)
            {
                return;
            }

            teamSuv.SetAliveCount(ETeam.Red, Players.Count(p => p.Team == ETeam.Red && p.Health > 0));
            teamSuv.SetAliveCount(ETeam.Blue, Players.Count(p => p.Team == ETeam.Blue && p.Health > 0));
        }

        /// <summary>
        /// How many players on a team are still in the match.
        /// </summary>
        public int AliveCountOn(ETeam team)
        {
            return Players.Count(p => p.Team == team && p.Health > 0);
        }

        /// <summary>
        /// How many players are still in the match, on any team.
        /// </summary>
        public int AliveCount()
        {
            return Players.Count(p => p.Health > 0);
        }

        /// <summary>
        /// How many kills a team has scored, which is how a team death match is
        /// won and how its kill limit is reached.
        /// </summary>
        public int TeamKillCount(ETeam team)
        {
            if (situation is not AbstractTeamMatchSituation teamSituation)
            {
                return 0;
            }

            return team == ETeam.Red ? teamSituation.RedTeamKill : teamSituation.BlueTeamKill;
        }

        /// <summary>
        /// The best single kill count in the match, which is what a kill
        /// condition is judged against.
        /// </summary>
        public int BestKillCount => situation?.MaxPlayerKillCount ?? 0;

        /// <summary>
        /// How long the match has left, which is what a client showing a clock
        /// reads.
        /// </summary>
        public float RemainingTimeSeconds => situation?.RemainingTimeSec ?? 0f;

        /// <summary>
        /// How many players have died in the match.
        /// </summary>
        public int TotalDeathCount => situation?.TotalDeath ?? 0;

        /// <summary>
        /// Whether the rule considers the match over.
        /// <para>
        /// The room owns the rule and the situation it is playing, and the two
        /// together are the only thing that can answer this. A caller that
        /// wanted to know used to have to take the rule and a situation of its
        /// own, which is a different situation from the one the room is playing.
        /// </para>
        /// </summary>
        public bool IsMatchFinished() => rule?.IsMatchFinished(situation) ?? true;

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

            // The alive counts are read from the moment the match starts, so they
            // have to be right before it does.
            RefreshAliveCounts();

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

        /// <summary>
        /// Puts the settings this room's rule is actually running from into the
        /// room state.
        /// <para>
        /// A client used to carry its own idea of these, so a scoreboard could
        /// say the match is first to five while the server ends it at three. The
        /// rule ends the match, so the rule's numbers are the ones worth sending,
        /// and a client showing a rule the server is not running is showing
        /// something that is not the match it is in.
        /// </para>
        /// </summary>
        private void AddRuleSettings(JObject json)
        {
            // The multiplier is not a rule setting, but it is a thing the two
            // sides have to agree on and it lives on the setting, so it is
            // published with the rest of them. A client that derived it from
            // the mode instead would be applying its own guess while the server
            // applies the configured one, and the two players would disagree
            // about how much health anybody has.
            if (Setting is SuvMatchSetting suv)
            {
                json["HealthMultiplier"] = suv.HealthMultiplier;
            }
            else if (Setting is TeamSurvivalMatchSetting teamSuv)
            {
                json["HealthMultiplier"] = teamSuv.HealthMultiplier;
            }

            switch (rule)
            {
                case CaptureTheFlagMatchRule ctf:
                    json["WinConditionPoint"] = ctf.FlagLimit;
                    break;
                case DeathMatchRule death:
                    json["KillLimit"] = death.KillLimit;
                    break;
                case SuvMatchRule suvRule:
                    json["WinConditionKill"] = suvRule.WinConditionKill;
                    break;
                case TDMMatchRule tdm:
                    json["TeamKillLimit"] = tdm.TeamKillLimit;
                    break;
            }
        }

        private JObject _lastSyncState = new();

        public JObject ToJSon()
        {
            var json = new JObject();
            json["RoomID"] = Id;
            json["RoomName"] = RoomName;
            json["PlayerCount"] = PlayerCount;
            json["IsPlaying"] = Playing;
            json["IsFinished"] = Finished;
            json["MatchTimeSeconds"] = situation.RemainingTimeSec;
            AddRuleSettings(json);

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

