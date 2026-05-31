using System;
using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    /// <summary>
    /// 既存のショップ資産が未整備でも、UI が空にならないようにするための
    /// ランタイム用デフォルトカタログ。
    /// </summary>
    public static class ShopCatalogFactory
    {
        private static readonly Dictionary<string, ShopItemData> itemCache = new Dictionary<string, ShopItemData>();

        public static List<ShopItemData> GetDefaultItems(EShopCategory category)
        {
            var items = new List<ShopItemData>();

            switch (category)
            {
                case EShopCategory.Weapon:
                    foreach (var weapon in GetDefaultWeapons())
                    {
                        items.Add(CreateWeaponItem(weapon));
                    }
                    break;
                case EShopCategory.InstantItem:
                    foreach (var instantItem in GetDefaultInstantItems())
                    {
                        items.Add(CreateInstantItem(instantItem));
                    }
                    break;
                case EShopCategory.Booster:
                    items.Add(CreateBoosterItem("BoostRed", "Red Booster", "Cosmetic booster. Changes the jet flame color only.", 400, Color.red));
                    items.Add(CreateBoosterItem("BoostBlue", "Blue Booster", "Cosmetic booster. Changes the jet flame color only.", 400, Color.cyan));
                    items.Add(CreateBoosterItem("BoostGreen", "Green Booster", "Cosmetic booster. Changes the jet flame color only.", 400, Color.green));
                    break;
                case EShopCategory.Character:
                    foreach (var character in GetDefaultCharacters())
                    {
                        items.Add(CreateCharacterItem(character));
                    }
                    break;
            }

            return items;
        }

        public static ShopItemData GetDefaultItemById(string id)
        {
            if (itemCache.TryGetValue(id ?? string.Empty, out var cached))
            {
                return cached;
            }

            foreach (var category in new[] { EShopCategory.Weapon, EShopCategory.InstantItem, EShopCategory.Booster, EShopCategory.Character })
            {
                foreach (var item in GetDefaultItems(category))
                {
                    if (string.Equals(item.id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<EWeaponType> GetDefaultWeapons()
        {
            foreach (var value in Enum.GetValues(typeof(EWeaponType)))
            {
                var weapon = (EWeaponType)value;
                if (weapon == EWeaponType.None)
                {
                    continue;
                }

                yield return weapon;
            }
        }

        private static IEnumerable<EInstantItemType> GetDefaultInstantItems()
        {
            foreach (var value in Enum.GetValues(typeof(EInstantItemType)))
            {
                var instantItem = (EInstantItemType)value;
                if (instantItem == EInstantItemType.None)
                {
                    continue;
                }

                yield return instantItem;
            }
        }

        private static IEnumerable<EPlayerCharacter> GetDefaultCharacters()
        {
            foreach (var value in Enum.GetValues(typeof(EPlayerCharacter)))
            {
                yield return (EPlayerCharacter)value;
            }
        }

        private static ShopItemData CreateWeaponItem(EWeaponType weapon)
        {
            var id = weapon.ToString();
            if (itemCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.id = id;
            item.itemName = WeaponVisualResolver.GetDisplayName(weapon);
            item.description = $"Weapon: {item.itemName}";
            item.price = GetWeaponPrice(weapon);
            item.category = EShopCategory.Weapon;
            item.icon = WeaponVisualResolver.GetSelectionSprite(weapon);
            item.itemColor = Color.white;
            itemCache[id] = item;
            return item;
        }

        private static ShopItemData CreateInstantItem(EInstantItemType instantItem)
        {
            var id = instantItem.ToString();
            if (itemCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.id = id;
            item.itemName = instantItem.ToString();
            item.description = $"Instant item: {instantItem}";
            item.price = GetInstantItemPrice(instantItem);
            item.category = EShopCategory.InstantItem;
            item.itemColor = Color.white;
            itemCache[id] = item;
            return item;
        }

        private static ShopItemData CreateBoosterItem(string id, string name, string description, int price, Color color)
        {
            if (itemCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.id = id;
            item.itemName = name;
            item.description = description;
            item.price = price;
            item.category = EShopCategory.Booster;
            item.itemColor = color;
            itemCache[id] = item;
            return item;
        }

        private static ShopItemData CreateCharacterItem(EPlayerCharacter character)
        {
            var id = character.ToString();
            if (itemCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.id = id;
            item.itemName = GetCharacterDisplayName(character);
            item.description = $"Playable character: {character}";
            item.price = character == EPlayerCharacter.Misty ? 0 : GetCharacterPrice(character);
            item.category = EShopCategory.Character;
            item.itemColor = Color.white;
            itemCache[id] = item;
            return item;
        }

        private static int GetWeaponPrice(EWeaponType weapon)
        {
            return weapon switch
            {
                EWeaponType.Glock or EWeaponType.MP5 => 250,
                EWeaponType.DesertEagle or EWeaponType.Scorpion or EWeaponType.Uzi => 350,
                EWeaponType.AK47 or EWeaponType.M16 or EWeaponType.FAMAS or EWeaponType.F2000 or EWeaponType.SteyrAug => 500,
                EWeaponType.Scout or EWeaponType.Dragunov or EWeaponType.PSG1 or EWeaponType.AWP => 650,
                EWeaponType.MG42 or EWeaponType.M60 or EWeaponType.FNMinimiSaw => 800,
                EWeaponType.LaserGun or EWeaponType.BubbleGun or EWeaponType.ChristmasGun => 900,
                _ => 300
            };
        }

        private static int GetInstantItemPrice(EInstantItemType instantItem)
        {
            return instantItem switch
            {
                EInstantItemType.HealthKit => 120,
                EInstantItemType.FireBullet or EInstantItemType.PoisonBullet => 180,
                EInstantItemType.PowerGrenadePack or EInstantItemType.ClusterGrenadePack or EInstantItemType.MagnetGrenadePack or EInstantItemType.MineGrenadePack => 220,
                _ => 150
            };
        }

        private static int GetCharacterPrice(EPlayerCharacter character)
        {
            return character switch
            {
                EPlayerCharacter.Ami or EPlayerCharacter.Yumi or EPlayerCharacter.Misty => 0,
                EPlayerCharacter.Jack or EPlayerCharacter.Jackle or EPlayerCharacter.LittleJ => 250,
                EPlayerCharacter.Liu or EPlayerCharacter.Mary or EPlayerCharacter.Seoul => 350,
                EPlayerCharacter.Wolf or EPlayerCharacter.Wyvern or EPlayerCharacter.Shue or EPlayerCharacter.Swaltz => 500,
                _ => 300
            };
        }

        private static string GetCharacterDisplayName(EPlayerCharacter character)
        {
            return character switch
            {
                EPlayerCharacter.Ami => "アミ",
                EPlayerCharacter.Yumi => "ユミ",
                EPlayerCharacter.Jack => "ジャック",
                EPlayerCharacter.Jackle => "ジャックル",
                EPlayerCharacter.Misty => "ミスティ",
                EPlayerCharacter.Liu => "リュウ",
                EPlayerCharacter.Mary => "メアリー",
                EPlayerCharacter.Wolf => "ウルフ",
                EPlayerCharacter.Wyvern => "ワイバーン",
                EPlayerCharacter.Seoul => "ソウル",
                EPlayerCharacter.LittleJ => "リトルJ",
                EPlayerCharacter.Shue => "シュウ",
                EPlayerCharacter.Swaltz => "スワルツ",
                _ => character.ToString()
            };
        }
    }
}
