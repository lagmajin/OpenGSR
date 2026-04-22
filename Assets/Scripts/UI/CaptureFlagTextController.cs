using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// CTFモード専用のアナウンスUIコンポーネント。
    /// 「(プレイヤー名) が [固定スプライトのメッセージ] ！」という形式で画面中央などに表示・フェードアウトさせる。
    /// 
    /// 【Prefab構成例】
    /// CaptureFlagText (CanvasGroup)
    ///  ├─ HorizontalLayoutGroup
    ///  │   ├─ PlayerNameText (TextMeshProUGUI)
    ///  │   └─ MessageImage (Image)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class CaptureFlagTextController : MonoBehaviour
    {
        [Header("UI パーツ")]
        [SerializeField] private TextMeshProUGUI playerNameText; // 動的なプレイヤー名のテキスト
        [SerializeField] private Image messageImage;             // 固定メッセージのスプライトを表示するImage

        [Header("メッセージスプライト")]
        [Tooltip("「Flag Captured」のスプライト")]
        [SerializeField] private Sprite capturedSprite;
        [Tooltip("「Flag Dropped」のスプライト")]
        [SerializeField] private Sprite droppedSprite;
        [Tooltip("「Flag Returned」のスプライト")]
        [SerializeField] private Sprite returnedSprite;

        [Header("アニメーション設定")]
        [SerializeField] private float inDuration = 0.3f;   // フェードイン＆スライドイン時間
        [SerializeField] private float holdDuration = 2.0f; // 表示をキープする時間
        [SerializeField] private float outDuration = 0.3f;  // フェードアウト＆スライドアウト時間

        public enum EFlagMessage
        {
            Captured,
            Dropped,
            Returned
        }

        private CanvasGroup canvasGroup;
        private Sequence currentSequence;
        private RectTransform rectTransform;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            // CTFMatchMainScript のイベントを購読して自動的にメッセージを表示する
            if (CTFMatchMainScript.Instance != null)
            {
                CTFMatchMainScript.Instance.OnFlagCaptured += HandleOnFlagCaptured;
                CTFMatchMainScript.Instance.OnFlagReturned += HandleOnFlagReturned;
                CTFMatchMainScript.Instance.OnFlagLost += HandleOnFlagLost;
                CTFMatchMainScript.Instance.OnFlagPickedUp += HandleOnFlagPickedUp;
            }
        }

        private void OnDisable()
        {
            if (CTFMatchMainScript.Instance != null)
            {
                CTFMatchMainScript.Instance.OnFlagCaptured -= HandleOnFlagCaptured;
                CTFMatchMainScript.Instance.OnFlagReturned -= HandleOnFlagReturned;
                CTFMatchMainScript.Instance.OnFlagLost -= HandleOnFlagLost;
                CTFMatchMainScript.Instance.OnFlagPickedUp -= HandleOnFlagPickedUp;
            }
        }

        private void HandleOnFlagCaptured(ETeam capturingTeam)
        {
            // チーム名などをプレイヤー名として表示するか、空にする
            Color teamColor = (capturingTeam == ETeam.Red) ? Color.red : Color.blue;
            ShowMessage($"{capturingTeam} TEAM", EFlagMessage.Captured, teamColor);
        }

        private void HandleOnFlagReturned(ETeam returningTeam)
        {
            Color teamColor = (returningTeam == ETeam.Red) ? Color.red : Color.blue;
            ShowMessage($"{returningTeam} TEAM FLAG", EFlagMessage.Returned, teamColor);
        }

        private void HandleOnFlagLost(ETeam flagTeam)
        {
            // フラッグを落とした（ロストした）
            ShowMessage("", EFlagMessage.Dropped, Color.white);
        }

        private void HandleOnFlagPickedUp(ETeam flagTeam, string playerName)
        {
            // 敵のフラッグを拾った。flagTeamは「拾われたフラッグ」のチームだが、
            // 通知としては「拾ったプレイヤー」の色を出したい。
            // 簡易的にフラッグと逆の色を表示（Redフラッグが拾われた = Blueプレイヤー）
            Color playerColor = (flagTeam == ETeam.Red) ? Color.blue : Color.red;
            ShowMessage(playerName, EFlagMessage.Captured, playerColor); // Capturedスプライトを流用するか、PickUp用があればそれを使う
        }

        /// <summary>
        /// アナウンスを表示してアニメーション再生し、終わったら非表示になる
        /// </summary>
        /// <param name="playerName">メッセージの対象となるプレイヤー名</param>
        /// <param name="messageType">表示するメッセージ(スプライト)の種類</param>
        /// <param name="playerColor">プレイヤー名の文字色</param>
        public void ShowMessage(string playerName, EFlagMessage messageType, Color playerColor)
        {
            // 動的プレイヤー名の反映
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
                playerNameText.color = playerColor;
                
                // プレイヤー名が空ならテキスト自体を無効化（「Flag Dropped」だけ出したい時用）
                playerNameText.gameObject.SetActive(!string.IsNullOrEmpty(playerName));
            }

            // メッセージスプライトの反映
            if (messageImage != null)
            {
                Sprite targetSprite = messageType switch
                {
                    EFlagMessage.Captured => capturedSprite,
                    EFlagMessage.Dropped => droppedSprite,
                    EFlagMessage.Returned => returnedSprite,
                    _ => null
                };

                if (targetSprite != null)
                {
                    messageImage.sprite = targetSprite;
                    messageImage.SetNativeSize(); // 元のスプライトの縦横比に合わせる
                    messageImage.gameObject.SetActive(true);
                }
                else
                {
                    messageImage.gameObject.SetActive(false);
                }
            }

            PlayAnimation();
        }

        private void PlayAnimation()
        {
            currentSequence?.Kill();

            // 初期化: 完全に透明、少し下からスタート
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -50f);

            currentSequence = DOTween.Sequence()
                .SetLink(gameObject)
                // 上へスライドしつつフェードイン
                .Append(canvasGroup.DOFade(1f, inDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOAnchorPosY(0f, inDuration).SetEase(Ease.OutBack))
                // 維持
                .AppendInterval(holdDuration)
                // さらに上へスライドしつつフェードアウト
                .Append(canvasGroup.DOFade(0f, outDuration).SetEase(Ease.InQuad))
                .Join(rectTransform.DOAnchorPosY(50f, outDuration).SetEase(Ease.InBack));

            currentSequence.Play();
        }

        private void OnDestroy()
        {
            currentSequence?.Kill();
        }
    }
}
