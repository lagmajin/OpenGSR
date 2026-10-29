using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyNetworkManager : MonoBehaviour
    {
        public void ConnectToLobbyServer(string ip, int port)
        {
            Debug.Log($"[ConnectToLobbyNetworkManager] ConnectToLobbyServer {ip}:{port}");

            var clientNetworkManager = FindFirstObjectByType<ClientNetworkManager>();
            if (clientNetworkManager == null)
            {
                var clientObject = new GameObject("ClientNetworkManager");
                DontDestroyOnLoad(clientObject);
                clientNetworkManager = clientObject.AddComponent<ClientNetworkManager>();
            }
            clientNetworkManager.ConfigureLobbyEndpoint(ip, port);

            try
            {
                var manager = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                manager?.TryConnectToServer(ip, port);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ConnectToLobbyNetworkManager] fallback connection failed: {ex.Message}");
            }

            Debug.LogWarning("[ConnectToLobbyNetworkManager] GeneralServerNetworkManager not found, lobby connect is mocked.");
        }
    }
}
