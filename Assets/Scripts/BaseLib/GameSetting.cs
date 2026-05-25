using UnityEngine;

namespace OpenGS
{
   public  interface ISetting
    {
        public void ToJson() ;
    }

    public class GameLightAndShadowSetting
    {
    }

    public class GamePostProcessingSetting
    {
    }

    public class GameGraphicsSetting
    {
        GameGraphicsSetting()
        {

        }

    }

    public class GameSoundSetting
    {
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
