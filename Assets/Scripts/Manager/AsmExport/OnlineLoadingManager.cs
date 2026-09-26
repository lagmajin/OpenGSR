#nullable enable
using System;
using System.Collections.Generic;
using OpenGSCore;

namespace OpenGS
{
    public class LoadingInfo
    {
        public string MapName { get; set; } = string.Empty;
        public EGameMode GameMode { get; set; }
    }

    public class OnlineLoadingManager
    {
        private static OnlineLoadingManager sharedInstance = new OnlineLoadingManager();

        /// <summary>
        /// Returns the DI-owned instance after startup, with a lightweight fallback before DI is ready.
        /// </summary>
        public static OnlineLoadingManager Instance => sharedInstance;

        internal static void SetSharedInstance(OnlineLoadingManager instance)
        {
            if (instance != null)
            {
                sharedInstance = instance;
            }
        }

        internal static void ResetSharedInstance()
        {
            sharedInstance = new OnlineLoadingManager();
        }

        private readonly Dictionary<string, LoadingGauge> gaugeList = new();
        private string loadingMessage = string.Empty;

        public event Action<string, float>? LoadingProgressUpdated;
        public event Action<string>? LoadingMessageUpdated;
        public event Action<LoadingInfo>? LoadingInfoUpdated;

        public LoadingInfo LoadingInfo { get; set; } = new();
        public string LoadingMessage => loadingMessage;

        public OnlineLoadingManager()
        {
        }

        public void AddLoadingPlayer(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (!gaugeList.ContainsKey(id))
            {
                gaugeList.Add(id, new LoadingGauge());
            }
        }

        public void UpdateLoading(in string id, float gauge)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            AddLoadingPlayer(id);
            gaugeList[id].SetRatio(gauge);
            InvokeSafely(LoadingProgressUpdated, id, gaugeList[id].Gauge, nameof(LoadingProgressUpdated));
        }

        [Obsolete("UI-bound Gauge support is not implemented. Use GetLoadingGauge() for data access.")]
        public Gauge? GetGauge(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (!gaugeList.TryGetValue(id, out _))
            {
                UnityEngine.Debug.LogWarning($"[OnlineLoadingManager] Loading gauge not found for id: {id}");
                return null;
            }

            UnityEngine.Debug.LogWarning($"[OnlineLoadingManager] UI-bound Gauge is not implemented for id: {id}. Use GetLoadingGauge() for progress data.");
            return null;
        }

        public LoadingGauge? GetLoadingGauge(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                UnityEngine.Debug.LogWarning("[OnlineLoadingManager] id is empty.");
                return null;
            }

            if (gaugeList.TryGetValue(id, out var gauge))
            {
                return gauge;
            }

            UnityEngine.Debug.LogWarning($"[OnlineLoadingManager] LoadingGauge not found for id: {id}");
            return null;
        }

        public IReadOnlyDictionary<string, LoadingGauge> GetAllGauges()
        {
            return gaugeList;
        }

        public void Clear()
        {
            gaugeList.Clear();
            loadingMessage = string.Empty;
            LoadingInfo = new LoadingInfo();
        }

        public void SetLoadingInfo(string mapName, EGameMode gameMode)
        {
            LoadingInfo.MapName = mapName ?? string.Empty;
            LoadingInfo.GameMode = gameMode;
            InvokeSafely(LoadingInfoUpdated, LoadingInfo, nameof(LoadingInfoUpdated));
        }

        public void SetLoadingMessage(in string message)
        {
            loadingMessage = message ?? string.Empty;
            InvokeSafely(LoadingMessageUpdated, loadingMessage, nameof(LoadingMessageUpdated));
        }

        public void MarkPlayerLoaded(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            AddLoadingPlayer(id);
            gaugeList[id].Full();
            InvokeSafely(LoadingProgressUpdated, id, 1f, nameof(LoadingProgressUpdated));
        }

        private static void InvokeSafely(Action<string, float>? handlers, string id, float value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string, float> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(id, value);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[OnlineLoadingManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<string>? handlers, string value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[OnlineLoadingManager] {eventName} subscriber failed: {ex}");
                }
            }
        }

        private static void InvokeSafely(Action<LoadingInfo>? handlers, LoadingInfo value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<LoadingInfo> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[OnlineLoadingManager] {eventName} subscriber failed: {ex}");
                }
            }
        }
    }
}
