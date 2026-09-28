namespace OpenGSCore
{
    public class AbstractStage
    {
        public string StageName { get; protected set; } = string.Empty;
        public EMap Map { get; protected set; } = EMap.Unknown;

        protected AbstractStage()
        {
        }

        protected AbstractStage(string stageName, EMap map)
        {
            StageName = stageName ?? string.Empty;
            Map = map;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(StageName) ? Map.ToString() : $"{StageName}({Map})";
        }
    }
}
