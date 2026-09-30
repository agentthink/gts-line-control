using System;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Core
{
    /// <summary>
    /// 工业级看门狗
    /// </summary>
    public class Watchdog : IDisposable
    {
        private readonly int _timeoutMs;
        private readonly int _checkIntervalMs;
        private readonly object _lock = new object();
        private DateTime _lastFeedTime;
        private CancellationTokenSource _cts;
        private Task _monitorTask;
        private bool _isRunning;
        private bool _disposed;

        /// <summary>
        /// 看门狗超时回调（可由外部设置）
        /// </summary>
        public Action OnTimeout { get; set; }

        /// <summary>
        /// 看门狗是否已触发（超时）
        /// </summary>
        public bool IsTimeout { get; private set; }

        /// <summary>
        /// 上次喂狗时间
        /// </summary>
        public DateTime LastFeedTime => _lastFeedTime;

        /// <summary>
        /// 距离超时剩余毫秒数（负数表示已超时）
        /// </summary>
        public int RemainingMs
        {
            get
            {
                lock (_lock)
                {
                    var elapsed = (DateTime.Now - _lastFeedTime).TotalMilliseconds;
                    return (int)(_timeoutMs - elapsed);
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Watchdog(int timeoutMs = 6000, int checkIntervalMs = 200, Action onTimeout = null)
        {
            _timeoutMs = timeoutMs;
            _checkIntervalMs = checkIntervalMs;
            OnTimeout = onTimeout;
            _lastFeedTime = DateTime.Now;
            IsTimeout = false;
        }

        /// <summary>
        /// 喂狗（重置计时器）
        /// </summary>
        public void Feed()
        {
            lock (_lock)
            {
                _lastFeedTime = DateTime.Now;
                IsTimeout = false;
            }
        }

        /// <summary>
        /// 启动看门狗监控
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Feed(); // 启动时立即喂狗，确保从零开始计时
            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token));
        }

        /// <summary>
        /// 停止看门狗
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            try { _monitorTask?.Wait(500); } catch { }
        }

        private void MonitorLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                Thread.Sleep(_checkIntervalMs);

                bool shouldTimeout = false;
                lock (_lock)
                {
                    var elapsed = (DateTime.Now - _lastFeedTime).TotalMilliseconds;
                    if (elapsed > _timeoutMs && !IsTimeout)
                    {
                        IsTimeout = true;
                        shouldTimeout = true;
                    }
                }

                if (shouldTimeout)
                {
                    OnTimeout?.Invoke();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }
    }
}