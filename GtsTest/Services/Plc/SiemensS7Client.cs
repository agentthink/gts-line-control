using GtsTest.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ⭐ 用别名解决命名空间冲突
using S7Plc = S7.Net.Plc;
using S7CpuType = S7.Net.CpuType;
using S7DataType = S7.Net.DataType;

namespace GtsTest.Services.Plc
{
    /// <summary>
    /// 西门子 S7 PLC 客户端
    /// 地址格式参考：
    ///   DB1.DBX0.0    位
    ///   DB1.DBB0      字节
    ///   DB1.DBW0      字（16位）
    ///   DB1.DBD0      双字（32位）
    ///   M0.0          M区位
    ///   MB0           M区字节
    ///   MW0           M区字
    ///   I0.0 / Q0.0   输入/输出
    /// </summary>
    public class SiemensS7Client : IPlcClient
    {
        private readonly object _lock = new();
        private S7Plc? _plc;
        private volatile bool _isConnected;
        private volatile bool _disposed;
        private Task? _reconnectTask;

        public PlcConfig Config { get; }
        public bool IsConnected => _isConnected;

        public event EventHandler<bool>? ConnectionStateChanged;
        public event EventHandler<string>? ErrorOccurred;

        public SiemensS7Client(PlcConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // ============================================================
        // 连接管理
        // ============================================================
        public bool Connect()
        {
            lock (_lock)
            {
                if (_isConnected && _plc != null && _plc.IsConnected)
                    return true;
                if (_disposed) return false;

                try
                {
                    S7CpuType cpuType = Config.Type switch
                    {
                        PlcType.SiemensS1200 => S7CpuType.S71200,
                        PlcType.SiemensS1500 => S7CpuType.S71500,
                        PlcType.SiemensS300 => S7CpuType.S7300,
                        PlcType.SiemensS400 => S7CpuType.S7400,
                        PlcType.SiemensS200Smart => S7CpuType.S7200Smart,
                        _ => S7CpuType.S71200
                    };

                    _plc = new S7Plc(cpuType, Config.IpAddress, Config.Rack, Config.Slot);
                    _plc.ReadTimeout = Config.ReadWriteTimeoutMs;
                    _plc.WriteTimeout = Config.ReadWriteTimeoutMs;

                    _plc.Open();

                    if (_plc.IsConnected)
                    {
                        _isConnected = true;
                        ConnectionStateChanged?.Invoke(this, true);
                        AppLogger.Info($"✅ PLC [{Config.Name}] 连接成功: {Config.IpAddress} " +
                            $"({Config.Type}, Rack={Config.Rack}, Slot={Config.Slot})", "PLC");
                        return true;
                    }

                    AppLogger.Warn($"⚠️ PLC [{Config.Name}] 打开成功但连接状态为 false", "PLC");
                    return false;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"❌ PLC [{Config.Name}] 连接失败: {ex.Message}", "PLC");
                    ErrorOccurred?.Invoke(this, ex.Message);
                    _isConnected = false;
                    CleanupPlc();

                    if (Config.AutoReconnectIntervalMs > 0 && !_disposed)
                        StartReconnectLoop();

                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                _isConnected = false;
                CleanupPlc();

                try { _reconnectTask?.Wait(500); } catch { }

                ConnectionStateChanged?.Invoke(this, false);
                AppLogger.Info($"🔌 PLC [{Config.Name}] 已断开", "PLC");
            }
        }

        private void CleanupPlc()
        {
            if (_plc != null)
            {
                try { if (_plc.IsConnected) _plc.Close(); } catch { }
                _plc = null;
            }
        }

        private void StartReconnectLoop()
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted) return;

            _reconnectTask = Task.Run(async () =>
            {
                while (!_disposed && !_isConnected)
                {
                    await Task.Delay(Config.AutoReconnectIntervalMs);
                    if (_disposed) return;

                    AppLogger.Info($"🔄 尝试重连 PLC [{Config.Name}]...", "PLC");
                    if (Connect())
                    {
                        AppLogger.Info($"✅ PLC [{Config.Name}] 重连成功", "PLC");
                        return;
                    }
                }
            });
        }

        // ============================================================
        // 读操作
        // ============================================================
        public bool ReadBool(string address, out bool value)
        {
            value = false;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = result is bool b && b;
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadShort(string address, out short value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToInt16(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadUShort(string address, out ushort value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToUInt16(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadInt(string address, out int value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToInt32(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadUInt(string address, out uint value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToUInt32(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadFloat(string address, out float value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToSingle(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadDouble(string address, out double value)
        {
            value = 0;
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                value = Convert.ToDouble(result);
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadString(string address, int length, out string value)
        {
            value = "";
            if (!EnsureConnected()) return false;
            try
            {
                var result = _plc!.Read(address);
                if (result is byte[] bytes)
                    value = System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0');
                else
                    value = result?.ToString() ?? "";
                return true;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        public bool ReadBytes(string address, int length, out byte[] value)
        {
            value = Array.Empty<byte>();
            if (!EnsureConnected()) return false;
            try
            {
                // 尝试解析形如 DB1.DBB0 / DB1.DBD0 / DB1.DBW0 的地址
                if (TryParseDbAddress(address, out int dbNum, out int startByte))
                {
                    value = _plc!.ReadBytes(S7DataType.DataBlock, dbNum, startByte, length);
                    return true;
                }

                // 兜底：用 Read 拿 object 再判断
                var result = _plc!.Read(address);
                if (result is byte[] bytes)
                {
                    value = bytes;
                    return true;
                }

                AppLogger.Warn($"⚠️ ReadBytes 无法解析地址: {address}", "PLC");
                return false;
            }
            catch (Exception ex) { HandleReadError(address, ex); return false; }
        }

        // ============================================================
        // 写操作
        // ============================================================
        public bool WriteBool(string address, bool value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteShort(string address, short value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteUShort(string address, ushort value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteInt(string address, int value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteUInt(string address, uint value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteFloat(string address, float value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteDouble(string address, double value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteString(string address, string value)
        {
            if (!EnsureConnected()) return false;
            try { _plc!.Write(address, value); return true; }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        public bool WriteBytes(string address, byte[] value)
        {
            if (!EnsureConnected()) return false;
            try
            {
                if (TryParseDbAddress(address, out int dbNum, out int startByte))
                {
                    _plc!.WriteBytes(S7DataType.DataBlock, dbNum, startByte, value);
                    return true;
                }

                AppLogger.Warn($"⚠️ WriteBytes 无法解析地址: {address}", "PLC");
                return false;
            }
            catch (Exception ex) { HandleWriteError(address, ex); return false; }
        }

        // ============================================================
        // 批量读
        // ============================================================
        public Dictionary<string, object?> ReadMulti(string[] addresses)
        {
            var result = new Dictionary<string, object?>();
            if (!EnsureConnected()) return result;

            foreach (var address in addresses)
            {
                try
                {
                    object? value = _plc!.Read(address);
                    result[address] = value;
                }
                catch (Exception ex)
                {
                    AppLogger.Debug($"批量读 [{address}] 失败: {ex.Message}", "PLC");
                    result[address] = null;
                }
            }
            return result;
        }

        // ============================================================
        // 辅助
        // ============================================================
        private bool EnsureConnected()
        {
            if (_isConnected && _plc != null && _plc.IsConnected) return true;
            AppLogger.Warn($"⚠️ PLC [{Config.Name}] 未连接，操作被跳过", "PLC");
            return false;
        }

        /// <summary>
        /// 解析 DB 地址：DB1.DBB0 / DB1.DBD0 / DB1.DBW0 / DB1.DBX0.0
        /// 返回 DB 号和起始字节偏移
        /// </summary>
        private static bool TryParseDbAddress(string address, out int dbNum, out int startByte)
        {
            dbNum = 0;
            startByte = 0;

            if (string.IsNullOrEmpty(address)) return false;

            // 匹配 DB<数字>.DB<B|W|D|X><数字>
            var match = Regex.Match(
                address,
                @"^DB(\d+)\.DB([BWDX])(\d+)",
                RegexOptions.IgnoreCase);

            if (!match.Success) return false;

            dbNum = int.Parse(match.Groups[1].Value);
            startByte = int.Parse(match.Groups[3].Value);
            return true;
        }

        private void HandleReadError(string address, Exception ex)
        {
            AppLogger.Warn($"⚠️ PLC [{Config.Name}] 读 [{address}] 失败: {ex.Message}", "PLC");
            if (ex.Message.Contains("Connection") || ex.Message.Contains("disconnected"))
                HandleConnectionLost();
        }

        private void HandleWriteError(string address, Exception ex)
        {
            AppLogger.Warn($"⚠️ PLC [{Config.Name}] 写 [{address}] 失败: {ex.Message}", "PLC");
            if (ex.Message.Contains("Connection") || ex.Message.Contains("disconnected"))
                HandleConnectionLost();
        }

        private void HandleConnectionLost()
        {
            if (!_isConnected) return;
            _isConnected = false;
            ConnectionStateChanged?.Invoke(this, false);

            if (Config.AutoReconnectIntervalMs > 0 && !_disposed)
                StartReconnectLoop();
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