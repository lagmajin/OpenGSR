namespace OpenGSCore
{
    public static class NetworkingConstants
    {
        public static class MessageType
        {
            public const string Heartbeat = "Heartbeat";
            public const string PlayerPosition = "PlayerPosition";
            public const string PlayerAction = "PlayerAction";
            public const string PlayerShot = "PlayerShot";
            public const string PlayerReload = "PlayerReload";
            public const string PlayerGrenade = "PlayerGrenade";
            public const string GrenadeState = "GrenadeState";
            public const string GameEvent = "GameEvent";
            public const string Chat = "Chat";
            public const string BroadcastMessage = "BroadcastMessage";
            public const string AdminLoginRequest = "AdminLoginRequest";
            public const string AdminLoginResponse = "AdminLoginResponse";
            public const string AdminLogoutRequest = "AdminLogoutRequest";
            public const string AdminLogoutResponse = "AdminLogoutResponse";
            public const string ServerStatusRequest = "ServerStatusRequest";
            public const string ServerStatusResponse = "ServerStatusResponse";
            public const string GetConnectedUsersRequest = "GetConnectedUsersRequest";
            public const string ConnectedUsersResponse = "ConnectedUsersResponse";
            public const string KickPlayerRequest = "KickPlayerRequest";
            public const string KickPlayerResponse = "KickPlayerResponse";
            public const string BroadcastMessageRequest = "BroadcastMessageRequest";
            public const string BroadcastMessageResponse = "BroadcastMessageResponse";
            public const string ShutdownServerRequest = "ShutdownServerRequest";
            public const string ShutdownResponse = "ShutdownResponse";
            public const string ErrorResponse = "ErrorResponse";
            public const string ConnectManagementServerSucceeded = "ConnectManagementServerSucceeded";
            public const string Ping = "Ping";
            public const string ServerStats = "ServerStats";
        }

        public static class ActionType
        {
            public const string Jump = "Jump";
            public const string Crouch = "Crouch";
            public const string Prone = "Prone";
            public const string Interaction = "Interaction";
        }
    }
}
