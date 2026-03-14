using UnityEngine;
using OpenGSR.Audio;
using OpenGSCore;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// 旧来の SoundManager。
    /// 現在はリファクタリング中であり、内部的には ISoundService に処理を委譲する。
    /// 将来的にはこのクラスの静的な使用を避け、ISoundService を直接 [Inject] することを推奨。
    /// </summary>
    public class SoundManager
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SoundManager();
                }
                return _instance;
            }
        }

        private ISoundService _service;

        private SoundManager()
        {
            // デフォルトで Offline/Local 用の Service を生成（後で Zenject 等で差し替え可能にする）
            _service = new SoundService(SoundMasterData.Instance());
        }

        /// <summary>
        /// 外部（Installer等）から Service を差し替えるためのメソッド
        /// </summary>
        public void SetService(ISoundService service)
        {
            _service = service;
        }

        public void PlayBGM(EMap map) => _service.PlayBGM(map);
        public void PlayBGM(string bgmName, float fadeTime = -1f) => _service.PlayBGM(bgmName, fadeTime);
        public void StopBgm(float fadeTime = -1f) => _service.StopBGM(fadeTime);

        public void PlaySystemSound(ESystemSound sound) => _service.PlaySystemSound(sound);
        public void PlayMatchSound(EMatchSound sound) => _service.PlayMatchSound(sound);
        public void PlaySoundEffect(ESoundEffect sound, float volume = 1.0f) => _service.PlaySoundEffect(sound, volume);
        
        public void PlayShotSound(EWeaponType type) => _service.PlayWeaponShot(type);
        public void PlayReloadSound(EWeaponType type) => _service.PlayWeaponReload(type);
        public void PlayHitSound(EWeaponType type) => _service.PlayWeaponHit(type);
        public void PlayThrowGrenadeSound(EGrenadeType type) => _service.PlayGrenadeThrow(type);

        public bool ValidateSoundSetup(bool logWarnings = true) => _service.ValidateSoundSetup(logWarnings);

        // 互換性のためのメソッド
        public void PlayGameSound(EMatchSound sound) => PlayMatchSound(sound);
        public void PlayPlayerSound() { /* 未実装 */ }

        public bool PlayOneShotSafe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, string context = null, bool warnIfMissing = false)
        {
            if (clip == null)
            {
                if (warnIfMissing) Debug.LogWarning($"[SoundManager] Clip is null. {context}");
                return false;
            }
            _service.PlayOneShot(clip, volume, pitch);
            return true;
        }

        // BGM 互換メソッド
        public bool PlayBgm(AudioClip clip, float volume = 1.0f, bool loop = true)
        {
            if (clip == null) return false;
            SimpleAudioManager.Instance.PlayBGM(clip, volume, loop);
            return true;
        }

        public bool PlayBgm(string bgmName, float fadeTime = -1f)
        {
            _service.PlayBGM(bgmName, fadeTime);
            return true;
        }

        public bool PlayBgmByName(string bgmName, float fadeTime = -1f, bool warnIfMissing = false)
        {
            _service.PlayBGM(bgmName, fadeTime);
            return true;
        }

        public void CrossFadeBgm(string bgmName, float fadeTime = -1f) => _service.PlayBGM(bgmName, fadeTime);
        public bool IsBgmPlaying() => SimpleAudioManager.Instance.IsPlayingBGM();
    }
}
