using System;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Manages RUDP connection to the match server.
    /// </summary>
    public class MatchRUDPServerNetworkManager
    {
        private readonly Subject<JObject> dataReceivedSubject = new Subject<JObject>();
        private readonly Subject<Unit> connectedSubject = new Subject<Unit>();
        private readonly Subject<Unit> disconnectedSubject = new Subject<Unit>();

        public System.IObservable<JObject> DataReceivedStream => dataReceivedSubject.AsObservable();
        public System.IObservable<Unit> ConnectedStream => connectedSubject.AsObservable();
        public System.IObservable<Unit> DisconnectedStream => disconnectedSubject.AsObservable();

        public bool IsConnected() => false;

        public void ConnectToServer(int port)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] ConnectToServer port={port}");
        }

        public void ConnectToLocalServer(int port)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] ConnectToLocalServer port={port}");
        }

        public void Disconnect()
        {
            Debug.Log("[MatchRUDPServerNetworkManager] Disconnect");
        }

        public void SendToServer(in JObject json)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] SendToServer: {json?["MessageType"]}");
        }

        public void SendToServer(JObject json)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] SendToServer: {json?["MessageType"]}");
        }
    }
}
