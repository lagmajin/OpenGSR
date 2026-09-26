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
                localServer = null;
            }

            Debug.Log("[MatchRUDPServerNetworkManager] Disconnect");
            PublishConnectionEvent(disconnectedSubject, "disconnected");
        }

        public void SendToServer(in JObject json)
        {
            SendToServer((JObject)json);
        }

        public void SendToServer(JObject json)
        {
            if (json == null)
            {
                Debug.LogWarning("[MatchRUDPServerNetworkManager] SendToServer ignored because message is null.");
                return;
            }

            if (!connected)
            {
                Debug.LogWarning($"[MatchRUDPServerNetworkManager] SendToServer ignored because not connected: {json?["MessageType"]}");
                return;
            }

            var messageType = MessageType.Normalize(json?["MessageType"]?.ToString());
            json["MessageType"] = messageType;

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
            if (connected || localServer != null || networkClient != null)
            {
                Disconnect();
            }

            // LocalTestMatchRUDPServer uses port 0 as an in-process
            // connection sentinel. Real network connections still require
            // a valid TCP/UDP port.
            if ((!isLocal && port < 1) || port > 65535)
            {
                Debug.LogWarning($"[MatchRUDPServerNetworkManager] Invalid port: {port}");
                connected = false;
                return;
            }

            Debug.Log($"[MatchRUDPServerNetworkManager] {(isLocal ? "ConnectToLocalServer" : "ConnectToServer")} port={port}");

            localServer = null;
            if (isLocal)
            {
                // S1: the local simulation is only ever used when the gate says
                // so, so a stray ConnectToLocalServer cannot quietly bypass
                // the authoritative server.
                if (!NetworkAuthorityGate.IsLocalSimulation())
                {
                    Debug.LogWarning("[MatchRUDPServerNetworkManager] Local connect requested but the authority gate is not in local mode.");
                    connected = false;
                    return;
                }

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

            if ((isLocal && localServer == null) || (!isLocal && networkClient == null))
            {
                connected = false;
                Debug.LogWarning("[MatchRUDPServerNetworkManager] Connection was not established because no transport is available.");
                return;
            }

            connected = true;
            PublishConnectionEvent(connectedSubject, "connected");
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

            try
            {
                dataReceivedSubject.OnNext(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchRUDPServerNetworkManager] RUDP subscriber failed: {ex}");
            }
        }

        private void OnNetworkClientMessage(JObject json)
        {
            if (json == null)
            {
                return;
            }

            OnServerProducedMessage(json);
        }

        private static void PublishConnectionEvent(ISubject<Unit> subject, string eventName)
        {
            try
            {
                subject.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchRUDPServerNetworkManager] {eventName} subscriber failed: {ex}");
            }
        }
    }
}
