using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    [Serializable]
    public sealed class ServerReplayEntry
    {
        public long elapsedMs;
        public string direction = string.Empty;
        public string messageType = string.Empty;
        public JObject message = new JObject();
    }

    /// <summary>
    /// Local test server 用の軽量 replay 記録。
    /// 受信した入力と返したイベントを JSONL で保存する。
    /// </summary>
    public sealed class ServerReplayTape
    {
        readonly List<ServerReplayEntry> entries = new List<ServerReplayEntry>(1024);
        readonly Stopwatch stopwatch = new Stopwatch();
        bool recording;

        public void StartRecording()
        {
            entries.Clear();
            stopwatch.Restart();
            recording = true;
        }

        public void StopRecording()
        {
            recording = false;
            stopwatch.Stop();
        }

        public void RecordInbound(JObject message)
        {
            Record("in", message);
        }

        public void RecordOutbound(JObject message)
        {
            Record("out", message);
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream);

            writer.WriteLine(new JObject
            {
                ["format"] = "OpenGSServerReplay",
                ["formatVersion"] = 1,
                ["recordedAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["entryCount"] = entries.Count
            }.ToString(Formatting.None));

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                writer.WriteLine(new JObject
                {
                    ["elapsedMs"] = entry.elapsedMs,
                    ["direction"] = entry.direction,
                    ["messageType"] = entry.messageType,
                    ["message"] = entry.message
                }.ToString(Formatting.None));
            }
        }

        void Record(string direction, JObject message)
        {
            if (!recording || message == null)
            {
                return;
            }

            entries.Add(new ServerReplayEntry
            {
                elapsedMs = stopwatch.ElapsedMilliseconds,
                direction = direction,
                messageType = message.GetStringOrNull("MessageType") ?? string.Empty,
                message = (JObject)message.DeepClone()
            });
        }
    }
}
