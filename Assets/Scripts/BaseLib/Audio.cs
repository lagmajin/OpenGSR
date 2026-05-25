using UnityEngine;

namespace OpenGS
{
    public static class Audio
    {
        private const float Step = 0.1f;

        public static float Volume => AudioListener.volume;

        public static void VolumePlus()
        {
            SetVolume(AudioListener.volume + Step);
        }

        public static void volumePlus()
        {
            VolumePlus();
        }

        public static void VolumeMinus()
        {
            SetVolume(AudioListener.volume - Step);
        }

        public static void volumeMinus()
        {
            VolumeMinus();
        }

        public static void SetVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public static void SetMuted(bool muted)
        {
            AudioListener.pause = muted;
        }

        public static void ToggleMute()
        {
            SetMuted(!AudioListener.pause);
        }
    }
}
