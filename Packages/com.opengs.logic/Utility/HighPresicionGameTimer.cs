using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class HighPrecisionGameTimer
{
    private readonly int _intervalMilliseconds;
    private bool _isRunning;

    // イベント: 毎回のタイムイベント
    public event Action OnTick;

    // コンストラクタ
    public HighPrecisionGameTimer(int intervalMilliseconds)
    {
        if (intervalMilliseconds <= 0)
        {
            throw new ArgumentException("Interval must be greater than 0 milliseconds.", nameof(intervalMilliseconds));
        }
        _intervalMilliseconds = intervalMilliseconds;
    }

    // タイマーを開始
    public async void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        var stopwatch = Stopwatch.StartNew();

        while (_isRunning)
        {
            // 次のタイミングでタスクを発火させる
            var elapsed = stopwatch.ElapsedMilliseconds;
            var nextTickTime = ((elapsed / _intervalMilliseconds) + 1) * _intervalMilliseconds;

            // 必要な遅延時間を計算し、次のイベントまで待機
            var delay = nextTickTime - elapsed;
            if (delay > 0)
            {
                await Task.Delay((int)delay); // 高精度に待機
            }

            // イベントを発火
            OnTick?.Invoke();
        }
    }

    // タイマーを停止
    public void Stop()
    {
        _isRunning = false;
    }
}
