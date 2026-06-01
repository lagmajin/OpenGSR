using System;
using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public static class SoundVisualResolver
    {
        private static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        public static string GetSystemDisplayName(ESystemSound sound)
        {
            return sound switch
            {
                ESystemSound.Click => "Click",
                ESystemSound.Error => "Error",
                ESystemSound.Check => "Check",
                ESystemSound.EnterLobby => "Enter Lobby",
                ESystemSound.Popup => "Popup",
                ESystemSound.Exit => "Exit",
                ESystemSound.Fanfare => "Fanfare",
                _ => sound.ToString()
            };
        }

        public static string GetMatchDisplayName(EMatchSound sound)
        {
            return sound switch
            {
                EMatchSound.GameStartVoice => "Game Start Voice",
                EMatchSound.SuddenDeathVoice => "Sudden Death Voice",
                EMatchSound.YouWon => "You Won",
                EMatchSound.YouLost => "You Lost",
                EMatchSound.RedTeamFlagCaptured => "Red Flag Captured",
                EMatchSound.BlueTeamFlagCaptured => "Blue Flag Captured",
                EMatchSound.FlagLost => "Flag Lost",
                EMatchSound.RedTeamFlagReturn => "Red Flag Returned",
                EMatchSound.BlueTeamFlagReturn => "Blue Flag Returned",
                _ => sound.ToString()
            };
        }

        public static string GetEffectDisplayName(ESoundEffect sound)
        {
            return sound switch
            {
                ESoundEffect.Explosion => "Explosion",
                ESoundEffect.HitStageObject => "Hit Stage Object",
                _ => sound.ToString()
            };
        }

        public static string GetGrenadeDisplayName(EGrenadeSound sound)
        {
            return sound switch
            {
                EGrenadeSound.ExplosionGrenade => "Grenade Explosion",
                EGrenadeSound.ExplosionFireGrenade => "Fire Grenade Explosion",
                _ => sound.ToString()
            };
        }

        public static AudioClip GetGrenadeExplosionClip(EGrenadeSound sound)
        {
            return SoundMasterData.Instance().TryGetGrenadeExplosionSound(sound, out var clip) ? clip : null;
        }

        public static string GetPlayerDisplayName(EPlayerSound sound)
        {
            return sound switch
            {
                EPlayerSound.DamageFemale1 => "Damage Female 1",
                EPlayerSound.DamageMale1 => "Damage Male 1",
                EPlayerSound.DeathFemale1 => "Death Female 1",
                EPlayerSound.DeathFemale2 => "Death Female 2",
                EPlayerSound.DeathFemale3 => "Death Female 3",
                EPlayerSound.DeathFemale4 => "Death Female 4",
                EPlayerSound.DeathMale1 => "Death Male 1",
                EPlayerSound.DeathMale2 => "Death Male 2",
                EPlayerSound.DeathMale3 => "Death Male 3",
                EPlayerSound.DeathMale4 => "Death Male 4",
                _ => sound.ToString()
            };
        }

        public static string GetPlayerGeneralDisplayName(EPlayerGeneralSound sound)
        {
            return sound switch
            {
                EPlayerGeneralSound.JumpStart => "Jump Start",
                EPlayerGeneralSound.JumpEnd => "Jump End",
                EPlayerGeneralSound.BoosterStart => "Booster Start",
                EPlayerGeneralSound.BoosterLoop => "Booster Loop",
                EPlayerGeneralSound.BoosterEnd => "Booster End",
                EPlayerGeneralSound.TakeItem => "Take Item",
                EPlayerGeneralSound.TakeGrenade => "Take Grenade",
                EPlayerGeneralSound.DropItem => "Drop Item",
                EPlayerGeneralSound.OpenGrenade => "Open Grenade",
                EPlayerGeneralSound.ThrowGrenade => "Throw Grenade",
                _ => sound.ToString()
            };
        }

        public static string GetTakeItemDisplayName(ETakeItemSound sound)
        {
            return sound switch
            {
                ETakeItemSound.TakePowerUpItemSound => "Take Power Up",
                ETakeItemSound.TakeDefenseUpItemSound => "Take Defense Up",
                ETakeItemSound.TakeSpeedUpItemSound => "Take Speed Up",
                ETakeItemSound.TakeHealItemSound => "Take Heal",
                ETakeItemSound.TakeRandomItemSound => "Take Random",
                _ => sound.ToString()
            };
        }

        public static AudioClip GetTakeItemClip(ETakeItemSound sound)
        {
            if (SoundMasterData.Instance().TryGetTakeItemSound(sound, out var clip) && clip != null)
            {
                return clip;
            }

            return LoadClip(GetTakeItemPaths(sound));
        }

        public static AudioClip GetSystemClip(ESystemSound sound)
        {
            return SoundMasterData.Instance().GetSystemSound(sound);
        }

        public static AudioClip GetMatchClip(EMatchSound sound)
        {
            return SoundMasterData.Instance().GetMatchSound(sound);
        }

        public static AudioClip GetEffectClip(ESoundEffect sound)
        {
            return SoundMasterData.Instance().GetEffectSound(sound);
        }

        public static bool TryGetGrenadeThrowClip(EGrenadeType type, out AudioClip clip)
        {
            return SoundMasterData.Instance().TryGetGrenadeThrowSound(type, out clip);
        }

        private static string[] GetTakeItemPaths(ETakeItemSound sound)
        {
            return sound switch
            {
                ETakeItemSound.TakePowerUpItemSound => new[] { "Sound/Item/sfx_take_offense" },
                ETakeItemSound.TakeDefenseUpItemSound => new[] { "Sound/Item/sfx_take_defence" },
                ETakeItemSound.TakeSpeedUpItemSound => new[] { "Sound/Item/sfx_take_speedup" },
                ETakeItemSound.TakeHealItemSound => new[] { "Sound/Item/sfx_take_medikit", "Sound/Item/sfx_take_immortal" },
                ETakeItemSound.TakeRandomItemSound => new[] { "Sound/Item/sfx_take_weapon", "Sound/Item/sfx_take_grenade" },
                _ => Array.Empty<string>()
            };
        }

        private static AudioClip LoadClip(IEnumerable<string> resourcePaths)
        {
            foreach (var path in resourcePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (clipCache.TryGetValue(path, out var cached))
                {
                    if (cached != null)
                    {
                        return cached;
                    }

                    continue;
                }

                var clip = Resources.Load<AudioClip>(path);
                clipCache[path] = clip;
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
