using UnityEngine;

namespace OpenGS
{
   public  interface ISetting
    {
        public void ToJson() ;
    }

    public class GameLightAndShadowSetting
    {
        public bool isOn = false;

        public bool onDropShadow = false;

    }

    public class GamePostProcessingSetting
    {
        public bool isOn = false;

        public bool isOnMotionBluer = false;

        public bool isOnLOD = false;

    }

    public class GameGraphicsSetting
    {
        GameGraphicsSetting()
        {

        }

    }

    public class GameSoundSetting
    {
        public float masterVolume = 1.0f;
        public float bgmVolume = 1.0f;
        public float seVolume = 1.0f;

        public bool isOnReverb = false;

        public void EnableReverb()
        {

        }
        public void DisableReverb()
        {

        }
    }



    public class GameSetting
    {
        private static GameSetting c1 = new GameSetting();

        private bool isOnline = false;

        public bool IsOnline { get => isOnline; set => isOnline = value; }

        public static GameSetting GetInstance()
        {

            return c1;
        }

    }
}
