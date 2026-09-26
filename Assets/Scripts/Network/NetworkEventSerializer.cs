using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// ゲームイベントをRUDPメッセージに変換するシリアライザー
    /// クライアントauthoritativeモデル: イベント発生 → ネットワークメッセージ化 → サーバー送信
    /// </summary>
    public static class NetworkEventSerializer
    {
        private static Vector2 SafeVector(Vector2 value, Vector2 fallback)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) ? value : fallback;
        }

        private static float SafeFloat(float value, float fallback = 0f)
        {
            return float.IsFinite(value) ? value : fallback;
        }

        /// <summary>
        /// ゲームイベントをRUDPメッセージ（JObject）に変換
        /// </summary>
        public static JObject Serialize(AbstractGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                Debug.LogWarning("[NetworkEventSerializer] Serialize called with null event.");
                return new JObject
                {
                    ["MessageType"] = "GameEvent",
                    ["EventName"] = string.Empty,
                    ["Timestamp"] = DateTime.UtcNow.ToString("o")
                };
            }

            var eventType = gameEvent.GetType();
            JObject json = null;

            // イベントタイプ別のシリアライズ
            if (eventType == typeof(PlayerDeadEvent))
            {
                json = SerializePlayerDeadEvent((PlayerDeadEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerKillEvent))
            {
                json = SerializePlayerKillEvent((PlayerKillEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerAssistEvent))
            {
                json = SerializePlayerAssistEvent((PlayerAssistEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerShotEvent))
            {
                json = SerializePlayerShotEvent((PlayerShotEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerDamageEvent))
            {
                json = SerializePlayerDamageEvent((PlayerDamageEvent)gameEvent);
            }
            else if (eventType == typeof(ScoreUpdateEvent))
            {
                json = SerializeScoreUpdateEvent((ScoreUpdateEvent)gameEvent);
            }
            else if (eventType == typeof(FlagScoreUpdateEvent))
            {
                json = SerializeFlagScoreUpdateEvent((FlagScoreUpdateEvent)gameEvent);
            }
            else if (eventType == typeof(StreakUpdateEvent))
            {
                json = SerializeStreakUpdateEvent((StreakUpdateEvent)gameEvent);
            }
            else if (eventType == typeof(FlagEvent))
            {
                json = SerializeFlagEvent((FlagEvent)gameEvent);
            }
            // システム系イベント
            else if (eventType == typeof(PlayerRespawnEvent))
            {
                json = SerializePlayerRespawnEvent((PlayerRespawnEvent)gameEvent);
            }
            else if (eventType == typeof(RespawnCountdownEvent))
            {
                json = SerializeRespawnCountdownEvent((RespawnCountdownEvent)gameEvent);
            }
            else if (eventType == typeof(RoundStartEvent))
            {
                json = SerializeRoundStartEvent((RoundStartEvent)gameEvent);
            }
            else if (eventType == typeof(RoundEndEvent))
            {
                json = SerializeRoundEndEvent((RoundEndEvent)gameEvent);
            }
            else if (eventType == typeof(MatchPauseEvent))
            {
                json = SerializeMatchPauseEvent((MatchPauseEvent)gameEvent);
            }
            else if (eventType == typeof(MatchResumeEvent))
            {
                json = SerializeMatchResumeEvent((MatchResumeEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerJoinedEvent))
            {
                json = SerializePlayerJoinedEvent((PlayerJoinedEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerLeftEvent))
            {
                json = SerializePlayerLeftEvent((PlayerLeftEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerTeamSwitchEvent))
            {
                json = SerializePlayerTeamSwitchEvent((PlayerTeamSwitchEvent)gameEvent);
            }
            else if (eventType == typeof(WeaponChangeEvent))
            {
                json = SerializeWeaponChangeEvent((WeaponChangeEvent)gameEvent);
            }
            else if (eventType == typeof(AmmoUpdateEvent))
            {
                json = SerializeAmmoUpdateEvent((AmmoUpdateEvent)gameEvent);
            }
            else if (eventType == typeof(GrenadeThrowEvent))
            {
                json = SerializeGrenadeThrowEvent((GrenadeThrowEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerReloadEvent))
            {
                json = SerializePlayerReloadEvent((PlayerReloadEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerMeleeEvent))
            {
                json = SerializePlayerMeleeEvent((PlayerMeleeEvent)gameEvent);
            }
            else if (eventType == typeof(VoteEvent))
            {
                json = SerializeVoteEvent((VoteEvent)gameEvent);
            }
            else if (eventType == typeof(VoteResultEvent))
            {
                json = SerializeVoteResultEvent((VoteResultEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerSpectatingEvent))
            {
                json = SerializePlayerSpectatingEvent((PlayerSpectatingEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerPoseEvent))
            {
                json = SerializePlayerPoseEvent((PlayerPoseEvent)gameEvent);
            }
            else if (eventType == typeof(PlayerReviveEvent))
            {
                json = SerializePlayerReviveEvent((PlayerReviveEvent)gameEvent);
            }
            else if (eventType == typeof(BuffEvent))
            {
                json = SerializeBuffEvent((BuffEvent)gameEvent);
            }
            else if (eventType == typeof(BuffExpiredEvent))
            {
                json = SerializeBuffExpiredEvent((BuffExpiredEvent)gameEvent);
            }
            else if (eventType == typeof(ObjectSpawnedEvent))
            {
                json = SerializeObjectSpawnedEvent((ObjectSpawnedEvent)gameEvent);
            }
            else if (eventType == typeof(ObjectDestroyedEvent))
            {
                json = SerializeObjectDestroyedEvent((ObjectDestroyedEvent)gameEvent);
            }
            else if (eventType == typeof(ItemPickupEvent))
            {
                json = SerializeItemPickupEvent((ItemPickupEvent)gameEvent);
            }
            else if (eventType == typeof(PingEvent))
            {
                json = SerializePingEvent((PingEvent)gameEvent);
            }
            else if (eventType == typeof(WarmupEvent))
            {
                json = SerializeWarmupEvent((WarmupEvent)gameEvent);
            }
            else if (eventType == typeof(MatchTimeSyncEvent))
            {
                json = SerializeMatchTimeSyncEvent((MatchTimeSyncEvent)gameEvent);
            }
            else
            {
                // 未知のイベントタイプ
                json = new JObject
                {
                    ["MessageType"] = "GameEvent",
                    ["EventName"] = gameEvent.EventName,
                    ["Timestamp"] = gameEvent.Timestamp.ToString("o")
                };
            }

            return NormalizeOutgoingKeys(json);
        }

        /// <summary>
        /// プレイヤ死亡イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerDeadEvent(PlayerDeadEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerDeath;
            json["PlayerId"] = e.PlayerID();
            json["KillerId"] = e.KillerID();
            json["Reason"] = e.Reason().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// プレイヤーキルイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerKillEvent(PlayerKillEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerKill;
            json["KillerId"] = e.KillerID();
            json["VictimId"] = e.VictimID();
            json["WeaponType"] = e.WeaponType() ?? "Unknown";
            json["Headshot"] = e.IsHeadshot();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// アシストイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerAssistEvent(PlayerAssistEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerAssist;
            json["AssisterId"] = e.AssisterID();
            json["VictimId"] = e.VictimID();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 射撃イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerShotEvent(PlayerShotEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            var direction = SafeVector(e.Direction(), Vector2.right);
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerShot;
            json["PlayerId"] = e.PlayerID();
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["DirX"] = direction.x;
            json["DirY"] = direction.y;
            json["WeaponType"] = e.WeaponType() ?? "Unknown";
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// ダメージイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerDamageEvent(PlayerDamageEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerDamage;
            json["TargetId"] = e.TargetID();
            json["AttackerId"] = e.AttackerID();
            json["Damage"] = Mathf.Max(0, e.Damage());
            json["RemainingHp"] = Mathf.Max(0, e.RemainingHp());
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// スコア更新イベントをシリアライズ
        /// </summary>
        private static JObject SerializeScoreUpdateEvent(ScoreUpdateEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.KillScoreUpdate;
            json["PlayerId"] = e.PlayerID();
            json["Kills"] = Mathf.Max(0, e.Kills());
            json["Deaths"] = Mathf.Max(0, e.Deaths());
            json["Score"] = Mathf.Max(0, e.Score());
            json["Team"] = e.Team().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// フラッグイベントをシリアライズ
        /// </summary>
        private static JObject SerializeFlagEvent(FlagEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            string messageType = e.FlagEventType() switch
            {
                EFlagEventType.Captured => RUDPMessageTypes.FlagCaptured,
                EFlagEventType.Lost => RUDPMessageTypes.FlagLost,
                EFlagEventType.Returned => RUDPMessageTypes.FlagReturn,
                EFlagEventType.Pickup => RUDPMessageTypes.FlagPickup,
                EFlagEventType.Burst => RUDPMessageTypes.FlagBurst,
                _ => RUDPMessageTypes.FlagPickup
            };

            return new JObject
            {
                ["MessageType"] = messageType,
                ["PlayerId"] = e.PlayerID(),
                ["Team"] = e.Team().ToString(),
                ["FlagEventType"] = e.FlagEventType().ToString(),
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeRoundStartEvent(RoundStartEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.RoundStart,
                ["RoundNumber"] = e.RoundNumber(),
                ["TotalRounds"] = e.TotalRounds(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeRoundEndEvent(RoundEndEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.RoundEnd,
                ["WinningTeam"] = e.WinningTeam(),
                ["RoundNumber"] = e.RoundNumber(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeMatchPauseEvent(MatchPauseEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.MatchPause,
                ["PausedBy"] = e.PausedByPlayerID(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeMatchResumeEvent(MatchResumeEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.MatchResume,
                ["ResumedBy"] = e.ResumedByPlayerID(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeAmmoUpdateEvent(AmmoUpdateEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.AmmoUpdate,
                ["PlayerId"] = e.PlayerID(),
                ["WeaponType"] = e.WeaponType(),
                ["CurrentAmmo"] = Mathf.Max(0, e.CurrentAmmo()),
                ["MaxAmmo"] = Mathf.Max(0, e.MaxAmmo()),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializePlayerMeleeEvent(PlayerMeleeEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            var direction = SafeVector(e.Direction(), Vector2.right);
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerMelee,
                ["PlayerId"] = e.PlayerID(),
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["DirX"] = direction.x,
                ["DirY"] = direction.y,
                ["WeaponId"] = e.WeaponID(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeBuffEvent(BuffEvent e)
        {
            var duration = e.Duration();
            var value = e.Value();
            return new JObject
            {
                ["MessageType"] = e.IsDebuff() ? RUDPMessageTypes.PlayerDebuff : RUDPMessageTypes.PlayerBuff,
                ["PlayerId"] = e.PlayerID(),
                ["BuffType"] = e.BuffType(),
                ["Duration"] = duration < 0 ? 0 : duration,
                ["Value"] = SafeFloat(value),
                ["IsDebuff"] = e.IsDebuff(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeObjectSpawnedEvent(ObjectSpawnedEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ObjectSpawned,
                ["ObjectId"] = e.ObjectID(),
                ["ObjectType"] = e.ObjectType(),
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["Rotation"] = SafeFloat(e.Rotation()),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeObjectDestroyedEvent(ObjectDestroyedEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ObjectDestroyed,
                ["ObjectId"] = e.ObjectID(),
                ["DestroyedBy"] = e.DestroyedBy(),
                ["PosX"] = position.x,
                ["PosY"] = position.y,
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeItemPickupEvent(ItemPickupEvent e)
        {
            return RUDPMessageBuilder.CreateItemPickup(
                e.PlayerID(),
                e.SpawnPointId() >= 0 ? e.SpawnPointId().ToString() : e.ItemType(),
                e.ItemType(),
                e.Position(),
                e.SpawnPointId(),
                e.EffectValue(),
                e.DurationSeconds());
        }

        private static JObject SerializeWarmupEvent(WarmupEvent e)
        {
            return new JObject
            {
                ["MessageType"] = e.IsStart() ? RUDPMessageTypes.WarmupStart : RUDPMessageTypes.WarmupEnd,
                ["IsStart"] = e.IsStart(),
                ["Duration"] = e.Duration(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        #region システム系イベントシリアライズ

        /// <summary>
        /// リスポーンイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerRespawnEvent(PlayerRespawnEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRespawn;
            json["PlayerId"] = e.PlayerID();
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// リスポーンカウントダウンイベントをシリアライズ
        /// </summary>
        private static JObject SerializeRespawnCountdownEvent(RespawnCountdownEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.RespawnCountdown;
            json["PlayerId"] = e.PlayerID();
            json["Countdown"] = Mathf.Max(0, e.CountdownSeconds());
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

    

        /// <summary>
        /// プレイヤー参加イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerJoinedEvent(PlayerJoinedEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerJoined;
            json["PlayerId"] = e.PlayerID();
            json["PlayerName"] = e.PlayerName();
            json["Team"] = e.Team().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// プレイヤー退出イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerLeftEvent(PlayerLeftEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerLeft;
            json["PlayerId"] = e.PlayerID();
            json["Reason"] = e.Reason();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// チーム切り替えイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerTeamSwitchEvent(PlayerTeamSwitchEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerTeamSwitch;
            json["PlayerId"] = e.PlayerID();
            json["NewTeam"] = e.NewTeam().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 武器切り替えイベントをシリアライズ
        /// </summary>
        private static JObject SerializeWeaponChangeEvent(WeaponChangeEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.WeaponChange;
            json["PlayerId"] = e.PlayerID();
            json["WeaponType"] = e.WeaponType();
            json["SlotIndex"] = e.SlotIndex();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }


        /// <summary>
        /// グレネード投擲イベントをシリアライズ
        /// </summary>
        private static JObject SerializeGrenadeThrowEvent(GrenadeThrowEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            var direction = SafeVector(e.Direction(), Vector2.right);
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.GrenadeThrow;
            json["PlayerID"] = e.PlayerID();
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["DirX"] = direction.x;
            json["DirY"] = direction.y;
            json["GrenadeType"] = e.GrenadeType();
            json["Power"] = Mathf.Max(0f, SafeFloat(e.Power(), 1f));
            json["Timestamp"] = e.Timestamp.ToString("o");
            return NormalizeOutgoingKeys(json);
        }

        /// <summary>
        /// 送信境界で、旧来の単数形キーを正規プロトコル名へ寄せる。
        /// 受信側の互換読み取りは維持しつつ、新規送信は常に canonical key を使う。
        /// </summary>
        private static JObject NormalizeOutgoingKeys(JObject json)
        {
            if (json == null)
            {
                return new JObject();
            }

            if (json["PlayerID"] == null && json["PlayerId"] != null)
            {
                json["PlayerID"] = json["PlayerId"];
                json.Remove("PlayerId");
            }

            if (json["RoomID"] == null && json["RoomId"] != null)
            {
                json["RoomID"] = json["RoomId"];
                json.Remove("RoomId");
            }

            return json;
        }

        private static JObject SerializeFlagScoreUpdateEvent(FlagScoreUpdateEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.FlagScoreUpdate,
                ["EventKey"] = e.EventKey(),
                ["RedTeamScore"] = Mathf.Max(0, e.RedTeamScore()),
                ["BlueTeamScore"] = Mathf.Max(0, e.BlueTeamScore()),
                ["RedTeamFlagScore"] = Mathf.Max(0, e.RedTeamScore()),
                ["BlueTeamFlagScore"] = Mathf.Max(0, e.BlueTeamScore()),
                ["RedTeamFlags"] = Mathf.Max(0, e.RedTeamFlags()),
                ["BlueTeamFlags"] = Mathf.Max(0, e.BlueTeamFlags()),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeStreakUpdateEvent(StreakUpdateEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.StreakUpdate,
                ["PlayerId"] = e.PlayerID(),
                ["StreakCount"] = Mathf.Max(0, e.StreakCount()),
                ["StreakType"] = e.StreakType(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        /// <summary>
        /// リロードイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerReloadEvent(PlayerReloadEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerReload;
            json["PlayerID"] = e.PlayerID();
            json["WeaponType"] = e.WeaponType();
            json["IsEmpty"] = e.IsEmpty();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 近接攻撃イベントをシリアライズ
        /// </summary>


        /// <summary>
        /// 投票イベントをシリアライズ
        /// </summary>
        private static JObject SerializeVoteEvent(VoteEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.VoteStart;
            json["VoteId"] = e.VoteID();
            json["VoteType"] = e.VoteType();
            json["InitiatedBy"] = e.InitiatedBy();
            json["TargetId"] = e.TargetID();
            json["Duration"] = e.Duration();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 投票結果イベントをシリアライズ
        /// </summary>
        private static JObject SerializeVoteResultEvent(VoteResultEvent e)
        {
            var json = new JObject();
            json["MessageType"] = e.Passed() ? RUDPMessageTypes.VotePassed : RUDPMessageTypes.VoteFailed;
            json["VoteId"] = e.VoteID();
            json["Message"] = e.Message();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// スペクテイター遷移イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerSpectatingEvent(PlayerSpectatingEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerSpectating;
            json["PlayerID"] = e.PlayerID();
            json["IsSpectating"] = e.IsSpectating();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        private static JObject SerializePlayerPoseEvent(PlayerPoseEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerPose;
            json["PlayerID"] = e.PlayerID();
            json["PoseState"] = e.PoseState().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 蘇生イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerReviveEvent(PlayerReviveEvent e)
        {
            var position = SafeVector(e.Position(), Vector2.zero);
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRevive;
            json["PlayerId"] = e.PlayerID();
            json["RevivedBy"] = e.RevivedByPlayerID();
            json["PosX"] = position.x;
            json["PosY"] = position.y;
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// バフ/デバフイベントをシリアライズ
        /// </summary>

        /// <summary>
        /// バフ期限切れイベントをシリアライズ
        /// </summary>
        private static JObject SerializeBuffExpiredEvent(BuffExpiredEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.BuffExpired;
            json["PlayerId"] = e.PlayerID();
            json["BuffType"] = e.BuffType();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }



        /// <summary>
        /// ピングイベントをシリアライズ
        /// </summary>
        private static JObject SerializePingEvent(PingEvent e)
        {
            var json = new JObject();
            json["MessageType"] = e.ServerTimestamp() > 0 ? RUDPMessageTypes.PingResponse : RUDPMessageTypes.PingRequest;
            json["PlayerId"] = e.PlayerID();
            json["ClientTimestamp"] = e.ClientTimestamp();
            if (e.ServerTimestamp() > 0)
            {
                json["ServerTimestamp"] = e.ServerTimestamp();
            }
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }



        /// <summary>
        /// 時間同期イベントをシリアライズ
        /// </summary>
        private static JObject SerializeMatchTimeSyncEvent(MatchTimeSyncEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.MatchTimeSync;
            json["RemainingTime"] = Mathf.Max(0, e.RemainingTime());
            json["ServerTimestamp"] = e.ServerTimestamp();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        #endregion

        /// <summary>
        /// イベントをシリアライズしてサーバーに送信
        /// </summary>
        public static void SerializeAndSend(AbstractGameEvent gameEvent)
        {
            var json = Serialize(gameEvent);
            if (json != null)
            {
                try
                {
                    var networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
                    if (networkManager != null && networkManager.IsConnected())
                    {
                        networkManager.SendToServer(json);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkEventSerializer] Failed to send: {ex.Message}");
                }
            }
        }
    }
}
