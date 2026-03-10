


using UnityEngine;
using OpenGSR.Audio;
//using Mono.Cecil;
using OpenGSCore;
//using Unity.VisualScripting;
using UnityEditor;


namespace OpenGS
{

    [DisallowMultipleComponent]
    public class SoundManager
    {
        public static SoundManager Instance { get; } = new();

        private BGNSoundMasterData data;

        private MatchSoundMasterData matchSoundMasterData;
        private AllGrenadeListMasterData allGrenadeMasterData;
        private WeaponListMasterData allWeaponListMasterData;
        private SoundEffectMasterData soundEffectMasterData;
        private SoundManager()
        {
            //Debug.LogError("tested");

            matchSoundMasterData = Resources.Load<MatchSoundMasterData>("MasterData/Sound/MatchSoundMasterData");
            soundEffectMasterData = Resources.Load<SoundEffectMasterData>("MasterData/Sound/SoundEffectMasterData");

            //Debug.LogError(soundEffectMasterData ? "Test" : "Test2");
        }

        public void PlayBGM(EMap map)
        {

        }

        public void PlaySystemSound(ESystemSound sound)
        {
            //SimpleAudioManager.Instance.PlaySE(sound.ToString());
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
            SimpleAudioManager.Instance.PlaySE(soundEffectMasterData.explosionSound, volume);

        }

        public void PlayPlayerSound()
        {

        }

        public void PlayThrowGrenadeSound(EGrenadeType type)
        {

        }

        public void PlayGameSound(EMatchSound sound)
        {
            if (matchSoundMasterData)
            {
                var audioClip = matchSoundMasterData.MatchSoundAudioClip(sound);

                SimpleAudioManager.Instance.PlaySE(audioClip);

            }

        }

    }

}
