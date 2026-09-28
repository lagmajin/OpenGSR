using System;
using OpenGSCore;
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

        [Header("Match Flow")]
        [SerializeField, Min(0f)] private float preMatchCountdownSeconds = 3f;
        [SerializeField, Min(0f)] private float postMatchHoldSeconds = 3f;
        [SerializeField] private TextMeshProUGUI matchStatusText;

        [Header("Animation")]
        [SerializeField] private float scorePopDuration = 0.3f;
        [SerializeField] private float scorePopScale = 1.3f;

        // スコア管理
        private int redScore = 0;
        private int blueScore = 0;
        private int captureLimit = 5;
        private int previousRedScore = 0;
        private int previousBlueScore = 0;
        private float remainingTime;
        private bool isMatchActive = false;
        private bool isPreparingMatch = false;
        private Coroutine matchCountdownRoutine;
        private CTFMatchMainScript subscribedMatch;

        // フラッグの状態
        // The shared contract's vocabulary. This used to be a third enum with
        // AtBase, Carried and Dropped, next to the flag component's own copy and
        // the server's. The indicators below are driven by what the server says
        // where a flag is, so the state they compare against has to be the same
        // state the server sends.
        private EFlagState redFlagState = EFlagState.FlagOnStand;
        private EFlagState blueFlagState = EFlagState.FlagOnStand;

        private void Awake()
        {
            matchDuration = NormalizeNonNegative(matchDuration, 600f);
            preMatchCountdownSeconds = NormalizeNonNegative(preMatchCountdownSeconds, 3f);
            postMatchHoldSeconds = NormalizeNonNegative(postMatchHoldSeconds, 3f);
            captureLimit = Mathf.Max(1, captureLimit);
            scorePopDuration = NormalizeNonNegative(scorePopDuration, 0.3f);
            scorePopScale = NormalizePositive(scorePopScale, 1.3f);

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CTFScoreUIManager] Duplicate instance found; destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            remainingTime = matchDuration;
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        private static float NormalizeNonNegative(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : fallback;
        }

        private static float NormalizePositive(float value, float fallback)
        {
            return float.IsFinite(value) && value > 0f ? value : fallback;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            TrySubscribeToMatch();
        }

        private void OnDisable()
        {
            if (subscribedMatch != null)
            {
                subscribedMatch.OnFlagCaptured -= HandleFlagCaptured;
                subscribedMatch.OnFlagReturned -= HandleFlagReturned;
                subscribedMatch.OnFlagLost -= HandleFlagLost;
                subscribedMatch.OnFlagPickedUp -= HandleFlagPickedUp;
                subscribedMatch = null;
            }
        }

        private void Start()
        {
            TrySubscribeToMatch();
            UpdateScoreDisplay();
            UpdateFlagStatusDisplay();
            PrepareMatch();
        }

        private void TrySubscribeToMatch()
        {
            var match = CTFMatchMainScript.Instance;
            if (match == null || subscribedMatch == match)
            {
                return;
            }

            if (subscribedMatch != null)
            {
                subscribedMatch.OnFlagCaptured -= HandleFlagCaptured;
                subscribedMatch.OnFlagReturned -= HandleFlagReturned;
                subscribedMatch.OnFlagLost -= HandleFlagLost;
                subscribedMatch.OnFlagPickedUp -= HandleFlagPickedUp;
            }

            subscribedMatch = match;
            subscribedMatch.OnFlagCaptured += HandleFlagCaptured;
            subscribedMatch.OnFlagReturned += HandleFlagReturned;
            subscribedMatch.OnFlagLost += HandleFlagLost;
            subscribedMatch.OnFlagPickedUp += HandleFlagPickedUp;
        }

        /// <summary>
        /// マッチを開始する
        /// </summary>
        public void StartMatch()
        {
            if (matchCountdownRoutine != null)
            {
                StopCoroutine(matchCountdownRoutine);
            }

            matchCountdownRoutine = StartCoroutine(BeginMatchRoutine());
        }

        public void PrepareMatch()
        {
            isMatchActive = false;
            isPreparingMatch = true;
            redScore = 0;
            blueScore = 0;
            previousRedScore = 0;
            previousBlueScore = 0;
            remainingTime = matchDuration;
            redFlagState = EFlagState.FlagOnStand;
            blueFlagState = EFlagState.FlagOnStand;
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
            UpdateScoreDisplay();
            UpdateFlagStatusDisplay();
            UpdateTimerDisplay();
            SetMatchStatusText("READY");
        }

        private System.Collections.IEnumerator BeginMatchRoutine()
        {
            isMatchActive = false;
            isPreparingMatch = true;

            var countdown = Mathf.Max(0f, preMatchCountdownSeconds);
            while (countdown > 0f)
            {
                SetMatchStatusText($"START {Mathf.CeilToInt(countdown)}");
                UpdateTimerDisplay();
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    yield return null;
                    continue;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                countdown = Mathf.Max(0f, countdown - deltaTime);
                yield return null;
            }

            isPreparingMatch = false;
            isMatchActive = true;
            SetMatchStatusText(string.Empty);
            UpdateTimerDisplay();
        }

        private void Update()
        {
            if (subscribedMatch == null)
            {
                TrySubscribeToMatch();
            }

            if (!isMatchActive) return;

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
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
                var now = Time.time;
                timerText.alpha = float.IsFinite(now) && now >= 0f
                    ? Mathf.PingPong(now * 4f, 1f)
                    : 1f;
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
            UpdateFlagStatusDisplay();
        }

        private void HandleFlagReturned(ETeam returningTeam)
        {
            // フラッグが自陣に戻っただけでは得点しない。
            // スタンド側の状態更新で表示は同期される。
            UpdateFlagStatusDisplay();
        }

        private void HandleFlagLost(ETeam flagTeam)
        {
            // 敵のフラッグをロストした（プレイヤーが倒された）
            if (flagTeam == ETeam.Red)
            {
                redFlagState = EFlagState.FlagOnGround;
            }
            else if (flagTeam == ETeam.Blue)
            {
                blueFlagState = EFlagState.FlagOnGround;
            }
            UpdateFlagStatusDisplay();
        }

        private void HandleFlagPickedUp(ETeam flagTeam, string playerName)
        {
            // 敵のフラッグを拾った
            if (flagTeam == ETeam.Red)
            {
                redFlagState = EFlagState.FlagCapturedPlayer;
            }
            else if (flagTeam == ETeam.Blue)
            {
                blueFlagState = EFlagState.FlagCapturedPlayer;
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
            redScore = Mathf.Max(0, redScore);
            blueScore = Mathf.Max(0, blueScore);
            var redChanged = redScore != this.redScore;
            var blueChanged = blueScore != this.blueScore;

            this.redScore = redScore;
            this.blueScore = blueScore;

            if (redChanged && redScore > previousRedScore)
            {
                AnimateScorePop(redScoreText);
            }
            if (blueChanged && blueScore > previousBlueScore)
            {
                AnimateScorePop(blueScoreText);
            }

            previousRedScore = redScore;
            previousBlueScore = blueScore;
            UpdateScoreDisplay();
            CheckVictoryCondition();
        }

        // Legacy compatibility
        public void UpdateScore(int redScore, int blueScore)
        {
            UpdateScoreFromServer(redScore, blueScore);
        }

        /// <summary>
        /// フラッグの状態を更新（サーバー同期用）
        /// </summary>
        public void UpdateFlagStateFromServer(ETeam flagTeam, EFlagState state)
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

        /// <summary>
        /// Says out loud that a delivery did not score, and why.
        /// <para>
        /// The flag indicators already show where each flag is, so a player can
        /// see that their own flag is not home. What was missing was the
        /// connection between the two: a player walked the flag to the enemy
        /// stand, nothing happened, and nothing said why. The reason is the
        /// useful part, so it is what this reports.
        /// </para>
        /// </summary>
        public void ShowCaptureRefused(ETeam scoringTeam, EFlagRefusal refusal)
        {
            var message = refusal switch
            {
                EFlagRefusal.OwnFlagNotAtBase =>
                    "Your own flag is not at base, so that capture does not count.",
                EFlagRefusal.NoEnemyFlagCarried =>
                    "You are not carrying the enemy flag, so there is nothing to deliver.",
                EFlagRefusal.NotATeam =>
                    "You are not on a team, so the delivery does not count.",
                _ => "That capture did not count."
            };

            Debug.LogWarning($"[CTF] {message} (team {scoringTeam})");
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
                redFlagAtBaseIndicator.SetActive(redFlagState == EFlagState.FlagOnStand);
            if (redFlagCarriedIndicator != null)
                redFlagCarriedIndicator.SetActive(redFlagState == EFlagState.FlagCapturedPlayer);
            if (redFlagDroppedIndicator != null)
                redFlagDroppedIndicator.SetActive(redFlagState == EFlagState.FlagOnGround);

            // Blue Flag Status
            if (blueFlagAtBaseIndicator != null)
                blueFlagAtBaseIndicator.SetActive(blueFlagState == EFlagState.FlagOnStand);
            if (blueFlagCarriedIndicator != null)
                blueFlagCarriedIndicator.SetActive(blueFlagState == EFlagState.FlagCapturedPlayer);
            if (blueFlagDroppedIndicator != null)
                blueFlagDroppedIndicator.SetActive(blueFlagState == EFlagState.FlagOnGround);
        }

        private void AnimateScorePop(TextMeshProUGUI text)
        {
            if (text == null) return;

            text.transform.DOKill();
            text.transform.localScale = Vector3.one * scorePopScale;
            text.transform.DOScale(Vector3.one, scorePopDuration)
                .SetEase(Ease.OutBack)
                .SetLink(text.gameObject);
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

        public void ShowVictory(ETeam winningTeam, int redFinalScore, int blueFinalScore)
        {
            redScore = redFinalScore;
            blueScore = blueFinalScore;
            previousRedScore = redFinalScore;
            previousBlueScore = blueFinalScore;
            ShowVictory(winningTeam);
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
            if (atBase) redFlagState = EFlagState.FlagOnStand;
            else if (carried) redFlagState = EFlagState.FlagCapturedPlayer;
            else if (dropped) redFlagState = EFlagState.FlagOnGround;
            UpdateFlagStatusDisplay();
        }

        /// <summary>
        /// フラッグの状態を外部から更新（FlagStandからの連携用）
        /// </summary>
        public void UpdateBlueFlagState(bool atBase, bool carried, bool dropped)
        {
            if (atBase) blueFlagState = EFlagState.FlagOnStand;
            else if (carried) blueFlagState = EFlagState.FlagCapturedPlayer;
            else if (dropped) blueFlagState = EFlagState.FlagOnGround;
            UpdateFlagStatusDisplay();
        }

        /// <summary>
        /// キャプチャ制限数を設定
        /// </summary>
        public void SetCaptureLimit(int limit)
        {
            captureLimit = Mathf.Max(1, limit);
        }

        /// <summary>
        /// マッチ時間を設定
        /// </summary>
        public void SetMatchDuration(float duration)
        {
            matchDuration = NormalizeNonNegative(duration, 600f);
            remainingTime = matchDuration;
        }

        private void SetMatchStatusText(string message)
        {
            if (matchStatusText != null)
            {
                matchStatusText.text = message;
            }
            else if (timerText != null && !isMatchActive)
            {
                timerText.text = message;
            }
        }
    }
}

