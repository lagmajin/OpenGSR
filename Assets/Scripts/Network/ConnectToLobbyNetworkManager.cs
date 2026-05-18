using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyNetworkManager : MonoBehaviour
    {
        public void ConnectToLobbyServer(string ip, int port)
        {
            Debug.Log($"[ConnectToLobbyNetworkManager] ConnectToLobbyServer {ip}:{port}");

            var connectScene = FindFirstObjectByType<ConnectToGeneralServerScene>();
            if (connectScene != null)
            {
                connectScene.GoToLobby();
                return;
            }

            Debug.LogWarning("[ConnectToLobbyNetworkManager] ConnectToGeneralServerScene not found, cannot request lobby transition.");
        }
    }
}
