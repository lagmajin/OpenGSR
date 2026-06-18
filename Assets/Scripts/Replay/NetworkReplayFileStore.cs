using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    public static class NetworkReplayFileStore
    {
        const string Magic = "OpenGSReplay";

        public static void Save(string filePath, NetworkReplayRecording recording)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            if (recording == null)
            {
                throw new ArgumentNullException(nameof(recording));
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
                ["magic"] = Magic,
                ["formatVersion"] = recording.formatVersion,
                ["source"] = recording.source,
                ["gameVersion"] = recording.gameVersion,
                ["sceneName"] = recording.sceneName,
                ["recordedAtUtc"] = recording.recordedAtUtc,
                ["frameCount"] = recording.frames?.Count ?? 0
            }.ToString(Formatting.None));

            if (recording.frames == null)
            {
                return;
            }

            for (var i = 0; i < recording.frames.Count; i++)
            {
                var frame = recording.frames[i];
                writer.WriteLine(new JObject
                {
                    ["elapsedMs"] = frame.elapsedMs,
                    ["direction"] = frame.direction,
                    ["messageType"] = frame.messageType,
                    ["message"] = frame.message
                }.ToString(Formatting.None));
            }
        }

        public static NetworkReplayRecording Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new InvalidDataException("Replay file is empty.");
            }

            var header = JObject.Parse(headerLine);
            if (!string.Equals(header.Value<string>("magic"), Magic, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Invalid replay file header.");
            }

            var recording = new NetworkReplayRecording
            {
                formatVersion = header.Value<int?>("formatVersion") ?? 1,
                source = header.Value<string>("source") ?? "client",
                gameVersion = header.Value<string>("gameVersion") ?? string.Empty,
                sceneName = header.Value<string>("sceneName") ?? string.Empty,
                recordedAtUtc = header.Value<string>("recordedAtUtc") ?? string.Empty,
                frames = new System.Collections.Generic.List<NetworkReplayFrame>()
            };

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var frameJson = JObject.Parse(line);
                recording.frames.Add(new NetworkReplayFrame
                {
                    elapsedMs = frameJson.Value<long?>("elapsedMs") ?? 0L,
                    direction = frameJson.Value<string>("direction") ?? string.Empty,
                    messageType = frameJson.Value<string>("messageType") ?? string.Empty,
                    message = frameJson["message"] as JObject ?? new JObject()
                });
            }

            return recording;
        }

        public static string BuildDefaultClientReplayPath(string fileName = null)
        {
            var root = Path.Combine(UnityEngine.Application.persistentDataPath, "replays");
            Directory.CreateDirectory(root);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "latest.replay";
            }
            return Path.Combine(root, fileName);
        }

        public static string BuildDefaultServerReplayPath(string fileName = null)
        {
            var root = Path.Combine(UnityEngine.Application.persistentDataPath, "replays", "server");
            Directory.CreateDirectory(root);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"server-{DateTime.UtcNow:yyyyMMdd-HHmmss}.replay";
            }
            return Path.Combine(root, fileName);
        }
    }
}
