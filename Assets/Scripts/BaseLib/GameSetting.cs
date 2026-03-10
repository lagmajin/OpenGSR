using UnityEngine;

namespace OpenGS
{
   public  interface ISetting
    {
        public void ToJson() ;
    }

    public class GameLightAndShadowSetting
    {
        bool isOn = false;

        bool onDropShadow = false;

    }

    public class GamePostProcessingSetting
    {
        bool isOn = false;

        bool isOnMotionBluer = false;

        bool isOnLOD = false;
        
    }

    public class GameGraphicsSetting
    {
        GameGraphicsSetting()
        {

        }

    }

    public class GameSoundSetting
    {
        float masterVolume = 1.0f;
        float bgmVolume = 1.0f;
        float seVolume = 1.0f;

        bool isOnReverb = false;

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
