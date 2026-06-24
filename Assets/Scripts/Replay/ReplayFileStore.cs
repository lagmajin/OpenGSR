using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenGS
{
    public static class ReplayFileStore
    {
        static readonly byte[] Magic = Encoding.ASCII.GetBytes("OGSRRPL1");
        static readonly string NetworkMagic = "OpenGSReplay";

        public static void Save(string filePath, ReplayRecording recording)
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

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, false);

            writer.Write(Magic);
            writer.Write(recording.formatVersion);
            writer.Write(recording.gameVersion ?? string.Empty);
            writer.Write(recording.mapId ?? string.Empty);
            writer.Write(recording.seed);
            writer.Write(recording.fixedDeltaTime);

            var frames = recording.frames ?? Array.Empty<ReplayFrame>();
            writer.Write(frames.Length);
            for (var i = 0; i < frames.Length; i++)
            {
                WriteFrame(writer, frames[i]);
            }
        }

        public static ReplayRecording Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);

            var magic = reader.ReadBytes(Magic.Length);
            if (magic.Length != Magic.Length)
            {
                throw new InvalidDataException("Replay file is too short.");
            }

            for (var i = 0; i < Magic.Length; i++)
            {
                if (magic[i] != Magic[i])
                {
                    throw new InvalidDataException("Invalid replay file header.");
                }
            }

            var recording = new ReplayRecording
            {
                formatVersion = reader.ReadInt32(),
                gameVersion = reader.ReadString(),
                mapId = reader.ReadString(),
                seed = reader.ReadInt32(),
                fixedDeltaTime = reader.ReadSingle(),
            };

            var frameCount = reader.ReadInt32();
            if (frameCount < 0)
            {
                throw new InvalidDataException("Negative frame count.");
            }

            recording.frames = new ReplayFrame[frameCount];
            for (var i = 0; i < frameCount; i++)
            {
                recording.frames[i] = ReadFrame(reader);
            }

            return recording;
        }

        static void WriteFrame(BinaryWriter writer, ReplayFrame frame)
        {
            writer.Write(frame.tick);
            writer.Write(frame.aimWorldPosition.x);
            writer.Write(frame.aimWorldPosition.y);
            writer.Write(frame.horizontal);
            writer.Write(frame.vertical);
            writer.Write(frame.firePressed);
            writer.Write(frame.fireJustPressed);
            writer.Write(frame.reloadJustPressed);
            writer.Write(frame.swapWeaponJustPressed);
            writer.Write(frame.dropWeaponJustPressed);
            writer.Write(frame.jumpJustPressed);
            writer.Write(frame.sitJustPressed);
            writer.Write(frame.lieDownJustPressed);
            writer.Write(frame.instantItemSlotJustPressed);
            writer.Write(frame.jumpPressed);
            writer.Write(frame.boosterPressed);
        }

        static ReplayFrame ReadFrame(BinaryReader reader)
        {
            return new ReplayFrame
            {
                tick = reader.ReadInt32(),
                aimWorldPosition = new UnityEngine.Vector2(reader.ReadSingle(), reader.ReadSingle()),
                horizontal = reader.ReadSingle(),
                vertical = reader.ReadSingle(),
                firePressed = reader.ReadBoolean(),
                fireJustPressed = reader.ReadBoolean(),
                reloadJustPressed = reader.ReadBoolean(),
                swapWeaponJustPressed = reader.ReadBoolean(),
                dropWeaponJustPressed = reader.ReadBoolean(),
                jumpJustPressed = reader.ReadBoolean(),
                sitJustPressed = reader.ReadBoolean(),
                lieDownJustPressed = reader.ReadBoolean(),
                instantItemSlotJustPressed = reader.ReadInt32(),
                jumpPressed = reader.ReadBoolean(),
                boosterPressed = reader.ReadBoolean(),
            };
        }

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
                ["magic"] = NetworkMagic,
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

            foreach (var frame in recording.frames)
            {
                writer.WriteLine(new JObject
                {
                    ["elapsedMs"] = frame.elapsedMs,
                    ["direction"] = frame.direction,
                    ["messageType"] = frame.messageType,
                    ["message"] = frame.message
                }.ToString(Formatting.None));
            }
        }

        public static NetworkReplayRecording LoadNetworkReplay(string filePath)
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
            if (!string.Equals(header.Value<string>("magic"), NetworkMagic, StringComparison.Ordinal))
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
