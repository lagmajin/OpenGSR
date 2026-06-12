using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// RUDP/TCP で受信した JObject (JSON) をゲームイベント (AbstractGameEvent) に復元するデシリアライザー。
    /// NetworkEventSerializer.Serialize() の逆変換を担当する。
    ///
    /// 【使い方】
    ///   JObject json = ...; // ネットワークから受信した JSON
    ///   AbstractGameEvent evt = NetworkEventDeserializer.Deserialize(json);
    ///   if (evt is PlayerKillEvent kill) { ... }
    /// </summary>
    public static class NetworkEventDeserializer
    {
        /// <summary>
        /// JObject の "MessageType" フィールドを見て、対応する AbstractGameEvent に復元する。
        /// 対応するメッセージタイプが無い場合は null を返す。
        /// </summary>
        public static AbstractGameEvent Deserialize(JObject json)
        {
            if (json == null)
            {
                Debug.LogWarning("[NetworkEventDeserializer] Deserialize called with null JSON.");
                return null;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning($"[NetworkEventDeserializer] Missing MessageType in JSON: {json}");
                return null;
            }

            return messageType switch
            {
                // ゲームプレイ系
                RUDPMessageTypes.PlayerDeath     => DeserializePlayerDead(json),
                RUDPMessageTypes.PlayerKill      => DeserializePlayerKill(json),
                RUDPMessageTypes.PlayerAssist    => DeserializePlayerAssist(json),
                RUDPMessageTypes.PlayerShot      => DeserializePlayerShot(json),
                RUDPMessageTypes.PlayerDamage    => DeserializePlayerDamage(json),
                RUDPMessageTypes.KillScoreUpdate => DeserializeScoreUpdate(json),

                // リスポーン
                RUDPMessageTypes.PlayerRespawn     => DeserializePlayerRespawn(json),
                RUDPMessageTypes.RespawnCountdown  => DeserializeRespawnCountdown(json),

                // ラウンド・マッチ
                RUDPMessageTypes.RoundStart   => DeserializeRoundStart(json),
                RUDPMessageTypes.RoundEnd     => DeserializeRoundEnd(json),
                RUDPMessageTypes.MatchPause   => DeserializeMatchPause(json),
                RUDPMessageTypes.MatchResume  => DeserializeMatchResume(json),
                RUDPMessageTypes.MatchTimeSync => DeserializeMatchTimeSync(json),
                RUDPMessageTypes.WarmupStart  => DeserializeWarmup(json, true),
                RUDPMessageTypes.WarmupEnd    => DeserializeWarmup(json, false),

                // プレイヤー状態
                RUDPMessageTypes.PlayerJoined      => DeserializePlayerJoined(json),
                RUDPMessageTypes.PlayerLeft        => DeserializePlayerLeft(json),
                RUDPMessageTypes.PlayerTeamSwitch   => DeserializePlayerTeamSwitch(json),
                RUDPMessageTypes.PlayerSpectating   => DeserializePlayerSpectating(json),
                RUDPMessageTypes.PlayerPose        => DeserializePlayerPose(json),
                RUDPMessageTypes.PlayerRevive       => DeserializePlayerRevive(json),

                // 武器関連
                RUDPMessageTypes.WeaponChange   => DeserializeWeaponChange(json),
                RUDPMessageTypes.AmmoUpdate     => DeserializeAmmoUpdate(json),
                RUDPMessageTypes.GrenadeThrow   => DeserializeGrenadeThrow(json),
                RUDPMessageTypes.PlayerReload   => DeserializePlayerReload(json),
                RUDPMessageTypes.PlayerMelee    => DeserializePlayerMelee(json),

                // 投票
                RUDPMessageTypes.VoteStart  => DeserializeVoteEvent(json),
                RUDPMessageTypes.VotePassed => DeserializeVoteResult(json, true),
                RUDPMessageTypes.VoteFailed => DeserializeVoteResult(json, false),

                // バフ
                RUDPMessageTypes.PlayerBuff  => DeserializeBuff(json, false),
                RUDPMessageTypes.PlayerDebuff => DeserializeBuff(json, true),
                RUDPMessageTypes.BuffExpired  => DeserializeBuffExpired(json),

                // オブジェクト
                RUDPMessageTypes.ObjectSpawned   => DeserializeObjectSpawned(json),
                RUDPMessageTypes.ObjectDestroyed => DeserializeObjectDestroyed(json),

                // Ping
                RUDPMessageTypes.PingRequest  => DeserializePing(json),
                RUDPMessageTypes.PingResponse => DeserializePing(json),

                // CTF (Flag) 関連
                RUDPMessageTypes.FlagCaptured => DeserializeFlagEvent(json, EFlagEventType.Captured),
                RUDPMessageTypes.FlagLost => DeserializeFlagEvent(json, EFlagEventType.Lost),
                RUDPMessageTypes.FlagReturn => DeserializeFlagEvent(json, EFlagEventType.Returned),
                RUDPMessageTypes.FlagBurst => DeserializeFlagEvent(json, EFlagEventType.Burst),
                RUDPMessageTypes.FlagPickup => DeserializeFlagEvent(json, EFlagEventType.Pickup),

                // 未対応
                _ => null
            };
        }

        // ─── 個別デシリアライズメソッド ──────────────────────────────

        private static PlayerDeadEvent DeserializePlayerDead(JObject json)
        {
            var playerId = S(json, "PlayerId");
            var killerId = S(json, "KillerId");
            var reasonStr = S(json, "Reason");

            Enum.TryParse<EDeadReason>(reasonStr, out var reason);

            var evt = new PlayerDeadEvent(reason, "", playerId, ETeam.NoTeam);
            if (!string.IsNullOrEmpty(killerId))
                evt.SetKillerID(killerId);
            return evt;
        }

        private static PlayerKillEvent DeserializePlayerKill(JObject json)
        {
            return new PlayerKillEvent(
                S(json, "KillerId"),
                S(json, "VictimId"),
                S(json, "WeaponType", "Unknown"),
                B(json, "Headshot")
            );
        }

        private static PlayerAssistEvent DeserializePlayerAssist(JObject json)
        {
            return new PlayerAssistEvent(S(json, "AssisterId"), S(json, "VictimId"));
        }

        private static PlayerShotEvent DeserializePlayerShot(JObject json)
        {
            return new PlayerShotEvent(
                S(json, "PlayerId"),
                new Vector2(F(json, "PosX"), F(json, "PosY")),
                new Vector2(F(json, "DirX"), F(json, "DirY")),
                S(json, "WeaponType", "Unknown")
            );
        }

        private static PlayerDamageEvent DeserializePlayerDamage(JObject json)
        {
            return new PlayerDamageEvent(
                S(json, "TargetId"),
                S(json, "AttackerId"),
                I(json, "Damage"),
                I(json, "RemainingHp")
            );
        }

        private static ScoreUpdateEvent DeserializeScoreUpdate(JObject json)
        {
            Enum.TryParse<ETeam>(S(json, "Team"), out var team);
            return new ScoreUpdateEvent(
                S(json, "PlayerId"),
                I(json, "Kills"),
                I(json, "Deaths"),
                I(json, "Score"),
                team
            );
        }

        private static PlayerRespawnEvent DeserializePlayerRespawn(JObject json)
        {
            return new PlayerRespawnEvent(
                S(json, "PlayerId"),
                new Vector2(F(json, "PosX"), F(json, "PosY"))
            );
        }

        private static RespawnCountdownEvent DeserializeRespawnCountdown(JObject json)
        {
            return new RespawnCountdownEvent(S(json, "PlayerId"), I(json, "Countdown"));
        }

        private static RoundStartEvent DeserializeRoundStart(JObject json)
        {
            return new RoundStartEvent(I(json, "RoundNumber"), I(json, "TotalRounds"));
        }

        private static RoundEndEvent DeserializeRoundEnd(JObject json)
        {
            return new RoundEndEvent(S(json, "WinningTeam"), I(json, "RoundNumber"));
        }

        private static MatchPauseEvent DeserializeMatchPause(JObject json)
        {
            return new MatchPauseEvent(S(json, "PausedBy"));
        }

        private static MatchResumeEvent DeserializeMatchResume(JObject json)
        {
            return new MatchResumeEvent(S(json, "ResumedBy"));
        }

        private static MatchTimeSyncEvent DeserializeMatchTimeSync(JObject json)
        {
            return new MatchTimeSyncEvent(I(json, "RemainingTime"), L(json, "ServerTimestamp"));
        }

        private static WarmupEvent DeserializeWarmup(JObject json, bool isStart)
        {
            return new WarmupEvent(isStart, I(json, "Duration"));
        }

        private static PlayerJoinedEvent DeserializePlayerJoined(JObject json)
        {
            Enum.TryParse<ETeam>(S(json, "Team"), out var team);
            return new PlayerJoinedEvent(S(json, "PlayerId"), S(json, "PlayerName"), team);
        }

        private static PlayerLeftEvent DeserializePlayerLeft(JObject json)
        {
            return new PlayerLeftEvent(S(json, "PlayerId"), S(json, "Reason", "unknown"));
        }

        private static PlayerTeamSwitchEvent DeserializePlayerTeamSwitch(JObject json)
        {
            Enum.TryParse<ETeam>(S(json, "NewTeam"), out var team);
            return new PlayerTeamSwitchEvent(S(json, "PlayerId"), team);
        }

        private static PlayerSpectatingEvent DeserializePlayerSpectating(JObject json)
        {
            return new PlayerSpectatingEvent(S(json, "PlayerId"), B(json, "IsSpectating"));
        }

        private static PlayerPoseEvent DeserializePlayerPose(JObject json)
        {
            Enum.TryParse<EPlayerPoseState>(S(json, "PoseState"), out var poseState);
            return new PlayerPoseEvent(S(json, "PlayerId"), poseState);
        }

        private static PlayerReviveEvent DeserializePlayerRevive(JObject json)
        {
            return new PlayerReviveEvent(
                S(json, "PlayerId"),
                S(json, "RevivedBy"),
                new Vector2(F(json, "PosX"), F(json, "PosY"))
            );
        }

        private static WeaponChangeEvent DeserializeWeaponChange(JObject json)
        {
            return new WeaponChangeEvent(S(json, "PlayerId"), S(json, "WeaponType"), I(json, "SlotIndex"));
        }

        private static AmmoUpdateEvent DeserializeAmmoUpdate(JObject json)
        {
            return new AmmoUpdateEvent(S(json, "PlayerId"), S(json, "WeaponType"), I(json, "CurrentAmmo"), I(json, "MaxAmmo"));
        }

        private static GrenadeThrowEvent DeserializeGrenadeThrow(JObject json)
        {
            return new GrenadeThrowEvent(
                S(json, "PlayerId"),
                new Vector2(F(json, "PosX"), F(json, "PosY")),
                new Vector2(F(json, "DirX"), F(json, "DirY")),
                S(json, "GrenadeType"),
                F(json, "Power", 1f)
            );
        }

        private static PlayerReloadEvent DeserializePlayerReload(JObject json)
        {
            return new PlayerReloadEvent(S(json, "PlayerId"), S(json, "WeaponType"), B(json, "IsEmpty"));
        }

        private static PlayerMeleeEvent DeserializePlayerMelee(JObject json)
        {
            return new PlayerMeleeEvent(
                S(json, "PlayerId"),
                new Vector2(F(json, "PosX"), F(json, "PosY")),
                new Vector2(F(json, "DirX"), F(json, "DirY")),
                S(json, "WeaponId")
            );
        }

        private static VoteEvent DeserializeVoteEvent(JObject json)
        {
            return new VoteEvent(
                S(json, "VoteId"),
                S(json, "VoteType"),
                S(json, "InitiatedBy"),
                S(json, "TargetId"),
                I(json, "Duration")
            );
        }

        private static VoteResultEvent DeserializeVoteResult(JObject json, bool passed)
        {
            return new VoteResultEvent(S(json, "VoteId"), passed, S(json, "Message"));
        }

        private static BuffEvent DeserializeBuff(JObject json, bool isDebuff)
        {
            return new BuffEvent(
                S(json, "PlayerId"),
                S(json, "BuffType"),
                I(json, "Duration"),
                F(json, "Value"),
                isDebuff
            );
        }

        private static BuffExpiredEvent DeserializeBuffExpired(JObject json)
        {
            return new BuffExpiredEvent(S(json, "PlayerId"), S(json, "BuffType"));
        }

        private static ObjectSpawnedEvent DeserializeObjectSpawned(JObject json)
        {
            return new ObjectSpawnedEvent(
                S(json, "ObjectId"),
                S(json, "ObjectType"),
                new Vector2(F(json, "PosX"), F(json, "PosY")),
                F(json, "Rotation")
            );
        }

        private static ObjectDestroyedEvent DeserializeObjectDestroyed(JObject json)
        {
            return new ObjectDestroyedEvent(
                S(json, "ObjectId"),
                S(json, "DestroyedBy"),
                new Vector2(F(json, "PosX"), F(json, "PosY"))
            );
        }

        private static PingEvent DeserializePing(JObject json)
        {
            return new PingEvent(
                S(json, "PlayerId"),
                L(json, "ClientTimestamp"),
                L(json, "ServerTimestamp")
            );
        }

        // ─── CTF (Flag) イベント ──────────────────────────────────────────

        private static FlagEvent DeserializeFlagEvent(JObject json, EFlagEventType eventType)
        {
            var playerId = S(json, "PlayerId", "");
            var teamStr = S(json, "Team", "Red");
            Enum.TryParse<ETeam>(teamStr, out var team);
            var position = new Vector2(F(json, "PosX"), F(json, "PosY"));

            return new FlagEvent(playerId, team, eventType, position);
        }

        // ─── JSON ヘルパー (null-safe) ──────────────────────────────

        private static string S(JObject json, string key, string fallback = "")
            => json[key]?.ToString() ?? fallback;

        private static int I(JObject json, string key, int fallback = 0)
            => json[key]?.ToObject<int>() ?? fallback;

        private static float F(JObject json, string key, float fallback = 0f)
            => json[key]?.ToObject<float>() ?? fallback;

        private static long L(JObject json, string key, long fallback = 0)
            => json[key]?.ToObject<long>() ?? fallback;

        private static bool B(JObject json, string key, bool fallback = false)
            => json[key]?.ToObject<bool>() ?? fallback;
    }
}
