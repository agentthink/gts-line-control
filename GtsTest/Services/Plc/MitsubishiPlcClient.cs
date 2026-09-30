using HslCommunication;
using HslCommunication.Profinet.Melsec;
using GtsTest.Core;
using System;
using System.Collections.Generic;

namespace GtsTest.Services.Plc
{
    /// <summary>
    /// 三菱 MC 协议客户端（基于 HslCommunication）
    /// </summary>
    public class MitsubishiPlcClient : IPlcClient
    {
        private MelsecMcNet? _plc;
        private volatile bool _isConnected;
        private volatile bool _disposed;

        public PlcConfig Config { get; }
        public bool IsConnected => _isConnected;

        public event EventHandler<bool>? ConnectionStateChanged;
        public event EventHandler<string>? ErrorOccurred;

        public MitsubishiPlcClient(PlcConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool Connect()
        {
            if (_disposed) return false;
            try
            {
                _plc = new MelsecMcNet(Config.IpAddress, Config.Port)
                {
                    ConnectTimeOut = Config.ConnectTimeoutMs,
                    ReceiveTimeOut = Config.ReadWriteTimeoutMs
                };
                var result = _plc.ConnectServer();
                _isConnected = result.IsSuccess;
                ConnectionStateChanged?.Invoke(this, _isConnected);

                if (_isConnected)
                    AppLogger.Info($"✅ 三菱 PLC [{Config.Name}] 连接成功: {Config.IpAddress}", "PLC");
                else
                    AppLogger.Warn($"⚠️ 三菱 PLC [{Config.Name}] 连接失败: {result.Message}", "PLC");

                return _isConnected;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ErrorOccurred?.Invoke(this, ex.Message);
                AppLogger.Error($"❌ 三菱 PLC [{Config.Name}] 连接异常: {ex.Message}", "PLC");
                return false;
            }
        }

        public void Disconnect()
        {
            _plc?.ConnectClose();
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
        }

        public bool ReadBool(string address, out bool value)
        {
            var result = _plc!.ReadBool(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadShort(string address, out short value)
        {
            var result = _plc!.ReadInt16(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadUShort(string address, out ushort value)
        {
            var result = _plc!.ReadUInt16(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadInt(string address, out int value)
        {
            var result = _plc!.ReadInt32(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadUInt(string address, out uint value)
        {
            var result = _plc!.ReadUInt32(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadFloat(string address, out float value)
        {
            var result = _plc!.ReadFloat(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadDouble(string address, out double value)
        {
            var result = _plc!.ReadDouble(address);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadString(string address, int length, out string value)
        {
            var result = _plc!.ReadString(address, (ushort)length);
            value = result.Content;
            return result.IsSuccess;
        }

        public bool ReadBytes(string address, int length, out byte[] value)
        {
            var result = _plc!.Read(address, (ushort)length);
            value = result.Content ?? Array.Empty<byte>();
            return result.IsSuccess;
        }

        public bool WriteBool(string address, bool value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteShort(string address, short value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteUShort(string address, ushort value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteInt(string address, int value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteUInt(string address, uint value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteFloat(string address, float value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteDouble(string address, double value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteString(string address, string value) => _plc!.Write(address, value).IsSuccess;
        public bool WriteBytes(string address, byte[] value) => _plc!.Write(address, value).IsSuccess;

        public Dictionary<string, object?> ReadMulti(string[] addresses)
        {
            var results = new Dictionary<string, object?>();
            foreach (var addr in addresses)
            {
                // HslCommunication 针对连续地址有优化接口，这里为演示简洁采用逐个读取
                results[addr] = _plc!.ReadInt32(addr).Content;
            }
            return results;
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