using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    /// <summary>
    /// ローカルテストサーバー用 replay 記録。
    /// 受信した入力と返したイベントを両方記録する。
    /// </summary>
    public sealed class ServerReplayTape
    {
        readonly List<NetworkReplayFrame> frames = new List<NetworkReplayFrame>(1024);
        readonly Stopwatch stopwatch = new Stopwatch();
        bool recording;

        public void StartRecording()
        {
            frames.Clear();
            stopwatch.Restart();
            recording = true;
        }

        public NetworkReplayRecording StopRecording(string source, string sceneName)
        {
            recording = false;
            stopwatch.Stop();
            return new NetworkReplayRecording
            {
                source = source,
                sceneName = sceneName,
                gameVersion = UnityEngine.Application.version,
                recordedAtUtc = DateTime.UtcNow.ToString("O"),
                frames = new List<NetworkReplayFrame>(frames)
            };
        }

        public void RecordInbound(JObject message)
        {
            Record("in", message);
        }

        public void RecordOutbound(JObject message)
        {
            Record("out", message);
        }

        public void Save(string filePath, string source, string sceneName)
        {
            ReplayFileStore.Save(filePath, StopRecording(source, sceneName));
        }

        void Record(string direction, JObject message)
        {
            if (!recording || message == null)
            {
                return;
            }

            frames.Add(new NetworkReplayFrame
            {
                elapsedMs = stopwatch.ElapsedMilliseconds,
                direction = direction,
                messageType = message.Value<string>("MessageType") ?? string.Empty,
                message = (JObject)message.DeepClone()
            });
        }
    }
}
