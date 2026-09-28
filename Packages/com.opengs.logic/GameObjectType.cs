namespace OpenGSCore
{
    public class GameObjectType
    {
        public eGameObjectType Type { get; set; } = eGameObjectType.Character;
        public string Name { get; set; } = string.Empty;

        public GameObjectType()
        {
        }

        public GameObjectType(eGameObjectType type, string name = "")
        {
            Type = type;
            Name = name ?? string.Empty;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? Type.ToString() : $"{Type}:{Name}";
        }
    }
}
