using System;

namespace GtsTest.Modbus
{
    public class ModbusConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string? ErrorMessage { get; }

        // 构造函数1：只有状态（连接成功时用）
        public ModbusConnectionEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
            ErrorMessage = null;
        }

        // 构造函数2：状态 + 错误信息（连接失败时用）
        public ModbusConnectionEventArgs(bool isConnected, string? errorMessage)
        {
            IsConnected = isConnected;
            ErrorMessage = errorMessage;
        }
    }
}