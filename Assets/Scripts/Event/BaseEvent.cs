using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenGS
{
    /// <summary>
    /// フラッグイベントタイプ
    /// </summary>
    public enum EFlagEventType
    {
        Unknown,
        Captured,
        Lost,
        Returned,
        Pickup,
        Burst
    }

    public enum EDeadReason
    {
        Unknown,
        Burst,
        KilledBy,
        Suicide
    }

    public class PlayerDeadEvent : AbstractGameEvent
    {
        private EDeadReason reason_ = EDeadReason.Unknown;
        private string playerName_;
        private string playerID_;
        private string killerID_ = "";
        private ETeam playerTeam_;

        public PlayerDeadEvent(EDeadReason reason, string playerName, string playerID, ETeam team)
        {
            reason_ = reason;
            playerName_ = playerName;
            playerID_ = playerID;
            playerTeam_ = team;
        }

        public string PlayerName() => playerName_;
        public string PlayerID() => playerID_;
        public string KillerID() => killerID_;
        public ETeam PlayerTeam() => playerTeam_;
        public EDeadReason Reason() => reason_;
        
        public void SetKillerID(string killerID) => killerID_ = killerID;
    }

    /// <summary>
    /// プレイヤーキルイベント
    /// </summary>
    public class PlayerKillEvent : AbstractGameEvent
    {
        private string killerID_;
        private string victimID_;
        private string weaponType_ = "Unknown";
        private bool headshot_ = false;

        public PlayerKillEvent(string killerId, string victimId, string weaponType = "Unknown", bool headshot = false)
        {
            killerID_ = killerId;
            victimID_ = victimId;
            weaponType_ = weaponType;
            headshot_ = headshot;
        }

        public string KillerID() => killerID_;
        public string VictimID() => victimID_;
        public string WeaponType() => weaponType_;
        public bool IsHeadshot() => headshot_;
    }

    /// <summary>
    /// アシストイベント
    /// </summary>
    public class PlayerAssistEvent : AbstractGameEvent
    {
        private string assisterID_;
        private string victimID_;

        public PlayerAssistEvent(string assisterId, string victimId)
        {
            assisterID_ = assisterId;
            victimID_ = victimId;
        }

        public string AssisterID() => assisterID_;
        public string VictimID() => victimID_;
    }

    /// <summary>
    /// 射撃イベント
    /// </summary>
    public class PlayerShotEvent : AbstractGameEvent
    {
        private string playerID_;
        private Vector2 position_;
        private Vector2 direction_;
        private string weaponType_ = "Unknown";

        public PlayerShotEvent(string playerId, Vector2 position, Vector2 direction, string weaponType = "Unknown")
        {
            playerID_ = playerId;
            position_ = position;
            direction_ = direction;
            weaponType_ = weaponType;
        }

        public string PlayerID() => playerID_;
        public Vector2 Position() => position_;
        public Vector2 Direction() => direction_;
        public string WeaponType() => weaponType_;
    }

    /// <summary>
    /// ダメージイベント
    /// </summary>
    public class PlayerDamageEvent : AbstractGameEvent
    {
        private string targetID_;
        private string attackerID_;
        private int damage_;
        private int remainingHp_;

        public PlayerDamageEvent(string targetId, string attackerId, int damage, int remainingHp)
        {
            targetID_ = targetId;
            attackerID_ = attackerId;
            damage_ = damage;
            remainingHp_ = remainingHp;
        }

        public string TargetID() => targetID_;
        public string AttackerID() => attackerID_;
        public int Damage() => damage_;
        public int RemainingHp() => remainingHp_;
    }

    /// <summary>
    /// スコア更新イベント
    /// </summary>
    public class ScoreUpdateEvent : AbstractGameEvent
    {
        private string playerID_;
        private int kills_;
        private int deaths_;
        private int score_;
        private ETeam team_;

        public ScoreUpdateEvent(string playerId, int kills, int deaths, int score, ETeam team)
        {
            playerID_ = playerId;
            kills_ = kills;
            deaths_ = deaths;
            score_ = score;
            team_ = team;
        }

        public string PlayerID() => playerID_;
        public int Kills() => kills_;
        public int Deaths() => deaths_;
        public int Score() => score_;
        public ETeam Team() => team_;
    }

    /// <summary>
    /// フラッグイベント（CTF用）
    /// </summary>
    public class FlagEvent : AbstractGameEvent
    {
        private string playerID_;
        private ETeam team_;
        private EFlagEventType flagEventType_;
        private Vector2 position_;

        public FlagEvent(string playerId, ETeam team, EFlagEventType eventType, Vector2 position)
        {
            playerID_ = playerId;
            team_ = team;
            flagEventType_ = eventType;
            position_ = position;
        }

        public string PlayerID() => playerID_;
        public ETeam Team() => team_;
        public EFlagEventType FlagEventType() => flagEventType_;
        public Vector2 Position() => position_;
    }

    // ======== システム系イベント ========

    /// <summary>
    /// リスポーンイベント
    /// </summary>
    public class PlayerRespawnEvent : AbstractGameEvent
    {
        private string playerID_;
        private Vector2 position_;

        public PlayerRespawnEvent(string playerId, Vector2 position)
        {
            playerID_ = playerId;
            position_ = position;
        }

        public string PlayerID() => playerID_;
        public Vector2 Position() => position_;
    }

    /// <summary>
    /// リスポーンカウントダウンイベント
    /// </summary>
    public class RespawnCountdownEvent : AbstractGameEvent
    {
        private string playerID_;
        private int countdownSeconds_;

        public RespawnCountdownEvent(string playerId, int countdownSeconds)
        {
            playerID_ = playerId;
            countdownSeconds_ = countdownSeconds;
        }

        public string PlayerID() => playerID_;
        public int CountdownSeconds() => countdownSeconds_;
    }

    /// <summary>
    /// ラウンド開始イベント
    /// </summary>
    public class RoundStartEvent : AbstractGameEvent
    {
        private int roundNumber_;
        private int totalRounds_;

        public RoundStartEvent(int roundNumber, int totalRounds)
        {
            roundNumber_ = roundNumber;
            totalRounds_ = totalRounds;
        }

        public int RoundNumber() => roundNumber_;
        public int TotalRounds() => totalRounds_;
    }

    /// <summary>
    /// ラウンド終了イベント
    /// </summary>
    public class RoundEndEvent : AbstractGameEvent
    {
        private string winningTeam_;
        private int roundNumber_;

        public RoundEndEvent(string winningTeam, int roundNumber)
        {
            winningTeam_ = winningTeam;
            roundNumber_ = roundNumber;
        }

        public string WinningTeam() => winningTeam_;
        public int RoundNumber() => roundNumber_;
    }

    /// <summary>
    /// マッチ一時停止イベント
    /// </summary>
    public class MatchPauseEvent : AbstractGameEvent
    {
        private string pausedByPlayerID_;

        public MatchPauseEvent(string pausedByPlayerId)
        {
            pausedByPlayerID_ = pausedByPlayerId;
        }

        public string PausedByPlayerID() => pausedByPlayerID_;
    }

    /// <summary>
    /// マッチ再開イベント
    /// </summary>
    public class MatchResumeEvent : AbstractGameEvent
    {
        private string resumedByPlayerID_;

        public MatchResumeEvent(string resumedByPlayerId)
        {
            resumedByPlayerID_ = resumedByPlayerId;
        }

        public string ResumedByPlayerID() => resumedByPlayerID_;
    }

    /// <summary>
    /// プレイヤー参加イベント
    /// </summary>
    public class PlayerJoinedEvent : AbstractGameEvent
    {
        private string playerID_;
        private string playerName_;
        private ETeam team_;

        public PlayerJoinedEvent(string playerId, string playerName, ETeam team)
        {
            playerID_ = playerId;
            playerName_ = playerName;
            team_ = team;
        }

        public string PlayerID() => playerID_;
        public string PlayerName() => playerName_;
        public ETeam Team() => team_;
    }

    /// <summary>
    /// プレイヤー退出イベント
    /// </summary>
    public class PlayerLeftEvent : AbstractGameEvent
    {
        private string playerID_;
        private string reason_;

        public PlayerLeftEvent(string playerId, string reason = "unknown")
        {
            playerID_ = playerId;
            reason_ = reason;
        }

        public string PlayerID() => playerID_;
        public string Reason() => reason_;
    }

    /// <summary>
    /// チーム切り替えイベント
    /// </summary>
    public class PlayerTeamSwitchEvent : AbstractGameEvent
    {
        private string playerID_;
        private ETeam newTeam_;

        public PlayerTeamSwitchEvent(string playerId, ETeam newTeam)
        {
            playerID_ = playerId;
            newTeam_ = newTeam;
        }

        public string PlayerID() => playerID_;
        public ETeam NewTeam() => newTeam_;
    }

    /// <summary>
    /// 武器切り替えイベント
    /// </summary>
    public class WeaponChangeEvent : AbstractGameEvent
    {
        private string playerID_;
        private string weaponType_;
        private int slotIndex_;

        public WeaponChangeEvent(string playerId, string weaponType, int slotIndex)
        {
            playerID_ = playerId;
            weaponType_ = weaponType;
            slotIndex_ = slotIndex;
        }

        public string PlayerID() => playerID_;
        public string WeaponType() => weaponType_;
        public int SlotIndex() => slotIndex_;
    }

    /// <summary>
    /// アモ更新イベント
    /// </summary>
    public class AmmoUpdateEvent : AbstractGameEvent
    {
        private string playerID_;
        private string weaponType_;
        private int currentAmmo_;
        private int maxAmmo_;

        public AmmoUpdateEvent(string playerId, string weaponType, int currentAmmo, int maxAmmo)
        {
            playerID_ = playerId;
            weaponType_ = weaponType;
            currentAmmo_ = currentAmmo;
            maxAmmo_ = maxAmmo;
        }

        public string PlayerID() => playerID_;
        public string WeaponType() => weaponType_;
        public int CurrentAmmo() => currentAmmo_;
        public int MaxAmmo() => maxAmmo_;
    }

    /// <summary>
    /// グレネード投擲イベント
    /// </summary>
    public class GrenadeThrowEvent : AbstractGameEvent
    {
        private string playerID_;
        private Vector2 position_;
        private Vector2 direction_;
        private string grenadeType_;

        public GrenadeThrowEvent(string playerId, Vector2 position, Vector2 direction, string grenadeType)
        {
            playerID_ = playerId;
            position_ = position;
            direction_ = direction;
            grenadeType_ = grenadeType;
        }

        public string PlayerID() => playerID_;
        public Vector2 Position() => position_;
        public Vector2 Direction() => direction_;
        public string GrenadeType() => grenadeType_;
    }

    /// <summary>
    /// リロードイベント
    /// </summary>
    public class PlayerReloadEvent : AbstractGameEvent
    {
        private string playerID_;
        private string weaponType_;
        private bool isEmpty_;

        public PlayerReloadEvent(string playerId, string weaponType, bool isEmpty)
        {
            playerID_ = playerId;
            weaponType_ = weaponType;
            isEmpty_ = isEmpty;
        }

        public string PlayerID() => playerID_;
        public string WeaponType() => weaponType_;
        public bool IsEmpty() => isEmpty_;
    }

    /// <summary>
    /// 近接攻撃イベント
    /// </summary>
    public class PlayerMeleeEvent : AbstractGameEvent
    {
        private string playerID_;
        private Vector2 position_;
        private Vector2 direction_;
        private string weaponID_;

        public PlayerMeleeEvent(string playerId, Vector2 position, Vector2 direction, string weaponId)
        {
            playerID_ = playerId;
            position_ = position;
            direction_ = direction;
            weaponID_ = weaponId;
        }

        public string PlayerID() => playerID_;
        public Vector2 Position() => position_;
        public Vector2 Direction() => direction_;
        public string WeaponID() => weaponID_;
    }

    /// <summary>
    /// 投票イベント
    /// </summary>
    public class VoteEvent : AbstractGameEvent
    {
        private string voteID_;
        private string voteType_;
        private string initiatedBy_;
        private string targetID_;
        private int duration_;

        public VoteEvent(string voteId, string voteType, string initiatedBy, string targetId, int duration)
        {
            voteID_ = voteId;
            voteType_ = voteType;
            initiatedBy_ = initiatedBy;
            targetID_ = targetId;
            duration_ = duration;
        }

        public string VoteID() => voteID_;
        public string VoteType() => voteType_;
        public string InitiatedBy() => initiatedBy_;
        public string TargetID() => targetID_;
        public int Duration() => duration_;
    }

    /// <summary>
    /// 投票結果イベント
    /// </summary>
    public class VoteResultEvent : AbstractGameEvent
    {
        private string voteID_;
        private bool passed_;
        private string message_;

        public VoteResultEvent(string voteId, bool passed, string message = "")
        {
            voteID_ = voteId;
            passed_ = passed;
            message_ = message;
        }

        public string VoteID() => voteID_;
        public bool Passed() => passed_;
        public string Message() => message_;
    }

    /// <summary>
    /// スペクテイター遷移イベント
    /// </summary>
    public class PlayerSpectatingEvent : AbstractGameEvent
    {
        private string playerID_;
        private bool isSpectating_;

        public PlayerSpectatingEvent(string playerId, bool isSpectating)
        {
            playerID_ = playerId;
            isSpectating_ = isSpectating;
        }

        public string PlayerID() => playerID_;
        public bool IsSpectating() => isSpectating_;
    }

    /// <summary>
    /// 蘇生イベント
    /// </summary>
    public class PlayerReviveEvent : AbstractGameEvent
    {
        private string playerID_;
        private string revivedByPlayerID_;
        private Vector2 position_;

        public PlayerReviveEvent(string playerId, string revivedByPlayerId, Vector2 position)
        {
            playerID_ = playerId;
            revivedByPlayerID_ = revivedByPlayerId;
            position_ = position;
        }

        public string PlayerID() => playerID_;
        public string RevivedByPlayerID() => revivedByPlayerID_;
        public Vector2 Position() => position_;
    }

    /// <summary>
    /// バフ/デバフイベント
    /// </summary>
    public class BuffEvent : AbstractGameEvent
    {
        private string playerID_;
        private string buffType_;
        private int duration_;
        private float value_;
        private bool isDebuff_;

        public BuffEvent(string playerId, string buffType, int duration, float value, bool isDebuff = false)
        {
            playerID_ = playerId;
            buffType_ = buffType;
            duration_ = duration;
            value_ = value;
            isDebuff_ = isDebuff;
        }

        public string PlayerID() => playerID_;
        public string BuffType() => buffType_;
        public int Duration() => duration_;
        public float Value() => value_;
        public bool IsDebuff() => isDebuff_;
    }

    /// <summary>
    /// バフ期限切れイベント
    /// </summary>
    public class BuffExpiredEvent : AbstractGameEvent
    {
        private string playerID_;
        private string buffType_;

        public BuffExpiredEvent(string playerId, string buffType)
        {
            playerID_ = playerId;
            buffType_ = buffType;
        }

        public string PlayerID() => playerID_;
        public string BuffType() => buffType_;
    }

    /// <summary>
    /// オブジェクト出現イベント
    /// </summary>
    public class ObjectSpawnedEvent : AbstractGameEvent
    {
        private string objectID_;
        private string objectType_;
        private Vector2 position_;
        private float rotation_;

        public ObjectSpawnedEvent(string objectId, string objectType, Vector2 position, float rotation)
        {
            objectID_ = objectId;
            objectType_ = objectType;
            position_ = position;
            rotation_ = rotation;
        }

        public string ObjectID() => objectID_;
        public string ObjectType() => objectType_;
        public Vector2 Position() => position_;
        public float Rotation() => rotation_;
    }

    /// <summary>
    /// オブジェクト破壊イベント
    /// </summary>
    public class ObjectDestroyedEvent : AbstractGameEvent
    {
        private string objectID_;
        private string destroyedBy_;
        private Vector2 position_;

        public ObjectDestroyedEvent(string objectId, string destroyedBy, Vector2 position)
        {
            objectID_ = objectId;
            destroyedBy_ = destroyedBy;
            position_ = position;
        }

        public string ObjectID() => objectID_;
        public string DestroyedBy() => destroyedBy_;
        public Vector2 Position() => position_;
    }

    /// <summary>
    /// ピングイベント
    /// </summary>
    public class PingEvent : AbstractGameEvent
    {
        private string playerID_;
        private long clientTimestamp_;
        private long serverTimestamp_;

        public PingEvent(string playerId, long clientTimestamp, long serverTimestamp = 0)
        {
            playerID_ = playerId;
            clientTimestamp_ = clientTimestamp;
            serverTimestamp_ = serverTimestamp;
        }

        public string PlayerID() => playerID_;
        public long ClientTimestamp() => clientTimestamp_;
        public long ServerTimestamp() => serverTimestamp_;
    }

    /// <summary>
    /// ウォームアップイベント
    /// </summary>
    public class WarmupEvent : AbstractGameEvent
    {
        private bool isStart_;
        private int duration_;

        public WarmupEvent(bool isStart, int duration = 0)
        {
            isStart_ = isStart;
            duration_ = duration;
        }

        public bool IsStart() => isStart_;
        public int Duration() => duration_;
    }

    /// <summary>
    /// 時間同期イベント
    /// </summary>
    public class MatchTimeSyncEvent : AbstractGameEvent
    {
        private int remainingTime_;
        private long serverTimestamp_;

        public MatchTimeSyncEvent(int remainingTime, long serverTimestamp)
        {
            remainingTime_ = remainingTime;
            serverTimestamp_ = serverTimestamp;
        }

        public int RemainingTime() => remainingTime_;
        public long ServerTimestamp() => serverTimestamp_;
    }

}
