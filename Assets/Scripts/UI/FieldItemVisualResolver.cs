using UnityEngine;

namespace OpenGS
{
    public static class FieldItemVisualResolver
    {
        public static string GetDisplayName(OpenGSCore.EFieldItemType type)
        {
            return type switch
            {
                OpenGSCore.EFieldItemType.GranadeLauncher => "Grenade Launcher",
                OpenGSCore.EFieldItemType.FlameThrower => "Flame Thrower",
                OpenGSCore.EFieldItemType.PowerUpItem => "Power Up",
                OpenGSCore.EFieldItemType.DefenceUpItem => "Defence Up",
                OpenGSCore.EFieldItemType.SpeedUpItem => "Speed Up",
                OpenGSCore.EFieldItemType.StealthItem => "Stealth",
                OpenGSCore.EFieldItemType.GrenadePack => "Grenade Pack",
                OpenGSCore.EFieldItemType.HealItem => "Heal",
                _ => type.ToString()
            };
        }

        public static string GetDisplayName(eFieldItemType type)
        {
            return type switch
            {
                eFieldItemType.PowerUp or eFieldItemType.PowerUpItem => "Power Up",
                eFieldItemType.DefenceUp or eFieldItemType.DefenceUpItem => "Defence Up",
                eFieldItemType.Stealth or eFieldItemType.StealthItem => "Stealth",
                eFieldItemType.SpeedUp or eFieldItemType.SpeedUpItem => "Speed Up",
                eFieldItemType.NormalGrenadePack or eFieldItemType.GrenadePack => "Grenade Pack",
                eFieldItemType.Random => "Random",
                eFieldItemType.RocketLauncher => "Rocket Launcher",
                eFieldItemType.FlameThrower => "Flame Thrower",
                eFieldItemType.None => "None",
                _ => type.ToString()
            };
        }

        public static bool TryParse(string value, out OpenGSCore.EFieldItemType type)
        {
            type = OpenGSCore.EFieldItemType.PowerUpItem;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (TryParseLegacy(value, out var legacy))
            {
                type = ToCoreType(legacy);
                return true;
            }

            if (System.Enum.TryParse(value, true, out OpenGSCore.EFieldItemType parsed))
            {
                type = parsed;
                return true;
            }

            return false;
        }

        public static OpenGSCore.EFieldItemType ToCoreType(eFieldItemType type)
        {
            return type switch
            {
                eFieldItemType.None => OpenGSCore.EFieldItemType.PowerUpItem,
                eFieldItemType.PowerUp or eFieldItemType.PowerUpItem => OpenGSCore.EFieldItemType.PowerUpItem,
                eFieldItemType.DefenceUp or eFieldItemType.DefenceUpItem => OpenGSCore.EFieldItemType.DefenceUpItem,
                eFieldItemType.Stealth or eFieldItemType.StealthItem => OpenGSCore.EFieldItemType.StealthItem,
                eFieldItemType.SpeedUp or eFieldItemType.SpeedUpItem => OpenGSCore.EFieldItemType.SpeedUpItem,
                eFieldItemType.NormalGrenadePack or eFieldItemType.GrenadePack => OpenGSCore.EFieldItemType.GrenadePack,
                eFieldItemType.Random => OpenGSCore.EFieldItemType.PowerUpItem,
                eFieldItemType.RocketLauncher => OpenGSCore.EFieldItemType.GranadeLauncher,
                eFieldItemType.FlameThrower => OpenGSCore.EFieldItemType.FlameThrower,
                _ => OpenGSCore.EFieldItemType.PowerUpItem
            };
        }

        public static eFieldItemType ToLegacyType(OpenGSCore.EFieldItemType type)
        {
            return type switch
            {
                OpenGSCore.EFieldItemType.GranadeLauncher => eFieldItemType.RocketLauncher,
                OpenGSCore.EFieldItemType.FlameThrower => eFieldItemType.FlameThrower,
                OpenGSCore.EFieldItemType.PowerUpItem => eFieldItemType.PowerUpItem,
                OpenGSCore.EFieldItemType.DefenceUpItem => eFieldItemType.DefenceUpItem,
                OpenGSCore.EFieldItemType.SpeedUpItem => eFieldItemType.SpeedUpItem,
                OpenGSCore.EFieldItemType.StealthItem => eFieldItemType.StealthItem,
                OpenGSCore.EFieldItemType.GrenadePack => eFieldItemType.GrenadePack,
                OpenGSCore.EFieldItemType.HealItem => eFieldItemType.PowerUpItem,
                _ => eFieldItemType.PowerUpItem
            };
        }

        public static bool TryParseLegacy(string value, out eFieldItemType type)
        {
            type = eFieldItemType.None;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return System.Enum.TryParse(value, true, out type)
                || TryParseLegacyAlias(value, out type);
        }

        private static bool TryParseLegacyAlias(string value, out eFieldItemType type)
        {
            type = eFieldItemType.None;
            switch (value.Trim().ToLowerInvariant())
            {
                case "powerup":
                case "powerupitem":
                    type = eFieldItemType.PowerUpItem;
                    return true;
                case "defenceup":
                case "defenceupitem":
                    type = eFieldItemType.DefenceUpItem;
                    return true;
                case "speedup":
                case "speedupitem":
                    type = eFieldItemType.SpeedUpItem;
                    return true;
                case "stealth":
                case "stealthitem":
                    type = eFieldItemType.StealthItem;
                    return true;
                case "grenadepack":
                case "normalgrenadepack":
                    type = eFieldItemType.GrenadePack;
                    return true;
                case "rocketlauncher":
                    type = eFieldItemType.RocketLauncher;
                    return true;
                case "flamethrower":
                    type = eFieldItemType.FlameThrower;
                    return true;
                default:
                    return false;
            }
        }
    }
}
