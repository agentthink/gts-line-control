using System;
using System.Threading;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// 报文总线单例：环形缓冲 + 节流批量通知
    /// </summary>
    public class FrameMonitorHub : IFrameMonitorHub, IDisposable
    {
        private static FrameMonitorHub? _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>全局单例（懒加载）</summary>
        public static FrameMonitorHub Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new FrameMonitorHub(50000);
                    }
                }
                return _instance;
            }
        }

        private readonly RingBuffer<FrameLogEntry> _buffer;
        private readonly FrameStats _stats = new FrameStats();
        private readonly object _statsLock = new object();
        private long _seq = 0;

        // 节流通知（★ 明确使用 System.Threading.Timer）
        private readonly System.Threading.Timer _notifyTimer;
        private volatile bool _dirty = false;
        private readonly object _notifyLock = new object();

        public event Action? FramesUpdated;

        public FrameMonitorHub(int capacity = 50000)
        {
            _buffer = new RingBuffer<FrameLogEntry>(capacity);

            // 100ms 检查一次，如果 dirty 就触发一次通知
            _notifyTimer = new System.Threading.Timer(OnNotifyTimer, null, 100, 100);
        }

        private void OnNotifyTimer(object? state)
        {
            if (!_dirty) return;

            lock (_notifyLock)
            {
                if (!_dirty) return;
                _dirty = false;
            }

            try { FramesUpdated?.Invoke(); }
            catch { /* 订阅者异常不影响主流程 */ }
        }

        public void Publish(FrameLogEntry entry)
        {
            if (entry == null) return;

            try
            {
                entry.Seq = Interlocked.Increment(ref _seq);
                if (entry.Timestamp == default) entry.Timestamp = DateTime.Now;

                // 写入环形缓冲
                _buffer.Add(entry);

                // 更新统计
                lock (_statsLock)
                {
                    switch (entry.Protocol)
                    {
                        case FrameProtocol.ModbusTcp: _stats.ModbusTcpCount++; break;
                        case FrameProtocol.ModbusRtu: _stats.ModbusRtuCount++; break;
                        case FrameProtocol.OpcUa: _stats.OpcUaCount++; break;
                        case FrameProtocol.Mqtt: _stats.MqttCount++; break;
                    }

                    if (entry.IsError || entry.Direction == FrameDirection.Error)
                        _stats.TotalError++;
                    else if (entry.Direction == FrameDirection.TX)
                        _stats.TotalTx++;
                    else if (entry.Direction == FrameDirection.RX)
                        _stats.TotalRx++;
                }

                // 标记 dirty（下一次 Timer 触发时通知 UI）
                _dirty = true;
            }
            catch { /* 绝不抛异常打断业务 */ }
        }

        public void Clear()
        {
            _buffer.Clear();
            ResetStats();
            _dirty = true;
        }

        public FrameLogEntry[] Snapshot(int maxCount = 0)
        {
            return _buffer.Snapshot(maxCount);
        }

        public FrameStats GetStats()
        {
            lock (_statsLock)
            {
                return new FrameStats
                {
                    TotalTx = _stats.TotalTx,
                    TotalRx = _stats.TotalRx,
                    TotalError = _stats.TotalError,
                    ModbusTcpCount = _stats.ModbusTcpCount,
                    ModbusRtuCount = _stats.ModbusRtuCount,
                    OpcUaCount = _stats.OpcUaCount,
                    MqttCount = _stats.MqttCount,
                    LastResetTime = _stats.LastResetTime
                };
            }
        }

        public void ResetStats()
        {
            lock (_statsLock) _stats.Reset();
        }

        public void Dispose()
        {
            try { _notifyTimer?.Dispose(); } catch { }
        }
    }
}