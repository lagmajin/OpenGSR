#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGSCore{

    public sealed class MatchLoadingTracker
    {
        private readonly Dictionary<PlayerID, PlayerLoadingInfo> _statuses = new();

        // UniRxの代わりに標準のActionイベントを使用
        public event Action<PlayerLoadingInfo>? OnProgressChanged;
        public event Action? OnAllCompleted;     

        public void AddPlayer(PlayerID playerId)
        {
            _statuses[playerId] = new PlayerLoadingInfo(playerId);
        }

        public void UpdateProgress(PlayerID playerId, int percent)
        {
            if (_statuses.TryGetValue(playerId, out var status))
            {
                var clamped = Math.Clamp(percent, 0, 100);
                status.LoadingProgress = clamped;

                // 進捗イベント発火
                OnProgressChanged?.Invoke(status);

                // 全員完了したら一度だけ通知
                if (_statuses.Values.All(s => s.IsCompleted))
                {
                    OnAllCompleted?.Invoke();
                }
            }
        }

        public IReadOnlyCollection<PlayerLoadingInfo> GetAllStatuses() => _statuses.Values;
    }
}
