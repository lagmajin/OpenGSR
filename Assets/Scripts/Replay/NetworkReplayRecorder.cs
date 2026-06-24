using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    [Serializable]
    public sealed class NetworkReplayFrame
    {
        public long elapsedMs;
        public string direction = string.Empty;
        public string messageType = string.Empty;
        public JObject message = new JObject();
    }

    [Serializable]
    public sealed class NetworkReplayRecording
    {
        public int formatVersion = 1;
        public string source = "client";
        public string gameVersion = string.Empty;
        public string sceneName = string.Empty;
        public string recordedAtUtc = string.Empty;
        public System.Collections.Generic.List<NetworkReplayFrame> frames = new System.Collections.Generic.List<NetworkReplayFrame>();
    }

    /// <summary>
    /// クライアント側の受信 UDP を記録する軽量レコーダー。
    /// </summary>
    public static class NetworkReplayRecorder
    {
        static readonly object Locker = new object();
        static readonly Stopwatch Stopwatch = new Stopwatch();
        static NetworkReplayRecording currentRecording;

        public static bool IsRecording
        {
            get
            {
                lock (Locker)
                {
                    return currentRecording != null;
                }
            }
        }

        public static NetworkReplayRecording CurrentRecording
        {
            get
            {
                lock (Locker)
                {
                    return currentRecording;
                }
            }
        }

        public static void StartRecording(string source = "client", string sceneName = null, string gameVersion = null)
        {
            lock (Locker)
            {
                currentRecording = new NetworkReplayRecording
                {
                    source = source ?? "client",
                    sceneName = sceneName ?? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    gameVersion = gameVersion ?? Application.version,
                    recordedAtUtc = DateTime.UtcNow.ToString("O"),
                    frames = new System.Collections.Generic.List<NetworkReplayFrame>()
                };
                Stopwatch.Restart();
            }
        }

        public static NetworkReplayRecording StopRecording()
        {
            lock (Locker)
            {
                Stopwatch.Stop();
                var recording = currentRecording;
                currentRecording = null;
                return recording;
            }
        }

        public static void RecordIncoming(JObject message)
        {
            Record("in", message);
        }

        public static void RecordOutgoing(JObject message)
        {
            Record("out", message);
        }

        public static void SaveLatest(string filePath)
        {
            var recording = CurrentRecording;
            if (recording == null)
            {
                throw new InvalidOperationException("No active replay recording.");
            }

            ReplayFileStore.Save(filePath, recording);
        }

        public static NetworkReplayRecording Load(string filePath)
        {
            return ReplayFileStore.LoadNetworkReplay(filePath);
        }

        static void Record(string direction, JObject message)
        {
            if (message == null)
            {
                return;
            }

            lock (Locker)
            {
                if (currentRecording == null)
                {
                    return;
                }

                currentRecording.frames.Add(new NetworkReplayFrame
                {
                    elapsedMs = Stopwatch.ElapsedMilliseconds,
                    direction = direction,
                    messageType = message.Value<string>("MessageType") ?? string.Empty,
                    message = (JObject)message.DeepClone()
                });
            }
        }
    }
}
