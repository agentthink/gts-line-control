using GtsTest.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GtsTest.Services.Plc
{
    /// <summary>
    /// 模拟 PLC 客户端（无硬件也能测试）
    /// 数据存在内存字典里，读写操作直接命中
    /// </summary>
    public class SimulatedPlcClient : IPlcClient
    {
        private readonly ConcurrentDictionary<string, object?> _memory = new();
        private volatile bool _isConnected;
        private volatile bool _disposed;

        public PlcConfig Config { get; }
        public bool IsConnected => _isConnected;

        public event EventHandler<bool>? ConnectionStateChanged;
        public event EventHandler<string>? ErrorOccurred;

        public SimulatedPlcClient(PlcConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool Connect()
        {
            if (_disposed) return false;
            _isConnected = true;
            ConnectionStateChanged?.Invoke(this, true);
            AppLogger.Info($"✅ 模拟 PLC [{Config.Name}] 已连接", "PLC");
            return true;
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
            AppLogger.Info($"🔌 模拟 PLC [{Config.Name}] 已断开", "PLC");
        }

        public bool ReadBool(string address, out bool value)
        {
            value = _memory.TryGetValue(address, out var v) && v is bool b && b;
            return true;
        }

        public bool ReadShort(string address, out short value)
        {
            value = _memory.TryGetValue(address, out var v) && v is short s ? s : (short)0;
            return true;
        }

        public bool ReadUShort(string address, out ushort value)
        {
            value = _memory.TryGetValue(address, out var v) && v is ushort s ? s : (ushort)0;
            return true;
        }

        public bool ReadInt(string address, out int value)
        {
            value = _memory.TryGetValue(address, out var v) && v is int i ? i : 0;
            return true;
        }

        public bool ReadUInt(string address, out uint value)
        {
            value = _memory.TryGetValue(address, out var v) && v is uint u ? u : 0u;
            return true;
        }

        public bool ReadFloat(string address, out float value)
        {
            value = _memory.TryGetValue(address, out var v) && v is float f ? f : 0f;
            return true;
        }

        public bool ReadDouble(string address, out double value)
        {
            value = _memory.TryGetValue(address, out var v) && v is double d ? d : 0d;
            return true;
        }

        public bool ReadString(string address, int length, out string value)
        {
            value = _memory.TryGetValue(address, out var v) ? v?.ToString() ?? "" : "";
            return true;
        }

        public bool ReadBytes(string address, int length, out byte[] value)
        {
            value = _memory.TryGetValue(address, out var v) && v is byte[] b ? b : new byte[length];
            return true;
        }

        public bool WriteBool(string address, bool value) { _memory[address] = value; return true; }
        public bool WriteShort(string address, short value) { _memory[address] = value; return true; }
        public bool WriteUShort(string address, ushort value) { _memory[address] = value; return true; }
        public bool WriteInt(string address, int value) { _memory[address] = value; return true; }
        public bool WriteUInt(string address, uint value) { _memory[address] = value; return true; }
        public bool WriteFloat(string address, float value) { _memory[address] = value; return true; }
        public bool WriteDouble(string address, double value) { _memory[address] = value; return true; }
        public bool WriteString(string address, string value) { _memory[address] = value; return true; }
        public bool WriteBytes(string address, byte[] value) { _memory[address] = value; return true; }

        public Dictionary<string, object?> ReadMulti(string[] addresses)
        {
            var result = new Dictionary<string, object?>();
            foreach (var addr in addresses)
            {
                result[addr] = _memory.TryGetValue(addr, out var v) ? v : null;
            }
            return result;
        }

        /// <summary>预置一些测试数据</summary>
        public void SeedTestData()
        {
            _memory["M0.0"] = true;
            _memory["M0.1"] = false;
            _memory["MW10"] = (short)1234;
            _memory["MD20"] = (int)123456;
            _memory["DB1.DBW0"] = (short)100;
            _memory["DB1.DBD2"] = (float)3.14f;
            _memory["DB1.DBD6"] = 20.5f;   // 模拟直径
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}