using System;

namespace GtsTest.Core
{
    /// <summary>
    /// 设备状态（替代 DeviceRuntime 中散落的 IsRunning/IsPaused/LastError 组合判断）
    /// </summary>
    public enum DeviceState
    {
        Idle,           // 空闲，等待启动
        Running,        // 正在执行工作流
        Paused,         // 暂停（错误后等待恢复）
        Error,          // 故障（需人工干预）
        EmergencyStop,  // 急停触发
        Disconnected    // Modbus 连接断开
    }

    /// <summary>状态变化事件参数</summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public DeviceState OldState { get; }
        public DeviceState NewState { get; }
        public string Reason { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public DeviceStateChangedEventArgs(DeviceState oldState, DeviceState newState, string reason)
        {
            OldState = oldState;
            NewState = newState;
            Reason = reason ?? "";
        }
    }
}