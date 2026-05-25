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
        public static OnlineLoadingManager Instance { get; } = new OnlineLoadingManager();

        private readonly Dictionary<string, LoadingGauge> gaugeList = new();
        private string loadingMessage = string.Empty;

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
            gaugeList[id].Gauge = gauge;
        }

        public Gauge GetGauge(in string id)
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

            UnityEngine.Debug.LogWarning($"[OnlineLoadingManager] UI Gauge is not bound for id: {id}");
            return null;
        }

        public LoadingGauge GetLoadingGauge(in string id)
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

        public void SetLoadingMessage(in string message)
        {
            loadingMessage = message ?? string.Empty;
        }

        public void MarkPlayerLoaded(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            AddLoadingPlayer(id);
            gaugeList[id].Gauge = 1f;
        }
    }
}
