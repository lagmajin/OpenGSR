
using UnityEngine;
using OpenGSR.Audio;
using OpenGSCore;

namespace OpenGS
{

    [DisallowMultipleComponent]
    public class SoundManager
    {
        public static SoundManager Instance { get; } = new();

        private readonly SoundMasterData soundMasterData;

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

        }

        public void PlayReloadSound(EWeaponType type)
        {

        }

        public void PlayHitSound(EWeaponType type)
        {


        }

        public void PlaySoundEffect(ESoundEffect type, float volume = 1.0f)
        {
            var clip = GetEffectSoundClip(type);
            PlayOneShotSafe(clip, volume, 1.0f, $"Effect:{type}");
        }

        public void PlayPlayerSound()
        {

        }

        public void PlayThrowGrenadeSound(EGrenadeType type)
        {

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

    }

}
