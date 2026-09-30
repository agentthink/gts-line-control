using System;

namespace GtsTest.Core
{
    public interface IDeviceStateMachine
    {
        DeviceState CurrentState { get; }
        event EventHandler<DeviceStateChangedEventArgs>? StateChanged;

        /// <summary>尝试合法转换，非法转换返回 false</summary>
        bool TryTransition(DeviceState target, string reason = "");

        /// <summary>强制设置状态（用于异常恢复 / 急停）</summary>
        void ForceSet(DeviceState state, string reason = "");
    }
}