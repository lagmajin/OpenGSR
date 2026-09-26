using System;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>エクイップ設定/UIの武器種を、プレイヤー装備用プレハブへ解決する。</summary>
    public static class WeaponPrefabResolver
    {
        public static GameObject Load(EWeaponType type)
        {
            var masterDataName = type switch
            {
                EWeaponType.FnP90 => "FnP90",
                EWeaponType.M60 => "M60",
                EWeaponType.FNMinimiSaw => "FNMinimiSaw",
                _ => type.ToString()
            };

            var masterData = Resources.Load<WeaponMasterData>($"MasterData/Weapon/{masterDataName}");
            if (masterData != null && masterData.weaponPrefab != null)
            {
                return masterData.weaponPrefab;
            }

            var path = type switch
            {
                EWeaponType.AK47 => "Prefabs/Weapon/Guns/AR/AK47",
                EWeaponType.M16 => "Prefabs/Weapon/Guns/AR/M16",
                EWeaponType.FAMAS => "Prefabs/Weapon/Guns/AR/FAMAS",
                EWeaponType.F2000 => "Prefabs/Weapon/Guns/AR/F2000",
                EWeaponType.SteyrAug => "Prefabs/Weapon/Guns/AR/SteyrAug",
                EWeaponType.Scorpion => "Prefabs/Weapon/Guns/SMG/Scorpion",
                EWeaponType.FnP90 => "Prefabs/Weapon/Guns/SMG/P90",
                EWeaponType.Uzi => "Prefabs/Weapon/Guns/SMG/Uzi",
                EWeaponType.MP5 => "Prefabs/Weapon/Guns/SMG/MP5",
                EWeaponType.Scout => "Prefabs/Weapon/Guns/Sniper/Scout",
                EWeaponType.Dragunov => "Prefabs/Weapon/Guns/Sniper/Dragunov",
                EWeaponType.PSG1 => "Prefabs/Weapon/Guns/Sniper/PSG1",
                EWeaponType.AWP => "Prefabs/Weapon/Guns/Sniper/AWP",
                EWeaponType.MG42 => "Prefabs/Weapon/Guns/MG/MG42",
                EWeaponType.M60 => "Prefabs/Weapon/Guns/MG/M60E4",
                EWeaponType.FNMinimiSaw => "Prefabs/Weapon/Guns/MG/FNMinimiSAW",
                EWeaponType.Glock => "Prefabs/Weapon/Guns/Pistol/Glock",
                EWeaponType.DesertEagle => "Prefabs/Weapon/Guns/Pistol/DesertEagle",
                EWeaponType.LaserGun => "Prefabs/Weapon/Guns/Special/LaserGun",
                EWeaponType.BubbleGun => "Prefabs/Weapon/Guns/Special/BubbleGun",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(path) ? null : Resources.Load<GameObject>(path);
        }
    }
}
