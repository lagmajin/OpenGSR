

using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenGSCore;



namespace OpenGS
{
    // eWeaponType enum moved to Interface/eWeaponType.cs

    static class WeaponType
    {
        public static Dictionary<string, eWeaponType> dic = new Dictionary<string, eWeaponType>()
        {
            { "ak47",eWeaponType.AK47},
            { "m16",eWeaponType.M16},
             { "Scout",eWeaponType.Scout},
             
            {"bubblegun",eWeaponType.BubbleGun },
            {"chirstmasgun",eWeaponType.BubbleGun }

        }
            ;



    }

}
