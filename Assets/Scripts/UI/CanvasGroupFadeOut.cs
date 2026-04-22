using UnityEngine;
using DG.Tweening;

namespace OpenGS
{
    /// <summary>
    /// アタッチされた CanvasGroup をフェードアウトさせる汎用コンポーネント。
    /// OnEnable() 時に自動実行することも、スクリプトから手動実行することも可能。
    /// 完了後に GameObject を破棄または非アクティブにするオプション備える。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class CanvasGroupFadeOut : MonoBehaviour
    {
        public enum OnCompleteAction
        {
            None,
            Deactivate,
            Destroy
        }

        [Header("設定")]
        [SerializeField] private float fadeTime = 0.5f;
        [SerializeField] private float delayTime = 0.0f;
        [SerializeField] private bool playOnEnable = false; // FadeOutは手動実行が多いためfalse開始

        [Header("完了時の処理")]
        [SerializeField] private OnCompleteAction onCompleteAction = OnCompleteAction.None;

        [Header("対象")]
        [SerializeField] private CanvasGroup group;

        private void Awake()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        public void Play()
        {
            if (group == null) return;

            // フェードアウト実行
            group.DOFade(0f, fadeTime)
                 .SetDelay(delayTime)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(HandleComplete);
        }

        private void HandleComplete()
        {
            switch (onCompleteAction)
            {
                case OnCompleteAction.Deactivate:
                    gameObject.SetActive(false);
                    break;
                case OnCompleteAction.Destroy:
                    Destroy(gameObject);
                    break;
            }
        }
    }
}
