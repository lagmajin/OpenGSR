namespace OpenGS
{
    /// <summary>
    /// パワーアップ効果を受け取るオブジェクトが実装するインターフェース
    /// </summary>
    public interface IPowerupable
    {
        bool IsSpeedUpNow();
        bool IsIncreaseAttackNow();
        bool IsIncreaseDefenseNow();

        void SpeedUp(float sec = 30.0f);
        void IncreaseAttack(float sec = 30.0f);
        void IncreaseDefense(float sec = 30.0f);
        void Invisible(float sec = 30.0f);

        void Berserk();
    }
}