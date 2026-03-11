using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// CTFモード用のチームスコア表示UIコンポーネント。
    /// 画面上部にチームごとのキャプチャ数とフラッグ状態を表示する。
    /// 
    /// 【Prefab構成例】
    /// CTFScoreUI (Canvas)
    /// ├─ LeftTeamPanel (HorizontalLayoutGroup)
    /// │   ├─ TeamFlagIcon (Image)
    /// │   ├─ ScoreText (TextMeshProUGUI)
    /// │   └─ FlagStatusIcons (HorizontalLayoutGroup)
    /// │       ├─ FlagIcon1 (Image)
    /// │       ├─ FlagIcon2 (Image)
    /// │       └─ ...
    /// ├─ TimerText (TextMeshProUGUI)
    /// └─ RightTeamPanel (HorizontalLayoutGroup)
    ///     ├─ FlagStatusIcons
    ///     ├─ ScoreText
    ///     └─ TeamFlagIcon
    /// </summary>
    [DisallowMultipleComponent]
    public class CTFScoreUIManager : MonoBehaviour
    {
        public static CTFScoreUIManager Instance { get; private set; }
        [Header("Red Team UI")]
        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private Image redFlagStatusIcon; // フラッグの状態（自陣にある/敵が所持/ドロップ）
        [SerializeField] private GameObject redFlagAtBaseIndicator;
        [SerializeField] private GameObject redFlagCarriedIndicator;
        [SerializeField] private GameObject redFlagDroppedIndicator;

        [Header("Blue Team UI")]
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private Image blueFlagStatusIcon;
        [SerializeField] private GameObject blueFlagAtBaseIndicator;
        [SerializeField] private GameObject blueFlagCarriedIndicator;
        [SerializeField] private GameObject blueFlagDroppedIndicator;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private float matchDuration = 600f; // 10分
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.5f, 0.2f); // オレンジ
        [SerializeField] private Color timerCriticalColor = Color.red;

        [Header("Victory Panel")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI redFinalScoreText;
        [SerializeField] private TextMeshProUGUI blueFinalScoreText;

        [Header("Animation")]
        [SerializeField] private float scorePopDuration = 0.3f;
        [SerializeField] private float scorePopScale = 1.3f;

        // スコア管理
        private int redScore = 0;
        private int blueScore = 0;
        private int captureLimit = 5;
        private float remainingTime;
        private bool isMatchActive = false;

        // フラッグの状態
        public enum FlagState
        {
            AtBase,
            Carried,
            Dropped
        }

        private FlagState redFlagState = FlagState.AtBase;
        private FlagState blueFlagState = FlagState.AtBase;

        private void Awake()
        {
            Instance = this;
            remainingTime = matchDuration;
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            // CTFMatchMainScript のイベントを購読
            if (CTFMatchMainScript.Instance != null)
            {
                CTFMatchMainScript.Instance.OnFlagCaptured += HandleFlagCaptured;
                CTFMatchMainScript.Instance.OnFlagReturned += HandleFlagReturned;
                CTFMatchMainScript.Instance.OnFlagLost += HandleFlagLost;
                CTFMatchMainScript.Instance.OnFlagPickedUp += HandleFlagPickedUp;
            }
        }

        private void OnDisable()
        {
            if (CTFMatchMainScript.Instance != null)
            {
                CTFMatchMainScript.Instance.OnFlagCaptured -= HandleFlagCaptured;
                CTFMatchMainScript.Instance.OnFlagReturned -= HandleFlagReturned;
                CTFMatchMainScript.Instance.OnFlagLost -= HandleFlagLost;
                CTFMatchMainScript.Instance.OnFlagPickedUp -= HandleFlagPickedUp;
            }
        }

        private void Start()
        {
            UpdateScoreDisplay();
            UpdateFlagStatusDisplay();
            StartMatch();
        }

        /// <summary>
        /// マッチを開始する
        /// </summary>
        public void StartMatch()
        {
            isMatchActive = true;
            redScore = 0;
            blueScore = 0;
            remainingTime = matchDuration;
            redFlagState = FlagState.AtBase;
            blueFlagState = FlagState.AtBase;
            UpdateScoreDisplay();
            UpdateFlagStatusDisplay();
        }

        private void Update()
        {
            if (!isMatchActive) return;

            remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (remainingTime <= 0)
            {
                EndMatch();
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // 残り時間に応じた色変更
            if (remainingTime <= 30f)
            {
                timerText.color = timerCriticalColor;
                // 点滅効果
                timerText.alpha = Mathf.PingPong(Time.time * 4f, 1f);
            }
            else if (remainingTime <= 60f)
            {
                timerText.color = timerWarningColor;
                timerText.alpha = 1f;
            }
            else
            {
                timerText.color = timerNormalColor;
                timerText.alpha = 1f;
            }
        }

        #region Event Handlers

        private void HandleFlagCaptured(ETeam capturingTeam)
        {
            if (capturingTeam == ETeam.Red)
            {
                redScore++;
                AnimateScorePop(redScoreText);
            }
            else if (capturingTeam == ETeam.Blue)
            {
                blueScore++;
                AnimateScorePop(blueScoreText);
            }

            UpdateScoreDisplay();
            CheckVictoryCondition();
        }

        private void HandleFlagReturned(ETeam returningTeam)
        {
            // 味方のフラッグが帰還（リターン）also gives a point per CTFRule.md
            if (returningTeam == ETeam.Red)
            {
                redScore++;
                AnimateScorePop(redScoreText);
            }
            else if (returningTeam == ETeam.Blue)
            {
                blueScore++;
                AnimateScorePop(blueScoreText);
            }

            UpdateScoreDisplay();
            CheckVictoryCondition();
        }

        private void HandleFlagLost(ETeam flagTeam)
        {
            // 敵のフラッグをロストした（プレイヤーが倒された）
            if (flagTeam == ETeam.Red)
            {
                redFlagState = FlagState.Dropped;
            }
            else if (flagTeam == ETeam.Blue)
            {
                blueFlagState = FlagState.Dropped;
            }
            UpdateFlagStatusDisplay();
        }

        private void HandleFlagPickedUp(ETeam flagTeam, string playerName)
        {
            // 敵のフラッグを拾った
            if (flagTeam == ETeam.Red)
            {
                redFlagState = FlagState.Carried;
            }
            else if (flagTeam == ETeam.Blue)
            {
                blueFlagState = FlagState.Carried;
            }
            UpdateFlagStatusDisplay();
        }

        #endregion

        #region Public Methods (called from CTFMatchMainScript)

        /// <summary>
        /// サーバーからスコア更新を受け取った場合に呼び出す
        /// </summary>
        public void UpdateScoreFromServer(int redScore, int blueScore)
        {
            this.redScore = redScore;
            this.blueScore = blueScore;
            UpdateScoreDisplay();
        }

        // Legacy compatibility
        public void UpdateScore(int redScore, int blueScore)
        {
            UpdateScoreFromServer(redScore, blueScore);
        }

        /// <summary>
        /// フラッグの状態を更新（サーバー同期用）
        /// </summary>
        public void UpdateFlagStateFromServer(ETeam flagTeam, FlagState state)
        {
            if (flagTeam == ETeam.Red)
            {
                redFlagState = state;
            }
            else if (flagTeam == ETeam.Blue)
            {
                blueFlagState = state;
            }
            UpdateFlagStatusDisplay();
        }

        #endregion

        #region UI Update Methods

        private void UpdateScoreDisplay()
        {
            if (redScoreText != null)
            {
                redScoreText.text = $"{redScore}";
            }
            if (blueScoreText != null)
            {
                blueScoreText.text = $"{blueScore}";
            }
        }

        private void UpdateFlagStatusDisplay()
        {
            // Red Flag Status
            if (redFlagAtBaseIndicator != null)
                redFlagAtBaseIndicator.SetActive(redFlagState == FlagState.AtBase);
            if (redFlagCarriedIndicator != null)
                redFlagCarriedIndicator.SetActive(redFlagState == FlagState.Carried);
            if (redFlagDroppedIndicator != null)
                redFlagDroppedIndicator.SetActive(redFlagState == FlagState.Dropped);

            // Blue Flag Status
            if (blueFlagAtBaseIndicator != null)
                blueFlagAtBaseIndicator.SetActive(blueFlagState == FlagState.AtBase);
            if (blueFlagCarriedIndicator != null)
                blueFlagCarriedIndicator.SetActive(blueFlagState == FlagState.Carried);
            if (blueFlagDroppedIndicator != null)
                blueFlagDroppedIndicator.SetActive(blueFlagState == FlagState.Dropped);
        }

        private void AnimateScorePop(TextMeshProUGUI text)
        {
            if (text == null) return;

            text.transform.DOKill();
            text.transform.localScale = Vector3.one * scorePopScale;
            text.transform.DOScale(Vector3.one, scorePopDuration).SetEase(Ease.OutBack);
        }

        private void CheckVictoryCondition()
        {
            if (redScore >= captureLimit)
            {
                ShowVictory(ETeam.Red);
            }
            else if (blueScore >= captureLimit)
            {
                ShowVictory(ETeam.Blue);
            }
        }

        private void ShowVictory(ETeam winningTeam)
        {
            isMatchActive = false;

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (winnerText != null)
            {
                winnerText.text = winningTeam == ETeam.Red ? "RED TEAM WINS!" : "BLUE TEAM WINS!";
                winnerText.color = winningTeam == ETeam.Red ? Color.red : Color.blue;
            }

            if (redFinalScoreText != null)
            {
                redFinalScoreText.text = $"Red: {redScore}";
            }
            if (blueFinalScoreText != null)
            {
                blueFinalScoreText.text = $"Blue: {blueScore}";
            }
        }

        private void EndMatch()
        {
            isMatchActive = false;

            // スコアが多い方の勝利
            if (redScore > blueScore)
            {
                ShowVictory(ETeam.Red);
            }
            else if (blueScore > redScore)
            {
                ShowVictory(ETeam.Blue);
            }
            else
            {
                // 引き分け
                if (victoryPanel != null) victoryPanel.SetActive(true);
                if (winnerText != null) winnerText.text = "DRAW!";
            }
        }

        #endregion

        /// <summary>
        /// フラッグの状態を外部から更新（FlagStandからの連携用）
        /// </summary>
        public void UpdateRedFlagState(bool atBase, bool carried, bool dropped)
        {
            if (atBase) redFlagState = FlagState.AtBase;
            else if (carried) redFlagState = FlagState.Carried;
            else if (dropped) redFlagState = FlagState.Dropped;
            UpdateFlagStatusDisplay();
        }

        /// <summary>
        /// フラッグの状態を外部から更新（FlagStandからの連携用）
        /// </summary>
        public void UpdateBlueFlagState(bool atBase, bool carried, bool dropped)
        {
            if (atBase) blueFlagState = FlagState.AtBase;
            else if (carried) blueFlagState = FlagState.Carried;
            else if (dropped) blueFlagState = FlagState.Dropped;
            UpdateFlagStatusDisplay();
        }

        /// <summary>
        /// キャプチャ制限数を設定
        /// </summary>
        public void SetCaptureLimit(int limit)
        {
            captureLimit = limit;
        }

        /// <summary>
        /// マッチ時間を設定
        /// </summary>
        public void SetMatchDuration(float duration)
        {
            matchDuration = duration;
            remainingTime = duration;
        }
    }
}
