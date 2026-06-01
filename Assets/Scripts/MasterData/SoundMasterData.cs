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
        public struct TakeItemSoundEntry
        {
            public ETakeItemSound sound;
            public AudioClip clip;
        }

        [Serializable]
        public struct PlayerSoundEntry
        {
            public EPlayerSound sound;
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

        [Serializable]
        public struct GrenadeExplosionSoundEntry
        {
            public EGrenadeSound sound;
            public AudioClip clip;
        }

        [SerializeField] private List<SystemSoundEntry> systemSounds = new List<SystemSoundEntry>();
        [SerializeField] private List<MatchSoundEntry> matchSounds = new List<MatchSoundEntry>();
        [SerializeField] private List<EffectSoundEntry> effectSounds = new List<EffectSoundEntry>();
        [SerializeField] private List<TakeItemSoundEntry> takeItemSounds = new List<TakeItemSoundEntry>();
        [SerializeField] private List<PlayerSoundEntry> playerSounds = new List<PlayerSoundEntry>();
        [SerializeField] private List<WeaponSoundEntry> weaponSounds = new List<WeaponSoundEntry>();
        [SerializeField] private List<GrenadeSoundEntry> grenadeSounds = new List<GrenadeSoundEntry>();
        [SerializeField] private List<GrenadeExplosionSoundEntry> grenadeExplosionSounds = new List<GrenadeExplosionSoundEntry>();

        private static SoundMasterData instance;

        private readonly Dictionary<ESystemSound, AudioClip> systemMap = new Dictionary<ESystemSound, AudioClip>();
        private readonly Dictionary<EMatchSound, AudioClip> matchMap = new Dictionary<EMatchSound, AudioClip>();
        private readonly Dictionary<ESoundEffect, AudioClip> effectMap = new Dictionary<ESoundEffect, AudioClip>();
        private readonly Dictionary<ETakeItemSound, AudioClip> takeItemMap = new Dictionary<ETakeItemSound, AudioClip>();
        private readonly Dictionary<EPlayerSound, AudioClip> playerMap = new Dictionary<EPlayerSound, AudioClip>();
        private readonly Dictionary<EWeaponType, WeaponSoundEntry> weaponMap = new Dictionary<EWeaponType, WeaponSoundEntry>();
        private readonly Dictionary<EGrenadeType, AudioClip> grenadeThrowMap = new Dictionary<EGrenadeType, AudioClip>();
        private readonly Dictionary<EGrenadeSound, AudioClip> grenadeExplosionMap = new Dictionary<EGrenadeSound, AudioClip>();

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

        public void RebuildMaps()
        {
            systemMap.Clear();
            foreach (var e in systemSounds) systemMap[e.sound] = e.clip;

            matchMap.Clear();
            foreach (var e in matchSounds) matchMap[e.sound] = e.clip;

            effectMap.Clear();
            foreach (var e in effectSounds) effectMap[e.sound] = e.clip;

            takeItemMap.Clear();
            foreach (var e in takeItemSounds) takeItemMap[e.sound] = e.clip;

            playerMap.Clear();
            foreach (var e in playerSounds) playerMap[e.sound] = e.clip;

            weaponMap.Clear();
            foreach (var e in weaponSounds) weaponMap[e.weaponType] = e;

            grenadeThrowMap.Clear();
            foreach (var e in grenadeSounds) grenadeThrowMap[e.grenadeType] = e.throwClip;

            grenadeExplosionMap.Clear();
            foreach (var e in grenadeExplosionSounds) grenadeExplosionMap[e.sound] = e.clip;
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

        public AudioClip GetTakeItemSound(ETakeItemSound sound)
        {
            TryGetTakeItemSound(sound, out var clip);
            return clip;
        }

        public AudioClip GetPlayerSound(EPlayerSound sound)
        {
            TryGetPlayerSound(sound, out var clip);
            return clip;
        }

        public bool TryGetSystemSound(ESystemSound sound, out AudioClip clip)
        {
            if (systemMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetSystemSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetMatchSound(EMatchSound sound, out AudioClip clip)
        {
            if (matchMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetMatchSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetEffectSound(ESoundEffect sound, out AudioClip clip)
        {
            if (effectMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetEffectSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetTakeItemSound(ETakeItemSound sound, out AudioClip clip)
        {
            if (takeItemMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetTakeItemSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetPlayerSound(EPlayerSound sound, out AudioClip clip)
        {
            if (playerMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetPlayerSoundPaths(sound));
            return clip != null;
        }

        public bool TryGetWeaponShotSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry)) return false;
            clip = entry.shotClip;
            return clip != null;
        }

        public bool TryGetWeaponReloadSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry)) return false;
            clip = entry.reloadClip;
            return clip != null;
        }

        public bool TryGetWeaponHitSound(EWeaponType weaponType, out AudioClip clip)
        {
            clip = null;
            if (!weaponMap.TryGetValue(weaponType, out var entry)) return false;
            clip = entry.hitClip;
            return clip != null;
        }

        public bool TryGetGrenadeThrowSound(EGrenadeType grenadeType, out AudioClip clip)
        {
            if (grenadeThrowMap.TryGetValue(grenadeType, out clip) && clip != null) return true;
            return false;
        }

        public bool TryGetGrenadeExplosionSound(EGrenadeSound sound, out AudioClip clip)
        {
            if (grenadeExplosionMap.TryGetValue(sound, out clip) && clip != null) return true;
            clip = LoadFirst(GetGrenadeExplosionSoundPaths(sound));
            return clip != null;
        }

        public int PreloadAll()
        {
            RebuildMaps();
            var loadedCount = 0;
            loadedCount += PreloadSystemSounds();
            loadedCount += PreloadMatchSounds();
            loadedCount += PreloadEffectSounds();
            loadedCount += PreloadTakeItemSounds();
            loadedCount += PreloadPlayerSounds();
            loadedCount += PreloadGrenadeExplosionSounds();
            return loadedCount;
        }

        public bool ValidateAllMappings(bool logWarnings = true)
        {
            RebuildMaps();

            var warnings = new List<string>();

            foreach (ESystemSound sound in Enum.GetValues(typeof(ESystemSound)))
            {
                if (!TryGetSystemSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"System sound missing: {sound}");
                }
            }

            foreach (EMatchSound sound in Enum.GetValues(typeof(EMatchSound)))
            {
                if (!TryGetMatchSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Match sound missing: {sound}");
                }
            }

            foreach (ESoundEffect sound in Enum.GetValues(typeof(ESoundEffect)))
            {
                if (!TryGetEffectSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Effect sound missing: {sound}");
                }
            }

            foreach (ETakeItemSound sound in Enum.GetValues(typeof(ETakeItemSound)))
            {
                if (!TryGetTakeItemSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Take item sound missing: {sound}");
                }
            }

            foreach (EPlayerSound sound in Enum.GetValues(typeof(EPlayerSound)))
            {
                if (!TryGetPlayerSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Player sound missing: {sound}");
                }
            }

            foreach (EWeaponType weaponType in Enum.GetValues(typeof(EWeaponType)))
            {
                if (!TryGetWeaponShotSound(weaponType, out var shot) || shot == null)
                {
                    warnings.Add($"Weapon shot sound missing: {weaponType}");
                }

                if (!TryGetWeaponReloadSound(weaponType, out var reload) || reload == null)
                {
                    warnings.Add($"Weapon reload sound missing: {weaponType}");
                }

                if (!TryGetWeaponHitSound(weaponType, out var hit) || hit == null)
                {
                    warnings.Add($"Weapon hit sound missing: {weaponType}");
                }
            }

            foreach (EGrenadeType grenadeType in Enum.GetValues(typeof(EGrenadeType)))
            {
                if (!TryGetGrenadeThrowSound(grenadeType, out var clip) || clip == null)
                {
                    warnings.Add($"Grenade throw sound missing: {grenadeType}");
                }
            }

            foreach (EGrenadeSound sound in Enum.GetValues(typeof(EGrenadeSound)))
            {
                if (!TryGetGrenadeExplosionSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Grenade explosion sound missing: {sound}");
                }
            }

            if (logWarnings)
            {
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[SoundMasterData] {warning}");
                }
            }

            return warnings.Count == 0;
        }

        public bool ValidateCombatMappings(out string report)
        {
            RebuildMaps();

            var sb = new StringBuilder();
            var valid = true;

            foreach (EMatchSound sound in Enum.GetValues(typeof(EMatchSound)))
            {
                if (!TryGetMatchSound(sound, out var clip) || clip == null)
                {
                    valid = false;
                    sb.AppendLine($"Missing match sound: {sound}");
                }
            }

            foreach (EWeaponType weaponType in Enum.GetValues(typeof(EWeaponType)))
            {
                if (!TryGetWeaponShotSound(weaponType, out var shot) || shot == null)
                {
                    valid = false;
                    sb.AppendLine($"Missing weapon shot sound: {weaponType}");
                }

                if (!TryGetWeaponReloadSound(weaponType, out var reload) || reload == null)
                {
                    valid = false;
                    sb.AppendLine($"Missing weapon reload sound: {weaponType}");
                }
            }

            report = sb.ToString();
            if (!valid)
            {
                Debug.LogWarning($"[SoundMasterData] Combat mapping validation failed.\n{report}");
            }

            return valid;
        }

        private static AudioClip LoadFirst(string[] candidates)
        {
            if (candidates == null)
            {
                Debug.LogWarning("[SoundMasterData] LoadFirst received null candidates.");
                return null;
            }

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                var clip = Resources.Load<AudioClip>(path);
                if (clip != null) return clip;
            }

            Debug.LogWarning($"[SoundMasterData] No AudioClip found from {candidates.Length} candidates.");
            return null;
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

        private static string[] GetTakeItemSoundPaths(ETakeItemSound sound)
        {
            switch (sound)
            {
                case ETakeItemSound.TakePowerUpItemSound: return new[] { "Sound/Item/sfx_take_offense" };
                case ETakeItemSound.TakeDefenseUpItemSound: return new[] { "Sound/Item/sfx_take_defence" };
                case ETakeItemSound.TakeSpeedUpItemSound: return new[] { "Sound/Item/sfx_take_speedup" };
                case ETakeItemSound.TakeHealItemSound: return new[] { "Sound/Item/sfx_take_medikit", "Sound/Item/sfx_take_immortal" };
                case ETakeItemSound.TakeRandomItemSound: return new[] { "Sound/Item/sfx_take_weapon", "Sound/Item/sfx_take_grenade" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetPlayerSoundPaths(EPlayerSound sound)
        {
            switch (sound)
            {
                case EPlayerSound.DamageFemale1: return new[] { "Sound/Player/sfx_pl_hit01", "Sound/Player/sfx_pl_deathF01" };
                case EPlayerSound.DamageMale1: return new[] { "Sound/Player/sfx_pl_hit01", "Sound/Player/sfx_pl_deathM01" };
                case EPlayerSound.DeathFemale1: return new[] { "Sound/Player/sfx_pl_deathF01" };
                case EPlayerSound.DeathFemale2: return new[] { "Sound/Player/sfx_pl_deathF02" };
                case EPlayerSound.DeathFemale3: return new[] { "Sound/Player/sfx_pl_deathF03" };
                case EPlayerSound.DeathFemale4: return new[] { "Sound/Player/sfx_pl_deathF04" };
                case EPlayerSound.DeathMale1: return new[] { "Sound/Player/sfx_pl_deathM01" };
                case EPlayerSound.DeathMale2: return new[] { "Sound/Player/sfx_pl_deathM02" };
                case EPlayerSound.DeathMale3: return new[] { "Sound/Player/sfx_pl_deathM03" };
                case EPlayerSound.DeathMale4: return new[] { "Sound/Player/sfx_pl_deathM04" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetGrenadeExplosionSoundPaths(EGrenadeSound sound)
        {
            switch (sound)
            {
                case EGrenadeSound.ExplosionGrenade:
                    return new[] { "Sound/Weapon/_grenade_explode", "Sound/Weapon/_gre_explode", "Sound/Weapon/_grenade_cluster" };
                case EGrenadeSound.ExplosionFireGrenade:
                    return new[] { "Sound/Item/sfx_granade_fire", "Sound/Weapon/_gre_explode" };
                default:
                    return Array.Empty<string>();
            }
        }

        private int PreloadSystemSounds()
        {
            var loadedCount = 0;
            foreach (ESystemSound sound in Enum.GetValues(typeof(ESystemSound)))
            {
                if (systemMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetSystemSoundPaths(sound));
                if (loaded != null) { systemMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }

        private int PreloadMatchSounds()
        {
            var loadedCount = 0;
            foreach (EMatchSound sound in Enum.GetValues(typeof(EMatchSound)))
            {
                if (matchMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetMatchSoundPaths(sound));
                if (loaded != null) { matchMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }

        private int PreloadEffectSounds()
        {
            var loadedCount = 0;
            foreach (ESoundEffect sound in Enum.GetValues(typeof(ESoundEffect)))
            {
                if (effectMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetEffectSoundPaths(sound));
                if (loaded != null) { effectMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }

        private int PreloadTakeItemSounds()
        {
            var loadedCount = 0;
            foreach (ETakeItemSound sound in Enum.GetValues(typeof(ETakeItemSound)))
            {
                if (takeItemMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetTakeItemSoundPaths(sound));
                if (loaded != null) { takeItemMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }

        private int PreloadPlayerSounds()
        {
            var loadedCount = 0;
            foreach (EPlayerSound sound in Enum.GetValues(typeof(EPlayerSound)))
            {
                if (playerMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetPlayerSoundPaths(sound));
                if (loaded != null) { playerMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }

        private int PreloadGrenadeExplosionSounds()
        {
            var loadedCount = 0;
            foreach (EGrenadeSound sound in Enum.GetValues(typeof(EGrenadeSound)))
            {
                if (grenadeExplosionMap.TryGetValue(sound, out var existing) && existing != null) continue;
                var loaded = LoadFirst(GetGrenadeExplosionSoundPaths(sound));
                if (loaded != null) { grenadeExplosionMap[sound] = loaded; loadedCount++; }
            }
            return loadedCount;
        }
    }
}
