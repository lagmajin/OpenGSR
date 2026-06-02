using UnityEngine;
//using KanKikuchi.AudioManager;
//using Cinemachine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;


using UnityEngine.SceneManagement;
//using Cysharp.Threading.Tasks;
using OpenGSCore;


#pragma warning disable 0414

namespace OpenGS
{



    //#DeathMatch
    [DisallowMultipleComponent]
    public class DMMatchMainScript : AbstractMatchMainScript, IDMMatchMainScript
    {
        private float nowTime = 0.0f;
        public GameObject uiManager;


        

        [SerializeField] [Required] public ReSpawnPoints respawnPoints;
        //public AudioClip randomSound2;



        private float testTime = 5;


        // Start is called before the first frame update


        


        private void Awake()
        {

            Application.targetFrameRate = 60;
        }

        

        protected new void Start()
        {
            base.Start();

            if (!CompareTag("MainScript"))
            {
                gameObject.tag = "MainScript";
            }

            SetupGame();

            Debug.Log("DeathMatch: GameStart");
            Invoke(nameof(GameStart), 0.1f);

            if (battleSceneMediateObject != null)
            {
                battleSceneMediateObject.mainscript = this;
            }
            else
            {
                Debug.LogWarning("[DMMatchMainScript] battleSceneMediateObject is not assigned.");
            }
            ShakeCamera();
        }
        // Update is called once per frame
        private void Update()
        {
            nowTime += Time.deltaTime;

            testGameTime -= Time.deltaTime;

            if ((!endFlag) && testGameTime <= 0)
            {
                GameEnd();
            }

            //Debug.Log(testTime.ToString());

        }

        public override void OnMyPlayerDead()
        {
            Debug.Log("MyPlayerDead");

            if (!MatchModeResolver.CanRespawnCurrentMatch())
            {
                Debug.Log("[DMMatchMainScript] Survival mode detected, switching to spectator flow.");
                return;
            }

            Invoke(nameof(HandleMyPlayerRespawn), 3f);
            if (battleSceneMediateObject != null && battleSceneMediateObject.uiManager != null)
            {
                battleSceneMediateObject.uiManager.ShowRespawnGauge(3.0f);
            }


        }

        private void OnApplicationQuit()
        {

        }

        private void SetupGame()
        {
            if (matchRoomManager != null && matchRoomManager.WaitRoom != null)
            {
                Debug.Log($"[DM] SetupGame room={matchRoomManager.WaitRoom.RoomName} players={matchRoomManager.WaitRoom.PlayerCount}");
            }
        }

        void GameStart()
        {
            PlayGameStartVoice();

            // 自プレイヤーをランダムな位置に生成
            Vector3 spawnPos = GetRandomSpawnPoint(respawnPoints);
            var myPlayer = CreateMyPlayer(spawnPos, ETeam.NoTeam);
            if (myPlayer == null)
            {
                Debug.LogError("[DMMatchMainScript] Failed to create my player.");
                return;
            }

            SetUpUI();
        }

        void SetUpUI()
        {
            Debug.Log("[DM] SetUpUI");
        }

        void SuddenDeathStart()
        {
            var canvasIf = uiManager.GetComponent(typeof(IBattleSceneUIManager)) as IBattleSceneUIManager;
            Debug.Log("[DM] SuddenDeathStart");

        }

        void GameEnd()
        {
            endFlag = true;

            var canvasIf = uiManager != null ? uiManager.GetComponent(typeof(IBattleSceneUIManager)) as IBattleSceneUIManager : null;
            var result = StoreOfflineMatchResult();
            var (winningLabel, myLabel) = ResolveMatchOutcome(result);

            if (canvasIf != null)
            {
                if (string.IsNullOrWhiteSpace(winningLabel) || winningLabel == "Draw" || winningLabel == "None" || winningLabel == "NoPlayers")
                {
                    canvasIf.ShowGameDefatead();
                }
                else if (string.Equals(winningLabel, myLabel, StringComparison.OrdinalIgnoreCase))
                {
                    canvasIf.ShowGameWin();
                }
                else
                {
                    canvasIf.ShowGameDefatead();
                }
            }

            Debug.Log("GameEnd");

            Invoke("GoToResult", gotoResultSceneWaitTime);

        }

        private JObject StoreOfflineMatchResult()
        {
            if (GameManager != null && GameManager.IsOnlineGameMode)
            {
                return null;
            }

            var select = GameModeSelectManager.Instance.OfflineGameSelect;
            var mode = select != null ? select.GameMode : EGameMode.DeathMatch;
            var evaluator = MatchResultEvaluatorFactory.CreateEvaluator(mode);
            var manager = matchRoomManager ?? MatchRoomManager();

            var players = new List<OpenGSCore.PlayerInfo>();
            if (manager != null && manager.WaitRoom != null)
            {
                players.AddRange(manager.WaitRoom.AllPlayers());
            }

            var result = evaluator.Evaluate(null, players);
            result["MyTeam"] = ResolveLocalTeam(players);

            manager?.StoreOfflineMatchResult(result);
            return result;
        }

        private static string ResolveLocalTeam(List<OpenGSCore.PlayerInfo> players)
        {
            if (players == null)
            {
                return "Draw";
            }

            foreach (var player in players)
            {
                if (player == null || player.IsBot)
                {
                    continue;
                }

                var team = player.Team.ToString();
                if (!string.IsNullOrWhiteSpace(team) && team != ETeam.NoTeam.ToString())
                {
                    return team;
                }
            }

            return "Draw";
        }


        private void OnlineEventParser(AbstractGameEvent e)
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
            var typeName = e.GetType().FullName;

            if (typeName == typeof(PlayerDeadEvent).FullName)
            {
                var deadEvent = e as PlayerDeadEvent;

                //var data=MatchRoom().Data;

                //data.Players[e.]

            }

            if (typeName == typeof(PlayerBurstEvent).FullName)
            {

            }

        }

        public override void PostEvent(AbstractGameEvent e)
        {
            if (GameManager.IsOnlineGameMode)
            {

                //OfflineEventParser(e);
            }
            else
            {
                //OnlineEventParser(e);
            }


        }

        protected override void OnNetworkDataRecved(JObject obj)
        {
            var messageType = MessageType.Normalize(obj["MessageType"]?.ToString());

            switch (messageType)
            {
                case RUDPMessageTypes.PlayerDeath:
                    Debug.Log($"[DM] PlayerDeath received: {obj["PlayerId"]?.ToString()}");
                    break;
                case RUDPMessageTypes.KillScoreUpdate:
                    Debug.Log($"[DM] KillScoreUpdate received: {obj["PlayerId"]?.ToString()}");
                    break;
                case MessageType.MatchEndNotification:
                    HandleMatchEnd(obj);
                    break;
            }
        }
        protected override void OnOneSec()
        {
            //Debug.Log("ov1Sec");
        }

        protected override void OnOneMin()
        {
            //Debug.Log("ov1Min");
        }

        private void HandleMyPlayerRespawn()
        {
            Debug.Log("MyPlayerRespawn");
        }

        private void HandleMatchEnd(JObject json)
        {
            endFlag = true;

            var (winningLabel, myLabel) = ResolveMatchOutcome(json);

            Debug.Log($"[DM] Match ended: winner={winningLabel}, myTeam={myLabel}");
            GoToResult();
        }

        public override void OnStartUnityEditor()
        {
            AutoSet();
            EnsureEditorMatchRoom();
            SetupGame();
            Debug.Log("[DMMatchMainScript] Editor start initialized.");
        }

        protected override void OnQuitUnityEditor()
        {
            CancelInvoke();
            endFlag = true;

            if (matchRoomManager != null)
            {
                matchRoomManager.RemoveOnlineMatchRoom();
            }

            Debug.Log("[DMMatchMainScript] Editor cleanup completed.");
        }

        protected override void OnStartFromEditorDirectly()
        {
            AutoSet();
            EnsureEditorMatchRoom();

            if (GameManager != null)
            {
                GameManager.IsOnlineGameMode = false;
            }

            Debug.Log("[DMMatchMainScript] Direct editor play initialized.");
        }

        private void EnsureEditorMatchRoom()
        {
            if (matchRoomManager == null)
            {
                try
                {
                    matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DMMatchMainScript] Failed to resolve MatchRoomManager: {ex.Message}");
                    return;
                }
            }

            if (matchRoomManager == null)
            {
                return;
            }

            if (!matchRoomManager.IsValidOnlineMatchRoom())
            {
                matchRoomManager.CreateNewOnlineMatchRoom("editor-match");
            }

            var room = matchRoomManager.OnlineMatchRoom;
            if (room == null)
            {
                return;
            }

            if (room.GameMode == EGameMode.Unknown)
            {
                room.GameMode = MatchModeResolver.ResolveCurrentGameMode();
            }

            var playerInfo = BuildEditorPlayerInfo();
            if (room.database.TryGetPlayer(playerInfo.Id, out _))
            {
                return;
            }

            room.AddPlayer(playerInfo);
        }

        private static OpenGSCore.PlayerInfo BuildEditorPlayerInfo()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            var playerId = string.IsNullOrWhiteSpace(profile?.GlobalUserId)
                ? "editor-player"
                : profile.GlobalUserId;
            var displayName = string.IsNullOrWhiteSpace(profile?.DisplayName)
                ? "EditorPlayer"
                : profile.DisplayName;

            return new OpenGSCore.PlayerInfo(playerId, displayName)
            {
                playerCharacter = GamePlayerManager.Instance != null
                    ? GamePlayerManager.Instance.SelectedPlayerCharacter()
                    : OpenGSCore.EPlayerCharacter.Misty
            };

        }

        private static (string winningLabel, string myLabel) ResolveMatchOutcome(JObject json)
        {
            if (json == null)
            {
                return ("Draw", "Spectator");
            }

            var winningTeam = ReadResultString(json, "WinningTeam", "WinnerTeam", "WinningSide", "Winner", "ResultTeam", "Team");
            if (string.IsNullOrWhiteSpace(winningTeam))
            {
                winningTeam = "Draw";
            }

            var myTeam = ReadResultString(json, "MyTeam", "PlayerTeam", "SelfTeam");
            if (string.IsNullOrWhiteSpace(myTeam))
            {
                myTeam = "Spectator";
            }

            var winningPlayerId = ReadResultString(json, "WinningPlayerId", "WinningPlayerID", "WinnerPlayerId");
            if (!IsTeamResultToken(winningTeam) && IsPlayerResultToken(winningPlayerId))
            {
                return (winningPlayerId, ResolveLocalPlayerId());
            }

            return (winningTeam, myTeam);
        }

        private static string ReadResultString(JObject json, params string[] keys)
        {
            if (json == null)
            {
                return string.Empty;
            }

            foreach (var key in keys)
            {
                var value = json[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ResolveLocalPlayerId()
        {
            var profile = AccountManager.Instance?.CurrentProfile;
            return string.IsNullOrWhiteSpace(profile?.GlobalUserId) ? "Spectator" : profile.GlobalUserId;
        }

        private static bool IsPlayerResultToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !string.Equals(value, "Draw", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "NoPlayers", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Spectator", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTeamResultToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return string.Equals(value, "Red", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Blue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Green", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Yellow", StringComparison.OrdinalIgnoreCase);
        }
    }


}
