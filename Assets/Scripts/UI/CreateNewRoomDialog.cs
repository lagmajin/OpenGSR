using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenGSCore;
using System;
using System.Linq;

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

        // ─── 内部状態 ───────────────────────────────────────────────

        private string roomName = "";
        private int maxPlayer = 8;
        private string password = "";
        private EGameMode selectedGameMode = EGameMode.TeamDeathMatch;
        private bool teamBalance = true;
        private bool isPasswordEnabled = false;

        // ─── Unity ライフサイクル ────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            InitializeUI();
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
                var gameModes = GameMode.AllGameMode();
                var modeOptions = gameModes.Select(m => GetGameModeDisplayName(m)).ToList();
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
            var gameModes = GameMode.AllGameMode();
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
                ShowDialog();
            }
        }

        private void OnCancelButtonClicked()
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
                ShowError("ルーム名を入力してください");
                return false;
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
            roomName = "";
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

            ClearError();
        }

        /// <summary>
        /// ゲームモードの表示名を取得する
        /// </summary>
        private string GetGameModeDisplayName(EGameMode mode)
        {
            switch (mode)
            {
                case EGameMode.DeathMatch:
                    return "デスマッチ (DM)";
                case EGameMode.TeamDeathMatch:
                    return "チームデスマッチ (TDM)";
                case EGameMode.Survival:
                    return "サバイバル (SUV)";
                case EGameMode.TeamSurvival:
                    return "チームサバイバル (TSUV)";
                case EGameMode.CaptureTheFlag:
                    return "キャプチャー・ザ・フラッグ (CTF)";
                case EGameMode.OneShotKill:
                    return "ワンショットキル";
                case EGameMode.ArmsRace:
                    return "アームズレース";
                case EGameMode.Sniper:
                    return "スナイパー";
                case EGameMode.TowerMatch:
                    return "タワーマッチ";
                case EGameMode.Practice:
                    return "プラクティス";
                case EGameMode.FreeStyle:
                    return "フリースタイル";
                default:
                    return mode.ToString();
            }
        }

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