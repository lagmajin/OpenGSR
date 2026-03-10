using System;
using System.Collections.Generic;
using UnityEngine;
//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using Zenject;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class TDMMatchMainScript : AbstractMatchMainScript, ITDMMatchMainScript
    {
        // Singleton instance for UI to access
        public static TDMMatchMainScript Instance { get; private set; }

        // TDM Events for UI
        public event Action<ETeam> OnPlayerKilled; // プレイヤーKill通知
        public event Action<ETeam, ETeam> OnTeamKill; // チームがキル獲った通知 (killerTeam, victimTeam)
        public event Action<ETeam> OnMatchEnded; // マッチ終了通知

        // ネットワークマネージャー
        private MatchRUDPServerNetworkManager networkManager;

        // UIマネージャー
        [SerializeField] private TDMScoreUIManager scoreUIManager;

        // チームリスポーンポイント
        [SerializeField] [Required] private TeamReSpawnPoints redTeamRespawnPoints;
        [SerializeField] [Required] private TeamReSpawnPoints blueTeamRespawnPoint;

        // スコア
        private int redTeamKills = 0;
        private int blueTeamKills = 0;

        void SpawnPlayers()
        {
        }

        private new void Start()
        {
            Application.targetFrameRate = 30;

            // Singleton設定
            Instance = this;

            // ネットワークマネージャーを取得
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TDMMatchMainScript: Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            PlayDefaultBGM();

            Debug.Log("TDM GameStart");

            Invoke("GameSetup", 0.1f);
        }

        private void GameSetup()
        {
            CreateMyPlayerLocally();

            // マッチ開始
            if (scoreUIManager != null)
            {
                scoreUIManager.StartMatch();
            }
        }

        private void CreateMyPlayerLocally()
        {
            // TDMではチームが必要
            ETeam myTeam = ETeam.Blue; // 本来はルーム設定から取得

            // チームごとのリスポーンポイントから選ぶ
            Vector3 spawnPos = (myTeam == ETeam.Blue)
                ? blueTeamRespawnPoint.RandomBlueTeam()
                : redTeamRespawnPoints.RandomRedTeam();

            CreateMyPlayer(spawnPos, myTeam);
        }

        private void CreateOtherPlayers()
        {
            var prefab = 0;
        }

        private void Update()
        {
        }

        void GoToResultScene()
        {
        }

        /// <summary>
        /// プレイヤー死亡イベントを処理（AbstractPlayerから呼ばれる）
        /// </summary>
        public void OnPlayerDead(ETeam victimTeam, ETeam killerTeam)
        {
            // キラーのチームにキル数を加算
            if (killerTeam == ETeam.Red)
            {
                redTeamKills++;
                scoreUIManager?.AddRedKill();
            }
            else if (killerTeam == ETeam.Blue)
            {
                blueTeamKills++;
                scoreUIManager?.AddBlueKill();
            }

            // イベント発火
            OnTeamKill?.Invoke(killerTeam, victimTeam);

            // オンラインの場合はサーバーに通知
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendKillEventToServer(killerTeam, victimTeam);
            }
        }

        /// <summary>
        /// キルイベントをサーバーに送信
        /// </summary>
        private void SendKillEventToServer(ETeam killerTeam, ETeam victimTeam)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            var json = new JObject
            {
                ["MessageType"] = "TeamKill",
                ["KillerTeam"] = killerTeam.ToString(),
                ["VictimTeam"] = victimTeam.ToString()
            };

            networkManager.SendToServer(json);
            Debug.Log($"[TDM] Sent kill event to server: {killerTeam} killed {victimTeam}");
        }

        private void OnlineEventParser(AbstractMatchEvent e)
        {
            var eventName = e.EventName;

            if ("FlagReturnEvent" == eventName)
            {
            }

            if ("FlagLostEvent" == eventName)
            {
                //PlaySound.PlayBGM()
            }
        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            //GameGeneralManager.GetInstance

        }

        public override void PostEvent(AbstractGameEvent e)
        {
            // キルイベントを処理
            if (e is PlayerKillEvent killEvent)
            {
                // ローカルでイベントを処理
                ProcessKillEvent(killEvent);
            }

            // オンラインの場合はサーバーに送信
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendEventToServer(e);
            }
        }

        /// <summary>
        /// キルイベントをローカルで処理
        /// </summary>
        private void ProcessKillEvent(PlayerKillEvent e)
        {
            // キラーのチームを取得（これはサーバーから情報をもらう必要がある）
            // 暫定的に処理
            Debug.Log($"[TDM] Kill event processed: {e.KillerId()} killed {e.VictimId()}");
        }

        /// <summary>
        /// イベントをサーバーに送信
        /// </summary>
        private void SendEventToServer(AbstractGameEvent e)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            JObject json = null;

            if (e is PlayerKillEvent killEvent)
            {
                json = RUDPMessageTypes.CreatePlayerKill(
                    killEvent.KillerId(),
                    killEvent.VictimId(),
                    killEvent.WeaponType(),
                    killEvent.IsHeadshot()
                );
            }
            else if (e is PlayerDeadEvent deadEvent)
            {
                json = RUDPMessageTypes.CreatePlayerDeath(deadEvent.PlayerID(), deadEvent.KillerID());
            }

            if (json != null)
            {
                networkManager.SendToServer(json);
            }
        }

        /// <summary>
        /// サーバーからのネットワークデータ受信処理
        /// </summary>
        protected override void OnNetworkDataRecved(JObject obj)
        {
            var messageType = obj["MessageType"]?.ToString();

            switch (messageType)
            {
                case "TeamKill":
                    HandleTeamKill(obj);
                    break;
                case RUDPMessageTypes.KillScoreUpdate:
                    HandleScoreUpdate(obj);
                    break;
                case RUDPMessageTypes.PlayerKill:
                    HandlePlayerKill(obj);
                    break;
                default:
                    base.OnNetworkDataRecved(obj);
                    break;
            }
        }

        /// <summary>
        /// チームキルイベントを処理
        /// </summary>
        private void HandleTeamKill(JObject json)
        {
            var killerTeamStr = json["KillerTeam"]?.ToString() ?? "Red";
            var victimTeamStr = json["VictimTeam"]?.ToString() ?? "Blue";

            Enum.TryParse<ETeam>(killerTeamStr, out var killerTeam);
            Enum.TryParse<ETeam>(victimTeamStr, out var victimTeam);

            // UI更新
            if (killerTeam == ETeam.Red)
            {
                redTeamKills++;
                scoreUIManager?.AddRedKill();
            }
            else if (killerTeam == ETeam.Blue)
            {
                blueTeamKills++;
                scoreUIManager?.AddBlueKill();
            }

            OnTeamKill?.Invoke(killerTeam, victimTeam);
            Debug.Log($"[TDM] Received team kill from server: {killerTeam} killed {victimTeam}");
        }

        /// <summary>
        /// スコア更新を処理
        /// </summary>
        private void HandleScoreUpdate(JObject json)
        {
            var redKills = json["RedTeamKills"]?.ToObject<int>() ?? 0;
            var blueKills = json["BlueTeamKills"]?.ToObject<int>() ?? 0;

            scoreUIManager?.UpdateScoreFromServer(redKills, blueKills);
            Debug.Log($"[TDM] Score update: Red={redKills}, Blue={blueKills}");
        }

        /// <summary>
        /// プレイヤーキルイベントを処理
        /// </summary>
        private void HandlePlayerKill(JObject json)
        {
            var killerId = json["KillerId"]?.ToString();
            var victimId = json["VictimId"]?.ToString();
            var headshot = json["Headshot"]?.ToObject<bool>() ?? false;

            Debug.Log($"[TDM] Player kill: {killerId} killed {victimId} (headshot: {headshot})");
        }
    }
}
