using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EBulletType
    {
        Normal,
        Poison,
        Fire

    }

    [System.Flags]
    public enum EGunType
    {
        None = 0,
        AssaultRifle = 1 << 0,
        SubMachineGun = 1 << 1,
        SniperRifle = 1 << 2,
        GrenadeLauncher = 1 << 3,
        Shotgun = 1 << 4,
        HandGun = 1 << 5,

        All = AssaultRifle | SubMachineGun | SniperRifle | GrenadeLauncher | Shotgun | HandGun
    }


    public enum ESpecialWeapon
    {
        FlameThrower,
        GrenadeLauncher
    }


    public static class EGunTypeExtensions
    {
        // 武器の所持数を数える
        public static int CountFlags(this EGunType gunType)
        {
            int count = 0;
            foreach (EGunType type in System.Enum.GetValues(typeof(EGunType)))
            {
                if (type == EGunType.None) continue;
                if (gunType.HasFlag(type))
                {
                    count++;
                }
            }
            return count;
        }

        // 装備してる武器だけリストにする
        public static List<EGunType> ToList(this EGunType gunType)
        {
            List<EGunType> list = new List<EGunType>();
            foreach (EGunType type in System.Enum.GetValues(typeof(EGunType)))
            {
                if (type == EGunType.None) continue;
                if (gunType.HasFlag(type))
                {
                    list.Add(type);
                }
            }
            return list;
        }

        // デバッグ用：装備武器を文字列化
        public static string ToNiceString(this EGunType gunType)
        {
            var list = gunType.ToList();
            return string.Join(", ", list);
        }

        public static int GetDamage(this EGunType gunType)
        {
            return gunType switch
            {
                EGunType.AssaultRifle => 35,
                EGunType.SubMachineGun => 25,
                EGunType.SniperRifle => 100,
                EGunType.Shotgun => 15, // 1粒あたりのダメージなど、本来は複雑
                EGunType.HandGun => 20,
                _ => 30
            };
        }

        public static EGunType FromString(string type)
        {
            return type.ToLower() switch
            {
                "rifle" or "assaultrifle" => EGunType.AssaultRifle,
                "smg" or "submachinegun" => EGunType.SubMachineGun,
                "sr" or "sniperrifle" => EGunType.SniperRifle,
                "shotgun" => EGunType.Shotgun,
                "hg" or "handgun" => EGunType.HandGun,
                _ => EGunType.None
            };
        }
    }


}
