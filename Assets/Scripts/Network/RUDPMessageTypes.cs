using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// RUDP で使用するメッセージタイプの定数定義
    /// </summary>
    public static class RUDPMessageTypes
    {
        // サーバー → クライアント
        public const string WelcomeMessage = MessageType.WelcomeMessage;
        public const string GameStateSync = MessageType.GameStateSync;
        public const string PlayerSpawned = MessageType.PlayerSpawned;
        public const string PlayerPositionUpdate = MessageType.PlayerPositionUpdate;
        public const string PlayerShot = MessageType.PlayerShot;
        public const string PlayerDamage = MessageType.PlayerDamage;
        public const string PlayerDeath = MessageType.PlayerDeath;
        public const string MatchStart = MessageType.GameStartNotification;
        public const string MatchEnd = MessageType.MatchEndNotification;
        public const string ClientConnect = "ClientConnect";
        public const string TeamKill = "TeamKill";
        public const string Snapshot = "Snapshot";
        public const string MatchJoined = "MatchJoined";
        public const string PositionUpdateAck = "PositionUpdateAck";
        public const string PlayerKilled = "PlayerKilled";
        public const string PlayerDamaged = "PlayerDamaged";
        public const string GrenadeThrown = "GrenadeThrown";

        // CTF (Flag) 関連 - サーバー → クライアント
        public const string FlagCaptured = "FlagCaptured";        // フラッグキャプチャ
        public const string FlagLost = "FlagLost";                // フラッグロスト（落下）
        public const string FlagReturn = "FlagReturn";            // フラッグリターン（復帰）
        public const string FlagBurst = "FlagBurst";              // フラッグバースト（爆破）
        public const string FlagPickup = "FlagPickup";            // フラッグピックアップ
        public const string FlagScoreUpdate = "FlagScoreUpdate";  // フラッグスコア更新

        // DM/TDM (Death Match) 関連 - サーバー → クライアント
        public const string PlayerKill = "PlayerKill";            // プレイヤーキル
        public const string PlayerAssist = "PlayerAssist";        // アシスト
        public const string KillScoreUpdate = "KillScoreUpdate";   // キルスコア更新
        public const string RespawnUpdate = "RespawnUpdate";       // リスポーン更新
        public const string StreakUpdate = "StreakUpdate";        // ストリーク（連続キル）更新

        // クライアント → サーバー
        public const string PlayerInput = "PlayerInput";
        public const string ShootRequest = "ShootRequest";
        public const string ItemUseRequest = "ItemUseRequest";
        public const string PlayerReady = "PlayerReady";

        // チャット関連
        public const string ChatMessage = "ChatMessage";           // チャットメッセージ
        public const string ChatBroadcast = "ChatBroadcast";       // ブロードキャストメッセージ

        // フィールドアイテム関連
        public const string ItemPickup = "ItemPickup";            // アイテムを拾う
        public const string ItemUse = "ItemUse";                  // アイテムを使用
        public const string ItemSpawn = "ItemSpawn";              // アイテムが出現

        // ======== システム系通信 ========

        // リスポーン関連 - サーバー → クライアント
        public const string PlayerRespawn = "PlayerRespawn";      // プレイヤーリスポーン
        public const string RespawnCountdown = "RespawnCountdown"; // リスポーンカウントダウン
        public const string RespawnPosition = "RespawnPosition";   // リスポーン位置通知

        // ゲーム状態関連 - サーバー → クライアント
        public const string RoundStart = "RoundStart";            // ラウンド開始
        public const string RoundEnd = "RoundEnd";                // ラウンド終了
        public const string MatchPause = "MatchPause";            // マッチ一時停止
        public const string MatchResume = "MatchResume";          // マッチ再開
        public const string MatchTimeSync = "MatchTimeSync";      // 時間同期
        public const string WarmupStart = "WarmupStart";          // ウォームアップ開始
        public const string WarmupEnd = "WarmupEnd";              // ウォームアップ終了

        // プレイヤー状態関連 - 双方向
        public const string PlayerJoined = "PlayerJoined";        // プレイヤー参加
        public const string PlayerLeft = "PlayerLeft";           // プレイヤー退出
        public const string PlayerTeamSwitch = "PlayerTeamSwitch"; // チーム切り替え
        public const string PlayerSpectating = "PlayerSpectating"; // スペクテイターモード
        public const string PlayerRevive = "PlayerRevive";        // 蘇生

        // 武器関連 - クライアント → サーバー / サーバー → クライアント
        public const string WeaponChange = "WeaponChange";       // 武器切り替え
        public const string WeaponPickup = "WeaponPickup";        // 武器拾得
        public const string AmmoUpdate = "AmmoUpdate";            // アモ更新
        public const string GrenadeThrow = "GrenadeThrow";        // グレネード投擲
        public const string PlayerReload = "PlayerReload";       // リロード
        public const string PlayerMelee = "PlayerMelee";          // 近接攻撃

        // 投票システム - 双方向
        public const string VoteStart = "VoteStart";              // 投票開始
        public const string VoteEnd = "VoteEnd";                 // 投票終了
        public const string VoteCast = "VoteCast";               // 投票
        public const string VotePassed = "VotePassed";            // 投票可決
        public const string VoteFailed = "VoteFailed";            // 投票否決

        // 順位/統計関連 - サーバー → クライアント
        public const string MatchStats = "MatchStats";            // マッチ統計
        public const string LeaderboardUpdate = "LeaderboardUpdate"; // リーダーボード更新
        public const string PlayerStatsUpdate = "PlayerStatsUpdate"; // プレイヤー統計更新

        // オブジェクト同期 - サーバー → クライアント
        public const string ObjectSpawned = "ObjectSpawned";      // オブジェクト出現
        public const string ObjectDestroyed = "ObjectDestroyed";  // オブジェクト破壊

        // ネットワーク状態 - 双方向
        public const string PingRequest = "PingRequest";          // ピング要求
        public const string PingResponse = "PingResponse";        // ピング応答
        public const string ServerInfo = "ServerInfo";           // サーバー情報
        public const string ErrorMessage = "ErrorMessage";        // エラーメッセージ

        // バフ/デバフ - サーバー → クライアント
        public const string PlayerBuff = "PlayerBuff";            // バフ付与
        public const string PlayerDebuff = "PlayerDebuff";        // デバフ付与
        public const string BuffExpired = "BuffExpired";          // バフ期限切れ

        // ======== 制御系メッセージ (TCP 互換) ========
        // 旧コード互換の別名。実体は OpenGSCore.MessageType 側を参照する。

        public const string LobbyEnter = MessageType.LobbyEnter; // ロビー入室
        public const string LobbyLeave = MessageType.LobbyLeave; // ロビー退室
        public const string LobbyPlayerList = MessageType.LobbyPlayerList; // ロビー一覧
        public const string LobbyChat = MessageType.LobbyChat; // ロビーチャット

        public const string WaitRoomEnter = MessageType.WaitRoomEnter; // 待機室入室
        public const string WaitRoomLeave = MessageType.WaitRoomLeave; // 待機室退室
        public const string WaitRoomPlayerList = MessageType.WaitRoomPlayerList; // 待機室一覧
        public const string WaitRoomChat = MessageType.WaitRoomChat; // 待機室チャット
        public const string WaitRoomPlayerReady = MessageType.WaitRoomPlayerReady; // 準備完了
        public const string WaitRoomPlayerUnready = MessageType.WaitRoomPlayerUnready; // 準備解除
        public const string WaitRoomSettingsChange = MessageType.WaitRoomSettingsChange; // ルーム設定変更
        public const string WaitRoomKickPlayer = MessageType.WaitRoomKickPlayer; // キック
        public const string WaitRoomOwnerChange = MessageType.WaitRoomOwnerChange; // オーナー変更
        public const string WaitRoomStartCountdown = MessageType.WaitRoomStartCountdown; // 開始カウントダウン
        public const string WaitRoomCancelCountdown = MessageType.WaitRoomCancelCountdown; // カウントダウン中止
        public const string WaitRoomUpdateNotification = MessageType.WaitRoomUpdateNotification; // 一括更新通知

        public const string RoomListUpdate = MessageType.RoomListUpdateNotification; // ルーム一覧更新
        public const string RoomCreated = MessageType.RoomCreated; // ルーム作成通知
        public const string RoomDeleted = MessageType.RoomDeleted; // ルーム削除通知
        public const string RoomFull = MessageType.RoomFull; // ルーム満員通知
    }

    /// <summary>
    /// RUDP メッセージのビルダー（便利メソッド集）
    /// </summary>
    public static class RUDPMessageBuilder
    {
        /// <summary>
        /// プレイヤー位置同期メッセージを作成
        /// </summary>
        public static JObject CreatePlayerPositionUpdate(string playerId, Vector2 position, float rotation)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerPositionUpdate;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["Rotation"] = rotation;
            return json;
        }

        /// <summary>
        /// 射撃メッセージを作成
        /// </summary>
        public static JObject CreatePlayerShot(string playerId, Vector2 position, Vector2 direction, string weaponType)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerShot;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["DirX"] = direction.x;
            json["DirY"] = direction.y;
            json["WeaponType"] = weaponType;
            return json;
        }

        /// <summary>
        /// ダメージメッセージを作成
        /// </summary>
        public static JObject CreatePlayerDamage(string targetId, string attackerId, int damage, int remainingHp)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerDamage;
            json["TargetId"] = targetId;
            json["AttackerId"] = attackerId;
            json["Damage"] = damage;
            json["RemainingHp"] = remainingHp;
            return json;
        }

        /// <summary>
        /// 死亡メッセージを作成
        /// </summary>
        public static JObject CreatePlayerDeath(string playerId, string killerId)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerDeath;
            json["PlayerId"] = playerId;
            json["KillerId"] = killerId;
            return json;
        }

        /// <summary>
        /// ゲーム状態同期メッセージを作成
        /// </summary>
        public static JObject CreateGameStateSync(int remainingTime, JObject scoreData)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.GameStateSync;
            json["RemainingTime"] = remainingTime;
            json["Scores"] = scoreData;
            return json;
        }

        /// <summary>
        /// マッチ開始メッセージを作成
        /// </summary>
        public static JObject CreateMatchStart(string mapName, string gameMode)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchStart;
            json["MapName"] = mapName;
            json["GameMode"] = gameMode;
            return json;
        }

        #region CTF Flag メッセージビルダー

        /// <summary>
        /// フラッグキャプチャメッセージを作成
        /// </summary>
        /// <param name="playerId">キャプチャしたプレイヤーID</param>
        /// <param name="team">キャプチャしたチームのフラッグ</param>
        /// <param name="capturedAtPosition">キャプチャ位置</param>
        public static JObject CreateFlagCaptured(string playerId, string team, Vector2 capturedAtPosition)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagCaptured;
            json["PlayerId"] = playerId;
            json["Team"] = team;
            json["PosX"] = capturedAtPosition.x;
            json["PosY"] = capturedAtPosition.y;
            return json;
        }

        /// <summary>
        /// フラッグロストメッセージを作成（プレイヤーが死亡してフラッグを落とした場合など）
        /// </summary>
        /// <param name="playerId">ロストしたプレイヤーID</param>
        /// <param name="team">ロストしたフラッグのチーム</param>
        /// <param name="position">ロストした位置</param>
        public static JObject CreateFlagLost(string playerId, string team, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagLost;
            json["PlayerId"] = playerId;
            json["Team"] = team;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        /// <summary>
        /// フラッグリターン消息を作成（味方のフラッグがベースに帰還した場合）
        /// </summary>
        /// <param name="team">帰還したフラッグのチーム</param>
        /// <param name="returnedByPlayerId">帰還させたプレイヤーID（-null の場合は自動帰還）</param>
        public static JObject CreateFlagReturn(string team, string returnedByPlayerId = null)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagReturn;
            json["Team"] = team;
            if (returnedByPlayerId != null)
            {
                json["ReturnedByPlayerId"] = returnedByPlayerId;
            }
            return json;
        }

        /// <summary>
        /// フラッグバーストメッセージを作成（フラッグが爆破された場合）
        /// </summary>
        /// <param name="team">バーストしたフラッグのチーム</param>
        /// <param name="position">バースト位置</param>
        /// <param name="burstByPlayerId">爆破したプレイヤーID</param>
        public static JObject CreateFlagBurst(string team, Vector2 position, string burstByPlayerId = null)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagBurst;
            json["Team"] = team;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            if (burstByPlayerId != null)
            {
                json["BurstByPlayerId"] = burstByPlayerId;
            }
            return json;
        }

        /// <summary>
        /// フラッグピックアップメッセージを作成（プレイヤーが敵のフラッグを拾った場合）
        /// </summary>
        /// <param name="playerId">フラッグを拾ったプレイヤーID</param>
        /// <param name="team">拾ったフラッグのチーム</param>
        /// <param name="position">ピックアップ位置</param>
        public static JObject CreateFlagPickup(string playerId, string team, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagPickup;
            json["PlayerId"] = playerId;
            json["Team"] = team;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        /// <summary>
        /// フラッグスコア更新メッセージを作成
        /// </summary>
        /// <param name="redTeamScore">レッドチームスコア</param>
        /// <param name="blueTeamScore">ブルーチームスコア</param>
        /// <param name="redTeamFlags">レッドチームフラッグ数</param>
        /// <param name="blueTeamFlags">ブルーチームフラッグ数</param>
        public static JObject CreateFlagScoreUpdate(int redTeamScore, int blueTeamScore, int redTeamFlags = 0, int blueTeamFlags = 0)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.FlagScoreUpdate;
            json["RedTeamScore"] = redTeamScore;
            json["BlueTeamScore"] = blueTeamScore;
            json["RedTeamFlags"] = redTeamFlags;
            json["BlueTeamFlags"] = blueTeamFlags;
            return json;
        }

        #endregion

        #region DM/TDM メッセージビルダー

        /// <summary>
        /// プレイヤーキルメッセージを作成
        /// </summary>
        /// <param name="killerId">キルしたプレイヤーID</param>
        /// <param name="victimId">キルされたプレイヤーID</param>
        /// <param name="weaponType">使用武器</param>
        /// <param name="headshot">ヘッドショットかどうか</param>
        public static JObject CreatePlayerKill(string killerId, string victimId, string weaponType, bool headshot = false)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerKill;
            json["KillerId"] = killerId;
            json["VictimId"] = victimId;
            json["WeaponType"] = weaponType;
            json["Headshot"] = headshot;
            return json;
        }

        /// <summary>
        /// アシストメッセージを作成
        /// </summary>
        /// <param name="assisterId">アシストしたプレイヤーID</param>
        /// <param name="victimId">アシストされたターゲットID</param>
        /// <param name="assisterName">アシストプレイヤー名</param>
        public static JObject CreatePlayerAssist(string assisterId, string victimId, string assisterName = null)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerAssist;
            json["AssisterId"] = assisterId;
            json["VictimId"] = victimId;
            if (assisterName != null)
            {
                json["AssisterName"] = assisterName;
            }
            return json;
        }

        /// <summary>
        /// キルスコア更新メッセージを作成
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="kills">キル数</param>
        /// <param name="deaths">死亡数</param>
        /// <param name="score">スコア</param>
        /// <param name="team">チーム（DMの場合はnull）</param>
        public static JObject CreateKillScoreUpdate(string playerId, int kills, int deaths, int score, string team = null)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.KillScoreUpdate;
            json["PlayerId"] = playerId;
            json["Kills"] = kills;
            json["Deaths"] = deaths;
            json["Score"] = score;
            if (team != null)
            {
                json["Team"] = team;
            }
            return json;
        }

        /// <summary>
        /// リスポーン更新メッセージを作成
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="position">リスポーン位置</param>
        /// <param name="respawnTime">リスポーンまでの時間（秒）</param>
        public static JObject CreateRespawnUpdate(string playerId, Vector2 position, float respawnTime = 0f)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RespawnUpdate;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["RespawnTime"] = respawnTime;
            return json;
        }

        /// <summary>
        /// ストリーク更新メッセージを作成（連続キルなど）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="streakCount">連続キル数</param>
        /// <param name="streakType">ストリークタイプ（kill, assist, captureなど）</param>
        public static JObject CreateStreakUpdate(string playerId, int streakCount, string streakType = "kill")
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.StreakUpdate;
            json["PlayerId"] = playerId;
            json["StreakCount"] = streakCount;
            json["StreakType"] = streakType;
            return json;
        }

        #endregion

        #region チャットメッセージビルダー

        /// <summary>
        /// チャットメッセージを作成
        /// </summary>
        public static JObject CreateChatMessage(string playerId, string playerName, string message, bool teamOnly = false)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ChatMessage;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            json["Message"] = message;
            json["TeamOnly"] = teamOnly;
            json["Timestamp"] = DateTime.Now.ToString("HH:mm:ss");
            return json;
        }

        /// <summary>
        /// ブロードキャストメッセージを作成
        /// </summary>
        public static JObject CreateChatBroadcast(string message, string type = "system")
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ChatBroadcast;
            json["Message"] = message;
            json["BroadcastType"] = type;
            json["Timestamp"] = DateTime.Now.ToString("HH:mm:ss");
            return json;
        }

        #endregion

        #region フィールドアイテムメッセージビルダー

        /// <summary>
        /// アイテムピックアップメッセージを作成
        /// </summary>
        /// <param name="playerId">アイテムを拾ったプレイヤーID</param>
        /// <param name="itemId">アイテムID</param>
        /// <param name="itemType">アイテムタイプ（health, ammo, weapon, etc.）</param>
        /// <param name="position">ピックアップ位置</param>
        public static JObject CreateItemPickup(string playerId, string itemId, string itemType, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ItemPickup;
            json["PlayerId"] = playerId;
            json["ItemId"] = itemId;
            json["ItemType"] = itemType;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        /// <summary>
        /// アイテム使用メッセージを作成
        /// </summary>
        /// <param name="playerId">アイテムを使用したプレイヤーID</param>
        /// <param name="itemId">アイテムID</param>
        /// <param name="itemType">アイテムタイプ</param>
        /// <param name="effect">エフェクト（hp_heal, ammo_refill, etc.）</param>
        public static JObject CreateItemUse(string playerId, string itemId, string itemType, string effect)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ItemUse;
            json["PlayerId"] = playerId;
            json["ItemId"] = itemId;
            json["ItemType"] = itemType;
            json["Effect"] = effect;
            return json;
        }

        /// <summary>
        /// アイテム使用リクエストメッセージを作成
        /// </summary>
        /// <param name="playerId">アイテムを使用したプレイヤーID</param>
        /// <param name="itemId">アイテムID</param>
        /// <param name="itemType">アイテムタイプ</param>
        /// <param name="effect">エフェクト</param>
        public static JObject CreateItemUseRequest(string playerId, string itemId, string itemType, string effect)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ItemUseRequest;
            json["PlayerId"] = playerId;
            json["ItemId"] = itemId;
            json["ItemType"] = itemType;
            json["Effect"] = effect;
            return json;
        }

        /// <summary>
        /// アイテム出現メッセージを作成
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="itemType">アイテムタイプ</param>
        /// <param name="position">出現位置</param>
        /// <param name="respawnTime">リスポーン時間（秒、0なら出現のみ）</param>
        public static JObject CreateItemSpawn(string itemId, string itemType, Vector2 position, float respawnTime = 0f)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ItemSpawn;
            json["ItemId"] = itemId;
            json["ItemType"] = itemType;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["RespawnTime"] = respawnTime;
            return json;
        }

        #endregion

        #region システム系通信ビルダー

        #region リスポーン関連

        /// <summary>
        /// プレイヤーリスポーンメッセージを作成
        /// </summary>
        public static JObject CreatePlayerRespawn(string playerId, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRespawn;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        /// <summary>
        /// リスポーンカウントダウンメッセージを作成
        /// </summary>
        public static JObject CreateRespawnCountdown(string playerId, int countdownSeconds)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RespawnCountdown;
            json["PlayerId"] = playerId;
            json["Countdown"] = countdownSeconds;
            return json;
        }

        /// <summary>
        /// リスポーン位置通知メッセージを作成
        /// </summary>
        public static JObject CreateRespawnPosition(string playerId, Vector2[] positions)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RespawnPosition;
            json["PlayerId"] = playerId;
            var positionsArray = new JArray();
            foreach (var pos in positions)
            {
                positionsArray.Add(new JObject { ["x"] = pos.x, ["y"] = pos.y });
            }
            json["Positions"] = positionsArray;
            return json;
        }

        #endregion

        #region ゲーム状態関連

        /// <summary>
        /// ラウンド開始メッセージを作成
        /// </summary>
        public static JObject CreateRoundStart(int roundNumber, int totalRounds)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RoundStart;
            json["RoundNumber"] = roundNumber;
            json["TotalRounds"] = totalRounds;
            return json;
        }

        /// <summary>
        /// ラウンド終了メッセージを作成
        /// </summary>
        public static JObject CreateRoundEnd(string winningTeam, int roundNumber)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RoundEnd;
            json["WinningTeam"] = winningTeam;
            json["RoundNumber"] = roundNumber;
            return json;
        }

        /// <summary>
        /// マッチ一時停止メッセージを作成
        /// </summary>
        public static JObject CreateMatchPause(string pausedByPlayerId)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchPause;
            json["PausedBy"] = pausedByPlayerId;
            return json;
        }

        /// <summary>
        /// マッチ再開メッセージを作成
        /// </summary>
        public static JObject CreateMatchResume(string resumedByPlayerId)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchResume;
            json["ResumedBy"] = resumedByPlayerId;
            return json;
        }

        /// <summary>
        /// 時間同期メッセージを作成
        /// </summary>
        public static JObject CreateMatchTimeSync(int remainingTime, long serverTimestamp)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchTimeSync;
            json["RemainingTime"] = remainingTime;
            json["ServerTimestamp"] = serverTimestamp;
            return json;
        }

        /// <summary>
        /// ウォームアップ開始メッセージを作成
        /// </summary>
        public static JObject CreateWarmupStart(int warmupDuration)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.WarmupStart;
            json["Duration"] = warmupDuration;
            return json;
        }

        /// <summary>
        /// ウォームアップ終了メッセージを作成
        /// </summary>
        public static JObject CreateWarmupEnd()
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.WarmupEnd;
            return json;
        }

        #endregion

        #region プレイヤー状態関連

        /// <summary>
        /// プレイヤー参加メッセージを作成
        /// </summary>
        public static JObject CreatePlayerJoined(string playerId, string playerName, string team)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerJoined;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            json["Team"] = team;
            return json;
        }

        /// <summary>
        /// プレイヤー退出メッセージを作成
        /// </summary>
        public static JObject CreatePlayerLeft(string playerId, string reason = "unknown")
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerLeft;
            json["PlayerId"] = playerId;
            json["Reason"] = reason;
            return json;
        }

        /// <summary>
        /// チーム切り替えメッセージを作成
        /// </summary>
        public static JObject CreatePlayerTeamSwitch(string playerId, string newTeam)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerTeamSwitch;
            json["PlayerId"] = playerId;
            json["NewTeam"] = newTeam;
            return json;
        }

        /// <summary>
        /// スペクテイター遷移メッセージを作成
        /// </summary>
        public static JObject CreatePlayerSpectating(string playerId, bool isSpectating)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerSpectating;
            json["PlayerId"] = playerId;
            json["IsSpectating"] = isSpectating;
            return json;
        }

        /// <summary>
        /// 蘇生メッセージを作成
        /// </summary>
        public static JObject CreatePlayerRevive(string playerId, string revivedByPlayerId, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRevive;
            json["PlayerId"] = playerId;
            json["RevivedBy"] = revivedByPlayerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        #endregion

        #region 武器関連

        /// <summary>
        /// 武器切り替えメッセージを作成
        /// </summary>
        public static JObject CreateWeaponChange(string playerId, string weaponType, int slotIndex)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.WeaponChange;
            json["PlayerId"] = playerId;
            json["WeaponType"] = weaponType;
            json["SlotIndex"] = slotIndex;
            return json;
        }

        /// <summary>
        /// 武器拾得メッセージを作成
        /// </summary>
        public static JObject CreateWeaponPickup(string playerId, string weaponId, string weaponType, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.WeaponPickup;
            json["PlayerId"] = playerId;
            json["WeaponId"] = weaponId;
            json["WeaponType"] = weaponType;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        /// <summary>
        /// アモ更新メッセージを作成
        /// </summary>
        public static JObject CreateAmmoUpdate(string playerId, string weaponType, int currentAmmo, int maxAmmo)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.AmmoUpdate;
            json["PlayerId"] = playerId;
            json["WeaponType"] = weaponType;
            json["CurrentAmmo"] = currentAmmo;
            json["MaxAmmo"] = maxAmmo;
            return json;
        }

        /// <summary>
        /// グレネード投擲メッセージを作成
        /// </summary>
        public static JObject CreateGrenadeThrow(string playerId, Vector2 position, Vector2 direction, string grenadeType)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.GrenadeThrow;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["DirX"] = direction.x;
            json["DirY"] = direction.y;
            json["GrenadeType"] = grenadeType;
            return json;
        }

        /// <summary>
        /// リロードメッセージを作成
        /// </summary>
        public static JObject CreatePlayerReload(string playerId, string weaponType, bool isEmpty)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerReload;
            json["PlayerId"] = playerId;
            json["WeaponType"] = weaponType;
            json["IsEmpty"] = isEmpty;
            return json;
        }

        /// <summary>
        /// 近接攻撃メッセージを作成
        /// </summary>
        public static JObject CreatePlayerMelee(string playerId, Vector2 position, Vector2 direction, string weaponId)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerMelee;
            json["PlayerId"] = playerId;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["DirX"] = direction.x;
            json["DirY"] = direction.y;
            json["WeaponId"] = weaponId;
            return json;
        }

        #endregion

        #region 投票システム

        /// <summary>
        /// 投票開始メッセージを作成
        /// </summary>
        public static JObject CreateVoteStart(string voteId, string voteType, string initiatedBy, string targetId, int duration)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VoteStart;
            json["VoteId"] = voteId;
            json["VoteType"] = voteType;
            json["InitiatedBy"] = initiatedBy;
            json["TargetId"] = targetId;
            json["Duration"] = duration;
            return json;
        }

        /// <summary>
        /// 投票終了メッセージを作成
        /// </summary>
        public static JObject CreateVoteEnd(string voteId, bool passed)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VoteEnd;
            json["VoteId"] = voteId;
            json["Passed"] = passed;
            return json;
        }

        /// <summary>
        /// 投票メッセージを作成
        /// </summary>
        public static JObject CreateVoteCast(string voteId, string playerId, bool voteYes)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VoteCast;
            json["VoteId"] = voteId;
            json["PlayerId"] = playerId;
            json["VoteYes"] = voteYes;
            return json;
        }

        /// <summary>
        /// 投票可決メッセージを作成
        /// </summary>
        public static JObject CreateVotePassed(string voteId, string message)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VotePassed;
            json["VoteId"] = voteId;
            json["Message"] = message;
            return json;
        }

        /// <summary>
        /// 投票否決メッセージを作成
        /// </summary>
        public static JObject CreateVoteFailed(string voteId, string reason)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VoteFailed;
            json["VoteId"] = voteId;
            json["Reason"] = reason;
            return json;
        }

        #endregion

        #region 順位/統計関連

        /// <summary>
        /// マッチ統計メッセージを作成
        /// </summary>
        public static JObject CreateMatchStats(JArray playerStats, int totalKills, int totalDeaths)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchStats;
            json["PlayerStats"] = playerStats;
            json["TotalKills"] = totalKills;
            json["TotalDeaths"] = totalDeaths;
            return json;
        }

        /// <summary>
        /// リーダーボード更新メッセージを作成
        /// </summary>
        public static JObject CreateLeaderboardUpdate(JArray rankings)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.LeaderboardUpdate;
            json["Rankings"] = rankings;
            return json;
        }

        /// <summary>
        /// プレイヤー統計更新メッセージを作成
        /// </summary>
        public static JObject CreatePlayerStatsUpdate(string playerId, int kills, int deaths, int assists, int score)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerStatsUpdate;
            json["PlayerId"] = playerId;
            json["Kills"] = kills;
            json["Deaths"] = deaths;
            json["Assists"] = assists;
            json["Score"] = score;
            return json;
        }

        #endregion

        #region オブジェクト同期

        /// <summary>
        /// オブジェクト出現メッセージを作成
        /// </summary>
        public static JObject CreateObjectSpawned(string objectId, string objectType, Vector2 position, float rotation)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ObjectSpawned;
            json["ObjectId"] = objectId;
            json["ObjectType"] = objectType;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["Rotation"] = rotation;
            return json;
        }

        /// <summary>
        /// オブジェクト破壊メッセージを作成
        /// </summary>
        public static JObject CreateObjectDestroyed(string objectId, string destroyedBy, Vector2 position)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ObjectDestroyed;
            json["ObjectId"] = objectId;
            json["DestroyedBy"] = destroyedBy;
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            return json;
        }

        #endregion

        #region ネットワーク状態

        /// <summary>
        /// ピング要求メッセージを作成
        /// </summary>
        public static JObject CreatePingRequest(string playerId, long clientTimestamp)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PingRequest;
            json["PlayerId"] = playerId;
            json["ClientTimestamp"] = clientTimestamp;
            return json;
        }

        /// <summary>
        /// ピング応答メッセージを作成
        /// </summary>
        public static JObject CreatePingResponse(string playerId, long clientTimestamp, long serverTimestamp)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PingResponse;
            json["PlayerId"] = playerId;
            json["ClientTimestamp"] = clientTimestamp;
            json["ServerTimestamp"] = serverTimestamp;
            return json;
        }

        /// <summary>
        /// サーバー情報メッセージを作成
        /// </summary>
        public static JObject CreateServerInfo(string serverName, int currentPlayers, int maxPlayers, string gameMode, string mapName)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ServerInfo;
            json["ServerName"] = serverName;
            json["CurrentPlayers"] = currentPlayers;
            json["MaxPlayers"] = maxPlayers;
            json["GameMode"] = gameMode;
            json["MapName"] = mapName;
            return json;
        }

        /// <summary>
        /// エラーメッセージを作成
        /// </summary>
        public static JObject CreateErrorMessage(string errorCode, string errorMessage)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.ErrorMessage;
            json["ErrorCode"] = errorCode;
            json["ErrorMessage"] = errorMessage;
            return json;
        }

        #endregion

        #region バフ/デバフ

        /// <summary>
        /// バフ付与メッセージを作成
        /// </summary>
        public static JObject CreatePlayerBuff(string playerId, string buffType, int duration, float value)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerBuff;
            json["PlayerId"] = playerId;
            json["BuffType"] = buffType;
            json["Duration"] = duration;
            json["Value"] = value;
            return json;
        }

        /// <summary>
        /// デバフ付与メッセージを作成
        /// </summary>
        public static JObject CreatePlayerDebuff(string playerId, string debuffType, int duration, float value)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerDebuff;
            json["PlayerId"] = playerId;
            json["DebuffType"] = debuffType;
            json["Duration"] = duration;
            json["Value"] = value;
            return json;
        }

        /// <summary>
        /// バフ期限切れメッセージを作成
        /// </summary>
        public static JObject CreateBuffExpired(string playerId, string buffType)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.BuffExpired;
            json["PlayerId"] = playerId;
            json["BuffType"] = buffType;
            return json;
        }

        #endregion

        #region ロビー・待機室系ビルダー

        #region ロビー関連

        /// <summary>
        /// ロビー入室メッセージを作成
        /// </summary>
        public static JObject CreateLobbyEnter(string playerId, string playerName)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.LobbyEnter;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            return json;
        }

        /// <summary>
        /// ロビー退室メッセージを作成
        /// </summary>
        public static JObject CreateLobbyLeave(string playerId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.LobbyLeave;
            json["PlayerId"] = playerId;
            return json;
        }

        /// <summary>
        /// ロビープレイヤー一覧メッセージを作成
        /// </summary>
        public static JObject CreateLobbyPlayerList(JArray players)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.LobbyPlayerList;
            json["Players"] = players;
            return json;
        }

        /// <summary>
        /// ロビーチャットメッセージを作成
        /// </summary>
        public static JObject CreateLobbyChat(string playerId, string playerName, string message)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.LobbyChat;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            json["Message"] = message;
            json["Timestamp"] = DateTime.Now.ToString("HH:mm:ss");
            return json;
        }

        #endregion

        #region 待機室関連

        /// <summary>
        /// 待機室入室メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomEnter(string playerId, string playerName, string roomId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomEnter;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            json["RoomId"] = roomId;
            return json;
        }

        /// <summary>
        /// 待機室退室メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomLeave(string playerId, string roomId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomLeave;
            json["PlayerId"] = playerId;
            json["RoomId"] = roomId;
            return json;
        }

        /// <summary>
        /// 待機室プレイヤー一覧メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomPlayerList(string roomId, JArray players)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomPlayerList;
            json["RoomId"] = roomId;
            json["Players"] = players;
            return json;
        }

        /// <summary>
        /// 待機室チャットメッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomChat(string playerId, string playerName, string message, string roomId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomChat;
            json["PlayerId"] = playerId;
            json["PlayerName"] = playerName;
            json["Message"] = message;
            json["RoomId"] = roomId;
            json["Timestamp"] = DateTime.Now.ToString("HH:mm:ss");
            return json;
        }

        /// <summary>
        /// プレイヤー準備完了メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomPlayerReady(string playerId, string roomId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomPlayerReady;
            json["PlayerId"] = playerId;
            json["RoomId"] = roomId;
            return json;
        }

        /// <summary>
        /// プレイヤー準備解除メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomPlayerUnready(string playerId, string roomId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomPlayerUnready;
            json["PlayerId"] = playerId;
            json["RoomId"] = roomId;
            return json;
        }

        /// <summary>
        /// ルーム設定変更メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomSettingsChange(string roomId, JObject settings)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomSettingsChange;
            json["RoomId"] = roomId;
            json["Settings"] = settings;
            return json;
        }

        /// <summary>
        /// プレイヤーキックメッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomKickPlayer(string playerId, string roomId, string reason)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomKickPlayer;
            json["PlayerId"] = playerId;
            json["RoomId"] = roomId;
            json["Reason"] = reason;
            return json;
        }

        /// <summary>
        /// オーナー変更メッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomOwnerChange(string roomId, string newOwnerId)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomOwnerChange;
            json["RoomId"] = roomId;
            json["NewOwnerId"] = newOwnerId;
            return json;
        }

        /// <summary>
        /// 開始カウントダウンメッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomStartCountdown(string roomId, int countdownSeconds)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomStartCountdown;
            json["RoomId"] = roomId;
            json["Countdown"] = countdownSeconds;
            return json;
        }

        /// <summary>
        /// カウントダウンキャンセルメッセージを作成
        /// </summary>
        public static JObject CreateWaitRoomCancelCountdown(string roomId, string reason)
        {
            var json = new JObject();
            json["MessageType"] = MessageType.WaitRoomCancelCountdown;
            json["RoomId"] = roomId;
            json["Reason"] = reason;
            return json;
        }

        #endregion

        #region ルームリスト関連

        /// <summary>
        /// ルーム一覧更新メッセージを作成
        /// </summary>
        public static JObject CreateRoomListUpdate(RoomListSnapshot snapshot)
        {
            if (snapshot == null)
            {
                snapshot = new RoomListSnapshot();
            }

            return snapshot.ToJson();
        }

        /// <summary>
        /// ルーム一覧更新メッセージを作成
        /// </summary>
        public static JObject CreateRoomListUpdate(JArray rooms)
        {
            var snapshot = new RoomListSnapshot();
            if (rooms != null)
            {
                foreach (var token in rooms)
                {
                    if (token is JObject roomJson)
                    {
                        snapshot.Rooms.Add(RoomListEntry.FromJson(roomJson));
                    }
                }
            }

            return snapshot.ToJson();
        }

        /// <summary>
        /// ルーム作成通知メッセージを作成
        /// </summary>
        public static JObject CreateRoomCreated(RoomInfoSnapshot roomInfo)
        {
            return (roomInfo ?? new RoomInfoSnapshot()).ToNotificationJson(MessageType.RoomCreated);
        }

        public static JObject CreateRoomCreated(string roomId, string roomName, string ownerId)
            => CreateRoomCreated(new RoomInfoSnapshot
            {
                RoomId = roomId,
                RoomName = roomName,
                OwnerId = ownerId
            });

        /// <summary>
        /// ルーム削除通知メッセージを作成
        /// </summary>
        public static JObject CreateRoomDeleted(RoomInfoSnapshot roomInfo)
        {
            return (roomInfo ?? new RoomInfoSnapshot()).ToNotificationJson(MessageType.RoomDeleted);
        }

        public static JObject CreateRoomDeleted(string roomId)
            => CreateRoomDeleted(new RoomInfoSnapshot { RoomId = roomId });

        /// <summary>
        /// ルーム満員通知メッセージを作成
        /// </summary>
        public static JObject CreateRoomFull(RoomInfoSnapshot roomInfo)
        {
            return (roomInfo ?? new RoomInfoSnapshot()).ToNotificationJson(MessageType.RoomFull);
        }

        public static JObject CreateRoomFull(string roomId)
            => CreateRoomFull(new RoomInfoSnapshot { RoomId = roomId });

        #endregion

        #endregion

        #endregion
    }
}
