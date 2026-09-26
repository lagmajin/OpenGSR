using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace OpenGS
{
    /// <summary>
    /// TDM（Team Death Match）用のチームスコア表示UIコンポーネント。
    /// 画面上部にチームごとのキル数と残り時間を表示する。
    /// </summary>
    [DisallowMultipleComponent]
    public class TDMScoreUIManager : MonoBehaviour
    {
        [Header("Red Team UI")]
        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private TextMeshProUGUI redKillCountText;

        [Header("Blue Team UI")]
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private TextMeshProUGUI blueKillCountText;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private float matchDuration = 600f; // 10分
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.5f, 0.2f); // オレンジ
        [SerializeField] private Color timerCriticalColor = Color.red;

        [Header("Kill Limit")]
        [SerializeField] private int killLimit = 50; // 勝利所需のキル数

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
        private int redKills = 0;
        private int blueKills = 0;
        private float remainingTime;
        private bool isMatchActive = false;

        // イベント
        public event Action<ETeam> OnMatchEnded;
        public event Action OnTimeUpdated;

        private void Awake()
        {
            matchDuration = NormalizeNonNegative(matchDuration, 600f);
            killLimit = Mathf.Max(1, killLimit);
            scorePopDuration = NormalizeNonNegative(scorePopDuration, 0.3f);
            scorePopScale = NormalizePositive(scorePopScale, 1.3f);

            if (victoryPanel != null)
                victoryPanel.SetActive(false);
        }

        private static float NormalizeNonNegative(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(0f, value) : fallback;
        }

        private static float NormalizePositive(float value, float fallback)
        {
            return float.IsFinite(value) && value > 0f ? value : fallback;
        }

        private static int SaturatingAdd(int value, int amount)
        {
            return value > int.MaxValue - amount ? int.MaxValue : value + amount;
        }

        private static int SaturatingMultiply(int value, int multiplier)
        {
            return value > int.MaxValue / multiplier ? int.MaxValue : value * multiplier;
        }

        private void OnDestroy()
        {
            // イベント購読解除
        }

        private void Start()
        {
            UpdateScoreDisplay();
            UpdateKillCountDisplay();
            UpdateTimerDisplay();
        }

        /// <summary>
        /// マッチを開始する
        /// </summary>
        public void StartMatch()
        {
            isMatchActive = true;
            redScore = 0;
            blueScore = 0;
            redKills = 0;
            blueKills = 0;
            remainingTime = matchDuration;
            UpdateScoreDisplay();
            UpdateKillCountDisplay();
            UpdateTimerDisplay();

            if (victoryPanel != null)
                victoryPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isMatchActive) return;

            var deltaTime = Time.deltaTime;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                return;
            }
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
            UpdateTimerDisplay();

            // 時間警告色の更新
            UpdateTimerColor();

            if (remainingTime <= 0)
            {
                EndMatch();
            }

            InvokeSafely(OnTimeUpdated, nameof(OnTimeUpdated));
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void UpdateTimerColor()
        {
            if (timerText == null) return;

            if (remainingTime <= 30f)
            {
                timerText.color = timerCriticalColor;
            }
            else if (remainingTime <= 60f)
            {
                timerText.color = timerWarningColor;
            }
            else
            {
                timerText.color = timerNormalColor;
            }
        }

        #region Score Management

        /// <summary>
        /// レッドチームのキル数を增加
        /// </summary>
        public void AddRedKill()
        {
            if (redKills < int.MaxValue) redKills++;
            redScore = SaturatingAdd(redScore, 100); // キルスコア
            UpdateKillCountDisplay();
            UpdateScoreDisplay();
            AnimateScorePop(redKillCountText);
            CheckWinCondition(ETeam.Red);
        }

        /// <summary>
        /// ブルーチームのキル数を增加
        /// </summary>
        public void AddBlueKill()
        {
            if (blueKills < int.MaxValue) blueKills++;
            blueScore = SaturatingAdd(blueScore, 100); // キルスコア
            UpdateKillCountDisplay();
            UpdateScoreDisplay();
            AnimateScorePop(blueKillCountText);
            CheckWinCondition(ETeam.Blue);
        }

        /// <summary>
        /// サーバーからのスコア更新（ネットワーク同期用）
        /// </summary>
        public void UpdateScoreFromServer(int redKills, int blueKills)
        {
            this.redKills = Mathf.Max(0, redKills);
            this.blueKills = Mathf.Max(0, blueKills);
            this.redScore = SaturatingMultiply(this.redKills, 100);
            this.blueScore = SaturatingMultiply(this.blueKills, 100);
            UpdateKillCountDisplay();
            UpdateScoreDisplay();
        }

        /// <summary>
        /// 勝利条件をチェック
        /// </summary>
        private void CheckWinCondition(ETeam lastKillingTeam)
        {
            if (!isMatchActive) return;

            if (redKills >= killLimit)
            {
                EndMatchWithWinner(ETeam.Red);
            }
            else if (blueKills >= killLimit)
            {
                EndMatchWithWinner(ETeam.Blue);
            }
        }

        /// <summary>
        /// マッチ終了（時間切れ）
        /// </summary>
        private void EndMatch()
        {
            isMatchActive = false;

            ETeam winner;
            if (redKills > blueKills)
                winner = ETeam.Red;
            else if (blueKills > redKills)
                winner = ETeam.Blue;
            else
                winner = ETeam.NoTeam; // 引き分け

            ShowVictoryPanel(winner);
            InvokeSafely(OnMatchEnded, winner, nameof(OnMatchEnded));
        }

        /// <summary>
        /// 勝利チームを指定してマッチ終了
        /// </summary>
        private void EndMatchWithWinner(ETeam winner)
        {
            isMatchActive = false;
            ShowVictoryPanel(winner);
            InvokeSafely(OnMatchEnded, winner, nameof(OnMatchEnded));
        }

        private static void InvokeSafely(Action handlers, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TDMScoreUIManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<ETeam> handlers, ETeam winner, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<ETeam> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(winner);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TDMScoreUIManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        /// <summary>
        /// 勝利パネルを表示
        /// </summary>
        private void ShowVictoryPanel(ETeam winner)
        {
            if (victoryPanel == null) return;

            victoryPanel.SetActive(true);

            if (winnerText != null)
            {
                if (winner == ETeam.Red)
                {
                    winnerText.text = "RED TEAM WINS!";
                    winnerText.color = Color.red;
                }
                else if (winner == ETeam.Blue)
                {
                    winnerText.text = "BLUE TEAM WINS!";
                    winnerText.color = Color.blue;
                }
                else
                {
                    winnerText.text = "DRAW!";
                    winnerText.color = Color.white;
                }
            }

            if (redFinalScoreText != null)
                redFinalScoreText.text = $"Red Kills: {redKills}";

            if (blueFinalScoreText != null)
                blueFinalScoreText.text = $"Blue Kills: {blueKills}";
        }

        #endregion

        #region UI Update Methods

        private void UpdateScoreDisplay()
        {
            if (redScoreText != null)
                redScoreText.text = $"{redScore}";
            if (blueScoreText != null)
                blueScoreText.text = $"{blueScore}";
        }

        private void UpdateKillCountDisplay()
        {
            if (redKillCountText != null)
                redKillCountText.text = $"Kills: {redKills} / {killLimit}";
            if (blueKillCountText != null)
                blueKillCountText.text = $"Kills: {blueKills} / {killLimit}";
        }

        private void AnimateScorePop(TextMeshProUGUI text)
        {
            if (text == null) return;

            text.transform.DOScale(scorePopScale, scorePopDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(text.gameObject)
                .OnComplete(() =>
                {
                    text.transform.DOScale(1f, scorePopDuration)
                        .SetEase(Ease.InQuad)
                        .SetLink(text.gameObject);
                });
        }

        #endregion

        #region Public Properties

        public int RedKills => redKills;
        public int BlueKills => blueKills;
        public int RedScore => redScore;
        public int BlueScore => blueScore;
        public float RemainingTime => remainingTime;
        public bool IsMatchActive => isMatchActive;
        public int KillLimit => killLimit;

        #endregion
    }
}
