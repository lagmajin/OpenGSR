using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Sound/SoundMasterData")]
    public class SoundMasterData : ScriptableObject
    {
        [Serializable]
        public struct SystemSoundEntry
        {
            public ESystemSound sound;
            public AudioClip clip;
        }

        [Serializable]
        public struct MatchSoundEntry
        {
            public EMatchSound sound;
            public AudioClip clip;
        }

        [Serializable]
        public struct EffectSoundEntry
        {
            public ESoundEffect sound;
            public AudioClip clip;
        }

        [Serializable]
        public struct WeaponSoundEntry
        {
            public EWeaponType weaponType;
            public AudioClip shotClip;
            public AudioClip reloadClip;
            public AudioClip hitClip;
        }

        [Serializable]
        public struct GrenadeSoundEntry
        {
            public EGrenadeType grenadeType;
            public AudioClip throwClip;
        }

        [SerializeField] private List<SystemSoundEntry> systemSounds = new List<SystemSoundEntry>();
        [SerializeField] private List<MatchSoundEntry> matchSounds = new List<MatchSoundEntry>();
        [SerializeField] private List<EffectSoundEntry> effectSounds = new List<EffectSoundEntry>();
        [SerializeField] private List<WeaponSoundEntry> weaponSounds = new List<WeaponSoundEntry>();
        [SerializeField] private List<GrenadeSoundEntry> grenadeSounds = new List<GrenadeSoundEntry>();

        private static SoundMasterData instance;

        private readonly Dictionary<ESystemSound, AudioClip> systemMap = new Dictionary<ESystemSound, AudioClip>();
        private readonly Dictionary<EMatchSound, AudioClip> matchMap = new Dictionary<EMatchSound, AudioClip>();
        private readonly Dictionary<ESoundEffect, AudioClip> effectMap = new Dictionary<ESoundEffect, AudioClip>();
        private readonly Dictionary<EWeaponType, WeaponSoundEntry> weaponMap = new Dictionary<EWeaponType, WeaponSoundEntry>();
        private readonly Dictionary<EGrenadeType, AudioClip> grenadeThrowMap = new Dictionary<EGrenadeType, AudioClip>();

        public static SoundMasterData Instance()
        {
            if (instance == null)
            {
                instance = Resources.Load<SoundMasterData>("MasterData/SoundMasterData");
                if (instance == null)
                {
                    instance = CreateInstance<SoundMasterData>();
                    Debug.LogWarning("SoundMasterData asset not found at Resources/MasterData/SoundMasterData. Fallback path lookup will be used.");
                }
                instance.RebuildMaps();
            }

            return instance;
        }

        private void OnEnable()
        {
            RebuildMaps();
        }

        private void RebuildMaps()
        {
            systemMap.Clear();
            foreach (var e in systemSounds)
            {
                systemMap[e.sound] = e.clip;
            }

            matchMap.Clear();
            foreach (var e in matchSounds)
            {
                matchMap[e.sound] = e.clip;
            }

            effectMap.Clear();
            foreach (var e in effectSounds)
            {
                effectMap[e.sound] = e.clip;
            }

            weaponMap.Clear();
            foreach (var e in weaponSounds)
            {
                weaponMap[e.weaponType] = e;
            }

            grenadeThrowMap.Clear();
            foreach (var e in grenadeSounds)
            {
                grenadeThrowMap[e.grenadeType] = e.throwClip;
            }
        }

        public AudioClip GetSystemSound(ESystemSound sound)
        {
            TryGetSystemSound(sound, out var clip);
            return clip;
        }

        public AudioClip GetMatchSound(EMatchSound sound)
        {
            TryGetMatchSound(sound, out var clip);
            return clip;
        }

        public AudioClip GetEffectSound(ESoundEffect sound)
        {
            TryGetEffectSound(sound, out var clip);
            return clip;
        }

        public bool TryGetSystemSound(ESystemSound sound, out AudioClip clip)
        {
            if (systemMap.TryGetValue(sound, out clip) && clip != null)
            {
                return true;
            }

            clip = LoadFirst(GetSystemSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetMatchSound(EMatchSound sound, out AudioClip clip)
        {
            if (matchMap.TryGetValue(sound, out clip) && clip != null)
            {
                return true;
            }

            clip = LoadFirst(GetMatchSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetEffectSound(ESoundEffect sound, out AudioClip clip)
        {
            if (effectMap.TryGetValue(sound, out clip) && clip != null)
            {
                return true;
            }

            clip = LoadFirst(GetEffectSoundPaths(sound));
            return clip != null;
        }

        public bool HasSystemSound(ESystemSound sound) => TryGetSystemSound(sound, out _);

        public bool HasMatchSound(EMatchSound sound) => TryGetMatchSound(sound, out _);

        public bool HasEffectSound(ESoundEffect sound) => TryGetEffectSound(sound, out _);

        public bool TryGetWeaponShotSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry))
            {
                return false;
            }

            clip = entry.shotClip;
            return clip != null;
        }

        public bool TryGetWeaponReloadSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry))
            {
                return false;
            }

            clip = entry.reloadClip;
            return clip != null;
        }

        public bool TryGetWeaponHitSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry))
            {
                return false;
            }

            clip = entry.hitClip;
            return clip != null;
        }

        public bool TryGetGrenadeThrowSound(EGrenadeType grenadeType, out AudioClip clip)
        {
            if (grenadeThrowMap.TryGetValue(grenadeType, out clip) && clip != null)
            {
                return true;
            }

            return false;
        }

        public string GetResolvedSystemSoundPath(ESystemSound sound)
        {
            return GetResolvedPath(GetSystemSoundPaths(sound));
        }

        public string GetResolvedMatchSoundPath(EMatchSound sound)
        {
            return GetResolvedPath(GetMatchSoundPaths(sound));
        }

        public string GetResolvedEffectSoundPath(ESoundEffect sound)
        {
            return GetResolvedPath(GetEffectSoundPaths(sound));
        }

        public int PreloadAll()
        {
            RebuildMaps();

            var loadedCount = 0;
            loadedCount += PreloadSystemSounds();
            loadedCount += PreloadMatchSounds();
            loadedCount += PreloadEffectSounds();
            return loadedCount;
        }

        public int Warmup()
        {
            return PreloadAll();
        }

        public bool ValidateAllMappings(out string report)
        {
            var missing = new List<string>();

            foreach (ESystemSound sound in Enum.GetValues(typeof(ESystemSound)))
            {
                if (!TryGetSystemSound(sound, out _))
                {
                    missing.Add($"System:{sound}");
                }
            }

            foreach (EMatchSound sound in Enum.GetValues(typeof(EMatchSound)))
            {
                if (!TryGetMatchSound(sound, out _))
                {
                    missing.Add($"Match:{sound}");
                }
            }

            foreach (ESoundEffect sound in Enum.GetValues(typeof(ESoundEffect)))
            {
                if (!TryGetEffectSound(sound, out _))
                {
                    missing.Add($"Effect:{sound}");
                }
            }

            if (missing.Count == 0)
            {
                report = "SoundMasterData validation passed. No missing mappings.";
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine("SoundMasterData validation failed. Missing mappings:");
            foreach (var item in missing)
            {
                sb.Append("- ");
                sb.AppendLine(item);
            }

            report = sb.ToString();
            return false;
        }

        public bool ValidateAllMappings(bool logWarnings = true)
        {
            var isValid = ValidateAllMappings(out var report);
            if (logWarnings)
            {
                if (isValid)
                {
                    Debug.Log(report);
                }
                else
                {
                    Debug.LogWarning(report);
                }
            }

            return isValid;
        }

        public bool ValidateCombatMappings(out string report)
        {
            var missing = new List<string>();

            foreach (EWeaponType weaponType in Enum.GetValues(typeof(EWeaponType)))
            {
                if (weaponType == EWeaponType.None)
                {
                    continue;
                }

                if (!TryGetWeaponShotSound(weaponType, out _))
                {
                    missing.Add($"WeaponShot:{weaponType}");
                }
            }

            foreach (EGrenadeType grenadeType in Enum.GetValues(typeof(EGrenadeType)))
            {
                if (grenadeType == EGrenadeType.Empty || grenadeType == EGrenadeType.ClusterChild)
                {
                    continue;
                }

                if (!TryGetGrenadeThrowSound(grenadeType, out _))
                {
                    missing.Add($"GrenadeThrow:{grenadeType}");
                }
            }

            if (missing.Count == 0)
            {
                report = "SoundMasterData combat mapping validation passed.";
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine("SoundMasterData combat mapping validation failed. Missing mappings:");
            foreach (var item in missing)
            {
                sb.Append("- ");
                sb.AppendLine(item);
            }

            report = sb.ToString();
            return false;
        }

        private static AudioClip LoadFirst(string[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static string GetResolvedPath(string[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (Resources.Load<AudioClip>(path) != null)
                {
                    return path;
                }
            }

            return null;
        }

        private int PreloadSystemSounds()
        {
            var loadedCount = 0;
            foreach (ESystemSound sound in Enum.GetValues(typeof(ESystemSound)))
            {
                if (systemMap.TryGetValue(sound, out var existing) && existing != null)
                {
                    continue;
                }

                var loaded = LoadFirst(GetSystemSoundPaths(sound));
                if (loaded != null)
                {
                    systemMap[sound] = loaded;
                    loadedCount++;
                }
            }

            return loadedCount;
        }

        private int PreloadMatchSounds()
        {
            var loadedCount = 0;
            foreach (EMatchSound sound in Enum.GetValues(typeof(EMatchSound)))
            {
                if (matchMap.TryGetValue(sound, out var existing) && existing != null)
                {
                    continue;
                }

                var loaded = LoadFirst(GetMatchSoundPaths(sound));
                if (loaded != null)
                {
                    matchMap[sound] = loaded;
                    loadedCount++;
                }
            }

            return loadedCount;
        }

        private int PreloadEffectSounds()
        {
            var loadedCount = 0;
            foreach (ESoundEffect sound in Enum.GetValues(typeof(ESoundEffect)))
            {
                if (effectMap.TryGetValue(sound, out var existing) && existing != null)
                {
                    continue;
                }

                var loaded = LoadFirst(GetEffectSoundPaths(sound));
                if (loaded != null)
                {
                    effectMap[sound] = loaded;
                    loadedCount++;
                }
            }

            return loadedCount;
        }

        private static string[] GetSystemSoundPaths(ESystemSound sound)
        {
            switch (sound)
            {
                case ESystemSound.Click: return new[] { "Sound/UI_Button_Click", "Sound/WaitRoom/ButtonClick" };
                case ESystemSound.Error: return new[] { "Sound/Common/Error" };
                case ESystemSound.Check: return new[] { "Sound/Common/ButtonCheck", "Sound/WaitRoom/Check" };
                case ESystemSound.EnterLobby: return new[] { "Sound/WaitRoom/EnterLobby" };
                case ESystemSound.Popup: return new[] { "Sound/WaitRoom/Popup" };
                case ESystemSound.Fanfare: return new[] { "Sound/Game/sfx_game_win_fanfare", "Sound/Game_Victory" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetMatchSoundPaths(EMatchSound sound)
        {
            switch (sound)
            {
                case EMatchSound.GameStartVoice: return new[] { "Sound/Game/voice_game_start" };
                case EMatchSound.YouWon: return new[] { "Sound/Game/sfx_game_win_fanfare", "Sound/Voice_Game_Blue_Scored" };
                case EMatchSound.YouLost: return new[] { "Sound/Game/sfx_game_lose", "Sound/Voice_Game_Defeat" };
                case EMatchSound.RedTeamFlagCaptured: return new[] { "Sound/Game/CTF/voice_game_rcapture", "Sound/Voice_Game_Red_Captured" };
                case EMatchSound.BlueTeamFlagCaptured: return new[] { "Sound/Game/CTF/voice_game_bcapture", "Sound/Voice_Game_Blue_Captured" };
                case EMatchSound.FlagLost: return new[] { "Sound/Game/CTF/sfx_ctf_lost" };
                case EMatchSound.RedTeamFlagReturn: return new[] { "Sound/Game/CTF/voice_game_rreturn", "Sound/Voice_Game_Red_Returned" };
                case EMatchSound.BlueTeamFlagReturn: return new[] { "Sound/Game/CTF/voice_game_breturn", "Sound/Voice_Game_Blue_Returned" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetEffectSoundPaths(ESoundEffect sound)
        {
            switch (sound)
            {
                case ESoundEffect.Explosion: return new[] { "Sound/Weapon/_grenade_explode", "Sound/Weapon_Grenade_Cluster_Explosion" };
                case ESoundEffect.HitStageObject: return new[] { "Sound/Bullet_Impact_Metal", "Sound/Weapon/sfx_ric01" };
                default: return Array.Empty<string>();
            }
        }
    }
}
