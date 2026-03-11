using System;
using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

//using KanKikuchi.AudioManager;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Sirenix.Serialization;
using Zenject;
using Newtonsoft.Json.Linq;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class CTFMatchMainScript : AbstractMatchMainScript, ICTFMatchMainScript
    {
        // Singleton instance for UI to access
        public static CTFMatchMainScript Instance { get; private set; }

        // CTF Events for UI
        public event Action<ETeam> OnFlagCaptured;
        public event Action<ETeam> OnFlagReturned;
        public event Action<ETeam> OnFlagLost;
        public event Action<ETeam, string> OnFlagPickedUp; // team, playerName

        // ネットワークマネージャー
        private MatchRUDPServerNetworkManager networkManager;

        //[SerializeField][OdinSerialize][Inject] ClientSessionData data;
        private MatchRoom matchRoom;

        //public AudioClip captureFlagSound;
        //public AudioClip returnFlagSound;

        [SerializeField]
        public GameObject BlueTeamReSpawnPoints;
        [SerializeField]
        public GameObject RedTeamReSpawnPoints;

        [SerializeField] [Required] public FlagStand redTeamFlagStand, blueTeamFlagStand;

        public new void Start()
        {
            // Singleton設定
            Instance = this;

            // ネットワークマネージャーを取得
            try
            {
                networkManager = DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CTFMatchMainScript: Failed to resolve MatchRUDPServerNetworkManager: {ex.Message}");
                networkManager = null;
            }

            Invoke("GameSetup", 0.1f);
        }

        public void CreateDebugRoom()
        {
        }

        void GameSetup()
        {
            //CreateNewMyPlayer();
            //CreateOtherPlayers();

            var roomManager = MatchRoomManager();

            //var matchRoom = roomManager.OfflineMatchRoom;

            //matchRoom.StartMatch();

            //NetworkManagerMainScript

            SubscribeEvent();

            SetUpUI();

            redTeamFlagStand.SetFlag();
            blueTeamFlagStand.SetFlag();

            Debug.Log("GameStarteted");
        }

        private void SetUpUI()
        {
        }

        private void CreateNewMyPlayer()
        {
            GameObject myPlayerPrefab = null;

            if (myPlayerPrefab)
            {
                var prefab = prefabMasterData.mistyPrefab;

                var player = Instantiate(prefab);

                var iPlayer = player.GetComponent<IPlayer>();

                iPlayer.SetTeam(ETeam.Blue);
                iPlayer.CreatePlayerLink(EPlayerType.MyPlayer, "");

                //playerCamera.LookAt = player.transform;

                playerCamera.Follow = player.transform;

                vcamera.Priority = 0;
                playerCamera.Priority = 10;
            }
        }

        private void CreateOtherPlayers()
        {
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        void OnDestroy()
        {
            UnSubscribeEvent();
            if (Instance == this) Instance = null;
        }

        // Update is called once per frame
        void Update()
        {
        }

        void FlagCaptured(in TeamEventPlayerInfo capturedPlayerInfo)
        {
            // var myPlayerInfo = GameManager.MyPlayerInfo
            //var playerStatus = GameManager.PlayerStatus
            //if(playerStatus.Team=capturedPlayerInfo.tea) 
        }

        void FlagReturn(in TeamEventPlayerInfo flagReturnInfo)
        {
            if (GameManager.IsOnlineGameMode)
            {
            }
            else
            {
            }
        }

        void FlagLost(in TeamEventPlayerInfo team)
        {
            if (GameManager.IsOnlineGameMode)
            {
            }
            else
            {
            }
        }

        void FlagBurst(ETeam team)
        {
        }

        void RecoveryRedFlag()
        {
        }

        void RecoveryBlueFlag()
        {
        }

        void GoToResultScene()
        {
        }

        private void OfflineEventParser(AbstractGameEvent e)
        {
            if (e.GetType() == typeof(FlagReturnSuccessEvent))
            {
                var ev = e as FlagReturnSuccessEvent;
                //matchRoom.Data;
            }
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

        public override void PostEvent(AbstractGameEvent e)
        {
            // オフライン/オンライン両方のイベントを処理
            OfflineEventParser(e);

            // オンラインの場合、サーバーに送信
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                SendFlagEventToServer(e);
            }
        }

        /// <summary>
        /// フラッグイベントをサーバーに送信
        /// </summary>
        private void SendFlagEventToServer(AbstractGameEvent e)
        {
            if (networkManager == null || !networkManager.IsConnected()) return;

            if (e is FlagEvent flagEvent)
            {
                var teamStr = flagEvent.Team().ToString();
                var playerId = flagEvent.PlayerID();
                var pos = flagEvent.Position();

                JObject json = flagEvent.FlagEventType() switch
                {
                    EFlagEventType.Captured => RUDPMessageBuilder.CreateFlagCaptured(playerId, teamStr, pos),
                    EFlagEventType.Lost => RUDPMessageBuilder.CreateFlagLost(playerId, teamStr, pos),
                    EFlagEventType.Returned => RUDPMessageBuilder.CreateFlagReturn(teamStr, playerId),
                    EFlagEventType.Burst => RUDPMessageBuilder.CreateFlagBurst(teamStr, pos, playerId),
                    EFlagEventType.Pickup => RUDPMessageBuilder.CreateFlagPickup(playerId, teamStr, pos),
                    _ => null
                };

                if (json != null)
                {
                    networkManager.SendToServer(json);
                    Debug.Log($"[CTF] Sent flag event to server: {flagEvent.FlagEventType()}");
                }
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
                case RUDPMessageTypes.FlagCaptured:
                    HandleFlagEvent(obj, EFlagEventType.Captured);
                    break;
                case RUDPMessageTypes.FlagLost:
                    HandleFlagEvent(obj, EFlagEventType.Lost);
                    break;
                case RUDPMessageTypes.FlagReturn:
                    HandleFlagEvent(obj, EFlagEventType.Returned);
                    break;
                case RUDPMessageTypes.FlagBurst:
                    HandleFlagEvent(obj, EFlagEventType.Burst);
                    break;
                case RUDPMessageTypes.FlagPickup:
                    HandleFlagEvent(obj, EFlagEventType.Pickup);
                    break;
                case RUDPMessageTypes.FlagScoreUpdate:
                    HandleFlagScoreUpdate(obj);
                    break;
                default:
                    base.OnNetworkDataRecved(obj);
                    break;
            }
        }

        /// <summary>
        /// サーバーからのフラッグイベントを処理
        /// </summary>
        private void HandleFlagEvent(JObject json, EFlagEventType eventType)
        {
            var playerId = json["PlayerId"]?.ToString() ?? "";
            var teamStr = json["Team"]?.ToString() ?? "Red";
            Enum.TryParse<ETeam>(teamStr, out var team);

            // UIイベントを発火
            switch (eventType)
            {
                case EFlagEventType.Captured:
                    PlayerFlagCaptured(team);
                    break;
                case EFlagEventType.Lost:
                    PlayerFlagLost(team);
                    break;
                case EFlagEventType.Returned:
                    PlayerFlagReturned(team);
                    break;
                case EFlagEventType.Pickup:
                    PlayerFlagPickedUp(team, playerId);
                    break;
            }

            Debug.Log($"[CTF] Received flag event from server: {eventType} for team {team}");
        }

        /// <summary>
        /// フラッグスコア更新を処理
        /// </summary>
        private void HandleFlagScoreUpdate(JObject json)
        {
            var redScore = json["RedTeamScore"]?.ToObject<int>() ?? 0;
            var blueScore = json["BlueTeamScore"]?.ToObject<int>() ?? 0;

            // CTFScoreUIManagerにスコアを通知
            if (CTFScoreUIManager.Instance != null)
            {
                CTFScoreUIManager.Instance.UpdateScore(redScore, blueScore);
            }

            Debug.Log($"[CTF] Score update: Red={redScore}, Blue={blueScore}");
        }

        public List<IFlagStand> AllFlagStands()
        {
            return null;
        }

        [Button("フラッグキャプチャーテスト")]
        public void PlayerFlagCaptured(ETeam team)
        {
            Debug.Log("FlagCaptured: " + team);
            OnFlagCaptured?.Invoke(team);
        }

        [Button("フラッグロストテスト")]
        public void PlayerFlagLost(ETeam team)
        {
            Debug.Log("FlagLost: " + team);
            OnFlagLost?.Invoke(team);
        }

        [Button("フラッグ帰還テスト")]
        public void PlayerFlagReturned(ETeam team)
        {
            Debug.Log("FlagReturned: " + team);
            OnFlagReturned?.Invoke(team);
        }

        [Button("フラッグピックテスト")]
        public void PlayerFlagPickedUp(ETeam team, string playerName)
        {
            Debug.Log("FlagPickedUp: " + team + " by " + playerName);
            OnFlagPickedUp?.Invoke(team, playerName);
        }
    }
}
