namespace OpenGSCore
{
    /// <summary>
    /// Shared event names for gameplay and network layers.
    /// </summary>
    public static class MatchEvent
    {
        public const string GameStarted = MessageType.GameStartNotification;
        public const string GameEnded = MessageType.MatchEndNotification;
        public const string PlayerShot = MessageType.PlayerShot;
        public const string PlayerGranadeThrough = "GranadeThrough";
        public const string FieldItemSpawned = MessageType.ItemSpawnNotification;
        public const string FieldItemDisappeared = MessageType.ItemDespawnNotification;
        public const string PlayerDead = MessageType.PlayerDeath;
        public const string PlayerDamage = MessageType.PlayerDamage;
        public const string PlayerRespawn = "PlayerRespawn";
        public const string PlayerJoined = "PlayerJoined";
        public const string PlayerLeft = "PlayerLeft";
        public const string PlayerTeamSwitch = "PlayerTeamSwitch";
        public const string PlayerReload = "PlayerReload";
        public const string PlayerSpectating = "PlayerSpectating";
        public const string VoteStart = "VoteStart";
        public const string VotePassed = "VotePassed";
        public const string VoteFailed = "VoteFailed";
        public const string BuffExpired = "BuffExpired";
    }
}
