

namespace OpenGSCore
{


    public enum EMap
    {
        Unknown,
        AuroraClassic,
        ArchLoadOfGunster,
        IceValley,
        DryDays,
        GreenHillSide1,
        GreenHillSide2,
        CityOfDarkness1,
        CityOfDarkness2,
        BluffStructure1,
        BluffStructure2,
        DesertedJungleSide1,
        DesertedJungleSide2,
        BattlePort1,
        BattlePortCTF,
        FullHouse,
        FactoryInGaol,
        RobotFactory,
        RedStorm1,
        RedStorm2,
        ThePark,
        TheParkCTF,
        RuinOfWarSide1,
        RuinOfWarSide2,
        Nocturne,
        Waterfall,
        SkyHigh,
        SkyHighCTF,
        GhostHouse,
        OnStudio,
        Christmas,
        SeaSideBase,
        RandomMap,
    }

    public enum eDificullcy
    {

    }

    public class MapInfo
    {
        public EGameMode GameMode { get; set; }

        //public eStage Stage { get; set; }


        public EMap Map { get; set; }
    }
}
