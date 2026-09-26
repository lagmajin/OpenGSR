using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyNetworkManager : MonoBehaviour
    {
        public void ConnectToLobbyServer(string ip, int port)
        {
            Debug.Log($"[ConnectToLobbyNetworkManager] ConnectToLobbyServer {ip}:{port}");
            try
            {
                var manager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                if (manager != null)
                {
                    manager.TryConnectToServer(ip, port);
                    return;
                }

                Debug.LogWarning("[ConnectToLobbyNetworkManager] GeneralServerNetworkManager resolved as null.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ConnectToLobbyNetworkManager] fallback connection failed: {ex.Message}");
            }

            Debug.LogWarning("[ConnectToLobbyNetworkManager] GeneralServerNetworkManager not found, lobby connect is mocked.");
        }
    }
}
