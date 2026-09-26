using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenGS.Network;
using OpenGSCore;
using UniRx;
using UnityEditor;
using UnityEngine;

namespace OpenGS.Editor
{
    /// <summary>
    /// Runs the local control-plane and match-plane flow without opening sockets.
    /// This is intentionally synchronous so it can be used as a fast pre-commit check.
    /// </summary>
    public static class NetworkIntegrationSmokeTest
    {
        [MenuItem("OpenGS/Tests/Run Network Integration Smoke Test")]
        public static void RunFromMenu()
        {
            try
            {
                Run();
                Debug.Log("[NetworkIntegrationSmokeTest] PASS: login -> lobby -> room -> loading -> match -> result");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NetworkIntegrationSmokeTest] FAIL: {exception}");
                throw;
            }
        }

        public static void Run()
        {
            RunAccountShopFriendFlow();
            RunWaitRoomAuthorityFlow();
            RunControlPlaneFlow();
            RunMatchPlaneFlow();
            RunMissionRouteFlow();
            Debug.Log("[NetworkIntegrationSmokeTest] PASS: login -> lobby -> room -> loading -> match -> result");
        }

        private static void RunMissionRouteFlow()
        {
            var requiredScenes = new[]
            {
                "Assets/Scenes/MissionLobbyScene.unity",
                "Assets/Scenes/Waitroom/MissionWaitroom.unity",
                "Assets/Scenes/Loading/OfflineLoadingScene.unity",
                "Assets/Scenes/Map/Mission/MissionResultScene.unity",
                "Assets/Scenes/Map/Mission/Mission1.unity",
                "Assets/Scenes/Map/Mission/Mission2.unity",
                "Assets/Scenes/Map/Mission/Mission3.unity",
                "Assets/Scenes/Map/Mission/Mission4.unity",
                "Assets/Scenes/Map/Mission/Mission5.unity",
                "Assets/Scenes/Map/Mission/Quest1.unity",
                "Assets/Scenes/Map/Mission/Quest2.unity",
                "Assets/Scenes/Map/Mission/Quest3.unity"
            };

            foreach (var scenePath in requiredScenes)
            {
                Require(File.Exists(Path.Combine(Application.dataPath, scenePath.Substring("Assets/".Length))),
                    $"mission route scene is missing: {scenePath}");
                var buildScene = Array.Find(EditorBuildSettings.scenes, scene => scene.path == scenePath);
                Require(buildScene != null && buildScene.enabled,
                    $"mission route scene is not enabled in Build Settings: {scenePath}");
            }

            var missionMainGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Mission/MissionMainScript.cs");
            foreach (var scenePath in requiredScenes[4..])
            {
                var sceneText = File.ReadAllText(Path.Combine(Application.dataPath, scenePath.Substring("Assets/".Length)));
                Require(CountOccurrences(sceneText, $"guid: {missionMainGuid}") == 1,
                    $"mission scene does not contain exactly one MissionMainScript: {scenePath}");
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static void RunAccountShopFriendFlow()
        {
            var server = new GeneralServerNetworkManager();
            var runId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var accountA = $"s1-smoke-account-a-{runId}";
            var accountB = $"s1-smoke-account-b-{runId}";
            var itemId = $"s1-smoke-item-{runId}";
            var responses = new List<JObject>();
            var subscription = server.DataReceivedStream.Subscribe(responses.Add);
            try
            {
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.CreateAccountRequest,
                    ["AccountName"] = "S1 Smoke A",
                    ["GlobalUserId"] = accountA
                });
                RequireMessage(responses, OpenGSCore.MessageType.CreateAccountResponse, "account create");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.LoginRequest,
                    ["PlayerID"] = accountA,
                    ["PlayerName"] = "S1 Smoke A"
                });
                var login = RequireMessage(responses, OpenGSCore.MessageType.LoginResponse, "account login");
                Require(login["Success"]?.ToObject<bool>() == true, "account login returned Success=false");

                responses.Clear();
                server.SendMessage(new JObject { ["MessageType"] = OpenGSCore.MessageType.ShopStateRequest });
                var shopState = RequireMessage(responses, OpenGSCore.MessageType.ShopStateResponse, "shop state");
                Require(shopState["Credits"] != null, "shop state did not return credits");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.ShopPurchaseRequest,
                    ["ItemId"] = itemId,
                    ["Price"] = 1
                });
                var purchase = RequireMessage(responses, OpenGSCore.MessageType.ShopPurchaseResponse, "shop purchase");
                Require(purchase["Success"]?.ToObject<bool>() == true, "shop purchase was not authoritative");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.FriendRequest,
                    ["PlayerID"] = accountA,
                    ["TargetPlayerID"] = accountB
                });
                var friendRequest = RequireMessage(responses, OpenGSCore.MessageType.FriendRequestResponse, "friend request");
                Require(friendRequest["Success"]?.ToObject<bool>() == true, "friend request was not accepted");
                RequireMessage(responses, OpenGSCore.MessageType.FriendRequestNotification, "friend request notification");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.FriendApproveRequest,
                    ["PlayerID"] = accountB,
                    ["RequestPlayerID"] = accountA,
                    ["Approve"] = true
                });
                var approval = RequireMessage(responses, OpenGSCore.MessageType.FriendApproveResponse, "friend approval");
                Require(approval["Success"]?.ToObject<bool>() == true, "friend approval was not accepted");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.FriendListRequest,
                    ["PlayerID"] = accountA
                });
                var friends = RequireMessage(responses, OpenGSCore.MessageType.FriendListResponse, "friend list");
                Require(friends["Friends"] is JArray friendList && friendList.Count == 1, "friend list did not persist the approved friend");

                server.ClearLastMatchResult();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.MatchEndNotification,
                    ["WinningTeam"] = "Red",
                    ["TotalDeaths"] = 3
                });
                Require(server.LastMatchResult != null, "general server did not cache the authoritative match result");

                var reconnectedServer = new GeneralServerNetworkManager();
                var reconnectResponses = new List<JObject>();
                var reconnectSubscription = reconnectedServer.DataReceivedStream.Subscribe(reconnectResponses.Add);
                try
                {
                    reconnectedServer.SendMessage(new JObject
                    {
                        ["MessageType"] = OpenGSCore.MessageType.LoginRequest,
                        ["PlayerID"] = accountA,
                        ["PlayerName"] = "S1 Smoke A"
                    });
                    RequireMessage(reconnectResponses, OpenGSCore.MessageType.LoginResponse, "reconnect login");

                    reconnectResponses.Clear();
                    reconnectedServer.SendMessage(new JObject { ["MessageType"] = OpenGSCore.MessageType.ShopStateRequest });
                    var restoredShop = RequireMessage(reconnectResponses, OpenGSCore.MessageType.ShopStateResponse, "reconnect shop state");
                    Require(restoredShop["PurchasedItems"] is JArray restoredItems && restoredItems.Values<string>().Any(value => string.Equals(value, itemId, StringComparison.Ordinal)),
                        "purchased item was not restored after reconnect");

                    reconnectResponses.Clear();
                    reconnectedServer.SendMessage(new JObject
                    {
                        ["MessageType"] = OpenGSCore.MessageType.FriendListRequest,
                        ["PlayerID"] = accountA
                    });
                    var restoredFriends = RequireMessage(reconnectResponses, OpenGSCore.MessageType.FriendListResponse, "reconnect friend list");
                    Require(restoredFriends["Friends"] is JArray restoredFriendList && restoredFriendList.Count == 1,
                        "friend state was not restored after reconnect");
                }
                finally
                {
                    reconnectSubscription.Dispose();
                }
            }
            finally
            {
                subscription.Dispose();
            }
        }

        private static void RunWaitRoomAuthorityFlow()
        {
            var server = new GeneralServerNetworkManager();
            var responses = new List<JObject>();
            var subscription = server.DataReceivedStream.Subscribe(responses.Add);
            try
            {
                server.SendCreateNewWaitRoomRequest("S1 Settings Smoke", 4, "DeathMatch", true);
                var roomCreated = RequireMessage(responses, OpenGSCore.MessageType.CreateRoomResponse, "authoritative room create");
                var roomId = roomCreated["RoomID"]?.ToString();
                Require(!string.IsNullOrWhiteSpace(roomId), "authoritative room create did not return RoomID");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.WaitRoomSettingsChange,
                    ["RoomID"] = roomId,
                    ["Settings"] = new JObject
                    {
                        ["Capacity"] = 6,
                        ["TeamBalance"] = false,
                        ["GameMode"] = "TeamDeathMatch",
                        ["BannedWeapons"] = new JArray("AK47", "M16")
                    }
                });
                var settingsResponse = RequireMessage(responses, OpenGSCore.MessageType.WaitRoomSettingsChange, "room settings change");
                Require(settingsResponse["Capacity"]?.ToObject<int>() == 6, "room capacity was not updated authoritatively");
                Require(settingsResponse["TeamBalance"]?.ToObject<bool>() == false, "room team balance was not updated authoritatively");
                Require(settingsResponse["GameMode"]?.ToString() == "TeamDeathMatch", "room game mode was not updated authoritatively");
                Require(settingsResponse["BannedWeapons"] is JArray bannedWeapons && bannedWeapons.Count == 2,
                    "room weapon limits were not updated authoritatively");

                responses.Clear();
                server.SendMessage(new JObject
                {
                    ["MessageType"] = OpenGSCore.MessageType.LeaveRoomRequest,
                    ["PlayerID"] = roomCreated["OwnerID"]?.ToString() ?? roomCreated["OwnerId"]?.ToString(),
                    ["RoomID"] = roomId
                });
                RequireMessage(responses, OpenGSCore.MessageType.LeaveRoomResponse, "authoritative room leave");
                RequireMessage(responses, OpenGSCore.MessageType.RoomDeleted, "empty room deletion");
            }
            finally
            {
                subscription.Dispose();
            }
        }

        private static void RunControlPlaneFlow()
        {
            var server = new LocalTestServerWrapper();
            var loginResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LoginRequest,
                ["PlayerID"] = "smoke-player-001"
            });
            RequireMessage(loginResponses, OpenGSCore.MessageType.LoginResponse, "login");

            var lobbyResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LobbyEnter,
                ["PlayerID"] = "smoke-player-001",
                ["PlayerName"] = "Smoke Player"
            });
            RequireMessage(lobbyResponses, OpenGSCore.MessageType.LobbyEnter, "lobby enter");

            var roomResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.CreateRoomRequest,
                ["PlayerID"] = "smoke-player-001",
                ["RoomName"] = "Smoke Room",
                ["GameMode"] = "DeathMatch",
                ["Capacity"] = 4
            });
            var roomCreated = RequireMessage(roomResponses, OpenGSCore.MessageType.CreateRoomResponse, "room create");
            var roomId = roomCreated["RoomID"]?.ToString();
            Require(!string.IsNullOrWhiteSpace(roomId), "room create did not return RoomID");

            var joinResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.JoinRoomRequest,
                ["PlayerID"] = "smoke-player-002",
                ["PlayerName"] = "Second Smoke Player",
                ["RoomID"] = roomId
            });
            var joinResponse = RequireMessage(joinResponses, OpenGSCore.MessageType.JoinRoomResponse, "room join");
            Require(joinResponse["Success"]?.ToObject<bool>() != false, "room join returned Success=false");

            var earlyStartResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.GameStartRequest,
                ["RoomID"] = roomId
            });
            var earlyStart = RequireMessage(earlyStartResponses, OpenGSCore.MessageType.ErrorNotification, "early match start rejection");
            Require(earlyStart["Success"]?.ToObject<bool>() == false, "server accepted match start before all players were ready");

            RequireMessage(Send(server, ReadyRequest(roomId, "smoke-player-001")), OpenGSCore.MessageType.PlayerReadyNotification, "owner ready");
            RequireMessage(Send(server, ReadyRequest(roomId, "smoke-player-002")), OpenGSCore.MessageType.PlayerReadyNotification, "guest ready");
            RequireMessage(Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.GameStartRequest,
                ["RoomID"] = roomId
            }), OpenGSCore.MessageType.WaitRoomStartCountdown, "match start countdown");

            var loadingEnteredResponse = RequireMessage(Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.ClientLoadingSceneEntered,
                ["PlayerID"] = "smoke-player-001",
                ["RoomID"] = roomId
            }), OpenGSCore.MessageType.MatchServerInfoResponse, "loading scene entered");
            Require(loadingEnteredResponse["Success"]?.ToObject<bool>() == true, "loading scene entry was not accepted");
            Require(loadingEnteredResponse["IP"]?.ToString() == "127.0.0.1", "match server IP was not returned");
            Require(loadingEnteredResponse["UdpPort"]?.ToObject<int>() == 63000, "match server UDP port was not returned");

            var matchInfoResponse = RequireMessage(Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.MatchServerInfoRequest,
                ["PlayerID"] = "smoke-player-001",
                ["RoomID"] = roomId
            }), OpenGSCore.MessageType.MatchServerInfoResponse, "match server info request");
            Require(matchInfoResponse["Success"]?.ToObject<bool>() == true, "match server info request was not accepted");

            RequireMessage(Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LoadingStarted,
                ["PlayerID"] = "smoke-player-001",
                ["RoomID"] = roomId
            }), OpenGSCore.MessageType.LoadingStartedNotification, "loading start");

            var progressResponse = RequireMessage(Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LoadingProgress,
                ["PlayerID"] = "smoke-player-001",
                ["RoomID"] = roomId,
                ["Progress"] = 0.5f
            }), OpenGSCore.MessageType.LoadingProgressNotification, "loading progress");
            var progress = progressResponse["Progress"]?.ToObject<float>() ?? -1f;
            Require(Math.Abs(progress - 0.5f) < 0.001f, "loading progress was not preserved");

            var loadingResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LoadingCompleted,
                ["PlayerID"] = "smoke-player-001",
                ["RoomID"] = roomId
            });
            var loadingResponse = RequireMessage(loadingResponses, OpenGSCore.MessageType.LoadingCompletedNotification, "loading complete");
            Require(loadingResponse["Success"]?.ToObject<bool>() == true, "loading completion was not successful");
            Require(!loadingResponses.Exists(message => message["MessageType"]?.ToString() == OpenGSCore.MessageType.AllowEnterMap),
                "server allowed map entry before all players completed loading");

            var finalLoadingResponses = Send(server, new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.LoadingCompleted,
                ["PlayerID"] = "smoke-player-002",
                ["RoomID"] = roomId
            });
            RequireMessage(finalLoadingResponses, OpenGSCore.MessageType.LoadingCompletedNotification, "second loading complete");
            RequireMessage(finalLoadingResponses, OpenGSCore.MessageType.AllowEnterMap, "map entry approval");
        }

        private static void RunMatchPlaneFlow()
        {
            var server = new LocalTestMatchRUDPServer();
            var produced = new List<JObject>();
            server.MessageProduced += message => produced.Add(message);

            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ClientConnect,
                ["PlayerID"] = "smoke-player-001"
            });
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ClientConnect,
                ["PlayerID"] = "smoke-player-002",
                ["Team"] = "Red"
            });

            var beforeUnknownMessage = produced.Count;
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = "UnknownSmokeMessage",
                ["PlayerID"] = "smoke-player-001"
            });
            Require(produced.Count == beforeUnknownMessage, "RUDP server produced output for an unknown message");

            var beforeSpoofedTeamKill = produced.Count;
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = "TeamKill",
                ["KillerTeam"] = "Blue",
                ["VictimTeam"] = "Red"
            });
            Require(produced.Count == beforeSpoofedTeamKill, "RUDP server accepted a TeamKill without player identities");

            var beforeUnknownMove = produced.Count;
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = "PlayerMove",
                ["PlayerID"] = "unknown-move-player",
                ["PosX"] = 999f,
                ["PosY"] = 999f
            });
            Require(produced.Count == beforeUnknownMove, "RUDP server accepted movement from an unknown player");

            var beforeUnknownKill = produced.Count;
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerKill,
                ["KillerId"] = "unknown-killer",
                ["VictimId"] = "smoke-player-001"
            });
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ItemUse,
                ["PlayerId"] = "unknown-item-player",
                ["ItemId"] = "smoke-item"
            });
            Require(produced.Count == beforeUnknownKill, "RUDP server accepted an input from an unknown player");

            server.ProcessIncomingMessage(RUDPMessageBuilder.CreatePlayerRespawn(
                "smoke-player-001", Vector2.zero));
            Require(produced.Exists(message =>
                message["MessageType"]?.ToString() == RUDPMessageTypes.PlayerRespawn &&
                message["PlayerID"]?.ToString() == "smoke-player-001"),
                "RUDP server did not produce an authoritative respawn event");

            server.ProcessIncomingMessage(RUDPMessageBuilder.CreatePlayerShot(
                "smoke-player-001", Vector2.zero, Vector2.right, "SmokeWeapon"));
            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.PlayerShot,
                ["PlayerId"] = "smoke-player-001",
                ["PosX"] = 1f,
                ["PosY"] = 2f,
                ["DirectionX"] = 0f,
                ["DirectionY"] = 1f,
                ["WeaponType"] = "LegacySmokeWeapon"
            });

            for (var index = 0; index < 3; index++)
            {
                server.ProcessIncomingMessage(RUDPMessageBuilder.CreatePlayerDeath(
                    "smoke-player-001", "smoke-player-002"));
            }

            var shot = produced.Find(message => message["MessageType"]?.ToString() == RUDPMessageTypes.PlayerShot);
            Require(shot != null, "RUDP shot echo was not produced");
            Require(shot["PlayerID"]?.ToString() == "smoke-player-001", "RUDP shot did not preserve canonical PlayerID");
            Require(produced.Exists(message =>
                message["MessageType"]?.ToString() == RUDPMessageTypes.PlayerShot &&
                message["WeaponType"]?.ToString() == "LegacySmokeWeapon" &&
                message["PlayerID"]?.ToString() == "smoke-player-001"),
                "RUDP server did not accept legacy PlayerId input while emitting canonical PlayerID");

            var matchEnd = produced.Find(message => message["MessageType"]?.ToString() == OpenGSCore.MessageType.MatchEndNotification);
            Require(matchEnd != null, "RUDP MatchEndNotification was not produced");
            Require(matchEnd["WinningTeam"]?.ToString() == "Red", "RUDP server did not derive the winning team from authoritative kill state");

            server.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.KillScoreUpdate,
                ["PlayerID"] = "smoke-player-001",
                ["Kills"] = 999,
                ["Deaths"] = 999,
                ["Score"] = 999999,
                ["Team"] = "Blue"
            });
            var authoritativeKillScore = produced.FindLast(message => message["MessageType"]?.ToString() == RUDPMessageTypes.KillScoreUpdate);
            Require(authoritativeKillScore?["Score"]?.ToObject<int>() != 999999, "RUDP server accepted a client-provided kill score");

            var flagServer = new LocalTestMatchRUDPServer();
            var flagProduced = new List<JObject>();
            flagServer.MessageProduced += message => flagProduced.Add(message);
            flagServer.ProcessIncomingMessage(RUDPMessageBuilder.CreateFlagCaptured(
                "unknown-flag-player", "Red", Vector2.zero, "unknown-flag-event"));
            Require(!flagProduced.Exists(message => message["MessageType"]?.ToString() == RUDPMessageTypes.FlagScoreUpdate), "RUDP server accepted a flag event from an unknown player");

            flagServer.ProcessIncomingMessage(new JObject
            {
                ["MessageType"] = RUDPMessageTypes.ClientConnect,
                ["PlayerID"] = "flag-player-001",
                ["Team"] = "Red"
            });

            for (var index = 0; index < 3; index++)
            {
                flagServer.ProcessIncomingMessage(RUDPMessageBuilder.CreateFlagCaptured(
                    "flag-player-001", "Red", Vector2.zero, $"flag-capture-{index}"));
            }
            flagServer.ProcessIncomingMessage(RUDPMessageBuilder.CreateFlagCaptured(
                "flag-player-001", "Red", Vector2.zero, "flag-capture-0"));

            var authoritativeFlagScore = flagProduced.FindLast(message => message["MessageType"]?.ToString() == RUDPMessageTypes.FlagScoreUpdate);
            Require(authoritativeFlagScore?["RedTeamScore"]?.ToObject<int>() == 3, "RUDP server accepted a client flag score instead of calculating it");
            Require(flagProduced.Exists(message => message["MessageType"]?.ToString() == OpenGSCore.MessageType.MatchEndNotification), "CTF authoritative flag score did not end the match");
        }

        private static JObject ReadyRequest(string roomId, string playerId)
        {
            return new JObject
            {
                ["MessageType"] = OpenGSCore.MessageType.PlayerReadyRequest,
                ["RoomID"] = roomId,
                ["PlayerID"] = playerId
            };
        }

        private static List<JObject> Send(LocalTestServerWrapper server, JObject request)
        {
            var responses = new List<JObject>();
            server.ProcessEvent(request, response => responses.Add(response));
            return responses;
        }

        private static JObject RequireMessage(List<JObject> responses, string messageType, string step)
        {
            var response = responses.Find(item => item["MessageType"]?.ToString() == messageType);
            Require(response != null, $"{step} did not produce {messageType}");
            return response;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
