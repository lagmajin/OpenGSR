using OpenGSCore;

namespace OpenGS
{
    public sealed class GamePlayerManager : IGamePlayerManager
    {
        public static GamePlayerManager Instance { get; } = new GamePlayerManager();

        public PlayerStatus Status { get; set; } = new PlayerStatus();
        public MatchData MatchData { get; private set; } = new MatchData();

        private EPlayerCharacter _selectedPlayerCharacter = EPlayerCharacter.Misty;

        private GamePlayerManager()
        {
        }

        public EPlayerCharacter SelectedPlayerCharacter()
        {
            return _selectedPlayerCharacter;
        }

        public void SetPlayerCharacter(EPlayerCharacter character = EPlayerCharacter.Misty)
        {
            _selectedPlayerCharacter = character;
        }
    }
}
