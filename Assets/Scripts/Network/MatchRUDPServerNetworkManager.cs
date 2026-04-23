using System;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using OpenGSCore;

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
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();

        private LocalTestMatchRUDPServer localServer;
        private bool connected;

        public System.IObservable<JObject> DataReceivedStream => dataReceivedSubject.AsObservable();
        public System.IObservable<Unit> ConnectedStream => connectedSubject.AsObservable();
        public System.IObservable<Unit> DisconnectedStream => disconnectedSubject.AsObservable();

        public bool IsConnected() => connected;

        public void ConnectToServer(int port)
        {
            ConnectInternal(port, isLocal: false);
        }

        public void ConnectToLocalServer(int port)
        {
            ConnectInternal(port, isLocal: true);
        }

        public void Disconnect()
        {
            connected = false;
            subscriptions.Clear();

            if (localServer != null)
            {
                localServer.MessageProduced -= OnServerProducedMessage;
            }

            Debug.Log("[MatchRUDPServerNetworkManager] Disconnect");
            disconnectedSubject.OnNext(Unit.Default);
        }

        public void SendToServer(in JObject json)
        {
            SendToServer((JObject)json);
        }

        public void SendToServer(JObject json)
        {
            if (!connected)
            {
                Debug.LogWarning($"[MatchRUDPServerNetworkManager] SendToServer ignored because not connected: {json?["MessageType"]}");
                return;
            }

            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            json["MessageType"] = messageType;
            Debug.Log($"[MatchRUDPServerNetworkManager] SendToServer: {messageType}");

            if (localServer != null)
            {
                localServer.ProcessIncomingMessage(json);
                return;
            }

            dataReceivedSubject.OnNext(json);
        }

        private void ConnectInternal(int port, bool isLocal)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] {(isLocal ? "ConnectToLocalServer" : "ConnectToServer")} port={port}");

            try
            {
                localServer = DependencyInjectionConfig.Resolve<LocalTestMatchRUDPServer>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchRUDPServerNetworkManager] Failed to resolve LocalTestMatchRUDPServer: {ex.Message}");
                localServer = null;
            }

            if (localServer != null)
            {
                localServer.MessageProduced -= OnServerProducedMessage;
                localServer.MessageProduced += OnServerProducedMessage;
            }

            connected = true;
            connectedSubject.OnNext(Unit.Default);
        }

        private void OnServerProducedMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            var messageType = MessageType.Normalize(json["MessageType"]?.ToString());
            if (messageType == MessageType.MatchEndNotification || messageType == MessageType.MatchResult)
            {
                try
                {
                    var generalServer = DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>();
                    generalServer?.SendMessage(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MatchRUDPServerNetworkManager] Failed to forward match result: {ex.Message}");
                }
            }

            dataReceivedSubject.OnNext(json);
        }
    }
}
