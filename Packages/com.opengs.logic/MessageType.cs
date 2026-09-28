namespace OpenGSCore
{
    /// <summary>
    /// サーバー・クライアント間の通信で使用するメッセージタイプの共通定義
    /// </summary>
    public static class MessageType
    {
        // --- システム・認証関連 (TCP) ---
        public const string LoginRequest = "LoginRequest";
        public const string LoginResponse = "LoginResponse";
        public const string LoginSuccessful = LoginResponse;
        public const string CreateAccountRequest = "CreateAccountRequest";
        public const string CreateAccountResponse = "CreateAccountResponse";
        public const string LogoutRequest = "LogoutRequest";
        public const string LogoutSuccessful = "LogoutSuccessful";
        public const string Heartbeat = "Heartbeat";
        public const string ErrorNotification = "ErrorNotification";
        public const string ConnectServerSuccessful = "ConnectServerSuccessful";
        public const string EncryptKey = "EncryptKey";

        // --- ロビー・ルーム管理関連 (TCP) ---
        public const string CreateRoomRequest = "CreateRoomRequest";
        public const string CreateRoomResponse = "CreateRoomResponse";
        public const string CreateNewWaitRoomRequest = CreateRoomRequest;
        public const string CreateNewWaitRoomResponse = CreateRoomResponse;
        public const string JoinRoomRequest = "JoinRoomRequest";
        public const string JoinRoomResponse = "JoinRoomResponse";
        public const string EnterWaitRoomRequest = JoinRoomRequest;
        public const string EnterWaitRoomResponse = JoinRoomResponse;
        public const string LeaveRoomRequest = "LeaveRoomRequest";
        public const string LeaveRoomResponse = "LeaveRoomResponse";
        public const string LeaveWaitRoomRequest = LeaveRoomRequest;
        public const string LeaveWaitRoomResponse = LeaveRoomResponse;
        public const string RoomListUpdateRequest = "RoomListUpdateRequest";
        public const string RoomListUpdateNotification = "RoomListUpdateNotification";
        public const string UpdateRoomRequest = RoomListUpdateRequest;
        public const string UpdateRoomResponse = RoomListUpdateNotification;
        public const string RoomCreated = "RoomCreated";
        public const string RoomDeleted = "RoomDeleted";
        public const string RoomFull = "RoomFull";
        public const string RoomNotFound = "RoomNotFound";
        public const string RoomSettingChanged = "RoomSettingChanged";
        public const string LobbyChatRequest = "LobbyChatRequest";
        public const string LobbyChatNotification = "LobbyChatNotification";
        public const string AddLobbyChat = LobbyChatRequest;
        public const string LobbyEnter = "LobbyEnter";
        public const string LobbyLeave = "LobbyLeave";
        public const string LobbyPlayerList = "LobbyPlayerList";
        public const string LobbyChat = LobbyChatRequest;
        public const string InvalidRoomId = "InvalidRoomId";
        public const string CreateNewWaitRoomSuccess = "CreateNewWaitRoomSuccess";
        public const string UpdateRoomResult = "UpdateRoomResult";
        public const string LobbyInfo = "LobbyInfo";
        public const string LobbyInfoResponse = "LobbyInfoResponse";

        // --- マッチメイキング・準備関連 (TCP) ---
        public const string MatchServerInfoRequest = "MatchServerInfoRequest";
        public const string MatchServerInfoResponse = "MatchServerInfoResponse";
        public const string PlayerReadyRequest = "PlayerReadyRequest";
        public const string PlayerReadyNotification = "PlayerReadyNotification";
        public const string PlayerUnready = "PlayerUnready";
        public const string GameStartRequest = "GameStartRequest";
        public const string PlayerReady = PlayerReadyRequest;
        public const string GameStartNotification = "GameStartNotification";
        public const string ItemSpawnNotification = "ItemSpawnNotification";
        public const string ItemDespawnNotification = "ItemDespawnNotification";
        public const string ClientLoadingSceneEntered = "ClientLoadingSceneEntered";
        public const string LoadingStarted = "LoadingStarted";
        public const string LoadingProgress = "LoadingProgress";
        public const string LoadingCompleted = "LoadingCompleted";
        public const string LoadingStartedNotification = "LoadingStartedNotification";
        public const string LoadingProgressNotification = "LoadingProgressNotification";
        public const string LoadingCompletedNotification = "LoadingCompletedNotification";
        public const string LoadingFailed = "LoadingFailed";
        public const string LoadingMessage = "LoadingMessage";
        public const string AllowEnterMap = "AllowEnterMap";
        public const string SceneTransitionRequest = "SceneTransitionRequest";
        public const string SceneTransitionResponse = "SceneTransitionResponse";
        public const string WaitRoomEnter = JoinRoomRequest;
        public const string WaitRoomLeave = LeaveRoomRequest;
        public const string WaitRoomPlayerList = "WaitRoomPlayerList";
        public const string WaitRoomChat = LobbyChatRequest;
        public const string WaitRoomPlayerReady = PlayerReadyRequest;
        public const string WaitRoomPlayerUnready = PlayerUnready;
        public const string WaitRoomSettingsChange = "WaitRoomSettingsChange";
        public const string WaitRoomKickPlayer = "WaitRoomKickPlayer";
        public const string WaitRoomOwnerChange = "WaitRoomOwnerChange";
        public const string WaitRoomStartCountdown = "WaitRoomStartCountdown";
        public const string WaitRoomCancelCountdown = "WaitRoomCancelCountdown";
        public const string WaitRoomUpdateNotification = "WaitRoomUpdateNotification";

        // --- リアルタイムゲームプレイ関連 (UDP/RUDP) ---
        public const string WelcomeMessage = "WelcomeMessage";
        public const string PlayerSpawned = "PlayerSpawned";
        public const string PlayerPositionUpdate = "PlayerPositionUpdate";
        public const string PlayerShot = "PlayerShot";

        // What a client claims about itself. The server decides neither, so a
        // claim arriving under either name is refused rather than applied.
        public const string PlayerDamage = "PlayerDamage";
        public const string PlayerDeath = "PlayerDeath";

        // What the server ruled. These are the same two facts told by the
        // authority rather than asserted by a client, so they are named here
        // next to the claims: the two used to exist only as a literal on the
        // server and a literal on the client, and the two literals did not
        // match, so the ruling reached nobody and the client's own prediction
        // of its health and its death stood.
        public const string PlayerDamaged = "PlayerDamaged";
        public const string PlayerKilled = "PlayerKilled";

        // A weapon lying on the ground, the claim on it, and the ruling on that
        // claim. The claim and the ruling were the same string on both sides
        // once, which meant a client could not tell the server's answer from
        // its own request, so the answer either looked like a second request or
        // was matched by a label the server never sent. They are told apart by
        // the trailing word, the same way damage and death are.
        public const string WeaponDropped = "WeaponDropped";
        public const string WeaponReserved = "WeaponReserved";
        public const string WeaponReleased = "WeaponReleased";

        // Spending a carried instant item, and what the server decided it was
        // worth. The client used to read the ruling under the name it sent the
        // request with, so it could not tell its own request from the answer.
        public const string ItemUsed = "ItemUsed";
        public const string ItemUseRefused = "ItemUseRefused";

        public const string PlayerPose = "PlayerPose";
        public const string GameStateSync = "GameStateSync";
        public const string MatchEndNotification = "MatchEndNotification";
        public const string MatchResult = MatchEndNotification;

        // --- 即席アイテム・武器の主張と裁定 ---
        // A claim is what a client asserts and a ruling is what the server
        // decided, so the two are named apart. They used to share a name on each
        // side, which meant a client could not tell its own request from the
        // server's answer, so an answer either looked like a second request or
        // was matched by a label the server never sent.
        public const string WeaponReserve = "WeaponReserve";    // 主張
        public const string WeaponRelease = "WeaponRelease";    // 主張の解除
        public const string ItemUse = "ItemUse";                // 使用の要求

        // --- フィールドアイテム (UDP/RUDP) ---
        // A field item claim and the ruling on it are the same fact on the wire,
        // so they share one name. The client used to claim with "ItemPickup" and
        // read the ruling under that name too, while the server ruled with
        // "FieldItemPickup", so a client that applied a pickup optimistically
        // never learned whether the server had granted it: the ruling fell into
        // the unhandled branch and only the local timeout took the buff back.
        public const string FieldItemPickup = "FieldItemPickup";
        public const string ItemPickup = FieldItemPickup;

        // A field item appearing and disappearing, plus the whole set. The server
        // sent ItemSpawnNotification and ItemDespawnNotification while the client
        // dispatched on FieldItemSpawn, FieldItemDespawn and ItemSpawn, so a spawn
        // reached a client under none of the names it was looking for and every
        // item on the map was a thing only the client that had spawned it could
        // see. The names below are what the server sends; the client's other
        // spellings resolve to them.
        public const string FieldItemStateSync = "FieldItemStateSync";
        public const string FieldItemSpawn = ItemSpawnNotification;
        public const string FieldItemDespawn = ItemDespawnNotification;
        public const string FieldItemSpawnBatch = "FieldItemSpawnBatch";

        // A question about the state of the match, and the answer to it. The
        // answer had no reader on the client, so a client that asked was told
        // nothing even where the question was answered.
        public const string MatchStatus = "MatchStatus";
        public const string MatchStatusRequest = "MatchStatusRequest";

        // The realtime keepalive pair. The server answered a ping with Pong,
        // which is a name no client reads, while the client answered a
        // PingRequest with a PingResponse that the server does accept.
        public const string PingRequest = "PingRequest";
        public const string PingResponse = "PingResponse";

        // The room state the server publishes, and the answer to a client that
        // has just been admitted to the realtime channel. A client read both and
        // the server sent neither: the room state went out under a comment and
        // the join was only ever confirmed on the lobby stream, so a client that
        // had just connected knew nothing about the room it was in.
        public const string Snapshot = "Snapshot";
        public const string MatchJoined = "MatchJoined";

        // A flag being destroyed. The client used to claim this about itself and
        // to wait for the same name back, so a flag that the server destroyed was
        // a flag nobody was told about. The server decides when a flag is gone,
        // because a flag going is a rule outcome rather than something a client
        // can assert: the standing flag exists for whoever is not carrying it.
        public const string FlagBurst = "FlagBurst";

        // A delivery that did not score. The client used to score the match
        // itself and push the number, and the server recomputed it from state it
        // never had, so an online capture never counted. The server is now the
        // only side that scores, and a refused delivery is answered rather than
        // logged, so the player who walked the flag across is told.
        public const string FlagCaptureRefused = "FlagCaptureRefused";

        // The flag transitions a client reports. The client raised these as local
        // events and sent nothing, so the server never knew a flag had moved and
        // every capture it was asked to judge was refused for want of a carrier.
        public const string FlagCaptured = "FlagCaptured";
        public const string FlagLost = "FlagLost";
        public const string FlagPickup = "FlagPickup";
        public const string FlagReturn = "FlagReturn";
        public const string FlagScoreUpdate = "FlagScoreUpdate";
        
        // プレイヤー情報取得関連
        public const string PlayerInfoRequest = "PlayerInfoRequest";
        public const string PlayerInfoResponse = "PlayerInfoResponse";
        public const string PlayerInfo = PlayerInfoRequest;

        // ショップ関連
        public const string ShopStateRequest = "ShopStateRequest";
        public const string ShopStateResponse = "ShopStateResponse";
        public const string ShopPurchaseRequest = "ShopPurchaseRequest";
        public const string ShopPurchaseResponse = "ShopPurchaseResponse";
        public const string ShopEquipRequest = "ShopEquipRequest";
        public const string ShopEquipResponse = "ShopEquipResponse";
        public const string ShopUnequipRequest = "ShopUnequipRequest";
        public const string PlayerEquipInfo = "PlayerEquipInfo";

        // フレンド関連 (TCP)
        public const string FriendRequest = "FriendRequest";
        public const string FriendRequestResponse = "FriendRequestResponse";
        public const string FriendRequestNotification = "FriendRequestNotification";
        public const string FriendApproveRequest = "FriendApproveRequest";
        public const string FriendApproveResponse = "FriendApproveResponse";
        public const string FriendListRequest = "FriendListRequest";
        public const string FriendListResponse = "FriendListResponse";

        // ギルド関連 (TCP)
        public const string GuildListRequest = "GuildListRequest";
        public const string GuildListResponse = "GuildListResponse";
        public const string GuildInfoRequest = "GuildInfoRequest";
        public const string GuildInfoResponse = "GuildInfoResponse";
        public const string GuildCreateRequest = "GuildCreateRequest";
        public const string GuildCreateResponse = "GuildCreateResponse";
        public const string GuildJoinRequest = "GuildJoinRequest";
        public const string GuildJoinResponse = "GuildJoinResponse";
        public const string GuildLeaveRequest = "GuildLeaveRequest";
        public const string GuildLeaveResponse = "GuildLeaveResponse";
        public const string GuildInviteRequest = "GuildInviteRequest";
        public const string GuildInviteResponse = "GuildInviteResponse";
        public const string GuildInviteNotification = "GuildInviteNotification";
        public const string GuildKickRequest = "GuildKickRequest";
        public const string GuildKickResponse = "GuildKickResponse";
        public const string GuildKickNotification = "GuildKickNotification";
        public const string GuildRoleRequest = "GuildRoleRequest";
        public const string GuildRoleResponse = "GuildRoleResponse";
        public const string GuildChatRequest = "GuildChatRequest";
        public const string GuildChatNotification = "GuildChatNotification";
        public const string DailyListRequest = "DailyListRequest";
        public const string DailyListResponse = "DailyListResponse";
        public const string DailyProgressRequest = "DailyProgressRequest";
        public const string DailyProgressResponse = "DailyProgressResponse";
        public const string DailyClaimRequest = "DailyClaimRequest";
        public const string DailyClaimResponse = "DailyClaimResponse";
        
        // --- Mission Route (TCP) ---
        public const string MissionStartRequest = "MissionStartRequest";
        public const string MissionStartNotification = "MissionStartNotification";
        public const string MissionCompleteNotification = "MissionCompleteNotification";
        public const string MissionFailedNotification = "MissionFailedNotification";

        // 旧互換用 (移行期間)
        public const string Notification = "Notification";

        public static string Normalize(string messageType)
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return messageType;
            }

            return messageType switch
            {
                CreateNewWaitRoomRequest => CreateRoomRequest,
                CreateNewWaitRoomResponse => CreateRoomResponse,
                UpdateRoomRequest => RoomListUpdateRequest,
                EnterWaitRoomRequest => JoinRoomRequest,
                EnterWaitRoomResponse => JoinRoomResponse,
                LeaveWaitRoomRequest => LeaveRoomRequest,
                LeaveWaitRoomResponse => LeaveRoomResponse,
                AddLobbyChat => LobbyChatRequest,
                LoginSuccessful => LoginResponse,
                "LogoutSuccess" => LogoutSuccessful,
                "SendEnterRoom" => JoinRoomRequest,
                "EquipRequest" => ShopEquipRequest,
                "CreateNewWaitRoomRequest" => CreateRoomRequest,
                "CreateNewWaitRoomResponse" => CreateRoomResponse,
                "UpdateRoomResponse" => RoomListUpdateNotification,
                "EnterWaitRoomResponse" => JoinRoomResponse,
                "LeaveWaitRoomResponse" => LeaveRoomResponse,
                "LobbyEnterRequest" => LobbyEnter,
                "LobbyLeaveRequest" => LobbyLeave,
                "WaitRoomEnterRequest" => WaitRoomEnter,
                "WaitRoomLeaveRequest" => WaitRoomLeave,
                "WaitRoomChatRequest" => WaitRoomChat,
                "WaitRoomPlayerReadyRequest" => WaitRoomPlayerReady,
                "WaitRoomPlayerUnreadyRequest" => WaitRoomPlayerUnready,
                "Welcome" => WelcomeMessage,
                "MatchEnd" => MatchEndNotification,

                // A spawn or a despawn has been spelled three ways on the two
                // sides. They are one fact, so they resolve to the name the server
                // actually sends rather than each side keeping its own.
                "ItemSpawn" => ItemSpawnNotification,
                "ItemDespawn" => ItemDespawnNotification,
                "FieldItemSpawn" => ItemSpawnNotification,
                "FieldItemDespawn" => ItemDespawnNotification,
                // A client built before the shared contract carried the claim
                // under the shorter name, so the ruling it reads back has to
                // resolve to the same fact rather than to an unknown message.
                "ItemPickup" => FieldItemPickup,
                "LoadingStartedNotification" => LoadingStartedNotification,
                "LoadingProgressNotification" => LoadingProgressNotification,
                "LoadingCompletedNotification" => LoadingCompletedNotification,
                _ => messageType
            };
        }
    }
}
