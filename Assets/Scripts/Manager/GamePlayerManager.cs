using OpenGSCore;

namespace OpenGS
{
    public sealed class GamePlayerManager : IGamePlayerManager
    {
        public static GamePlayerManager Instance { get; } = new GamePlayerManager();

        public MatchData MatchData { get; private set; } = new MatchData();

        private EPlayerCharacter _selectedPlayerCharacter = EPlayerCharacter.Ami;

        private GamePlayerManager()
        {
        }

        public EPlayerCharacter SelectedPlayerCharacter()
        {
            return _selectedPlayerCharacter;
        }

        public void SetPlayerCharacter(EPlayerCharacter character = EPlayerCharacter.Ami)
        {
            _selectedPlayerCharacter = character;
        }
    }
}
