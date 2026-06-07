using System.Collections.Generic;
using OpenGSCore;

namespace OpenGS
{
    // eWeaponType enum moved to Interface/eWeaponType.cs
    static class WeaponType
    {
        public static readonly Dictionary<string, eWeaponType> dic = new Dictionary<string, eWeaponType>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "AK47", eWeaponType.AK47 },
            { "ak47", eWeaponType.AK47 },
            { "UI_W_ak47", eWeaponType.AK47 },
            { "Ak47", eWeaponType.AK47 },

            { "M16", eWeaponType.M16 },
            { "m16", eWeaponType.M16 },

            { "FAMAS", eWeaponType.FAMAS },
            { "famas", eWeaponType.FAMAS },

            { "F2000", eWeaponType.F2000 },
            { "f2000", eWeaponType.F2000 },

            { "Scorpion", eWeaponType.Scorpion },
            { "Skorpion", eWeaponType.Scorpion },
            { "scorpion", eWeaponType.Scorpion },

            { "FnP90", eWeaponType.FN_P90 },
            { "FNP90", eWeaponType.FN_P90 },
            { "FN_P90", eWeaponType.FN_P90 },
            { "P-90", eWeaponType.FN_P90 },
            { "P90", eWeaponType.FN_P90 },

            { "Scout", eWeaponType.Scout },
            { "scout", eWeaponType.Scout },

            { "Dragunov", eWeaponType.Dragunov },
            { "dragunov", eWeaponType.Dragunov },

            { "PSG1", eWeaponType.PSG1 },
            { "PSG-1", eWeaponType.PSG1 },
            { "psg1", eWeaponType.PSG1 },

            { "AWP", eWeaponType.AWP },
            { "awp", eWeaponType.AWP },

            { "Uzi", eWeaponType.Uzi },
            { "uzi", eWeaponType.Uzi },
            { "IMIUzi", eWeaponType.Uzi },

            { "MG42", eWeaponType.MG42 },
            { "mg42", eWeaponType.MG42 },

            { "M60", eWeaponType.M60 },
            { "m60", eWeaponType.M60 },
            { "M60E4", eWeaponType.M60 },

            { "FNMinimiSaw", eWeaponType.FNMinimi_SAW },
            { "FNMinimi_SAW", eWeaponType.FNMinimi_SAW },
            { "FNMinimiSAW", eWeaponType.FNMinimi_SAW },

            { "LaserGun", eWeaponType.LaserGun },
            { "lasergun", eWeaponType.LaserGun },

            { "BubbleGun", eWeaponType.BubbleGun },
            { "bubblegun", eWeaponType.BubbleGun },
            { "Bubble", eWeaponType.BubbleGun },

            { "ChristmasGun", eWeaponType.ChirstmasGun },
            { "ChirstmasGun", eWeaponType.ChirstmasGun },
            { "Christmasgun", eWeaponType.ChirstmasGun },
            { "xmas", eWeaponType.ChirstmasGun },
            { "Xmas", eWeaponType.ChirstmasGun },

            { "SteyrAug", eWeaponType.SteyAug },
            { "SteyrAUG", eWeaponType.SteyAug },
            { "SteyAug", eWeaponType.SteyAug },

            { "Glock", eWeaponType.Glock },
            { "glock", eWeaponType.Glock },
            { "Glock18c", eWeaponType.Glock },

            { "DE", eWeaponType.DE },
            { "DesertEagle", eWeaponType.DE },
            { "deserteagle", eWeaponType.DE },

            { "MP5", eWeaponType.MP5 },
            { "mp5", eWeaponType.MP5 }
        };
    }
}
