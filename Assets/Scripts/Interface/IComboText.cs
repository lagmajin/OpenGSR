namespace OpenGS
{
    /// <summary>
    /// コンボカウンター表示コンポーネントのインターフェース
    /// </summary>
    public interface IComboText
    {
        /// <summary>コンボ数をセットして演出を再生する</summary>
        void ShowCombo(int count);

        /// <summary>コンボ表示を即座に非表示にする</summary>
        void Hide();
    }
}
