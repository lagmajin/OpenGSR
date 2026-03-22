using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// 招待ダイアログクラス
    /// ウェイトルームから使用し、ロビーにいるプレイヤーやフレンドをルームに招待する
    /// </summary>
    public class InviteDialog : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("検索")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        
        [Header("タブ")]
        [SerializeField] private Button lobbyTabButton;
        [SerializeField] private Button friendTabButton;
        [SerializeField] private GameObject lobbyTabHighlight;
        [SerializeField] private GameObject friendTabHighlight;
        
        [Header("プレイヤーリスト")]
        [SerializeField] private Transform playerListContent;
        [SerializeField] private GameObject playerItemPrefab;
        [SerializeField] private GameObject emptyMessage;
        
        [Header("招待情報")]
        [SerializeField] private TextMeshProUGUI selectedPlayerText;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private TextMeshProUGUI roomInfoText;
        
        [Header("ボタン")]
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button cancelButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<PlayerInfo> lobbyPlayers = new List<PlayerInfo>();
        private List<PlayerInfo> friendList = new List<PlayerInfo>();
        private List<PlayerInfo> filteredPlayers = new List<PlayerInfo>();
        private PlayerInfo selectedPlayer = null;
        private bool isLobbyTabActive = true;
        private string currentRoomId = "";
        private string currentRoomName = "";

        // ─── デリゲート ─────────────────────────────────────────────

        public Action<string, string> OnInviteSent; // (playerId, message)
        public Action OnDialogClosed;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
        }

        private void OnEnable()
        {
            RefreshPlayerList();
            UpdateRoomInfo();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // メッセージ入力のデフォルト設定
            if (messageInput != null)
            {
                messageInput.text = "一緒にプレイしませんか？";
                messageInput.characterLimit = 50;
            }

            // エラーテキストをクリア
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }

            // 空メッセージを非表示
            if (emptyMessage != null)
            {
                emptyMessage.SetActive(false);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // 検索
            if (searchButton != null)
            {
                searchButton.onClick.AddListener(OnSearchButtonClicked);
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchValueChanged);
            }

            // タブ
            if (lobbyTabButton != null)
            {
                lobbyTabButton.onClick.AddListener(OnLobbyTabClicked);
            }

            if (friendTabButton != null)
            {
                friendTabButton.onClick.AddListener(OnFriendTabClicked);
            }

            // ボタン
            if (inviteButton != null)
            {
                inviteButton.onClick.AddListener(OnInviteButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// ダイアログを表示する
        /// </summary>
        /// <param name="roomId">ルームID</param>
        /// <param name="roomName">ルーム名</param>
        public void Show(string roomId, string roomName)
        {
            currentRoomId = roomId;
            currentRoomName = roomName;
            gameObject.SetActive(true);
            RefreshPlayerList();
            UpdateRoomInfo();
        }

        /// <summary>
        /// ロビーにいるプレイヤーリストを更新する
        /// </summary>
        /// <param name="players">プレイヤーリスト</param>
        public void UpdateLobbyPlayers(List<PlayerInfo> players)
        {
            lobbyPlayers = players ?? new List<PlayerInfo>();
            if (isLobbyTabActive)
            {
                RefreshPlayerList();
            }
        }

        /// <summary>
        /// フレンドリストを更新する
        /// </summary>
        /// <param name="friends">フレンドリスト</param>
        public void UpdateFriendList(List<PlayerInfo> friends)
        {
            friendList = friends ?? new List<PlayerInfo>();
            if (!isLobbyTabActive)
            {
                RefreshPlayerList();
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnSearchButtonClicked()
        {
            FilterPlayers(searchInput.text);
        }

        private void OnSearchValueChanged(string value)
        {
            FilterPlayers(value);
        }

        private void OnLobbyTabClicked()
        {
            isLobbyTabActive = true;
            UpdateTabHighlight();
            RefreshPlayerList();
        }

        private void OnFriendTabClicked()
        {
            isLobbyTabActive = false;
            UpdateTabHighlight();
            RefreshPlayerList();
        }

        private void OnInviteButtonClicked()
        {
            if (selectedPlayer == null)
            {
                ShowStatus("プレイヤーを選択してください", true);
                return;
            }

            if (string.IsNullOrEmpty(currentRoomId))
            {
                ShowStatus("ルーム情報がありません", true);
                return;
            }

            // 招待を送信
            string message = messageInput != null ? messageInput.text : "";
            SendInvite(selectedPlayer.PlayerId, message);
        }

        private void OnCancelButtonClicked()
        {
            CloseDialog();
        }

        // ─── プレイヤーリスト管理 ─────────────────────────────────────

        /// <summary>
        /// プレイヤーリストを更新する
        /// </summary>
        private void RefreshPlayerList()
        {
            // リストをクリア
            ClearPlayerList();

            // 現在のタブに応じたプレイヤーリストを取得
            var sourceList = isLobbyTabActive ? lobbyPlayers : friendList;
            filteredPlayers = new List<PlayerInfo>(sourceList);

            // プレイヤーアイテムを生成
            if (filteredPlayers.Count == 0)
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(true);
                }
            }
            else
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(false);
                }

                foreach (var player in filteredPlayers)
                {
                    CreatePlayerItem(player);
                }
            }

            // 選択をクリア
            selectedPlayer = null;
            UpdateSelectedPlayerUI();
        }

        /// <summary>
        /// プレイヤーリストをクリアする
        /// </summary>
        private void ClearPlayerList()
        {
            if (playerListContent == null) return;

            foreach (Transform child in playerListContent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// プレイヤーアイテムを生成する
        /// </summary>
        private void CreatePlayerItem(PlayerInfo player)
        {
            if (playerItemPrefab == null || playerListContent == null) return;

            var item = Instantiate(playerItemPrefab, playerListContent);
            var itemScript = item.GetComponent<InvitePlayerItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(player, OnPlayerSelected);
            }
            else
            {
                // フォールバック：直接UIを設定
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = player.PlayerName;
                }

                var button = item.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnPlayerSelected(player));
                }
            }
        }

        /// <summary>
        /// プレイヤーが選択されたときの処理
        /// </summary>
        private void OnPlayerSelected(PlayerInfo player)
        {
            selectedPlayer = player;
            UpdateSelectedPlayerUI();
        }

        /// <summary>
        /// 選択されたプレイヤーのUIを更新する
        /// </summary>
        private void UpdateSelectedPlayerUI()
        {
            if (selectedPlayerText != null)
            {
                selectedPlayerText.text = selectedPlayer != null 
                    ? $"選択中: {selectedPlayer.PlayerName}" 
                    : "プレイヤーを選択してください";
            }

            if (inviteButton != null)
            {
                inviteButton.interactable = selectedPlayer != null;
            }
        }

        /// <summary>
        /// プレイヤーをフィルタリングする
        /// </summary>
        private void FilterPlayers(string keyword)
        {
            var sourceList = isLobbyTabActive ? lobbyPlayers : friendList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                filteredPlayers = new List<PlayerInfo>(sourceList);
            }
            else
            {
                filteredPlayers = sourceList
                    .Where(p => p.PlayerName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // リストを再構築
            ClearPlayerList();

            if (filteredPlayers.Count == 0)
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(true);
                }
            }
            else
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(false);
                }

                foreach (var player in filteredPlayers)
                {
                    CreatePlayerItem(player);
                }
            }
        }

        // ─── タブ管理 ───────────────────────────────────────────────

        /// <summary>
        /// タブのハイライトを更新する
        /// </summary>
        private void UpdateTabHighlight()
        {
            if (lobbyTabHighlight != null)
            {
                lobbyTabHighlight.SetActive(isLobbyTabActive);
            }

            if (friendTabHighlight != null)
            {
                friendTabHighlight.SetActive(!isLobbyTabActive);
            }
        }

        // ─── 招待送信 ───────────────────────────────────────────────

        /// <summary>
        /// 招待を送信する
        /// </summary>
        private void SendInvite(string playerId, string message)
        {
            Debug.Log($"[InviteDialog] 招待送信: PlayerID={playerId}, Message={message}");

            // 招待送信イベントを発火
            OnInviteSent?.Invoke(playerId, message);

            ShowStatus("招待を送信しました", false);

            // 少し待ってからダイアログを閉じる
            Invoke(nameof(CloseDialog), 1.5f);
        }

        // ─── ルーム情報 ─────────────────────────────────────────────

        /// <summary>
        /// ルーム情報を更新する
        /// </summary>
        private void UpdateRoomInfo()
        {
            if (roomInfoText != null)
            {
                roomInfoText.text = !string.IsNullOrEmpty(currentRoomName)
                    ? $"ルーム: {currentRoomName}"
                    : "ルーム情報なし";
            }
        }

        // ─── ステータス表示 ─────────────────────────────────────────

        /// <summary>
        /// ステータスメッセージを表示する
        /// </summary>
        private void ShowStatus(string message, bool isError)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = isError ? Color.red : Color.green;
                statusText.gameObject.SetActive(true);

                // 3秒後に非表示
                CancelInvoke(nameof(HideStatus));
                Invoke(nameof(HideStatus), 3f);
            }
        }

        /// <summary>
        /// ステータスメッセージを非表示にする
        /// </summary>
        private void HideStatus()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // ─── ダイアログ制御 ─────────────────────────────────────────

        /// <summary>
        /// ダイアログを閉じる
        /// </summary>
        private void CloseDialog()
        {
            // 状態をリセット
            selectedPlayer = null;
            searchInput.text = "";
            filteredPlayers.Clear();

            // ダイアログを非表示
            gameObject.SetActive(false);

            // コールバックを発火
            OnDialogClosed?.Invoke();
        }
    }

    /// <summary>
    /// プレイヤー情報クラス
    /// </summary>
    [System.Serializable]
    public class PlayerInfo
    {
        public string PlayerId;
        public string PlayerName;
        public int Level;
        public bool IsOnline;
        public bool IsInRoom;

        public PlayerInfo(string id, string name, int level = 1, bool isOnline = true, bool isInRoom = false)
        {
            PlayerId = id;
            PlayerName = name;
            Level = level;
            IsOnline = isOnline;
            IsInRoom = isInRoom;
        }
    }
}