using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGSR.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "OpenGSR/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Serializable]
        public class AudioItem
        {
            public string Name;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        [Header("BGM List")]
        public List<AudioItem> BGMList = new List<AudioItem>();

        [Header("SE List")]
        public List<AudioItem> SEList = new List<AudioItem>();

        private void OnValidate()
        {
            Sanitize(BGMList);
            Sanitize(SEList);
        }

        private static void Sanitize(List<AudioItem> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                if (item == null) continue;
                if (!float.IsFinite(item.Volume)) item.Volume = 1f;
                item.Volume = Mathf.Clamp01(item.Volume);
            }
        }
    }
}
