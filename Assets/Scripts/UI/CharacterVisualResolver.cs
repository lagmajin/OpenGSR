using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public static class CharacterVisualResolver
    {
        private static readonly Dictionary<EPlayerCharacter, Sprite> thumbnailCache = new Dictionary<EPlayerCharacter, Sprite>();
        private static readonly Dictionary<EPlayerCharacter, Sprite> portraitCache = new Dictionary<EPlayerCharacter, Sprite>();
        private static readonly Dictionary<string, Sprite> resourceCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static ScriptableObject thumbnailMasterData;

        public static string GetDisplayName(EPlayerCharacter character)
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

        public static string GetDescription(EPlayerCharacter character)
        {
            return character switch
            {
                EPlayerCharacter.Ami => "バランス型のキャラクター",
                EPlayerCharacter.Yumi => "スピードに優れたキャラクター",
                EPlayerCharacter.Jack => "パワー型のキャラクター",
                EPlayerCharacter.Jackle => "防御に優れたキャラクター",
                EPlayerCharacter.Misty => "テクニック型のキャラクター",
                EPlayerCharacter.Liu => "攻撃型のキャラクター",
                EPlayerCharacter.Mary => "サポート型のキャラクター",
                EPlayerCharacter.Wolf => "アグレッシブなキャラクター",
                EPlayerCharacter.Wyvern => "エアリアル戦闘に優れたキャラクター",
                EPlayerCharacter.Seoul => "バランス型のキャラクター",
                EPlayerCharacter.LittleJ => "小回りの利くキャラクター",
                EPlayerCharacter.Shue => "スピード型のキャラクター",
                EPlayerCharacter.Swaltz => "テクニカルなキャラクター",
                _ => ""
            };
        }

        public static Sprite GetThumbnail(EPlayerCharacter character)
        {
            if (thumbnailCache.TryGetValue(character, out var cached))
            {
                return cached;
            }

            var sprite = ResolveFromMasterData(character, true)
                ?? LoadSprite(GetThumbnailPaths(character));

            thumbnailCache[character] = sprite;
            return sprite;
        }

        public static Sprite GetPortrait(EPlayerCharacter character)
        {
            if (portraitCache.TryGetValue(character, out var cached))
            {
                return cached;
            }

            var sprite = ResolveFromMasterData(character, false)
                ?? LoadSprite(GetPortraitPaths(character));

            portraitCache[character] = sprite;
            return sprite;
        }

        public static Sprite GetShopIcon(EPlayerCharacter character)
        {
            return GetThumbnail(character) ?? GetPortrait(character);
        }

        private static ScriptableObject GetThumbnailMasterData()
        {
            if (thumbnailMasterData != null)
            {
                return thumbnailMasterData;
            }

            thumbnailMasterData = Resources.LoadAll<ScriptableObject>("MasterData/Player")
                .FirstOrDefault(asset => asset != null && string.Equals(asset.name, "Player Thumbnail Master Data", StringComparison.OrdinalIgnoreCase));

            return thumbnailMasterData;
        }

        private static Sprite ResolveFromMasterData(EPlayerCharacter character, bool thumbnail)
        {
            var asset = GetThumbnailMasterData();
            if (asset == null)
            {
                return null;
            }

            var fieldNames = GetFieldNames(character, thumbnail);
            foreach (var fieldName in fieldNames)
            {
                var sprite = GetMemberValue<Sprite>(asset, fieldName);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static string[] GetFieldNames(EPlayerCharacter character, bool thumbnail)
        {
            return character switch
            {
                EPlayerCharacter.Ami => thumbnail ? new[] { "ami" } : new[] { "ami_B", "ami" },
                EPlayerCharacter.Yumi => thumbnail ? new[] { "yumi" } : new[] { "yumi_B", "yumi" },
                EPlayerCharacter.Jack => thumbnail ? new[] { "jack", "Jack" } : new[] { "jack_B", "Jack" },
                EPlayerCharacter.Jackle => thumbnail ? new[] { "jackle", "jackal", "Jackle" } : new[] { "jackle_B", "jackal_B", "Jackle" },
                EPlayerCharacter.Misty => thumbnail ? new[] { "misty" } : new[] { "misty_B", "misty" },
                EPlayerCharacter.Liu => thumbnail ? new[] { "liu" } : new[] { "liu_B", "liu" },
                EPlayerCharacter.Mary => thumbnail ? new[] { "mary" } : new[] { "mary_B", "mary" },
                EPlayerCharacter.Wolf => thumbnail ? new[] { "wolf" } : new[] { "wolf_B", "wolf" },
                EPlayerCharacter.Wyvern => thumbnail ? new[] { "wyvern" } : new[] { "wyvern_B", "wyvern" },
                EPlayerCharacter.Seoul => thumbnail ? new[] { "seoul" } : new[] { "seoul_B", "seoul" },
                EPlayerCharacter.LittleJ => thumbnail ? new[] { "littleJ", "littlej" } : new[] { "littleJ_B", "littlej_B", "littleJ" },
                EPlayerCharacter.Shue => thumbnail ? new[] { "shue" } : new[] { "shue_B", "shue" },
                EPlayerCharacter.Swaltz => thumbnail ? new[] { "schwartz", "swaltz" } : new[] { "schwartz_B", "swaltz_B", "schwartz", "swaltz" },
                _ => Array.Empty<string>()
            };
        }

        private static IEnumerable<string> GetThumbnailPaths(EPlayerCharacter character)
        {
            var id = GetCharacterIndex(character);
            if (id.HasValue)
            {
                yield return $"Sprites/PlayerSelect/PlayerSelect_Character{id.Value:00}_Thumbnail";
                yield return $"Sprites/WaitRoom/WaitRoom_Character{id.Value:00}";
            }

            foreach (var name in GetResourceAliases(character))
            {
                yield return $"Sprites/PlayerSelect/{name}_Thumbnail";
                yield return $"Sprites/PlayerSelect/{name}";
                yield return $"Sprites/WaitRoom/{name}";
                yield return $"Sprites/Player/{name}/{name}";
            }
        }

        private static IEnumerable<string> GetPortraitPaths(EPlayerCharacter character)
        {
            var id = GetCharacterIndex(character);
            if (id.HasValue)
            {
                yield return $"Sprites/PlayerSelect/PlayerSelect_Character{id.Value:00}_Portrait";
            }

            foreach (var name in GetResourceAliases(character))
            {
                yield return $"Sprites/PlayerSelect/{name}_Portrait";
                yield return $"Sprites/PlayerSelect/{name}";
            }
        }

        private static IEnumerable<string> GetResourceAliases(EPlayerCharacter character)
        {
            yield return character.ToString();

            switch (character)
            {
                case EPlayerCharacter.Jackle:
                    yield return "Jackal";
                    break;
                case EPlayerCharacter.Swaltz:
                    yield return "Schwartz";
                    yield return "schwartz";
                    break;
                case EPlayerCharacter.LittleJ:
                    yield return "LittleJ";
                    yield return "Littlej";
                    break;
            }
        }

        private static int? GetCharacterIndex(EPlayerCharacter character)
        {
            return character switch
            {
                EPlayerCharacter.Ami => 2,
                EPlayerCharacter.Yumi => 3,
                EPlayerCharacter.Jack => 4,
                EPlayerCharacter.Jackle => 5,
                EPlayerCharacter.Misty => 6,
                EPlayerCharacter.Liu => 7,
                EPlayerCharacter.Mary => 8,
                EPlayerCharacter.Wolf => 9,
                EPlayerCharacter.Wyvern => 11,
                EPlayerCharacter.Seoul => 12,
                EPlayerCharacter.LittleJ => 15,
                EPlayerCharacter.Shue => 16,
                EPlayerCharacter.Swaltz => 17,
                _ => null
            };
        }

        private static Sprite LoadSprite(IEnumerable<string> resourcePaths)
        {
            foreach (var path in resourcePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (resourceCache.TryGetValue(path, out var cached))
                {
                    if (cached != null)
                    {
                        return cached;
                    }

                    continue;
                }

                var sprite = Resources.Load<Sprite>(path);
                resourceCache[path] = sprite;
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static T GetMemberValue<T>(object target, string memberName) where T : class
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = target.GetType();

            var field = type.GetField(memberName, flags);
            if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(target) as T;
            }

            var property = type.GetProperty(memberName, flags);
            if (property != null && typeof(T).IsAssignableFrom(property.PropertyType))
            {
                return property.GetValue(target) as T;
            }

            return null;
        }
    }
}
