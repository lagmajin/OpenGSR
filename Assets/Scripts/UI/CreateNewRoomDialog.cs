using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenGSCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// 部屋作成ダイアログの具象実装クラス
    /// ルーム名、人数、パスワード、ゲームモードを設定するUIを提供する
    /// </summary>
    public class CreateNewRoomDialog : AbstractCreateNewRoomDialog
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("ルーム設定")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_Dropdown maxPlayerDropdown;
        
        [Header("パスワード設定")]
        [SerializeField] private Toggle passwordToggle;
        [SerializeField] private GameObject passwordPanel;
        [SerializeField] private TMP_InputField passwordInput;
        
        [Header("ゲームモード")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        
        [Header("チームバランス")]
        [SerializeField] private Toggle teamBalanceToggle;
        
        [Header("ボタン")]
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelButton;
        
        [Header("エラー表示")]
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("接続先")]
        [SerializeField] private OnlineLobbyScene lobbyScene;

        // ─── 内部状態 ───────────────────────────────────────────────

        private string roomName = "Room";
        private int maxPlayer = 8;
        private string password = "";
        private EGameMode selectedGameMode = EGameMode.TeamDeathMatch;
        private bool teamBalance = true;
        private bool isPasswordEnabled = false;
        private TextMeshProUGUI fallbackTitleText;
        private TextMeshProUGUI fallbackSummaryText;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            if (lobbyScene == null)
            {
                lobbyScene = FindFirstObjectByType<OnlineLobbyScene>();
            }

            AutoBindFallbackControls();
            InitializeUI();
            EnsureFallbackConfirmationUi();
            SetupListeners();
        }

        private void OnEnable()
        {
            ResetDialog();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // 人数ドロップダウンの初期化（2, 4, 6, 8, 10, 12）
            if (maxPlayerDropdown != null)
            {
                maxPlayerDropdown.ClearOptions();
                var playerOptions = new System.Collections.Generic.List<string>();
                for (int i = 2; i <= 12; i += 2)
                {
                    playerOptions.Add($"{i}人");
                }
                maxPlayerDropdown.AddOptions(playerOptions);
                maxPlayerDropdown.value = 3; // デフォルト8人
            }

            // ゲームモードドロップダウンの初期化
            if (gameModeDropdown != null)
            {
                gameModeDropdown.ClearOptions();
                var gameModes = OpenGSCore.GameMode.AllGameMode();
                var modeOptions = gameModes.Select(GameModeVisualResolver.GetDisplayName).ToList();
                gameModeDropdown.AddOptions(modeOptions);
                
                // デフォルトでTeamDeathMatchを選択
                var tdmIndex = gameModes.IndexOf(EGameMode.TeamDeathMatch);
                if (tdmIndex >= 0)
                {
                    gameModeDropdown.value = tdmIndex;
                }
            }

            // パスワードパネルを非表示
            if (passwordPanel != null)
            {
                passwordPanel.SetActive(false);
            }

            // エラーテキストをクリア
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // ルーム名入力
            if (roomNameInput != null)
            {
                roomNameInput.onValueChanged.AddListener(OnRoomNameChanged);
            }

            // 人数選択
            if (maxPlayerDropdown != null)
            {
                maxPlayerDropdown.onValueChanged.AddListener(OnMaxPlayerChanged);
            }

            // パスワードトグル
            if (passwordToggle != null)
            {
                passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
            }

            // パスワード入力
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
                passwordInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                passwordInput.characterLimit = 4;
            }

            // ゲームモード選択
            if (gameModeDropdown != null)
            {
                gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            }

            // チームバランストグル
            if (teamBalanceToggle != null)
            {
                teamBalanceToggle.onValueChanged.AddListener(OnTeamBalanceChanged);
            }

            // ボタン
            if (createButton != null)
            {
                createButton.onClick.AddListener(OnCreateButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        private void AutoBindFallbackControls()
        {
            roomNameInput ??= GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault();
            passwordInput ??= GetComponentsInChildren<TMP_InputField>(true).Skip(roomNameInput != null ? 1 : 0).FirstOrDefault();
            passwordPanel ??= passwordInput != null ? passwordInput.transform.parent?.gameObject : null;

            var toggles = GetComponentsInChildren<Toggle>(true).ToList();
            if (passwordToggle == null && toggles.Count > 0)
            {
                passwordToggle = toggles[0];
            }
            if (teamBalanceToggle == null && toggles.Count > 1)
            {
                teamBalanceToggle = toggles[1];
            }

            errorText ??= GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0);

            if (createButton == null)
            {
                createButton = FindChildComponent<Button>("CreateButton");
            }

            createButton ??= GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0
                    || button.name.IndexOf("create", StringComparison.OrdinalIgnoreCase) >= 0);

            if (cancelButton == null)
            {
                cancelButton = FindChildComponent<Button>("CancelButton");
            }

            cancelButton ??= GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button != createButton && (
                    button.name.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0
                    || button.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0));

            if (lobbyScene == null)
            {
                lobbyScene = FindFirstObjectByType<OnlineLobbyScene>();
            }
        }

        private void EnsureFallbackConfirmationUi()
        {
            if (!NeedsFallbackConfirmationUi())
            {
                return;
            }

            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            fallbackTitleText ??= CreateFallbackText("Title", new Vector2(0f, 120f), new Vector2(420f, 42f), 30f, FontStyles.Bold);
            fallbackSummaryText ??= CreateFallbackText("Summary", new Vector2(0f, 18f), new Vector2(420f, 120f), 22f, FontStyles.Normal);

            fallbackTitleText.text = "Create Room";
            fallbackSummaryText.text = BuildFallbackSummary();

            ApplyButtonLabel(createButton, "Create");
            ApplyButtonLabel(cancelButton, "Cancel");
        }

        private bool NeedsFallbackConfirmationUi()
        {
            return roomNameInput == null
                && maxPlayerDropdown == null
                && passwordToggle == null
                && passwordInput == null
                && gameModeDropdown == null
                && teamBalanceToggle == null
                && createButton != null
                && cancelButton != null;
        }

        private TextMeshProUGUI CreateFallbackText(string name, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles fontStyle)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(32, 48, 74, 255);
            text.raycastTarget = false;

            return text;
        }

        private void ApplyButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var existing = button.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.gameObject.name == "FallbackLabel");
            if (existing == null)
            {
                var go = new GameObject("FallbackLabel", typeof(RectTransform));
                go.transform.SetParent(button.transform, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                existing = go.AddComponent<TextMeshProUGUI>();
                existing.fontSize = 18f;
                existing.fontStyle = FontStyles.Bold;
                existing.alignment = TextAlignmentOptions.Center;
                existing.color = Color.white;
                existing.raycastTarget = false;
            }

            existing.text = label;
        }

        private string BuildFallbackSummary()
        {
            var summary = new List<string>
            {
                $"Mode: {GameModeVisualResolver.GetDisplayName(selectedGameMode)}",
                $"Players: {maxPlayer}",
                $"Password: {(isPasswordEnabled && !string.IsNullOrEmpty(password) ? "Enabled" : "None")}",
                $"Team Balance: {(teamBalance ? "On" : "Off")}"
            };

            return string.Join("\n", summary);
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            foreach (var component in GetComponentsInChildren<T>(true))
            {
                if (component != null && component.name == childName)
                {
                    return component;
                }
            }

            return null;
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnRoomNameChanged(string value)
        {
            roomName = value;
            ClearError();
        }

        private void OnMaxPlayerChanged(int index)
        {
            // 2, 4, 6, 8, 10, 12 のいずれか
            maxPlayer = (index + 1) * 2;
        }

        private void OnPasswordToggleChanged(bool isOn)
        {
            isPasswordEnabled = isOn;
            if (passwordPanel != null)
            {
                passwordPanel.SetActive(isOn);
            }

            if (!isOn)
            {
                password = "";
                if (passwordInput != null)
                {
                    passwordInput.text = "";
                }
            }
        }

        private void OnPasswordChanged(string value)
        {
            password = value;
            ClearError();
        }

        private void OnGameModeChanged(int index)
        {
            var gameModes = OpenGSCore.GameMode.AllGameMode();
            if (index >= 0 && index < gameModes.Count)
            {
                selectedGameMode = gameModes[index];
            }
        }

        private void OnTeamBalanceChanged(bool isOn)
        {
            teamBalance = isOn;
        }

        private void OnCreateButtonClicked()
        {
            if (ValidateInput())
            {
                SubmitToLobby();
            }
        }

        private void OnCancelButtonClicked()
        {
            gameObject.SetActive(false);
        }

        private void SubmitToLobby()
        {
            if (lobbyScene == null)
            {
                lobbyScene = FindFirstObjectByType<OnlineLobbyScene>();
            }

            if (lobbyScene == null)
            {
                ShowError("ロビーが見つかりません");
                return;
            }

            lobbyScene.OnCreateNewRoom(this);
        }

        /// <summary>
        /// 外部からダイアログを閉じるための公開メソッド。
        /// </summary>
        public void CloseDialog()
        {
            gameObject.SetActive(false);
        }

        // ─── バリデーション ─────────────────────────────────────────

        /// <summary>
        /// 入力値を検証する
        /// </summary>
        /// <returns>検証結果</returns>
        private bool ValidateInput()
        {
            // ルーム名の検証
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = "Room";
            }

            if (roomName.Length > 20)
            {
                ShowError("ルーム名は20文字以内で入力してください");
                return false;
            }

            // パスワードの検証
            if (isPasswordEnabled)
            {
                if (string.IsNullOrEmpty(password))
                {
                    ShowError("パスワードを入力してください");
                    return false;
                }

                if (password.Length != 4)
                {
                    ShowError("パスワードは4桁の数字で入力してください");
                    return false;
                }

                if (!int.TryParse(password, out _))
                {
                    ShowError("パスワードは数字のみで入力してください");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// エラーメッセージを表示する
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// エラーメッセージをクリアする
        /// </summary>
        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        // ─── AbstractCreateNewRoomDialog の実装 ──────────────────────

        public override string RoomName()
        {
            return roomName;
        }

        public override int MaxPlayer()
        {
            return maxPlayer;
        }

        public override string Password()
        {
            return isPasswordEnabled ? password : "";
        }

        public override EGameMode GameMode()
        {
            return selectedGameMode;
        }

        public override bool TeamBalance()
        {
            return teamBalance;
        }

        public override void ShowDialog()
        {
            Debug.Log($"[CreateNewRoomDialog] ルーム作成: " +
                $"名前={roomName}, " +
                $"人数={maxPlayer}, " +
                $"モード={selectedGameMode}, " +
                $"パスワード={(!string.IsNullOrEmpty(password) ? "設定済み" : "なし")}, " +
                $"チームバランス={teamBalance}");

            gameObject.SetActive(true);
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// ダイアログをリセットする
        /// </summary>
        private void ResetDialog()
        {
            roomName = "Room";
            maxPlayer = 8;
            password = "";
            selectedGameMode = EGameMode.TeamDeathMatch;
            teamBalance = true;
            isPasswordEnabled = false;

            if (roomNameInput != null) roomNameInput.text = "";
            if (maxPlayerDropdown != null) maxPlayerDropdown.value = 3;
            if (passwordToggle != null) passwordToggle.isOn = false;
            if (passwordInput != null) passwordInput.text = "";
            if (gameModeDropdown != null) gameModeDropdown.value = 1; // TeamDeathMatch
            if (teamBalanceToggle != null) teamBalanceToggle.isOn = true;
            if (passwordPanel != null) passwordPanel.SetActive(false);

            if (fallbackSummaryText != null)
            {
                fallbackSummaryText.text = BuildFallbackSummary();
            }

            ClearError();
        }

        /// <summary>
        /// ゲームモードの表示名を取得する
        /// </summary>
        /// <summary>
        /// 現在の設定をJSON形式で取得する（デバッグ用）
        /// </summary>
        public string GetSettingsAsJson()
        {
            var settings = new
            {
                RoomName = roomName,
                MaxPlayer = maxPlayer,
                GameMode = selectedGameMode.ToString(),
                HasPassword = isPasswordEnabled,
                TeamBalance = teamBalance
            };
            return JsonUtility.ToJson(settings, true);
        }
    }
}
