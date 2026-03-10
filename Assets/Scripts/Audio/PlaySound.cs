using OpenGSR.Audio;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Legacy-compatible audio facade.
    /// Routes old PlaySound calls to SimpleAudioManager.
    /// </summary>
    public static class PlaySound
    {
        public static void PlaySE(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            SimpleAudioManager.Instance.PlaySE(clip, volume, pitch);
        }

        public static void PlaySE(string name, float pitch = 1.0f)
        {
            SimpleAudioManager.Instance.PlaySE(name, pitch);
        }

        public static void PlayBGM(AudioClip clip, float volume = 1.0f, bool loop = true)
        {
            SimpleAudioManager.Instance.PlayBGM(clip, volume, loop);
        }

        public static void PlayBGM(string name, float fadeTime = -1f)
        {
            SimpleAudioManager.Instance.PlayBGM(name, fadeTime);
        }

        public static void StopBGM(float fadeTime = -1f)
        {
            SimpleAudioManager.Instance.StopBGM(fadeTime);
        }

        public static bool IsPlayingBGM()
        {
            return SimpleAudioManager.Instance.IsPlayingBGM();
        }
    }
}
