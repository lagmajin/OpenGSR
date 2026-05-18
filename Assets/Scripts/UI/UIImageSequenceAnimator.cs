using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// UI Image の sprite を順番に切り替えるシンプルなアニメーター。
    /// 3 枚のロゴやロード演出を 1 枚の Image で表示したいときに使う。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class UIImageSequenceAnimator : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameInterval = 0.12f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool hideWhenStopped = false;

        private Coroutine playCoroutine;
        private int currentIndex;

        private void Reset()
        {
            targetImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            currentIndex = 0;
        }

        public void Play()
        {
            if (targetImage == null || frames == null || frames.Length == 0)
            {
                return;
            }

            Stop();
            playCoroutine = StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            if (hideWhenStopped && targetImage != null)
            {
                targetImage.gameObject.SetActive(false);
            }
        }

        public void Restart()
        {
            currentIndex = 0;
            Play();
        }

        private IEnumerator PlayRoutine()
        {
            if (hideWhenStopped && targetImage != null)
            {
                targetImage.gameObject.SetActive(true);
            }

            float interval = Mathf.Max(0.01f, frameInterval);
            currentIndex = 0;

            while (true)
            {
                targetImage.sprite = frames[currentIndex];
                currentIndex++;

                if (currentIndex >= frames.Length)
                {
                    if (!loop)
                    {
                        break;
                    }

                    currentIndex = 0;
                }

                yield return new WaitForSeconds(interval);
            }

            playCoroutine = null;
        }
    }
}
