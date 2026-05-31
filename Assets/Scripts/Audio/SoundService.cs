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
        private readonly Dictionary<string, AudioClip> _playerSoundClipCache = new();

        public SoundService(SoundMasterData soundMasterData, BGMMasterData bgmMasterData = null)
        {
            _soundMasterData = soundMasterData;
            _bgmMasterData = bgmMasterData ?? Resources.Load<BGMMasterData>("MasterData/BGMMasterData");
            
            Debug.Log($"[SoundService] Initialized. SoundData: {(soundMasterData != null ? "Loaded" : "Null")}, BGMData: {(_bgmMasterData != null ? "Loaded" : "Null")}");
        }

        public void PlayBGM(EBgm bgm, float fadeTime = -1f)
        {
            Debug.Log($"[SoundService] PlayBGM(Enum): {bgm}");
            if (_bgmMasterData != null && _bgmMasterData.TryGetBGM(bgm, out var clip))
            {
                Debug.Log($"[SoundService] Found BGM clip for {bgm}: {clip.name}");
                SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
                SimpleAudioManager.Instance.SetCurrentBGMName(bgm.ToString());
            }
            else
            {
                Debug.Log($"[SoundService] BGM {bgm} not found in MasterData, falling back to named load.");
                SimpleAudioManager.Instance.PlayBGM(bgm.ToString(), fadeTime);
            }
        }

        public void PlayBGM(EMap map)
        {
            Debug.Log($"[SoundService] PlayBGM(Map): {map}");
            var resolvedBgm = ResolveMapBgm(map);
            if (_bgmMasterData != null && _bgmMasterData.TryGetBGM(resolvedBgm, out var clip))
            {
                Debug.Log($"[SoundService] Found map BGM clip for {map} -> {resolvedBgm}: {clip.name}");
                SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
                SimpleAudioManager.Instance.SetCurrentBGMName(resolvedBgm.ToString());
                return;
            }

            Debug.Log($"[SoundService] Map BGM {resolvedBgm} not found in MasterData, falling back to named load.");
            SimpleAudioManager.Instance.PlayBGM(resolvedBgm.ToString());
        }

        public void PlayBGM(string bgmName, float fadeTime = -1f)
        {
            Debug.Log($"[SoundService] PlayBGM(String): {bgmName}");
            // 1. BGMMasterData から文字列名で探す
            if (_bgmMasterData != null && _bgmMasterData.TryGetBGMByName(bgmName, out var clip))
            {
                Debug.Log($"[SoundService] Found BGM clip by name {bgmName}: {clip.name}");
                SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
                SimpleAudioManager.Instance.SetCurrentBGMName(bgmName);
            }
            else
            {
                Debug.Log($"[SoundService] BGM {bgmName} not in MasterData, trying direct load.");
                // 2. なければ直接ファイル名として再生
                SimpleAudioManager.Instance.PlayBGM(bgmName, fadeTime);
            }
        }

        public void PlayBGM(AudioClip clip, float fadeTime = -1f)
        {
            if (clip == null) return;
            Debug.Log($"[SoundService] PlayBGM(Clip): {clip.name}");
            SimpleAudioManager.Instance.PlayBGM(clip, 1.0f, true);
            SimpleAudioManager.Instance.SetCurrentBGMName(clip.name);
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
        public void PlayPlayerSound(EPlayerSound sound) => PlayOneShot(GetPlayerSoundClip(sound), 1.0f, 1.0f);

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

        private AudioClip GetPlayerSoundClip(EPlayerSound sound)
        {
            string key = sound.ToString();
            if (_playerSoundClipCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            AudioClip loaded = sound switch
            {
                EPlayerSound.DamageFemale1 => LoadFirst("Sound/Player/sfx_pl_hit01", "Sound/Player/sfx_pl_deathF01"),
                EPlayerSound.DamageMale1 => LoadFirst("Sound/Player/sfx_pl_hit01", "Sound/Player/sfx_pl_deathM01"),
                EPlayerSound.DeathFemale1 => LoadFirst("Sound/Player/sfx_pl_deathF01"),
                EPlayerSound.DeathFemale2 => LoadFirst("Sound/Player/sfx_pl_deathF02"),
                EPlayerSound.DeathFemale3 => LoadFirst("Sound/Player/sfx_pl_deathF03"),
                EPlayerSound.DeathFemale4 => LoadFirst("Sound/Player/sfx_pl_deathF04"),
                EPlayerSound.DeathMale1 => LoadFirst("Sound/Player/sfx_pl_deathM01"),
                EPlayerSound.DeathMale2 => LoadFirst("Sound/Player/sfx_pl_deathM02"),
                EPlayerSound.DeathMale3 => LoadFirst("Sound/Player/sfx_pl_deathM03"),
                EPlayerSound.DeathMale4 => LoadFirst("Sound/Player/sfx_pl_deathM04"),
                _ => NoPlayerSoundClip(sound)
            };

            _playerSoundClipCache[key] = loaded;
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

            Debug.LogWarning($"[SoundService] AudioClip not found from {candidates.Length} candidates.");
            return null;
        }

        private static AudioClip NoPlayerSoundClip(EPlayerSound sound)
        {
            Debug.LogWarning($"[SoundService] No player sound mapping for {sound}.");
            return null;
        }

        private static EBgm ResolveMapBgm(EMap map)
        {
            return map switch
            {
                EMap.AuroraClassic => EBgm.AuroraClassic,
                EMap.ArchLoadOfGunster => EBgm.BattleBase,
                EMap.IceValley => EBgm.AuroraClassic,
                EMap.DryDays => EBgm.DryDays,
                EMap.GreenHillSide1 => EBgm.Green,
                EMap.GreenHillSide2 => EBgm.Green,
                EMap.CityOfDarkness1 => EBgm.Ruin,
                EMap.CityOfDarkness2 => EBgm.Ruin,
                EMap.BluffStructure1 => EBgm.BattleBase,
                EMap.BluffStructure2 => EBgm.BattleBase,
                EMap.DesertedJungleSide1 => EBgm.Forest,
                EMap.DesertedJungleSide2 => EBgm.Forest,
                EMap.BattlePort1 => EBgm.Pipe,
                EMap.BattlePortCTF => EBgm.Pipe,
                EMap.FullHouse => EBgm.BattleBase,
                EMap.FactoryInGaol => EBgm.Factory,
                EMap.RobotFactory => EBgm.Factory,
                EMap.RedStorm1 => EBgm.Pipe,
                EMap.RedStorm2 => EBgm.Pipe,
                EMap.ThePark => EBgm.AmusementPark,
                EMap.TheParkCTF => EBgm.AmusementPark,
                EMap.RuinOfWarSide1 => EBgm.Ruin,
                EMap.RuinOfWarSide2 => EBgm.Ruin,
                EMap.Nocturne => EBgm.AuroraClassic,
                EMap.Waterfall => EBgm.AuroraClassic,
                EMap.SkyHigh => EBgm.AuroraClassic,
                EMap.SkyHighCTF => EBgm.AuroraClassic,
                EMap.GhostHouse => EBgm.Forest,
                EMap.OnStudio => EBgm.AmusementPark,
                EMap.Christmas => EBgm.Green,
                EMap.SeaSideBase => EBgm.Pipe,
                _ => EBgm.DryDays
            };
        }
    }
}
