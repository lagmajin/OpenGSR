using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    [CreateAssetMenu(menuName = "MasterData/Sound/PlayerGeneralSoundMasterData")]
    public class PlayerGeneralSoundMasterData : ScriptableObject
    {
        [SerializeField] private AudioClip jumpStartSound;
        [SerializeField] private AudioClip jumpEndSound;
        [SerializeField] private AudioClip boosterStartSound;
        [SerializeField] private AudioClip boosterSound;
        [SerializeField] private AudioClip boosterEndSound;
        [SerializeField] private AudioClip takeItemSound;
        [SerializeField] private AudioClip takeGrenadeSound;
        [SerializeField] private AudioClip dropItemSound;
        [SerializeField] private AudioClip openGrenadeSound;
        [SerializeField] private AudioClip throwGrenadeSound;

        private readonly Dictionary<EPlayerGeneralSound, AudioClip> soundMap = new Dictionary<EPlayerGeneralSound, AudioClip>();

        private void OnEnable()
        {
            RebuildMap();
        }

        public void RebuildMap()
        {
            soundMap.Clear();
            soundMap[EPlayerGeneralSound.JumpStart] = jumpStartSound;
            soundMap[EPlayerGeneralSound.JumpEnd] = jumpEndSound;
            soundMap[EPlayerGeneralSound.BoosterStart] = boosterStartSound;
            soundMap[EPlayerGeneralSound.BoosterLoop] = boosterSound;
            soundMap[EPlayerGeneralSound.BoosterEnd] = boosterEndSound;
            soundMap[EPlayerGeneralSound.TakeItem] = takeItemSound;
            soundMap[EPlayerGeneralSound.TakeGrenade] = takeGrenadeSound;
            soundMap[EPlayerGeneralSound.DropItem] = dropItemSound;
            soundMap[EPlayerGeneralSound.OpenGrenade] = openGrenadeSound;
            soundMap[EPlayerGeneralSound.ThrowGrenade] = throwGrenadeSound;
        }

        public AudioClip GetSound(EPlayerGeneralSound sound)
        {
            TryGetSound(sound, out var clip);
            return clip;
        }

        public bool TryGetSound(EPlayerGeneralSound sound, out AudioClip clip)
        {
            if (soundMap.TryGetValue(sound, out clip) && clip != null)
            {
                return true;
            }

            clip = LoadFirst(GetSoundPaths(sound));
            return clip != null;
        }

        public bool ValidateAllMappings(bool logWarnings = true)
        {
            RebuildMap();

            var warnings = new List<string>();
            foreach (EPlayerGeneralSound sound in Enum.GetValues(typeof(EPlayerGeneralSound)))
            {
                if (!TryGetSound(sound, out var clip) || clip == null)
                {
                    warnings.Add($"Player general sound missing: {sound}");
                }
            }

            if (logWarnings)
            {
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[PlayerGeneralSoundMasterData] {warning}");
                }
            }

            return warnings.Count == 0;
        }

        public int PreloadAll()
        {
            RebuildMap();

            var loadedCount = 0;
            foreach (EPlayerGeneralSound sound in Enum.GetValues(typeof(EPlayerGeneralSound)))
            {
                if (soundMap.TryGetValue(sound, out var existing) && existing != null)
                {
                    continue;
                }

                var loaded = LoadFirst(GetSoundPaths(sound));
                if (loaded != null)
                {
                    soundMap[sound] = loaded;
                    loadedCount++;
                }
            }

            return loadedCount;
        }

        private static AudioClip LoadFirst(params string[] candidates)
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

        private static string[] GetSoundPaths(EPlayerGeneralSound sound)
        {
            switch (sound)
            {
                case EPlayerGeneralSound.JumpStart:
                    return new[] { "Sound/sfx_pl_jump_start", "Sound/Player/sfx_pl_jump_start" };
                case EPlayerGeneralSound.JumpEnd:
                    return new[] { "Sound/sfx_pl_jump_end", "Sound/Player/sfx_pl_jump_end" };
                case EPlayerGeneralSound.BoosterStart:
                    return new[] { "Sound/sfx_pl_booster_start" };
                case EPlayerGeneralSound.BoosterLoop:
                    return new[] { "Sound/sfx_pl_booster_loop" };
                case EPlayerGeneralSound.BoosterEnd:
                    return new[] { "Sound/sfx_pl_booster_end" };
                case EPlayerGeneralSound.TakeItem:
                    return new[] { "Sound/Item/sfx_take_weapon", "Sound/Item/sfx_take_grenade" };
                case EPlayerGeneralSound.TakeGrenade:
                    return new[] { "Sound/sfx_take_grenade", "Sound/Item/sfx_take_grenade" };
                case EPlayerGeneralSound.DropItem:
                    return new[] { "Sound/Item/sfx_take_weapon", "Sound/sfx_take_weapon" };
                case EPlayerGeneralSound.OpenGrenade:
                    return new[] { "Sound/Weapon/_grenade_ready", "Sound/sfx_grenade_ready" };
                case EPlayerGeneralSound.ThrowGrenade:
                    return new[] { "Sound/sfx_grenade_throw", "Sound/Weapon/_grenade_throw" };
                default:
                    return Array.Empty<string>();
            }
        }
    }
}
