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
        /// 武器切り替えボタンが押された瞬間か
        /// </summary>
        bool IsSwapWeaponJustPressed();

        /// <summary>
        /// 現在の武器をドロップするボタンが押された瞬間か
        /// </summary>
        bool IsDropWeaponJustPressed();

        /// <summary>
        /// ジャンプボタンが押された瞬間か
        /// </summary>
        bool IsJumpJustPressed();

        /// <summary>
        /// しゃがみ切り替えボタンが押された瞬間か
        /// </summary>
        bool IsSitJustPressed();

        /// <summary>
        /// 伏せボタンが押された瞬間か
        /// </summary>
        bool IsLieDownJustPressed();

        /// <summary>
        /// 瞬間アイテムの使用スロット。未入力なら 0 を返す。
        /// </summary>
        int GetInstantItemSlotJustPressed();

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
