

using OpenGSCore;


namespace OpenGS
{


    public interface IStage
    {

    }
    public class Stage
    {
        private string mapName;
        OpenGSCore.EGameMode mode;
        public string MapName { get => mapName; set => mapName = value; }
        public OpenGSCore.EGameMode Mode { get => mode; set => mode = value; }
    }
}
