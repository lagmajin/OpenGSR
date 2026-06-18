using System;
using System.Collections.Generic;

namespace OpenGS
{
    public sealed class ReplaySession
    {
        readonly List<ReplayFrame> recordedFrames = new List<ReplayFrame>(2048);

        ReplayRecording playbackRecording;
        ReplayFrame currentPlaybackFrame;
        int playbackIndex;
        bool hasCurrentPlaybackFrame;

        bool isRecording;
        float recordingFixedDeltaTime;
        string recordingGameVersion = string.Empty;
        string recordingMapId = string.Empty;
        int recordingSeed;

        public bool IsRecording => isRecording;
        public bool IsPlaying => playbackRecording != null;
        public bool HasCurrentPlaybackFrame => hasCurrentPlaybackFrame;
        public int PlaybackIndex => playbackIndex;

        public void StartRecording(float fixedDeltaTime, string gameVersion, string mapId, int seed)
        {
            recordedFrames.Clear();
            playbackRecording = null;
            hasCurrentPlaybackFrame = false;
            playbackIndex = 0;

            isRecording = true;
            recordingFixedDeltaTime = fixedDeltaTime;
            recordingGameVersion = gameVersion ?? string.Empty;
            recordingMapId = mapId ?? string.Empty;
            recordingSeed = seed;
        }

        public ReplayRecording StopRecording()
        {
            if (!isRecording)
            {
                return null;
            }

            isRecording = false;
            return new ReplayRecording
            {
                formatVersion = 1,
                gameVersion = recordingGameVersion,
                mapId = recordingMapId,
                seed = recordingSeed,
                fixedDeltaTime = recordingFixedDeltaTime,
                frames = recordedFrames.ToArray(),
            };
        }

        public void StartPlayback(ReplayRecording recording)
        {
            if (recording == null)
            {
                throw new ArgumentNullException(nameof(recording));
            }

            isRecording = false;
            playbackRecording = recording;
            playbackIndex = 0;
            hasCurrentPlaybackFrame = false;
            AdvancePlaybackFrame();
        }

        public void StopPlayback()
        {
            playbackRecording = null;
            hasCurrentPlaybackFrame = false;
            playbackIndex = 0;
        }

        public void CaptureFrame(ReplayFrame frame)
        {
            if (!isRecording)
            {
                return;
            }

            frame.tick = recordedFrames.Count;
            recordedFrames.Add(frame);
        }

        public bool AdvancePlaybackFrame()
        {
            if (playbackRecording == null)
            {
                hasCurrentPlaybackFrame = false;
                return false;
            }

            var frames = playbackRecording.frames ?? Array.Empty<ReplayFrame>();
            if (playbackIndex >= frames.Length)
            {
                hasCurrentPlaybackFrame = false;
                return false;
            }

            currentPlaybackFrame = frames[playbackIndex];
            currentPlaybackFrame.tick = playbackIndex;
            playbackIndex++;
            hasCurrentPlaybackFrame = true;
            return true;
        }

        public bool TryGetCurrentPlaybackFrame(out ReplayFrame frame)
        {
            if (!hasCurrentPlaybackFrame)
            {
                frame = default;
                return false;
            }

            frame = currentPlaybackFrame;
            return true;
        }

        public ReplayRecording CurrentRecording => playbackRecording;
    }
}
