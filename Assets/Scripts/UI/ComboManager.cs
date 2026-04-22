using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// コンボの状態を管理し、ComboText Prefab を生成して表示するマネージャー。
    /// バトルシーンの Canvas 配下に置き、プレイヤーがキルするたびに
    /// AddCombo() を呼び出す。
    ///
    /// 【Prefab 設定手順】
    ///   1. Canvas 配下に空 GameObject を作成し、このスクリプトをアタッチ
    ///   2. comboPrefab に ComboText スクリプト付き Prefab をアサイン
    ///   3. comboDisplayParent に表示先の RectTransform をアサイン (nullなら自身)
    /// </summary>
    [DisallowMultipleComponent]
    public class ComboManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────

        [Header("コンボ Prefab")]
        [SerializeField] private ComboText comboPrefab;

        [Header("表示位置")]
        [SerializeField] private RectTransform comboDisplayParent;

        [Header("コンボリセット時間 (秒)")]
        [SerializeField] private float comboResetTime = 3.0f;

        // ─── 内部状態 ────────────────────────────────────────────────

        private int     currentComboCount = 0;
        private float   comboTimer        = 0f;
        private bool    isComboActive     = false;
        private ComboText activeComboText = null;

        // ─── Unity ライフサイクル ─────────────────────────────────────

        private void Update()
        {
            if (!isComboActive) return;

            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        // ─── 公開メソッド ─────────────────────────────────────────────

        /// <summary>
        /// キル/ヒット時に呼ぶ。コンボをインクリメントして表示を更新する。
        /// </summary>
        public void AddCombo()
        {
            currentComboCount++;
            comboTimer    = comboResetTime;
            isComboActive = true;

            ShowComboUI(currentComboCount);
        }

        /// <summary>
        /// コンボを手動でリセットする。
        /// </summary>
        public void ResetCombo()
        {
            currentComboCount = 0;
            comboTimer        = 0f;
            isComboActive     = false;

            if (activeComboText != null)
            {
                activeComboText.Hide();
                activeComboText = null;
            }
        }

        /// <summary>現在のコンボ数を返す</summary>
        public int CurrentCombo => currentComboCount;

        // ─── 内部実装 ─────────────────────────────────────────────────

        private void ShowComboUI(int count)
        {
            if (comboPrefab == null)
            {
                Debug.LogWarning("ComboManager: comboPrefab がアサインされていません");
                return;
            }

            // 既存の表示を使い回す (既に表示中の場合)
            if (activeComboText == null || !activeComboText.gameObject)
            {
                var parent = comboDisplayParent != null
                    ? comboDisplayParent
                    : transform as RectTransform;

                activeComboText = Instantiate(comboPrefab, parent);
            }

            // コンボ背景フラッシュ
            var comboImage = activeComboText.GetComponent<ComboImage>();
            comboImage?.PlayFlash();

            activeComboText.ShowCombo(count);
        }
    }
}
