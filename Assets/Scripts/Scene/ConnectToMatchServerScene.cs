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

        private bool connectSucceeded = false;
        private static TcpClient client = null;

        private updateFunc up;

        void TestConnect()
        {

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
            //NewtworkCoreServerManager.GetInstance().Discconect();
        }



        private void ServerUpdate()
        {
            Debug.Log("ServerUpdate");
        }

        private void ClientUpdate()
        {
            Debug.Log("ClientUpdate");
        }

        // Update is called once per frame
        void Update()
        {
            up();
        }

        void ConnectError()
        {

        }

        private void ConnectToMatchServer()
        {
            string ip = "127.0.0.1";
            int port = 2001;

            var client = new TcpClient();

            int tryCount = 0;
            int maxRetry = 3;


            while (true)
            {




                if (client.ConnectAsync(ip, port).Wait(2000) == false)
                {
                    tryCount++;

                    if (tryCount >= maxRetry)
                    {

                        break;
                    }
                }
                else
                {
                    connectSucceeded = true;
                    break;
                }

            }

            if (connectSucceeded)
            {
                var json = new JObject();

                json["MessageType"] = "ConnectionTest";
                json["id"] = "";
                json["TimeStamp"] = DateTime.Now;




            }
            else
            {
                ConnectError();
            }



        }



    }
}
