using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Manages TCP connection to the general (lobby) server.
    /// </summary>
    public class GeneralServerNetworkManager
    {
        private readonly Subject<JObject> dataReceivedSubject = new Subject<JObject>();
        private readonly Subject<Unit> connectedSubject = new Subject<Unit>();
        private readonly Subject<Unit> disconnectedSubject = new Subject<Unit>();

        private readonly List<INetworkManagerScript> scripts = new List<INetworkManagerScript>();

        public System.IObservable<JObject> DataReceivedStream => dataReceivedSubject.AsObservable();
        public System.IObservable<Unit> ConnectedStream => connectedSubject.AsObservable();
        public System.IObservable<Unit> DisconnectedStream => disconnectedSubject.AsObservable();

        public bool Online { get; private set; } = false;
        public JObject LastMatchResult { get; private set; }

        public void ClearLastMatchResult() => LastMatchResult = null;

        public void Subscribe(INetworkManagerScript script)
        {
            if (!scripts.Contains(script)) scripts.Add(script);
        }

        public void UnSubscribe(INetworkManagerScript script) => scripts.Remove(script);

        public void ConnectToGeneralServerSync(string ip, int port, string id, string pass)
        {
            Debug.Log($"[GeneralServerNetworkManager] ConnectToGeneralServerSync {ip}:{port}");
        }

        public void TryConnectToServer(string ip, int port)
        {
            Debug.Log($"[GeneralServerNetworkManager] TryConnectToServer {ip}:{port}");
        }

        public void Disconnect()
        {
            Online = false;
            Debug.Log("[GeneralServerNetworkManager] Disconnect");
        }

        public void SendMessage(in JObject json)
        {
            Debug.Log($"[GeneralServerNetworkManager] SendMessage: {json?["MessageType"]}");
        }

        public void SendMessage(JObject json)
        {
            Debug.Log($"[GeneralServerNetworkManager] SendMessage: {json?["MessageType"]}");
        }

        public void SendUpdateRoomRequest()
        {
        }

        public void SendUpdateRoomRequest(in System.Collections.Generic.List<OpenGSCore.EGameMode> modeList)
        {
        }

        public void SendUpdateRoomRequest(System.Collections.Generic.List<OpenGSCore.EGameMode> modes)
        {
        }
    }
}
