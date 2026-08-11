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
        private static TcpClient client = null;

        private updateFunc up;

        public bool TestConnect()
        {
            return connectSucceeded && client != null && client.Connected;
        }
        private void Awake()
        {

            DebugFlagManager.SetFirstSceneName(this.GetType().FullName);


        }
        // Start is called before the first frame update
        void Start()
        {





            var task = Task.Run(() =>
                {
                    ConnectToMatchServer();
                });
        }

        private void OnApplicationQuit()
        {
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
            up?.Invoke();
        }

        private void ConnectError()
        {
            Debug.LogError($"[ConnectToMatchServerScene] Failed to connect to {serverAddress}:{serverPort} after {maxRetryCount} attempts.");
        }

        private void ConnectToMatchServer()
        {
            int tryCount = 0;

            while (tryCount < Mathf.Max(1, maxRetryCount))
            {
                var candidate = new TcpClient();
                if (!candidate.ConnectAsync(serverAddress, serverPort).Wait(Mathf.Max(100, connectTimeoutMilliseconds)))
                {
                    candidate.Close();
                    tryCount++;
                }
                else
                {
                    client = candidate;
                    connectSucceeded = true;
                    up = ClientUpdate;
                    break;
                }

            }

            if (connectSucceeded)
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
            else
            {
                ConnectError();
            }



        }



    }
}
