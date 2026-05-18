using UnityEngine;
using UnityEngine.SceneManagement;

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

            var lobbyScene = GeneralSceneMasterData.Instance().LobbyScene();
            SceneManager.LoadSceneAsync(lobbyScene);
        }
    }
}
