using System.Collections.Generic;
using UnityEngine;
using OpenGSCore;
using OpenGSR.Audio;

namespace OpenGS
{
    /// <summary>
    /// ISoundService の具体的な実装クラス。
    /// </summary>
    public class SoundService : ISoundService
    {
        private readonly SoundMasterData _soundMasterData;
        private readonly BGMMasterData _bgmMasterData;

        // キャッシュ
        private readonly Dictionary<string, AudioClip> _weaponShotClipCache = new();
        private readonly Dictionary<string, AudioClip> _weaponReloadClipCache = new();
        private readonly Dictionary<string, AudioClip> _weaponHitClipCache = new();
        private readonly Dictionary<string, AudioClip> _grenadeThrowClipCache = new();

        public SoundService(SoundMasterData soundMasterData, BGMMasterData bgmMasterData = null)
        {
            _soundMasterData = soundMasterData;
            _bgmMasterData = bgmMasterData;
        }

        public void PlayBGM(EBgm bgm, float fadeTime = -1f)
        {
            if (_bgmMasterData != null && _bgmMasterData.TryGetBGM(bgm, out var clip))
            {
                SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
            }
            else
            {
                Debug.LogWarning($"[SoundService] BGM {bgm} not found in MasterData.");
            }
        }

        public void PlayBGM(EMap map) => SimpleAudioManager.Instance.PlayBGM(map.ToString());

        public void PlayBGM(string bgmName, float fadeTime = -1f)
        {
            // 1. BGMMasterData から文字列名で探す
            if (_bgmMasterData != null && _bgmMasterData.TryGetBGMByName(bgmName, out var clip))
            {
                SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
            }
            else
            {
                // 2. なければ直接ファイル名として再生
                SimpleAudioManager.Instance.PlayBGM(bgmName, fadeTime);
            }
        }

        public void PlayBGM(AudioClip clip, float fadeTime = -1f)
        {
            if (clip == null) return;
            SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
        }

        public void StopBGM(float fadeTime = -1f) => SimpleAudioManager.Instance.StopBGM(fadeTime);

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

        public void PlayWeaponShot(EWeaponType type, float pitch = 1.0f) => PlayOneShot(GetWeaponClip(type, "shot", _weaponShotClipCache), 1.0f, pitch);
        public void PlayWeaponReload(EWeaponType type, float pitch = 1.0f) => PlayOneShot(GetWeaponClip(type, "reload", _weaponReloadClipCache), 1.0f, pitch);
        public void PlayWeaponHit(EWeaponType type, float pitch = 1.0f) => PlayOneShot(GetWeaponClip(type, "hit", _weaponHitClipCache), 1.0f, pitch);
        public void PlayGrenadeThrow(EGrenadeType type, float pitch = 1.0f) => PlayOneShot(GetGrenadeThrowClip(type), 1.0f, pitch);

        public void PlayOneShot(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip == null) return;
            SimpleAudioManager.Instance.PlaySE(clip, volume, pitch);
        }

        public bool ValidateSoundSetup(bool logWarnings = true)
        {
            if (_soundMasterData == null) return false;
            return _soundMasterData.ValidateAllMappings(logWarnings);
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

            string weaponName = type.ToString();
            string lower = weaponName.ToLowerInvariant();
            AudioClip loaded = LoadFirst(
                $"Sound/Weapon/{weaponName}_{category}",
                $"Sound/Weapon/{lower}_{category}",
                $"Sound/Weapon/sfx_{lower}_{category}");

            cache[key] = loaded;
            return loaded;
        }

        private AudioClip GetGrenadeThrowClip(EGrenadeType type)
        {
            if (_soundMasterData != null && _soundMasterData.TryGetGrenadeThrowSound(type, out var mappedClip)) return mappedClip;
            string key = type.ToString();
            if (_grenadeThrowClipCache.TryGetValue(key, out var cached)) return cached;
            AudioClip loaded = LoadFirst($"Sound/Grenade/{type}_throw", "Sound/Weapon/grenade_throw");
            _grenadeThrowClipCache[key] = loaded;
            return loaded;
        }

        private static AudioClip LoadFirst(params string[] candidates)
        {
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
