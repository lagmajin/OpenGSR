using UnityEngine;
using Newtonsoft.Json.Linq;
using OpenGSCore;

namespace OpenGS
{
    public class OnlineLobbySceneController : MonoBehaviour
    {
        public void TickInput(
            bool canInput,
            ref int updateCount,
            int maxUpdateCount,
            System.Action onUpdateRooms,
            System.Action onBackToTitle,
            System.Action onOpenShop)
        {
            if (!canInput)
            {
                return;
            }

            if (Input.anyKeyDown)
            {
                updateCount = 0;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                onUpdateRooms?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKey(KeyCode.Escape))
            {
                onBackToTitle?.Invoke();
                return;
            }

            if (Input.GetKey(KeyCode.S))
            {
                onOpenShop?.Invoke();
            }

            if (updateCount >= maxUpdateCount)
            {
                onBackToTitle?.Invoke();
                return;
            }

            updateCount++;
        }

        public void ParseServerMessage(
            JObject json,
            System.Action<string, string, int> onRoomCreateSuccess,
            System.Action<string> onRoomCreateFailed,
            System.Action<RoomListSnapshot> onRoomListUpdated,
            System.Action<string, string, int, int> onRoomEnterSuccess = null,
            System.Action<string> onRoomEnterFailed = null)
        {
            var messageType = json?["MessageType"]?.ToString();
            messageType = MessageType.Normalize(messageType);
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return;
            }

            switch (messageType)
            {
                case MessageType.CreateRoomResponse:
                    HandleCreateNewWaitRoomResponse(json, onRoomCreateSuccess, onRoomCreateFailed);
                    break;
                case MessageType.RoomListUpdateNotification:
                    onRoomListUpdated?.Invoke(RoomListSnapshot.FromJson(json));
                    break;
                case MessageType.JoinRoomResponse:
                    HandleEnterWaitRoomResponse(json, onRoomEnterSuccess, onRoomEnterFailed);
                    break;
                default:
                    Debug.LogWarning($"OnlineLobbySceneController: Unknown message type: {messageType}");
                    break;
            }
        }

        private static void HandleCreateNewWaitRoomResponse(
            JObject json,
            System.Action<string, string, int> onRoomCreateSuccess,
            System.Action<string> onRoomCreateFailed)
        {
            bool success = json["Success"]?.ToObject<bool>() ?? false;
            if (success)
            {
                string roomId = json["RoomID"]?.ToString();
                string roomName = json["RoomName"]?.ToString();
                int capacity = json["Capacity"]?.ToObject<int>() ?? 8;
                onRoomCreateSuccess?.Invoke(roomId, roomName, capacity);
                return;
            }

            string errorMessage = json["ErrorMessage"]?.ToString() ?? "Unknown error";
            onRoomCreateFailed?.Invoke(errorMessage);
        }

        private static void HandleEnterWaitRoomResponse(
            JObject json,
            System.Action<string, string, int, int> onRoomEnterSuccess,
            System.Action<string> onRoomEnterFailed)
        {
            bool success = json["Success"]?.ToObject<bool>() ?? false;
            if (success)
            {
                string roomId = json["RoomID"]?.ToString();
                string roomName = json["RoomName"]?.ToString();
                int capacity = json["Capacity"]?.ToObject<int>() ?? 0;
                int playerCount = ReadPlayerCount(json);
                onRoomEnterSuccess?.Invoke(roomId, roomName, capacity, playerCount);
                return;
            }

            string errorMessage = json["ErrorMessage"]?.ToString() ?? "Unknown error";
            onRoomEnterFailed?.Invoke(errorMessage);
        }

        private static int ReadPlayerCount(JObject json)
        {
            if (json == null)
            {
                return 1;
            }

            var playerCountToken = json["PlayerCount"];
            if (playerCountToken != null && int.TryParse(playerCountToken.ToString(), out var playerCount))
            {
                return playerCount;
            }

            var playersToken = json["Players"];
            if (playersToken is JArray playersArray)
            {
                return playersArray.Count;
            }

            if (playersToken != null && int.TryParse(playersToken.ToString(), out playerCount))
            {
                return playerCount;
            }

            return 1;
        }
    }
}
