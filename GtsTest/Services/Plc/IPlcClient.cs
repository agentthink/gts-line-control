using System;
using System.Collections.Generic;

namespace GtsTest.Services.Plc
{
    /// <summary>
    /// PLC 客户端统一接口
    /// 所有品牌 PLC 驱动都实现此接口
    /// </summary>
    public interface IPlcClient : IDisposable
    {
        /// <summary>配置</summary>
        PlcConfig Config { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>连接状态变化</summary>
        event EventHandler<bool>? ConnectionStateChanged;

        /// <summary>错误事件</summary>
        event EventHandler<string>? ErrorOccurred;

        // ============ 连接管理 ============
        bool Connect();
        void Disconnect();

        // ============ 读操作 ============
        bool ReadBool(string address, out bool value);
        bool ReadShort(string address, out short value);
        bool ReadUShort(string address, out ushort value);
        bool ReadInt(string address, out int value);
        bool ReadUInt(string address, out uint value);
        bool ReadFloat(string address, out float value);
        bool ReadDouble(string address, out double value);
        bool ReadString(string address, int length, out string value);
        bool ReadBytes(string address, int length, out byte[] value);

        // ============ 写操作 ============
        bool WriteBool(string address, bool value);
        bool WriteShort(string address, short value);
        bool WriteUShort(string address, ushort value);
        bool WriteInt(string address, int value);
        bool WriteUInt(string address, uint value);
        bool WriteFloat(string address, float value);
        bool WriteDouble(string address, double value);
        bool WriteString(string address, string value);
        bool WriteBytes(string address, byte[] value);

        // ============ 批量读（用于快速采集） ============
        /// <summary>
        /// 批量读取多个地址
        /// 地址格式：["DB1.DBD0", "M10.0", "I0.0"]
        /// </summary>
        Dictionary<string, object?> ReadMulti(string[] addresses);
    }
}