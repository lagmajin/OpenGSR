
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

        }

        public void PlaySystemSound(ESystemSound sound)
        {
            var clip = GetSystemSoundClip(sound);
            if (clip != null)
            {
                SimpleAudioManager.Instance.PlaySE(clip);
            }
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
            if (clip != null)
            {
                SimpleAudioManager.Instance.PlaySE(clip, volume);
            }
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
            if (clip != null)
            {
                SimpleAudioManager.Instance.PlaySE(clip);
            }
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

    }

}
