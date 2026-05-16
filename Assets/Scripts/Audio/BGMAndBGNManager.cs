

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
        private bool playBGMOnlyIfNotPlaying = false; // 追加
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
            if (bgm)
            {
                if (overridePlayingBGM)
                {
                    SoundManager.Instance.PlayBgm(bgm, bgmVolume, loopBgm);
                }
                else
                {
                    if (!SoundManager.Instance.IsBgmPlaying())
                    {
                        SoundManager.Instance.PlayBgm(bgm, bgmVolume, loopBgm);
                    }
                }
            }
        }

        public void StopBGM()
        {
            if (SoundManager.Instance.IsBgmPlaying())
            {
                SoundManager.Instance.StopBgm();
            }
        }

        public void StopBGMAll()
        {

        }

        public void PlayBGN()
        {
            if (bgn)
            {
                SoundManager.Instance.PlayBgm(bgn, bgnVolume, true);
            }
        }

        public bool IsPlayBGMNow()
        {
            return SoundManager.Instance.IsBgmPlaying();
        }

        [Button("自動セット")]
        public void AutoSet()
        {

        }


    }
}
