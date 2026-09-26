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
                InvokeSafely(onUpdateRooms, nameof(onUpdateRooms));
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKeyDown(KeyCode.Escape))
            {
                InvokeSafely(onBackToTitle, nameof(onBackToTitle));
                return;
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                InvokeSafely(onOpenShop, nameof(onOpenShop));
            }

            if (updateCount >= maxUpdateCount)
            {
                InvokeSafely(onBackToTitle, nameof(onBackToTitle));
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
                    InvokeSafely(onRoomListUpdated, RoomListSnapshot.FromJson(json), nameof(onRoomListUpdated));
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
            bool success = ReadSuccess(json["Success"]);
            if (success)
            {
                string roomId = json["RoomID"]?.ToString();
                string roomName = json["RoomName"]?.ToString();
                int capacity = ReadNonNegativeInt(json["Capacity"], 8);
                InvokeSafely(onRoomCreateSuccess, roomId, roomName, capacity, nameof(onRoomCreateSuccess));
                return;
            }

            string errorMessage = json["ErrorMessage"]?.ToString() ?? "Unknown error";
            InvokeSafely(onRoomCreateFailed, errorMessage, nameof(onRoomCreateFailed));
        }

        private static void HandleEnterWaitRoomResponse(
            JObject json,
            System.Action<string, string, int, int> onRoomEnterSuccess,
            System.Action<string> onRoomEnterFailed)
        {
            bool success = ReadSuccess(json["Success"]);
            if (success)
            {
                string roomId = json["RoomID"]?.ToString();
                string roomName = json["RoomName"]?.ToString();
                int capacity = ReadNonNegativeInt(json["Capacity"]);
                int playerCount = ReadPlayerCount(json);
                InvokeSafely(onRoomEnterSuccess, roomId, roomName, capacity, playerCount, nameof(onRoomEnterSuccess));
                return;
            }

            string errorMessage = json["ErrorMessage"]?.ToString() ?? "Unknown error";
            InvokeSafely(onRoomEnterFailed, errorMessage, nameof(onRoomEnterFailed));
        }

        private static void InvokeSafely(System.Action handler, string eventName)
        {
            try
            {
                handler?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OnlineLobbySceneController] {eventName} callback failed: {ex}");
            }
        }

        private static void InvokeSafely<T>(System.Action<T> handler, T value, string eventName)
        {
            try
            {
                handler?.Invoke(value);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OnlineLobbySceneController] {eventName} callback failed: {ex}");
            }
        }

        private static void InvokeSafely(System.Action<string, string, int> handler, string roomId, string roomName, int capacity, string eventName)
        {
            try
            {
                handler?.Invoke(roomId, roomName, capacity);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OnlineLobbySceneController] {eventName} callback failed: {ex}");
            }
        }

        private static void InvokeSafely(System.Action<string, string, int, int> handler, string roomId, string roomName, int capacity, int playerCount, string eventName)
        {
            try
            {
                handler?.Invoke(roomId, roomName, capacity, playerCount);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OnlineLobbySceneController] {eventName} callback failed: {ex}");
            }
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
                return Mathf.Max(0, playerCount);
            }

            var playersToken = json["Players"];
            if (playersToken is JArray playersArray)
            {
                return Mathf.Max(0, playersArray.Count);
            }

            if (playersToken != null && int.TryParse(playersToken.ToString(), out playerCount))
            {
                return Mathf.Max(0, playerCount);
            }

            return 1;
        }

        private static int ReadNonNegativeInt(JToken token, int fallback = 0)
        {
            try
            {
                return Mathf.Max(0, token?.ToObject<int>() ?? fallback);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OnlineLobbySceneController] Invalid room number: {ex.Message}");
                return Mathf.Max(0, fallback);
            }
        }

        private static bool ReadSuccess(JToken token)
        {
            try
            {
                return token?.ToObject<bool>() ?? false;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OnlineLobbySceneController] Invalid success value: {ex.Message}");
                return false;
            }
        }
    }
}
