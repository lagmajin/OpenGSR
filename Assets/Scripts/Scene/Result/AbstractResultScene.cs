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
                resultElapsedTime += Time.deltaTime;
                if (resultElapsedTime >= timeOut)
                {
                    GoToNextScene();
                    hasReturnedFromResult = true;
                    isResultSet = false;
                    return;
                }
            }

            // クリックやエンターキーで次の画面へ
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                GoToNextScene();
                hasReturnedFromResult = true;
                isResultSet = false;
            }
        }

        /// <summary>
        /// 継承先から勝敗データを渡してUIを更新する
        /// </summary>
        protected void ShowResult(string winningTeam, string myTeam)
        {
            Invoke(nameof(PlayFanfare), fanfareDelay);
            resultElapsedTime = 0f;
            hasReturnedFromResult = false;

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
            if (fanfare != null)
            {
                // SE再生
                SoundManager.Instance.PlayOneShotSafe(fanfare, context: nameof(AbstractResultScene));
            }
        }

        public override SynchronizationContext MainThread()
        {
            return SynchronizationContext.Current;
        }

        /// <summary>
        /// 次のシーン（WaitRoomなど）への遷移を行う。オンライン・オフラインで異なるため抽象化。
        /// </summary>
        protected abstract void GoToNextScene();
    }
}

