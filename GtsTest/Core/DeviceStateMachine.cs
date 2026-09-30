using System;
using System.Collections.Generic;

namespace GtsTest.Core
{
    /// <summary>
    /// 轻量状态机：只负责状态转换合法性校验 + 事件通知
    /// 业务逻辑（怎么响应）由订阅者处理
    /// 未来可平滑替换为 Stateless 库
    /// </summary>
    public class DeviceStateMachine : IDeviceStateMachine
    {
        private readonly object _lock = new object();
        private DeviceState _current;

        // 合法转换表：从 → 允许到达的状态集合
        private static readonly Dictionary<DeviceState, HashSet<DeviceState>> _allowed = new()
        {
            [DeviceState.Idle] = new()
            {
                DeviceState.Running, DeviceState.Error,
                DeviceState.EmergencyStop, DeviceState.Disconnected
            },
            [DeviceState.Running] = new()
            {
                DeviceState.Idle, DeviceState.Paused, DeviceState.Error,
                DeviceState.EmergencyStop, DeviceState.Disconnected
            },
            [DeviceState.Paused] = new()
            {
                DeviceState.Running, DeviceState.Idle, DeviceState.Error,
                DeviceState.EmergencyStop, DeviceState.Disconnected
            },
            [DeviceState.Error] = new()
            {
                DeviceState.Idle, DeviceState.Paused,
                DeviceState.EmergencyStop, DeviceState.Disconnected
            },
            [DeviceState.EmergencyStop] = new()
            {
                DeviceState.Idle, DeviceState.Disconnected
            },
            [DeviceState.Disconnected] = new()
            {
                DeviceState.Idle, DeviceState.Error
            },
        };

        public DeviceState CurrentState { get { lock (_lock) return _current; } }
        public event EventHandler<DeviceStateChangedEventArgs>? StateChanged;

        public DeviceStateMachine(DeviceState initial = DeviceState.Idle)
        {
            _current = initial;
        }

        public bool TryTransition(DeviceState target, string reason = "")
        {
            DeviceState old;
            lock (_lock)
            {
                if (_current == target) return false;
                if (!_allowed.TryGetValue(_current, out var allowed) || !allowed.Contains(target))
                    return false;
                old = _current;
                _current = target;
            }
            RaiseChanged(old, target, reason);
            return true;
        }

        public void ForceSet(DeviceState state, string reason = "")
        {
            DeviceState old;
            lock (_lock)
            {
                if (_current == state) return;
                old = _current;
                _current = state;
            }
            RaiseChanged(old, state, reason);
        }

        private void RaiseChanged(DeviceState oldState, DeviceState newState, string reason)
        {
            try { StateChanged?.Invoke(this, new DeviceStateChangedEventArgs(oldState, newState, reason)); }
            catch (Exception ex) { AppLogger.Warn($"状态机事件处理异常: {ex.Message}", "StateMachine"); }
        }
    }
}