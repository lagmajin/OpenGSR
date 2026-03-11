using System;
using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] private List<SystemSoundEntry> systemSounds = new List<SystemSoundEntry>();
        [SerializeField] private List<MatchSoundEntry> matchSounds = new List<MatchSoundEntry>();
        [SerializeField] private List<EffectSoundEntry> effectSounds = new List<EffectSoundEntry>();

        private static SoundMasterData instance;

        private readonly Dictionary<ESystemSound, AudioClip> systemMap = new Dictionary<ESystemSound, AudioClip>();
        private readonly Dictionary<EMatchSound, AudioClip> matchMap = new Dictionary<EMatchSound, AudioClip>();
        private readonly Dictionary<ESoundEffect, AudioClip> effectMap = new Dictionary<ESoundEffect, AudioClip>();

        public static SoundMasterData Instance()
        {
            if (instance == null)
            {
                instance = Resources.Load<SoundMasterData>("MasterData/Sound/SoundMasterData");
                if (instance == null)
                {
                    instance = CreateInstance<SoundMasterData>();
                    Debug.LogWarning("SoundMasterData asset not found at Resources/MasterData/Sound/SoundMasterData. Fallback path lookup will be used.");
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

        private static string[] GetSystemSoundPaths(ESystemSound sound)
        {
            switch (sound)
            {
                case ESystemSound.Click: return new[] { "Sound/sfx_UI_btn_click", "Sound/WaitRoom/ButtonClick" };
                case ESystemSound.Error: return new[] { "Sound/Common/Error" };
                case ESystemSound.Check: return new[] { "Sound/Common/ButtonCheck", "Sound/WaitRoom/Check" };
                case ESystemSound.EnterLobby: return new[] { "Sound/WaitRoom/EnterLobby" };
                case ESystemSound.Popup: return new[] { "Sound/WaitRoom/Popup" };
                case ESystemSound.Fanfare: return new[] { "Sound/Game/sfx_game_win_fanfare", "Sound/sfx_game_win" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetMatchSoundPaths(EMatchSound sound)
        {
            switch (sound)
            {
                case EMatchSound.GameStartVoice: return new[] { "Sound/Game/voice_game_start" };
                case EMatchSound.YouWon: return new[] { "Sound/Game/sfx_game_win_fanfare", "Sound/voice_game_bscore" };
                case EMatchSound.YouLost: return new[] { "Sound/Game/sfx_game_lose", "Sound/voice_game_lose" };
                case EMatchSound.RedTeamFlagCaptured: return new[] { "Sound/Game/CTF/voice_game_rcapture", "Sound/voice_game_rcapture" };
                case EMatchSound.BlueTeamFlagCaptured: return new[] { "Sound/Game/CTF/voice_game_bcapture", "Sound/voice_game_bcapture" };
                case EMatchSound.FlagLost: return new[] { "Sound/Game/CTF/sfx_ctf_lost" };
                case EMatchSound.RedTeamFlagReturn: return new[] { "Sound/Game/CTF/voice_game_rreturn", "Sound/voice_game_rreturn" };
                case EMatchSound.BlueTeamFlagReturn: return new[] { "Sound/Game/CTF/voice_game_breturn", "Sound/voice_game_breturn" };
                default: return Array.Empty<string>();
            }
        }

        private static string[] GetEffectSoundPaths(ESoundEffect sound)
        {
            switch (sound)
            {
                case ESoundEffect.Explosion: return new[] { "Sound/Weapon/_grenade_explode", "Sound/sfx_granade_cluster" };
                case ESoundEffect.HitStageObject: return new[] { "Sound/bullet_bound", "Sound/Weapon/sfx_ric01" };
                default: return Array.Empty<string>();
            }
        }
    }
}
