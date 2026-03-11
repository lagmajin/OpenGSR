﻿using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using OpenGSCore;
using UniRx;

#pragma warning disable 0414

namespace OpenGS
{
    /// <summary>
    /// オンラインロビーシーンの管理クラス。
    /// ルーム一覧の表示・作成・入室などのロビー機能を担当する。
    /// </summary>
    [DisallowMultipleComponent]
    public class OnlineLobbyScene : AbstractNonBattleScene, INetworkManagerScript
    {
        // ─── 定数 ──────────────────────────────────────────────────

        private const int MaxUpdateCount = 50000;

        // ─── Inspector フィールド ───────────────────────────────────

        [SerializeField] public GameObject createNewRoomDialog;
        [SerializeField] public GameObject RoomButton;
        [SerializeField] public GameObject InfoDialog;
        [SerializeField] public GameObject robbyNetworkManager;
        [SerializeField] public GameObject roomPanel;

        [SerializeField] [Required] public LobbySceneMediateObject mediateObject;

        // ─── 内部状態 ───────────────────────────────────────────────

        private GeneralServerNetworkManager networkManager;
        private MatchRoomManager matchRoomManager;
        private WaitRoomManager waitRoomManager;
        private SynchronizationContext mainThread;
        private bool canInput = true;
        private int updateCount = 0;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            mainThread = SynchronizationContext.Current;
            networkManager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
            matchRoomManager = DependencyInjectionConfig.Resolve<MatchRoomManager>();
            waitRoomManager = DependencyInjectionConfig.Resolve<WaitRoomManager>();

            if (DebugFlagManager.IsDebug())
            {
                DebugFlagManager.SetFirstSceneName(this.GetType().FullName);
                BackToConnectServerScene();
            }
        }

        void Start()
        {
            SceneManager.sceneLoaded += OnGameSceneLoaded;
            StartCoroutine(PeriodicUpdateCoroutine());
            ShowDefaultRooms();

            try
            {
                networkManager.DataReceivedStream
                    .ObserveOnMainThread()
                    .Subscribe(ParseMessageFromServer)
                    .AddTo(this.gameObject);
                Debug.Log("OnlineLobbyScene: Subscribed to GeneralServerNetworkManager.DataReceivedStream");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"OnlineLobbyScene: Failed to subscribe to DataReceivedStream: {ex.Message}");
            }
        }

        void OnDestroy()
        {
            networkManager.UnSubscribe(this);
        }

        private void OnApplicationQuit()
        {
            networkManager.Disconnect();
        }

        void Update()
        {
            if (!canInput) return;

            if (Input.anyKeyDown)
            {
                updateCount = 0;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("F5 pressed: Sending UpdateRoomRequest");
                networkManager.SendUpdateRoomRequest(new List<EGameMode>());
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKey(KeyCode.Escape))
            {
                DisconnectAndBackToTitle();
            }

            if (Input.GetKey(KeyCode.S))
            {
                GoToShop();
            }

            // 長時間操作なしでタイトルへ戻る
            if (updateCount >= MaxUpdateCount)
            {
                DisconnectAndBackToTitle();
            }

            updateCount++;
        }

        // ─── ルーム管理 ──────────────────────────────────────────────

        [Button("ルーム全消去")]
        public void RemoveAllRoom()
        {
            var children = new GameObject[roomPanel.transform.childCount];
            for (int i = 0; i < roomPanel.transform.childCount; i++)
            {
                children[i] = roomPanel.transform.GetChild(i).gameObject;
            }
            foreach (var child in children)
            {
                Destroy(child);
            }
        }

        [Button("ルーム作成ダイアログ表示テスト")]
        public void ShowCreateNewRoomDialog()
        {
            mediateObject.createNewRoomDialog.gameObject.SetActive(true);
        }

        /// <summary>
        /// オンラインロビーのダイアログから古い形式で呼ばれる場合のテスト用
        /// </summary>
        [Button("部屋作成テスト (旧)")]
        public void CreateNewWaitRoom()
        {
            var dialogScript = createNewRoomDialog.GetComponent<ICreateNewRoomDialog>();
            createNewRoomDialog.SetActive(false);

            string roomName = dialogScript != null ? dialogScript.RoomName() : "One Shot One Kill!";

            var v = MakeJSON.EnterRoomRequest();
            // TODO: networkManager.SendCreateRoomRequest(v);
        }

        /// <summary>
        /// 入室リクエストをサーバーへ送信する。
        /// </summary>
        public void SendEnterRoomRequest()
        {
            networkManager.SendMessage(MakeJSON.EnterRoomRequest());
        }

        // ─── ルーム絞り込み (UI ボタンから呼ばれる) ──────────────────

        public void ShowAllRooms()   { /* TODO: フィルタなしで全ルームを表示 */ }
        public void ShowDMRooms()    { /* TODO: Death Match ルームのみ表示 */ }
        public void ShowTDMRooms()   { /* TODO: Team Death Match ルームのみ表示 */ }
        public void ShowSUVRooms()   { /* TODO: SUV ルームのみ表示 */ }
        public void ShowTSUVRooms()  { /* TODO: Team SUV ルームのみ表示 */ }
        public void ShowCTFRooms()   { /* TODO: CTF ルームのみ表示 */ }
        public void ShowArmsRaceRooms() { /* TODO: Arms Race ルームのみ表示 */ }

        // ─── シーン遷移 ──────────────────────────────────────────────

        /// <summary>
        /// サーバーとの接続を切断してタイトルへ戻る。
        /// </summary>
        public void DisconnectAndBackToTitle()
        {
            networkManager.Disconnect();
            SceneManager.LoadSceneAsync(mediateObject.GeneralSceneMasterData().TitleScene());
        }

        public void GoToShop()
        {
            SceneManager.LoadScene("ShopScene");
        }

        public void GotoOnlineWaitRoom()
        {
            mainThread.Post(__ =>
            {
                SceneManager.LoadScene(mediateObject.GeneralSceneMasterData().OnlineWaitRoomScene());
            }, null);
        }

        public void SwitchToMissionServer()
        {
            Debug.Log("SwitchToMissionServer");
            // TODO: ミッションサーバーへの切り替え処理
        }

        // ─── ネットワーク受信 ─────────────────────────────────────────

        public void ParseMessageFromServer(JObject json)
        {
            var messageType = json["MessageType"]?.ToString();
            if (string.IsNullOrEmpty(messageType)) return;

            switch (messageType)
            {
                case "CreateNewWaitRoomResponse":
                    OnCreateNewWaitRoomResponse(json);
                    break;
                case "UpdateRoomResponse":
                    OnUpdateRoomResponse(json);
                    break;
                default:
                    Debug.LogWarning($"OnlineLobbyScene: Unknown message type: {messageType}");
                    break;
            }
        }

        private void OnCreateNewWaitRoomResponse(JObject json)
        {
            var success = json["Success"]?.ToObject<bool>() ?? false;
            if (success)
            {
                var roomId = json["RoomID"]?.ToString();
                var roomName = json["RoomName"]?.ToString();
                var capacity = json["Capacity"]?.ToObject<int>() ?? 8;

                Debug.Log($"OnlineLobbyScene: Room created. RoomID={roomId}, RoomName={roomName}");

                // MatchRoomManager & WaitRoomManager に情報をセット
                matchRoomManager.CreateNewOnlineWaitRoom(roomName, capacity);
                waitRoomManager.CreateNewWaitRoom(roomName, roomId, capacity);
                
                // 待機部屋シーンへ遷移
                GotoOnlineWaitRoom();
            }
            else
            {
                var errorMessage = json["ErrorMessage"]?.ToString() ?? "Unknown error";
                Debug.LogWarning($"OnlineLobbyScene: Failed to create room: {errorMessage}");
                
                // TODO: エラーダイアログを表示（InfoDialog などを使用）
                if (InfoDialog != null)
                {
                    InfoDialog.SetActive(true);
                    // エラーメッセージのセットロジックが InfoDialog にあれば実行
                }
            }
        }

        private void OnUpdateRoomResponse(JObject json)
        {
            var rooms = json["Rooms"] as JArray;
            Debug.Log($"OnlineLobbyScene: Received {rooms?.Count ?? 0} rooms");
            // TODO: ルームリスト UI を更新
        }

        // ─── INetworkManagerScript の実装 ────────────────────────────

        public void OnConnected()
        {
        }

        public void OnDisconnected()
        {
        }

        public void ParseNetworkMatchMessageFromServer(JObject json)
        {
        }

        public void TestFunc()
        {
        }

        // ─── ロビー内アクション ───────────────────────────────────────

        public void OnCreateNewRoom()
        {
            Debug.Log("OnCreateNewRoom");

            ICreateNewRoomDialog dialogScript = null;
            if (createNewRoomDialog != null) dialogScript = createNewRoomDialog.GetComponent<ICreateNewRoomDialog>();
            if (dialogScript == null && mediateObject.createNewRoomDialog != null) dialogScript = mediateObject.createNewRoomDialog;

            if (dialogScript == null)
            {
                Debug.LogWarning("OnlineLobbyScene.OnCreateNewRoom: dialogScript is null");
                return;
            }

            var maxPlayer = dialogScript.MaxPlayer();
            var password  = dialogScript.Password();
            var gameMode  = dialogScript.GameMode();

            var json = new JObject
            {
                ["MessageType"] = "CreateNewWaitRoomRequest",
                ["RoomName"] = dialogScript.RoomName(),
                ["Capacity"] = maxPlayer.ToString(),
                ["GameMode"] = gameMode.ToString(),
                ["TeamBalance"] = dialogScript.TeamBalance() ? "True" : "False",
                ["Password"] = password ?? ""
            };

            networkManager.SendMessage(json);
            Debug.Log($"OnlineLobbyScene: Sent CreateNewWaitRoomRequest: {json.ToString(Formatting.None)}");

            if (createNewRoomDialog != null) createNewRoomDialog.SetActive(false);
            if (mediateObject.createNewRoomDialog != null) mediateObject.createNewRoomDialog.gameObject.SetActive(false);

            // 以前のロジックの名残で必要なら処理を挟む
            switch (gameMode)
            {
                case EGameMode.DeathMatch:
                    break;
                case EGameMode.TowerMatch:
                    break;
            }
        }

        [Button("チャット送信テスト")]
        public void SendChat(string str)
        {
            var json = new JObject
            {
                ["MessageType"] = "AddLobbyChat",
                ["Chat"] = str
            };
            networkManager.SendMessage(json);
        }

        // ─── 入力ブロック ─────────────────────────────────────────────

        [Button("入力ブロック")]
        public void BlockInput()
        {
            StartCoroutine(BlockKeyInputCoroutine());
        }

        private IEnumerator BlockKeyInputCoroutine()
        {
            canInput = false;
            yield return new WaitForSeconds(30.0f);
            canInput = true;
        }

        // ─── プライベートユーティリティ ───────────────────────────────

        private void ShowDefaultRooms()
        {
            Debug.Log("ShowDefaultRooms");
        }

        private IEnumerator PeriodicUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                // TODO: 定期的なルームリスト更新
            }
        }

        private void OnGameSceneLoaded(Scene next, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }

        private void BackToConnectServerScene()
        {
            SceneManager.LoadScene("ConnectServerScene");
        }

        // ─── AbstractNonBattleScene の実装 ────────────────────────────

        public override SynchronizationContext MainThread() => mainThread;

        protected override void OnStartUnityEditor()
        {
        }

        protected override void OnQuitUnityEditor()
        {
        }

        protected override void OnStartFromEditorDirectly()
        {
        }
    }
}

