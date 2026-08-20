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
        private ClientNetworkManager networkClient;
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

            if (networkClient != null)
            {
                networkClient.UdpMessageReceived -= OnNetworkClientMessage;
                networkClient = null;
            }

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

            if (networkClient != null)
            {
                // ClientNetworkManager owns the LiteNetLib peer and event
                // polling. Keep this facade transport-agnostic for callers.
                networkClient.SendUdpInput(json);
                return;
            }

            Debug.LogWarning("[MatchRUDPServerNetworkManager] No LiteNetLib client is available; message was not sent.");
        }

        private void ConnectInternal(int port, bool isLocal)
        {
            Debug.Log($"[MatchRUDPServerNetworkManager] {(isLocal ? "ConnectToLocalServer" : "ConnectToServer")} port={port}");

            localServer = null;
            if (isLocal)
            {
                try
                {
                    localServer = DependencyInjectionConfig.Resolve<LocalTestMatchRUDPServer>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MatchRUDPServerNetworkManager] Failed to resolve LocalTestMatchRUDPServer: {ex.Message}");
                }
            }

            if (localServer != null)
            {
                localServer.MessageProduced -= OnServerProducedMessage;
                localServer.MessageProduced += OnServerProducedMessage;
            }

            if (!isLocal)
            {
                networkClient = UnityEngine.Object.FindFirstObjectByType<ClientNetworkManager>();
                if (networkClient != null)
                {
                    networkClient.UdpMessageReceived -= OnNetworkClientMessage;
                    networkClient.UdpMessageReceived += OnNetworkClientMessage;
                }
                else
                {
                    Debug.LogWarning("[MatchRUDPServerNetworkManager] ClientNetworkManager was not found; LiteNetLib UDP is unavailable.");
                }
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
            if (messageType == RUDPMessageTypes.LegacyMatchEnd)
            {
                // Older match servers emit MatchEnd while the result scene
                // consumes MatchEndNotification. Normalize at this boundary
                // so the notification is cached and forwarded consistently.
                json["MessageType"] = MessageType.MatchEndNotification;
                messageType = MessageType.MatchEndNotification;
            }

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

        private void OnNetworkClientMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            OnServerProducedMessage(json);
        }
    }
}
