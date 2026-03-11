
using UnityEngine;
using OpenGSR.Audio;
using OpenGSCore;
using System.Collections.Generic;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class SoundManager
    {
        public static SoundManager Instance { get; } = new();

        private readonly SoundMasterData soundMasterData;
        private readonly Dictionary<string, AudioClip> weaponShotClipCache = new();
        private readonly Dictionary<string, AudioClip> weaponReloadClipCache = new();
        private readonly Dictionary<string, AudioClip> weaponHitClipCache = new();
        private readonly Dictionary<string, AudioClip> grenadeThrowClipCache = new();

        private SoundManager()
        {
            soundMasterData = SoundMasterData.Instance();
        }

        public void PlayBGM(EMap map)
        {
            PlayBgm(map.ToString());
        }

        public void PlaySystemSound(ESystemSound sound)
        {
            var clip = GetSystemSoundClip(sound);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"System:{sound}");
        }

        public void PlayShotSound(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "shot", weaponShotClipCache);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"WeaponShot:{type}");
        }

        public void PlayReloadSound(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "reload", weaponReloadClipCache);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"WeaponReload:{type}");
        }

        public void PlayHitSound(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "hit", weaponHitClipCache);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"WeaponHit:{type}");
        }

        public void PlaySoundEffect(ESoundEffect type, float volume = 1.0f)
        {
            var clip = GetEffectSoundClip(type);
            PlayOneShotSafe(clip, volume, 1.0f, $"Effect:{type}");
        }

        public void PlayPlayerSound()
        {
            // Keep this API for compatibility. Player voice mapping is not finalized yet.
        }

        public void PlayThrowGrenadeSound(EGrenadeType type)
        {
            var clip = GetGrenadeThrowClip(type);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"GrenadeThrow:{type}");
        }

        public void PlayGameSound(EMatchSound sound)
        {
            var clip = GetMatchSoundClip(sound);
            PlayOneShotSafe(clip, 1.0f, 1.0f, $"Match:{sound}");
        }

        public bool PlayOneShotSafe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, string context = null, bool warnIfMissing = false)
        {
            if (clip == null)
            {
                if (warnIfMissing)
                {
                    Debug.LogWarning($"[SoundManager] OneShot clip is null. {context}");
                }
                return false;
            }

            SimpleAudioManager.Instance.PlaySE(clip, Mathf.Clamp01(volume), pitch);
            return true;
        }

        public bool PlayBgmByName(string bgmName, float fadeTime = -1f, bool warnIfMissing = false)
        {
            if (string.IsNullOrWhiteSpace(bgmName))
            {
                if (warnIfMissing)
                {
                    Debug.LogWarning("[SoundManager] BGM name is empty.");
                }
                return false;
            }

            SimpleAudioManager.Instance.PlayBGM(bgmName, fadeTime);
            return true;
        }

        public bool PlayBgm(AudioClip clip, float volume = 1.0f, bool loop = true)
        {
            if (clip == null)
            {
                return false;
            }

            SimpleAudioManager.Instance.PlayBGM(clip, Mathf.Clamp01(volume), loop);
            return true;
        }

        public bool PlayBgm(string bgmName, float fadeTime = -1f)
        {
            return PlayBgmByName(bgmName, fadeTime);
        }

        public void StopBgm(float fadeTime = -1f)
        {
            SimpleAudioManager.Instance.StopBGM(fadeTime);
        }

        public void CrossFadeBgm(string bgmName, float fadeTime = -1f)
        {
            PlayBgmByName(bgmName, fadeTime);
        }

        public bool IsBgmPlaying()
        {
            return SimpleAudioManager.Instance.IsPlayingBGM();
        }

        public AudioClip GetSystemSoundClip(ESystemSound sound)
        {
            return soundMasterData != null ? soundMasterData.GetSystemSound(sound) : null;
        }

        public AudioClip GetMatchSoundClip(EMatchSound sound)
        {
            return soundMasterData != null ? soundMasterData.GetMatchSound(sound) : null;
        }

        public AudioClip GetEffectSoundClip(ESoundEffect sound)
        {
            return soundMasterData != null ? soundMasterData.GetEffectSound(sound) : null;
        }

        public int Warmup()
        {
            return soundMasterData != null ? soundMasterData.Warmup() : 0;
        }

        public bool ValidateSoundSetup(bool logWarnings = true)
        {
            if (soundMasterData == null)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[SoundManager] SoundMasterData is null.");
                }
                return false;
            }

            return soundMasterData.ValidateAllMappings(logWarnings);
        }

        private AudioClip GetWeaponClip(EWeaponType type, string category, Dictionary<string, AudioClip> cache)
        {
            string key = $"{category}:{type}";
            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            string weaponName = type.ToString();
            string lower = weaponName.ToLowerInvariant();
            AudioClip loaded = LoadFirst(
                $"Sound/Weapon/{weaponName}_{category}",
                $"Sound/Weapon/{lower}_{category}",
                $"Sound/Weapon/sfx_{lower}_{category}",
                $"Sound/Weapon/{category}_{lower}",
                $"Sound/{category}_{lower}");

            cache[key] = loaded;
            return loaded;
        }

        private AudioClip GetGrenadeThrowClip(EGrenadeType type)
        {
            string key = type.ToString();
            if (grenadeThrowClipCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            string name = type.ToString();
            string lower = name.ToLowerInvariant();
            AudioClip loaded = LoadFirst(
                $"Sound/Grenade/{name}_throw",
                $"Sound/Grenade/{lower}_throw",
                $"Sound/Weapon/grenade_throw_{lower}",
                "Sound/Weapon/grenade_throw");

            grenadeThrowClipCache[key] = loaded;
            return loaded;
        }

        private static AudioClip LoadFirst(params string[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

    }

}
