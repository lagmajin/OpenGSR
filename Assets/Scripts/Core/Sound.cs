
namespace OpenGS
{
    public enum EBgn
    {
        AmusementPark,
        ArchLord,
        CityOfDarkness,
        DryDays,
        DesertedJungle,
        Factory,
        FactoryInGaol,

        Jungle,
        BluffStructure,
        Nocturne

    }

    /// <summary>
    /// 全ての利用可能な BGM の列挙型。
    /// Resources/BGM フォルダ内のファイルに対応。
    /// </summary>
    public enum EBgm
    {
        None,
        Title,
        SplashScreen,
        WaitRoom,
        Shop,
        Base,
        BattleBase,
        
        // Map BGMs
        AmusementPark,
        ArchLord,
        AuroraClassic,
        BluffStructure,
        CityOfDarkness,
        DryDays,
        Factory,
        Forest,
        Green,
        HiddenBunker,
        House,
        Jungle,
        LavaCave,
        MetalBreaker,
        Pipe,
        Ruin,
        SkyFighter,
        Snow,
        Village,
        WaterFall
    }

    public enum ESystemSound
    {
        Click,
        Error,
        Check,
        EnterLobby,
        Popup,
        Exit,
        Fanfare


    }

    public enum ESoundEffect
    {
        Explosion,
        HitStageObject,
        //Fanfare,

    }

    public enum ETakeItemSound
    {
        TakePowerUpItemSound,
        TakeDefenseUpItemSound,
        TakeSpeedUpItemSound,
        TakeHealItemSound,
        TakeRandomItemSound,


    }


    public enum EPlayerSound
    {
        DamageFemale1 = 0,
        DamageMale1,
        DeathFemale1,
        DeathFemale2,
        DeathFemale3,
        DeathFemale4,
        DeathMale1,
        DeathMale2,
        DeathMale3,
        DeathMale4

    }

    public enum EGrenadeSound
    {
        ExplosionGrenade,
        ExplosionFireGrenade,
    }

    public enum EMatchSound
    {
        GameStartVoice = 0,
        SuddenDeathVoice,
        YouWon,
        YouLost,

        RedTeamFlagCaptured,
        BlueTeamFlagCaptured,
        FlagLost,
        RedTeamFlagReturn,
        BlueTeamFlagReturn,

    }

}
