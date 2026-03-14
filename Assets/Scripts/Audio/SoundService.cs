using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;
using OpenGSR.Audio;

namespace OpenGS
{
    /// <summary>
    /// ISoundService の具体的な実装クラス。
    /// SoundMasterData から AudioClip を取得し、SimpleAudioManager を通じて再生する。
    /// </summary>
    public class SoundService : ISoundService
    {
        private readonly SoundMasterData _soundMasterData;

        // キャッシュ（旧 SoundManager のロジックを継承）
        private readonly Dictionary<string, AudioClip> _weaponShotClipCache = new();
        private readonly Dictionary<string, AudioClip> _weaponReloadClipCache = new();
        private readonly Dictionary<string, AudioClip> _weaponHitClipCache = new();
        private readonly Dictionary<string, AudioClip> _grenadeThrowClipCache = new();

        public SoundService(SoundMasterData soundMasterData)
        {
            _soundMasterData = soundMasterData;
        }

        public void PlayBGM(EMap map)
        {
            SimpleAudioManager.Instance.PlayBGM(map.ToString());
        }

        public void PlayBGM(string bgmName, float fadeTime = -1f)
        {
            SimpleAudioManager.Instance.PlayBGM(bgmName, fadeTime);
        }

        public void StopBGM(float fadeTime = -1f)
        {
            SimpleAudioManager.Instance.StopBGM(fadeTime);
        }

        public void PlaySystemSound(ESystemSound sound)
        {
            var clip = _soundMasterData != null ? _soundMasterData.GetSystemSound(sound) : null;
            PlayOneShot(clip);
        }

        public void PlayMatchSound(EMatchSound sound)
        {
            var clip = _soundMasterData != null ? _soundMasterData.GetMatchSound(sound) : null;
            PlayOneShot(clip);
        }

        public void PlaySoundEffect(ESoundEffect sound, float volume = 1.0f)
        {
            var clip = _soundMasterData != null ? _soundMasterData.GetEffectSound(sound) : null;
            PlayOneShot(clip, volume);
        }

        public void PlayWeaponShot(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "shot", _weaponShotClipCache);
            PlayOneShot(clip);
        }

        public void PlayWeaponReload(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "reload", _weaponReloadClipCache);
            PlayOneShot(clip);
        }

        public void PlayWeaponHit(EWeaponType type)
        {
            var clip = GetWeaponClip(type, "hit", _weaponHitClipCache);
            PlayOneShot(clip);
        }

        public void PlayGrenadeThrow(EGrenadeType type)
        {
            var clip = GetGrenadeThrowClip(type);
            PlayOneShot(clip);
        }

        public void PlayOneShot(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip == null) return;
            SimpleAudioManager.Instance.PlaySE(clip, volume, pitch);
        }

        public bool ValidateSoundSetup(bool logWarnings = true)
        {
            if (_soundMasterData == null)
            {
                if (logWarnings) Debug.LogWarning("[SoundService] SoundMasterData is null.");
                return false;
            }

            bool generalValid = _soundMasterData.ValidateAllMappings(logWarnings);
            bool combatValid = _soundMasterData.ValidateCombatMappings(out var combatReport);
            if (logWarnings)
            {
                if (combatValid) Debug.Log(combatReport);
                else Debug.LogWarning(combatReport);
            }

            return generalValid && combatValid;
        }

        private AudioClip GetWeaponClip(EWeaponType type, string category, Dictionary<string, AudioClip> cache)
        {
            if (_soundMasterData != null)
            {
                if (category == "shot" && _soundMasterData.TryGetWeaponShotSound(type, out var shotClip)) return shotClip;
                if (category == "reload" && _soundMasterData.TryGetWeaponReloadSound(type, out var reloadClip)) return reloadClip;
                if (category == "hit" && _soundMasterData.TryGetWeaponHitSound(type, out var hitClip)) return hitClip;
            }

            string key = $"{category}:{type}";
            if (cache.TryGetValue(key, out var cached)) return cached;

            // フォールバック（Resources.Load）
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
            if (_soundMasterData != null && _soundMasterData.TryGetGrenadeThrowSound(type, out var mappedClip)) return mappedClip;

            string key = type.ToString();
            if (_grenadeThrowClipCache.TryGetValue(key, out var cached)) return cached;

            string name = type.ToString();
            string lower = name.ToLowerInvariant();
            AudioClip loaded = LoadFirst(
                $"Sound/Grenade/{name}_throw",
                $"Sound/Grenade/{lower}_throw",
                $"Sound/Weapon/grenade_throw_{lower}",
                "Sound/Weapon/grenade_throw");

            _grenadeThrowClipCache[key] = loaded;
            return loaded;
        }

        private static AudioClip LoadFirst(params string[] candidates)
        {
            if (candidates == null) return null;
            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                var clip = Resources.Load<AudioClip>(path);
                if (clip != null) return clip;
            }
            return null;
        }
    }
}
