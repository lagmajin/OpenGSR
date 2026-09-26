

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenGS
{


    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)]
    public class BGMAndBGNManager : MonoBehaviour, IBGMAndBGNManager
    {
        [SerializeField]
        private AudioClip bgm;


        [SerializeField]
        private AudioClip bgn;

        [SerializeField]
        private bool loopBgm = false;

        [SerializeField]
        private float bgmVolume = 1.0f;

        [SerializeField]
        private float bgnVolume = 1.0f;



        [SerializeField][BoxGroup("Setting")]
        private bool playBGMWhenStart = true;

        [SerializeField][BoxGroup("Setting")]
        private bool playBGNWhenStart = false;
        [SerializeField][BoxGroup("Setting")]
        private bool overridePlayingBGM = true;







        [SerializeField]
        [BoxGroup("Setting")]
        private bool playBGMOnlyIfNotPlaying = false;

        [SerializeField] [Required] private SystemSoundMasterData masterdata;
        //[SerializeField][Required]private Map



        void Start()
        {
            if (playBGMWhenStart)
            {
                PlayBGM();

            }

            if (playBGNWhenStart)
            {
                PlayBGN();
            }



        }

        public void PlayBGM()
        {
            var soundManager = SoundManager.Instance;
            if (bgm && soundManager != null)
            {
                if (playBGMOnlyIfNotPlaying && soundManager.IsBgmPlaying())
                {
                    return;
                }

                if (overridePlayingBGM)
                {
                    soundManager.PlayBgm(bgm, bgmVolume, loopBgm);
                }
                else
                {
                    if (!soundManager.IsBgmPlaying())
                    {
                        soundManager.PlayBgm(bgm, bgmVolume, loopBgm);
                    }
                }
            }
        }

        public void StopBGM()
        {
            var soundManager = SoundManager.Instance;
            if (soundManager != null && soundManager.IsBgmPlaying())
            {
                soundManager.StopBgm();
            }
        }

        public void StopBGMAll()
        {
            var soundManager = SoundManager.Instance;
            if (soundManager != null && soundManager.IsBgmPlaying())
            {
                soundManager.StopBgm();
            }
        }

        public void PlayBGN()
        {
            var soundManager = SoundManager.Instance;
            if (bgn && soundManager != null)
            {
                soundManager.PlayBgm(bgn, bgnVolume, true);
            }
        }

        public bool IsPlayBGMNow()
        {
            return SoundManager.Instance != null && SoundManager.Instance.IsBgmPlaying();
        }

        [Button("自動セット")]
        public void AutoSet()
        {
            if (masterdata == null)
            {
                Debug.LogWarning("[BGMAndBGNManager] SystemSoundMasterData is not assigned. Assign the ScriptableObject in the inspector.");
            }
        }


    }
}
