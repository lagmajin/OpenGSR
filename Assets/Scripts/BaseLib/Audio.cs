using UnityEngine;

namespace OpenGS
{
    class Audio
    {
        private const float Step = 0.1f;

        static public void volumePlus()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + Step);
        }

        static public void volumeMinus()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume - Step);
        }
    }
}
