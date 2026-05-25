


namespace OpenGS
{
    public class GameFlagsManager
    {
        private static GameFlagsManager instance = new GameFlagsManager();

        string beforeSceneName = "";

        string beforeStage = "";
        
        private GameFlagsManager()
        {
            beforeSceneName = string.Empty;
            beforeStage = string.Empty;
        }

        public string BeforeSceneName { get => beforeSceneName; set => beforeSceneName = value; }

        public static GameFlagsManager GetInstance()
        {
            return instance;
        }




    }



}
