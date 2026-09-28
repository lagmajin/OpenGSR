using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public sealed class MatchLimitations
    {
        public bool CanUseFieldItem { get; private set; } = true;

        public bool CanEquipBooster { get; private set; } = true;

        public bool CanEquipInstantItem { get; private set; } = true;


        public MatchLimitations(bool canUseFieldItem, bool canEquipBooster, bool canEquipInstantItem)
        {
            CanUseFieldItem = canUseFieldItem;
            CanEquipBooster = canEquipBooster;
            CanEquipInstantItem = canEquipInstantItem;
        }

        public static MatchLimitations Default() => new MatchLimitations(true, true, true);
        public static MatchLimitations NoItems() => new MatchLimitations(false, false, false);
        public static MatchLimitations BoosterOnly() => new MatchLimitations(true, true, false);
    }
}



