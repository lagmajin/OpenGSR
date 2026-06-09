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

            return json;
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
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerShot;
            json["PlayerId"] = e.PlayerID();
            json["PosX"] = e.Position().x;
            json["PosY"] = e.Position().y;
            json["DirX"] = e.Direction().x;
            json["DirY"] = e.Direction().y;
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
            json["Damage"] = e.Damage();
            json["RemainingHp"] = e.RemainingHp();
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
            json["Kills"] = e.Kills();
            json["Deaths"] = e.Deaths();
            json["Score"] = e.Score();
            json["Team"] = e.Team().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// フラッグイベントをシリアライズ
        /// </summary>
        private static JObject SerializeFlagEvent(FlagEvent e)
        {
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
                ["PosX"] = e.Position().x,
                ["PosY"] = e.Position().y,
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
                ["CurrentAmmo"] = e.CurrentAmmo(),
                ["MaxAmmo"] = e.MaxAmmo(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializePlayerMeleeEvent(PlayerMeleeEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerMelee,
                ["PlayerId"] = e.PlayerID(),
                ["PosX"] = e.Position().x,
                ["PosY"] = e.Position().y,
                ["DirX"] = e.Direction().x,
                ["DirY"] = e.Direction().y,
                ["WeaponId"] = e.WeaponID(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeBuffEvent(BuffEvent e)
        {
            return new JObject
            {
                ["MessageType"] = e.IsDebuff() ? RUDPMessageTypes.PlayerDebuff : RUDPMessageTypes.PlayerBuff,
                ["PlayerId"] = e.PlayerID(),
                ["BuffType"] = e.BuffType(),
                ["Duration"] = e.Duration(),
                ["Value"] = e.Value(),
                ["IsDebuff"] = e.IsDebuff(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeObjectSpawnedEvent(ObjectSpawnedEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ObjectSpawned,
                ["ObjectId"] = e.ObjectID(),
                ["ObjectType"] = e.ObjectType(),
                ["PosX"] = e.Position().x,
                ["PosY"] = e.Position().y,
                ["Rotation"] = e.Rotation(),
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
        }

        private static JObject SerializeObjectDestroyedEvent(ObjectDestroyedEvent e)
        {
            return new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ObjectDestroyed,
                ["ObjectId"] = e.ObjectID(),
                ["DestroyedBy"] = e.DestroyedBy(),
                ["PosX"] = e.Position().x,
                ["PosY"] = e.Position().y,
                ["Timestamp"] = e.Timestamp.ToString("o")
            };
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
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRespawn;
            json["PlayerId"] = e.PlayerID();
            json["PosX"] = e.Position().x;
            json["PosY"] = e.Position().y;
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
            json["Countdown"] = e.CountdownSeconds();
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
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.GrenadeThrow;
            json["PlayerId"] = e.PlayerID();
            json["PosX"] = e.Position().x;
            json["PosY"] = e.Position().y;
            json["DirX"] = e.Direction().x;
            json["DirY"] = e.Direction().y;
            json["GrenadeType"] = e.GrenadeType();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// リロードイベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerReloadEvent(PlayerReloadEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerReload;
            json["PlayerId"] = e.PlayerID();
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
            json["PlayerId"] = e.PlayerID();
            json["IsSpectating"] = e.IsSpectating();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        private static JObject SerializePlayerPoseEvent(PlayerPoseEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerPose;
            json["PlayerId"] = e.PlayerID();
            json["PoseState"] = e.PoseState().ToString();
            json["Timestamp"] = e.Timestamp.ToString("o");
            return json;
        }

        /// <summary>
        /// 蘇生イベントをシリアライズ
        /// </summary>
        private static JObject SerializePlayerReviveEvent(PlayerReviveEvent e)
        {
            var json = new JObject();
            json["MessageType"] = RUDPMessageTypes.PlayerRevive;
            json["PlayerId"] = e.PlayerID();
            json["RevivedBy"] = e.RevivedByPlayerID();
            json["PosX"] = e.Position().x;
            json["PosY"] = e.Position().y;
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
            json["RemainingTime"] = e.RemainingTime();
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
                        Debug.Log($"[NetworkEventSerializer] Sent: {json["MessageType"]}");
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
