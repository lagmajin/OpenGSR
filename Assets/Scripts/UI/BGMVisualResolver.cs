using UnityEngine;

namespace OpenGS
{
    public static class BGMVisualResolver
    {
        public static string GetDisplayName(EBgm bgm)
        {
            return bgm switch
            {
                EBgm.None => "None",
                EBgm.Title => "Title",
                EBgm.SplashScreen => "Splash Screen",
                EBgm.WaitRoom => "Wait Room",
                EBgm.Shop => "Shop",
                EBgm.Base => "Base",
                EBgm.BattleBase => "Battle Base",
                EBgm.AmusementPark => "Amusement Park",
                EBgm.ArchLord => "Arch Lord",
                EBgm.AuroraClassic => "Aurora Classic",
                EBgm.BluffStructure => "Bluff Structure",
                EBgm.CityOfDarkness => "City of Darkness",
                EBgm.DryDays => "Dry Days",
                EBgm.Factory => "Factory",
                EBgm.Forest => "Forest",
                EBgm.Green => "Green",
                EBgm.HiddenBunker => "Hidden Bunker",
                EBgm.House => "House",
                EBgm.Jungle => "Jungle",
                EBgm.LavaCave => "Lava Cave",
                EBgm.MetalBreaker => "Metal Breaker",
                EBgm.Pipe => "Pipe",
                EBgm.Ruin => "Ruin",
                EBgm.SkyFighter => "Sky Fighter",
                EBgm.Snow => "Snow",
                EBgm.Village => "Village",
                EBgm.WaterFall => "Water Fall",
                _ => bgm.ToString()
            };
        }
    }
}
