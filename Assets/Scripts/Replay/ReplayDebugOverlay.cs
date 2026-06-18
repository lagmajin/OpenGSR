using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// 簡易 replay UI。
    /// 収録開始/停止、保存、読み込み、再生を行う。
    /// </summary>
    public sealed class ReplayDebugOverlay : MonoBehaviour
    {
        [SerializeField] string replayFileName = string.Empty;
        [SerializeField] float playbackSpeed = 1f;

        ClientNetworkManager clientNetworkManager;
        NetworkReplayRecording loadedRecording;
        Coroutine playbackRoutine;
        bool isPlayingBack;

        void Awake()
        {
            clientNetworkManager = FindFirstObjectByType<ClientNetworkManager>();
        }

        void OnGUI()
        {
            EnsureClientNetworkManager();
            GUILayout.BeginArea(new Rect(10, 10, 320, 220), GUI.skin.box);
            GUILayout.Label("Replay Debug");
            GUILayout.Label($"Recording: {(NetworkReplayRecorder.IsRecording ? "ON" : "OFF")}");
            GUILayout.Label($"Playback: {(isPlayingBack ? "ON" : "OFF")}");

            if (GUILayout.Button("Start Recording"))
            {
                NetworkReplayRecorder.StartRecording("client");
            }

            if (GUILayout.Button("Stop & Save"))
            {
                StopRecordingAndSave();
            }

            if (GUILayout.Button("Load Replay"))
            {
                LoadReplay();
            }

            if (GUILayout.Button("Play Loaded Replay"))
            {
                StartPlayback();
            }

            if (GUILayout.Button("Stop Playback"))
            {
                StopPlayback();
            }

            GUILayout.EndArea();
        }

        void StopRecordingAndSave()
        {
            var recording = NetworkReplayRecorder.StopRecording();
            if (recording == null)
            {
                return;
            }

            var path = ResolveReplayPath();
            NetworkReplayFileStore.Save(path, recording);
            Debug.Log($"[ReplayDebugOverlay] Saved replay: {path}");
        }

        void LoadReplay()
        {
            var path = ResolveReplayPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ReplayDebugOverlay] Replay file not found: {path}");
                return;
            }

            loadedRecording = NetworkReplayFileStore.Load(path);
            Debug.Log($"[ReplayDebugOverlay] Loaded replay: {path}, frames={loadedRecording.frames.Count}");
        }

        void StartPlayback()
        {
            EnsureClientNetworkManager();
            if (loadedRecording == null)
            {
                LoadReplay();
            }

            if (loadedRecording == null)
            {
                return;
            }

            StopPlayback();
            playbackRoutine = StartCoroutine(PlaybackCoroutine(loadedRecording));
        }

        void StopPlayback()
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            isPlayingBack = false;
        }

        void EnsureClientNetworkManager()
        {
            if (clientNetworkManager == null)
            {
                clientNetworkManager = FindFirstObjectByType<ClientNetworkManager>();
            }
        }

        IEnumerator PlaybackCoroutine(NetworkReplayRecording recording)
        {
            isPlayingBack = true;
            var previousElapsed = 0L;

            foreach (var frame in recording.frames)
            {
                var waitMs = frame.elapsedMs - previousElapsed;
                if (waitMs < 0L)
                {
                    waitMs = 0L;
                }
                previousElapsed = frame.elapsedMs;

                if (waitMs > 0)
                {
                    yield return new WaitForSecondsRealtime(waitMs / 1000f / Mathf.Max(0.01f, playbackSpeed));
                }

                if (clientNetworkManager != null && frame.message != null)
                {
                    clientNetworkManager.ReplayUdpMessage((JObject)frame.message.DeepClone());
                }
            }

            isPlayingBack = false;
            playbackRoutine = null;
        }

        string ResolveReplayPath()
        {
            if (string.IsNullOrWhiteSpace(replayFileName))
            {
                return NetworkReplayFileStore.BuildDefaultClientReplayPath();
            }

            if (Path.IsPathRooted(replayFileName))
            {
                return replayFileName;
            }

            return NetworkReplayFileStore.BuildDefaultClientReplayPath(replayFileName);
        }
    }
}
