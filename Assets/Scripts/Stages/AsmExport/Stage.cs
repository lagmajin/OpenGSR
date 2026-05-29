
using OpenGSCore;
using Newtonsoft.Json.Linq;


namespace OpenGS
{


    public interface IStage
    {

    }
    public class Stage
    {
        private string mapName;
        OpenGSCore.EGameMode mode;
        public GameScene Scene { get; } = new GameScene();
        public string MapName { get => mapName; set => mapName = value; }
        public OpenGSCore.EGameMode Mode { get => mode; set => mode = value; }

        public void ApplySnapshot(JObject snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            Scene.ApplySnapshot(snapshot);
        }
    }
}
