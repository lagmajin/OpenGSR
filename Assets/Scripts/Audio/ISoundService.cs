using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// ゲーム全体のサウンド再生を抽象化するサービスインターフェース。
    /// 各Enum（ESystemSoundなど）から実際のAudioClipへの解決と、再生の実行を担う。
    /// </summary>
    public interface ISoundService
    {
        // BGM 関連
        void PlayBGM(EMap map);
        void PlayBGM(string bgmName, float fadeTime = -1f);
        void StopBGM(float fadeTime = -1f);

        // システム・ゲーム進行音
        void PlaySystemSound(ESystemSound sound);
        void PlayMatchSound(EMatchSound sound);
        void PlaySoundEffect(ESoundEffect sound, float volume = 1.0f);

        // 戦闘・アクション音
        void PlayWeaponShot(EWeaponType type, float pitch = 1.0f);
        void PlayWeaponReload(EWeaponType type, float pitch = 1.0f);
        void PlayWeaponHit(EWeaponType type, float pitch = 1.0f);
        void PlayGrenadeThrow(EGrenadeType type, float pitch = 1.0f);

        // 汎用再生
        void PlayOneShot(AudioClip clip, float volume = 1.0f, float pitch = 1.0f);

        bool ValidateSoundSetup(bool logWarnings = true);
    }
}
