using Sirenix.OdinInspector;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// リザルト画面の共通基底クラス。
    /// オフライン・オンラインで分岐するため、勝敗の表示や次画面への遷移など、共通のUIロジックだけを担当する。
    /// 継承先がデータを取得後、ShowResult(...) を呼ぶことで画面が更新される。
    /// </summary>
    public abstract class AbstractResultScene : AbstractScene
    {
        [Header("Audio")]
        public AudioClip fanfare;
        public float fanfareDelay = 0.4f;

        [Header("UI Images")]
        public Image winImage;
        public Image loseImage;
        public Image drawImage;

        [Header("Settings")]
        public float timeOut = 5.0f;

        protected bool isResultSet = false;
        private float resultElapsedTime = 0f;
        private bool hasReturnedFromResult = false;
        private bool transitionRequested = false;

        protected virtual void OnValidate()
        {
            fanfareDelay = float.IsFinite(fanfareDelay) ? Mathf.Max(0f, fanfareDelay) : 0f;
            timeOut = float.IsFinite(timeOut) ? Mathf.Max(0f, timeOut) : 0f;
        }

        protected override void OnStartUnityEditor() { }
        protected override void OnStartFromEditorDirectly() { }

        protected virtual void Start()
        {
            if (winImage != null) winImage.gameObject.SetActive(false);
            if (loseImage != null) loseImage.gameObject.SetActive(false);
            if (drawImage != null) drawImage.gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();
            if (!isResultSet) return;

            if (!hasReturnedFromResult && timeOut > 0f)
            {
                var deltaTime = Time.deltaTime;
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                {
                    return;
                }
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                resultElapsedTime = (float.IsFinite(resultElapsedTime) ? resultElapsedTime : 0f) + deltaTime;
                if (resultElapsedTime >= timeOut)
                {
                    TryReturnFromResult("timeout");
                    return;
                }
            }

            // クリックやエンターキーで次の画面へ
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                TryReturnFromResult("input");
            }
        }

        private void TryReturnFromResult(string reason)
        {
            if (transitionRequested || hasReturnedFromResult)
            {
                return;
            }

            transitionRequested = true;
            hasReturnedFromResult = true;
            isResultSet = false;
            Debug.Log($"[AbstractResultScene] Returning from result. reason={reason}");
            GoToNextScene();
        }

        /// <summary>
        /// 継承先から勝敗データを渡してUIを更新する
        /// </summary>
        protected void ShowResult(string winningTeam, string myTeam)
        {
            OnValidate();
            CancelInvoke(nameof(PlayFanfare));
            Invoke(nameof(PlayFanfare), fanfareDelay);
            resultElapsedTime = 0f;
            hasReturnedFromResult = false;
            transitionRequested = false;

            if (winImage != null) winImage.gameObject.SetActive(false);
            if (loseImage != null) loseImage.gameObject.SetActive(false);
            if (drawImage != null) drawImage.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(winningTeam) || winningTeam == "Draw" || winningTeam == "None" || winningTeam == "NoPlayers")
            {
                if (drawImage != null) drawImage.gameObject.SetActive(true);
            }
            else if (winningTeam == myTeam)
            {
                if (winImage != null) winImage.gameObject.SetActive(true);
            }
            else
            {
                if (loseImage != null) loseImage.gameObject.SetActive(true);
            }

            isResultSet = true;
        }

        private void PlayFanfare()
        {
            if (fanfare != null && SoundManager.Instance != null)
            {
                // SE再生
                SoundManager.Instance.PlayOneShotSafe(fanfare, context: nameof(AbstractResultScene));
            }
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current ?? new SynchronizationContext();
        }

        /// <summary>
        /// 次のシーン（WaitRoomなど）への遷移を行う。オンライン・オフラインで異なるため抽象化。
        /// </summary>
        protected abstract void GoToNextScene();
    }
}

