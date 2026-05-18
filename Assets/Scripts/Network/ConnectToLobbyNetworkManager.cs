using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    [DisallowMultipleComponent]
    public class ConnectToLobbyNetworkManager : MonoBehaviour
    {
        [SerializeField] private string clientPlayerName = "Player";

        private TcpClient tcpClient;
        private NetworkStream tcpStream;
        private readonly StringBuilder messageBuffer = new StringBuilder();
        private ConnectToGeneralServerScene parentScene;
        private SynchronizationContext mainThread;
        private bool enteredLobby;

        private void Awake()
        {
            mainThread = SynchronizationContext.Current;
            parentScene = GetComponentInParent<ConnectToGeneralServerScene>();
            if (parentScene == null)
            {
                parentScene = FindFirstObjectByType<ConnectToGeneralServerScene>();
            }
        }

        private void OnDestroy()
        {
            DisconnectFromServer();
        }

        public void ConnectToLobbyServer(string ip, int port)
        {
            Debug.Log($"[ConnectToLobbyNetworkManager] ConnectToLobbyServer requested: {ip}:{port}");
            _ = ConnectAsync(ip, port);
        }

        public void DisconnectFromServer()
        {
            try
            {
                tcpStream?.Close();
                tcpClient?.Close();
                tcpClient?.Dispose();
            }
            catch
            {
            }
            finally
            {
                tcpStream = null;
                tcpClient = null;
            }
        }

        private async Task ConnectAsync(string ip, int port)
        {
            try
            {
                DisconnectFromServer();
                tcpClient = new TcpClient();
                Debug.Log($"[ConnectToLobbyNetworkManager] Connecting to {ip}:{port}");
                await tcpClient.ConnectAsync(ip, port);
                tcpStream = tcpClient.GetStream();
                Debug.Log($"[ConnectToLobbyNetworkManager] Connected to {ip}:{port}");

                RunOnMainThread(() => parentScene?.OnConnected());

                _ = ReceiveLoopAsync();
                SendLoginRequest();

                DebugSettingsManager.EnsureLoaded();
                if (DebugSettingsManager.settings?.localServerTestMode == true)
                {
                    Debug.Log("[ConnectToLobbyNetworkManager] Local test mode is enabled. Proceeding to lobby after TCP connect.");
                    RunOnMainThread(() =>
                    {
                        parentScene?.EnterServerAccepted();
                        GoToLobbyOnce();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConnectToLobbyNetworkManager] Failed to connect: {ex.Message}");
                RunOnMainThread(() => parentScene?.OnLoginFailed());
            }
        }

        private void SendLoginRequest()
        {
            if (tcpStream == null || !tcpStream.CanWrite)
            {
                Debug.LogWarning("[ConnectToLobbyNetworkManager] Cannot send login request because the stream is not writable.");
                return;
            }

            var playerId = AccountManager.Instance.CurrentProfile.GlobalUserId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = Guid.NewGuid().ToString("N");
            }

            var playerName = AccountManager.Instance.CurrentProfile.DisplayName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = clientPlayerName;
            }

            var json = new JObject
            {
                ["MessageType"] = MessageType.LoginRequest,
                ["PlayerID"] = playerId,
                ["PlayerName"] = playerName,
                ["AccountName"] = playerName,
                ["GlobalUserId"] = playerId
            };

            Debug.Log($"[ConnectToLobbyNetworkManager] Sending login request for {playerName} ({playerId})");
            SendJson(json);
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                var buffer = new byte[8192];
                while (tcpClient != null && tcpClient.Connected && tcpStream != null)
                {
                    int bytesRead = await tcpStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                    {
                        Debug.LogWarning("[ConnectToLobbyNetworkManager] Receive loop ended because the stream returned 0 bytes.");
                        break;
                    }

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"[ConnectToLobbyNetworkManager] Received chunk ({bytesRead} bytes): {chunk}");
                    messageBuffer.Append(chunk);

                    string fullBuffer = messageBuffer.ToString();
                    string[] parts = fullBuffer.Split('\x1F');
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        Debug.Log($"[ConnectToLobbyNetworkManager] Processing packet: {parts[i]}");
                        HandleRawPacket(parts[i]);
                    }

                    messageBuffer.Clear();
                    messageBuffer.Append(parts[^1]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConnectToLobbyNetworkManager] Receive loop ended: {ex.Message}");
            }
            finally
            {
                RunOnMainThread(() => parentScene?.OnDisconnected());
                DisconnectFromServer();
            }
        }

        private void HandleRawPacket(string rawPacket)
        {
            if (string.IsNullOrWhiteSpace(rawPacket))
            {
                return;
            }

            string parseTarget = rawPacket.Trim();
            int firstBrace = parseTarget.IndexOf('{');
            int lastBrace = parseTarget.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                parseTarget = parseTarget.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else if (firstBrace >= 0)
            {
                parseTarget = parseTarget.Substring(firstBrace);
            }

            try
            {
                Debug.Log($"[ConnectToLobbyNetworkManager] Raw packet before parse: {rawPacket}");
                var json = JObject.Parse(parseTarget);
                HandleServerMessage(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConnectToLobbyNetworkManager] Failed to parse packet: {ex.Message}");
            }
        }

        private void HandleServerMessage(JObject json)
        {
            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            switch (messageType)
            {
                case "ConnectServerSuccessful":
                    Debug.Log("[ConnectToLobbyNetworkManager] Received ConnectServerSuccessful.");
                    RunOnMainThread(() =>
                    {
                        parentScene?.EnterServerAccepted();
                        GoToLobbyOnce();
                    });
                    break;
                case MessageType.LoginResponse:
                {
                    var success = json["Success"]?.ToObject<bool>() ?? false;
                    Debug.Log($"[ConnectToLobbyNetworkManager] Received LoginResponse success={success}.");
                    if (success)
                    {
                        RunOnMainThread(() =>
                        {
                            parentScene?.EnterServerAccepted();
                            GoToLobbyOnce();
                        });
                    }
                    else
                    {
                        RunOnMainThread(() => parentScene?.OnLoginFailed());
                    }
                    break;
                }
                default:
                    Debug.Log($"[ConnectToLobbyNetworkManager] Received: {messageType}");
                    break;
            }
        }

        private void GoToLobbyOnce()
        {
            if (enteredLobby)
            {
                return;
            }

            enteredLobby = true;
            Debug.Log("[ConnectToLobbyNetworkManager] Moving to lobby scene.");
            parentScene?.GoToLobby();
        }

        private void RunOnMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var context = mainThread ?? SynchronizationContext.Current;
            if (context == null || context == SynchronizationContext.Current)
            {
                action();
                return;
            }

            context.Post(_ => action(), null);
        }

        private void SendJson(JObject json)
        {
            if (tcpStream == null || !tcpStream.CanWrite)
            {
                Debug.LogWarning("[ConnectToLobbyNetworkManager] Cannot send packet because the stream is not writable.");
                return;
            }

            string jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
            byte[] payload = Encoding.UTF8.GetBytes(jsonString);
            byte[] separator = { 0x1F };
            byte[] data = new byte[payload.Length + separator.Length];
            Buffer.BlockCopy(payload, 0, data, 0, payload.Length);
            Buffer.BlockCopy(separator, 0, data, payload.Length, separator.Length);
            tcpStream.Write(data, 0, data.Length);
        }
    }
}
