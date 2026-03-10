namespace OpenGS
{
    public interface IMultipleTags
    {
        bool HasPlayerTag();
        bool HasBotTag();
        bool HasBurstAreaTag();
        bool HasLavaTag();
        bool HasEnemyTag();

        bool HasMyPlayerTag();

        bool HasFieldWeaponTag();
        bool HasWaterFallTag();
        bool HasStageObjectTag();

        void AddTag(string tag);
    }
}
