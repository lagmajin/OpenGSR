using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// 2 つの状態を切り替える画像コンポーネント
    /// 外部からパブリックメソッドで状態切り替え可能
    /// </summary>
    public class ImageSwitcherTwoState : MonoBehaviour
    {
        [SerializeField] private Image img;
        [SerializeField] private Sprite state1;    // 1 つ目の状態のスプライト
        [SerializeField] private Sprite state2;    // 2 つ目の状態のスプライト

        private bool isState1 = true;              // 現在の状態

        /// <summary>
        /// 初期化処理
        /// </summary>
        void Start()
        {
            if (img == null)
            {
                img = GetComponent<Image>();   // Image がアタッチされていない場合、コンポーネントを取得
            }
            
            Debug.Assert(img != null, "Image component is missing on ImageSwitcherTwoState!");
            Debug.Assert(state1 != null, "State1 sprite is not assigned!");
            Debug.Assert(state2 != null, "State2 sprite is not assigned!");
            
            img.sprite = state1;  // 初期スプライトを state1 に設定
        }

        /// <summary>
        /// 状態 1 に設定
        /// </summary>
        public void SetState1()
        {
            isState1 = true;
            if (img != null)
            {
                img.sprite = state1;
            }
        }

        /// <summary>
        /// 状態 2 に設定
        /// </summary>
        public void SetState2()
        {
            isState1 = false;
            if (img != null)
            {
                img.sprite = state2;
            }
        }

        /// <summary>
        /// 状態をトグル（切り替え）
        /// </summary>
        public void Toggle()
        {
            SwitchState();
        }

        /// <summary>
        /// 現在の状態を取得（状態 1 なら true）
        /// </summary>
        public bool IsState1 => isState1;

        // ステートを切り替える内部メソッド
        private void SwitchState()
        {
            isState1 = !isState1;  // 状態を反転
            if (img != null)
            {
                img.sprite = isState1 ? state1 : state2;  // 反転した状態に応じてスプライトを切り替え
            }
        }
    }

}