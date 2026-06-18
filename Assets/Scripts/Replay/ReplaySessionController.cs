using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace OpenGS
{
    public sealed class ReplaySessionController : MonoBehaviour
    {
        [SerializeField] string replayFileName = "replays/latest.replay";
        [SerializeField] string gameVersion = "dev";
        [SerializeField] int seed;
        [SerializeField] bool autoRecord;
        [SerializeField] bool autoPlayback;

        [Inject] ReplayInputService replayInputService;

        ReplaySession Session => replayInputService.Session;

        void Start()
        {
            if (replayInputService == null)
            {
                Debug.LogWarning("[ReplaySessionController] ReplayInputService was not injected.");
                enabled = false;
                return;
            }

            if (autoPlayback)
            {
                LoadReplayNow();
            }

            if (autoRecord)
            {
                StartRecordingNow();
            }
        }

        [ContextMenu("Start Recording")]
        public void StartRecordingNow()
        {
            Session.StartRecording(Time.fixedDeltaTime, gameVersion, SceneManager.GetActiveScene().name, seed);
        }

        [ContextMenu("Stop Recording And Save")]
        public void StopRecordingAndSave()
        {
            var recording = Session.StopRecording();
            if (recording == null)
            {
                return;
            }

            ReplayFileStore.Save(GetReplayPath(), recording);
        }

        [ContextMenu("Load Replay Now")]
        public void LoadReplayNow()
        {
            var path = GetReplayPath();
            if (!File.Exists(path))
            {
                return;
            }

            Session.StartPlayback(ReplayFileStore.Load(path));
        }

        [ContextMenu("Stop Playback")]
        public void StopPlayback()
        {
            Session.StopPlayback();
        }

        string GetReplayPath()
        {
            return Path.Combine(Application.persistentDataPath, replayFileName);
        }
    }
}
