using UnityEngine;
using Newtonsoft.Json.Linq;

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
            System.Action<JArray> onRoomListUpdated)
        {
            var messageType = json?["MessageType"]?.ToString();
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return;
            }

            switch (messageType)
            {
                case "CreateNewWaitRoomResponse":
                    HandleCreateNewWaitRoomResponse(json, onRoomCreateSuccess, onRoomCreateFailed);
                    break;
                case "UpdateRoomResponse":
                    onRoomListUpdated?.Invoke(json["Rooms"] as JArray);
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
    }
}
