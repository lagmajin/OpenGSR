namespace OpenGS
{
    /// <summary>
    /// Player type enumeration for distinguishing local player, remote players, and AI
    /// </summary>
    public enum EPlayerType
    {
        Unknown,
        MyPlayer,       // Local player controlled by the user
        OtherPlayer,    // Remote player in the same team
        EnemyPlayer,    // Remote player in the enemy team
        FriendPlayer,   // AI or NPC friend
        AIPlayer        // AI controlled player
    }
}
