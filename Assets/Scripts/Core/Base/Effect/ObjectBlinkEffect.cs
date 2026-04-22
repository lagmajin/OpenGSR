using UnityEngine;
using DG.Tweening;
using System.Collections;
//using TMPro;

namespace OpenGS
{
    public interface IBlinkEffect
    {
        void SetAutoDelete();
    }
    public enum BlinkEaseType
    {
        InOutSine,
        InQuad,
        OutQuad,
        Linear,
        InOutElastic,
        OutBounce
    }
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class ObjectBlinkEffect : MonoBehaviour
    {
        public GameObject targetObject; // 点滅対象
        public float blinkInterval = 0.5f; // ブリンク間隔（秒）
        public bool autoDelete = false; // 自動削除
        public float deleteTime = 5f; // 自動削除までの時間
        public BlinkEaseType blinkEase = BlinkEaseType.InOutSine;
        private Tween blinkTween;


        private void Start()
        {
            if (targetObject == null) targetObject = gameObject;

            StartBlinking();

            if (autoDelete) Destroy(gameObject, deleteTime);
        }

        private void StartBlinking()
        {
            SpriteRenderer spriteRenderer = targetObject.GetComponent<SpriteRenderer>();

            blinkTween = spriteRenderer
                .DOFade(0f, blinkInterval * 0.5f)          // 点滅の透明度を設定
                .SetLoops(-1, LoopType.Yoyo)              // 無限ループ
                .SetEase(GetEaseFromEnum(blinkEase));      // enumからEaseを取得して適用
        }

        public void SetBlinkInterval(float interval)
        {
            blinkInterval = Mathf.Max(0.01f, interval);
            Restart();
        }

        public void Restart()
        {
            blinkTween.Kill();
            StartBlinking();
        }

        private void OnDestroy()
        {
            blinkTween.Kill();
        }

        // enumに対応するEaseを返すメソッド
        private Ease GetEaseFromEnum(BlinkEaseType easeType)
        {
            switch (easeType)
            {
                case BlinkEaseType.InOutSine:
                    return Ease.InOutSine;
                case BlinkEaseType.InQuad:
                    return Ease.InQuad;
                case BlinkEaseType.OutQuad:
                    return Ease.OutQuad;
                case BlinkEaseType.Linear:
                    return Ease.Linear;
                case BlinkEaseType.InOutElastic:
                    return Ease.InOutElastic;
                case BlinkEaseType.OutBounce:
                    return Ease.OutBounce;
                default:
                    return Ease.InOutSine; // デフォルトはInOutSine
            }
        }

    }

    }


