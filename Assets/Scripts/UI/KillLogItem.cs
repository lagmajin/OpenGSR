using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// キルログ（フィード）の1行分を担当するUIコンポーネント。
    /// 生成されると自動的に[フェードイン]し、一定時間後に[フェードアウト＆自己破棄]する。
    ///
    /// 【Prefab 構成例】
    /// KillLogItem
    ///  ├─ CanvasGroup (フェード制御)
    ///  ├─ HorizontalLayoutGroup
    ///  │   ├─ KillerText (TMP: 赤とか青のプレイヤー名)
    ///  │   ├─ WeaponIcon (Image: 使われた武器)
    ///  │   └─ VictimText (TMP: 倒されたプレイヤー名)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class KillLogItem : MonoBehaviour
    {
        [Header("UI パーツ")]
        [SerializeField] private TextMeshProUGUI killerText;
        [SerializeField] private TextMeshProUGUI victimText;
        [SerializeField] private Image weaponIconImage;

        [Header("アニメーション設定")]
        [SerializeField] private float displayDuration = 4.0f; // 表示し続ける時間
        [SerializeField] private float fadeDuration = 0.3f;    // フェードの時間

        private CanvasGroup canvasGroup;
        private Sequence currentSequence;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// キルログの情報をセットし、アニメーションを開始する。
        /// </summary>
        public void Setup(string killerName, string victimName, Sprite weaponSprite, Color killerColor, Color victimColor)
        {
            if (killerText != null)
            {
                killerText.text = killerName;
                killerText.color = killerColor;
            }

            if (victimText != null)
            {
                victimText.text = victimName;
                victimText.color = victimColor;
            }

            if (weaponIconImage != null)
            {
                if (weaponSprite != null)
                {
                    weaponIconImage.sprite = weaponSprite;
                    weaponIconImage.gameObject.SetActive(true);
                }
                else
                {
                    weaponIconImage.gameObject.SetActive(false);
                }
            }

            PlayAnimation();
        }

        private void PlayAnimation()
        {
            currentSequence?.Kill();

            // 初期位置から少しだけ右にスライドしながらフェードインさせる演出
            transform.localPosition += new Vector3(30f, 0, 0);

            currentSequence = DOTween.Sequence()
                .SetLink(gameObject)
                // スライド＆フェードイン
                .Append(canvasGroup.DOFade(1f, fadeDuration))
                .Join(transform.DOLocalMoveX(-30f, fadeDuration).SetRelative(true).SetEase(Ease.OutCubic))
                // 維持
                .AppendInterval(displayDuration)
                // フェードアウト
                .Append(canvasGroup.DOFade(0f, fadeDuration))
                // 破棄
                .OnComplete(() => Destroy(gameObject));

            currentSequence.Play();
        }

        /// <summary>
        /// ログが多すぎる場合に即座にフェードアウトさせて消す用。
        /// </summary>
        public void ForceFadeOut()
        {
            currentSequence?.Kill();
            canvasGroup.DOFade(0f, fadeDuration).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            currentSequence?.Kill();
        }
    }
}
