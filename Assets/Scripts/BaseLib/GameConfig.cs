

namespace OpenGS
{
    public sealed class GameConfig
    {
        private static GameConfig c1 = new GameConfig();

        float masterVolume = 1.0f;
        float bgmVolume = 1.0f;
        float seVolume = 1.0f;

        public float BgmVolume { get => bgmVolume; set => bgmVolume = value; }
        public float SeVolume { get => seVolume; set => seVolume = value; }

        private GameConfig()
        {


        }

        public void MuteBGM()
        {
            bgmVolume = 0.0f;

        }

        public void MuteSE()
        {
            seVolume = 0.0f;
        }


        public static GameConfig GetInstance()
        {

            return c1;
        }

      


    }


}
