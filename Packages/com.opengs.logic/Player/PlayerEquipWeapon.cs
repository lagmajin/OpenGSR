using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public enum EPlayerEquipWeapon
    {
        Unknown,
        MainWeapon,
        SecondaryWeapon,
        SpecialWeapon
    }

    public struct WeaponData
    {
        public EWeaponType type;
    }

    public class FavoriteWeapon
    {
        private WeaponData[] weapons = new WeaponData[6];

        // 特殊武器の状態
        public bool HasSpecialWeapon { get; set; } = false;
        public ESpecialWeapon CurrentSpecialWeapon { get; set; }
        public int SpecialWeaponAmmo { get; set; } = 0;

        public FavoriteWeapon()
        {
            SetDefault();
        }

        public void SetDefault()
        {
            weapons[0].type = EWeaponType.AK47;
            weapons[1].type = EWeaponType.AWP;
            weapons[2].type = EWeaponType.MP5;
            weapons[3].type = EWeaponType.AWP;
            weapons[4].type = EWeaponType.AK47;
            weapons[5].type = EWeaponType.M60;
        }

        public WeaponData GetWeapon(int index)
        {
            if (index < 0 || index >= weapons.Length)
                return default;

            return weapons[index];
        }

        public void SetWeapon(int index, WeaponData weapon)
        {
            if (index < 0 || index >= weapons.Length)
                return;

            weapons[index] = weapon;
        }

        public void FillAll(WeaponData weapon)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i] = weapon;
            }
        }
    }






}
