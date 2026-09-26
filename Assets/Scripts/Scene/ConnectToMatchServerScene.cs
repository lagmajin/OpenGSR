using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


#pragma warning disable 0414


namespace OpenGS
{
    public class ConnectToMatchServerScene : MonoBehaviour
    {
        delegate void updateFunc();

        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 2001;
        [SerializeField] private int maxRetryCount = 3;
        [SerializeField] private int connectTimeoutMilliseconds = 2000;

        private bool connectSucceeded = false;
        private bool isShuttingDown;
        private TcpClient client = null;

        private updateFunc up;

        public bool TestConnect()
        {
            return connectSucceeded && client != null && client.Connected;
        }
        private void Awake()
        {
            serverAddress = serverAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                serverAddress = "127.0.0.1";
            }

            serverPort = Mathf.Clamp(serverPort, 1, 65535);
            maxRetryCount = Mathf.Max(1, maxRetryCount);
            connectTimeoutMilliseconds = Mathf.Max(100, connectTimeoutMilliseconds);

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }
        // Start is called before the first frame update
        void Start()
        {





            _ = ConnectToMatchServerAsync();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
            up = null;
            connectSucceeded = false;
            client?.Close();
            client = null;
        }

        private void OnDestroy()
        {
            isShuttingDown = true;
            up = null;
            connectSucceeded = false;
            client?.Close();
            client = null;
        }



        private void ServerUpdate()
        {
            Debug.Log("ServerUpdate");
        }

        private void ClientUpdate()
        {
            if (client == null || !client.Connected)
            {
                up = null;
                connectSucceeded = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (up == null)
            {
                return;
            }

            foreach (Action handler in up.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConnectToMatchServerScene] update callback failed: {ex}");
                }
            }
        }

        private void ConnectError()
        {
            Debug.LogError($"[ConnectToMatchServerScene] Failed to connect to {serverAddress}:{serverPort} after {maxRetryCount} attempts.");
        }

        private async Task ConnectToMatchServerAsync()
        {
            int tryCount = 0;

            while (!isShuttingDown && tryCount < Math.Max(1, maxRetryCount))
            {
                var candidate = new TcpClient();
                try
                {
                    var connectTask = candidate.ConnectAsync(serverAddress, serverPort);
                    var timeoutTask = Task.Delay(Math.Max(100, connectTimeoutMilliseconds));
                    if (await Task.WhenAny(connectTask, timeoutTask) != connectTask)
                    {
                        _ = connectTask.ContinueWith(
                            task => _ = task.Exception,
                            TaskContinuationOptions.OnlyOnFaulted);
                        candidate.Close();
                        candidate.Dispose();
                        tryCount++;
                    }
                    else
                    {
                        await connectTask;
                        if (isShuttingDown)
                        {
                            candidate.Close();
                            candidate.Dispose();
                            return;
                        }

                        client = candidate;
                        connectSucceeded = true;
                        up = ClientUpdate;
                        break;
                    }
                }
                catch (SocketException)
                {
                    candidate.Close();
                    candidate.Dispose();
                    tryCount++;
                }
                catch (ObjectDisposedException)
                {
                    candidate.Close();
                    candidate.Dispose();
                    tryCount++;
                }
                catch (AggregateException)
                {
                    candidate.Close();
                    candidate.Dispose();
                    tryCount++;
                }
                catch (Exception ex)
                {
                    candidate.Close();
                    candidate.Dispose();
                    tryCount++;
                    Debug.LogWarning($"[ConnectToMatchServerScene] Connection attempt failed: {ex.Message}");
                }

                if (!connectSucceeded)
                {
                    await Task.Delay(50);
                }

            }

            if (!isShuttingDown && connectSucceeded && client != null)
            {
                try
                {
                    var json = new JObject
                    {
                        ["MessageType"] = "ConnectionTest",
                        ["id"] = "",
                        ["TimeStamp"] = DateTime.UtcNow
                    };
                    var payload = Encoding.UTF8.GetBytes(json.ToString(Formatting.None) + "\n");
                    var stream = client.GetStream();
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    connectSucceeded = false;
                    Debug.LogWarning($"[ConnectToMatchServerScene] Connection test failed: {ex.Message}");
                    client?.Close();
                    client = null;
                }
            }
            else
            {
                ConnectError();
            }



        }



    }
}
