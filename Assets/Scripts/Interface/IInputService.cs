using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ゲームの入力を抽象化するインターフェース。
    /// 実際のデバイス入力 (UnityInputService) やリプレイ、AI入力などで差し替え可能にする。
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// マウス/エイムのワールド座標を取得
        /// </summary>
        Vector2 GetAimWorldPosition();

        /// <summary>
        /// 指定した起点からのエイム方向（正規化済み）を取得
        /// </summary>
        Vector2 GetAimDirection(Vector3 origin);

        /// <summary>
        /// 射撃ボタンがホールドされているか
        /// </summary>
        bool IsFirePressed();

        /// <summary>
        /// 射撃ボタンが押された瞬間か
        /// </summary>
        bool IsFireJustPressed();

        /// <summary>
        /// リロードボタンが押された瞬間か
        /// </summary>
        bool IsReloadJustPressed();

        /// <summary>
        /// 移動（横方向 -1.0 ~ 1.0）
        /// </summary>
        float GetHorizontalAxis();

        /// <summary>
        /// 移動（縦方向 -1.0 ~ 1.0）
        /// </summary>
        float GetVerticalAxis();

        /// <summary>
        /// ジャンプボタンが押されているか
        /// </summary>
        bool IsJumpPressed();

        /// <summary>
        /// ブースターボタンが押されているか
        /// </summary>
        bool IsBoosterPressed();
    }
}
