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

        protected override void OnStartUnityEditor() { }
        protected override void OnStartFromEditorDirectly() { }

        protected virtual void Start()
        {
            if (winImage != null) winImage.gameObject.SetActive(false);
            if (loseImage != null) loseImage.gameObject.SetActive(false);
            if (drawImage != null) drawImage.gameObject.SetActive(false);
        }

        protected virtual void Update()
        {
            if (!isResultSet) return;

            // クリックやエンターキーで次の画面へ
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                GoToNextScene();
            }
        }

        /// <summary>
        /// 継承先から勝敗データを渡してUIを更新する
        /// </summary>
        protected void ShowResult(string winningTeam, string myTeam)
        {
            Invoke(nameof(PlayFanfare), fanfareDelay);

            if (winImage != null) winImage.gameObject.SetActive(false);
            if (loseImage != null) loseImage.gameObject.SetActive(false);
            if (drawImage != null) drawImage.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(winningTeam) || winningTeam == "Draw" || winningTeam == "None")
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
                PlaySound.PlaySE(fanfare);
            }
        }

        public override SynchronizationContext MainThread()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 次のシーン（WaitRoomなど）への遷移を行う。オンライン・オフラインで異なるため抽象化。
        /// </summary>
        protected abstract void GoToNextScene();
    }
}

