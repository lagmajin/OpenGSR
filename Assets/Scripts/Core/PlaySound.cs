


//using GitHub.Unity;
using UnityEngine;
using KanKikuchi.AudioManager;


namespace OpenGS
{
    public class PlaySound
    {
        public void Pitch()
        {

        }

        public static void PlayBGM(AudioClip bgm,float vol=1.0f,float delay=0.0f)
        {
            BGMManager.Instance.Play(bgm,vol,delay);

        }



        public static void StopBGM(string path)
        {
            BGMManager.Instance.Stop(path);
        }

        public static void StopBGMAll()
        {
            BGMManager.Instance.Stop();
        }

        public static void PlayBGN(AudioClip bgn)
        {
            SEManager.Instance.Play(bgn,0.5f,1.0f,1.0f,true);

        }

        public static void PlayBGN(AudioClip bgn,float volume=1.0f)
        {

        }

        public static bool IsPlayingBGM()
        {
            


            return BGMManager.Instance.IsPlaying();
        }


        public static void PlaySE(AudioClip se,float vol=1.0f,float delay=0.0f)
        {
            SEManager.Instance.Play(se,vol,delay);
        }
    }
}
