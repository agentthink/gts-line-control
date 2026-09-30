using System.Linq;
using System;
using System.Collections.Generic;
using GtsTest.Core;
using GtsTest.Diagnostics;
using NModbus;
using NModbus.Device;
using NModbus.Serial;
using System.IO.Ports;
using System.Net.Sockets;

namespace GtsTest.Modbus
{
    public class ModbusClient : IDisposable
    {
        private readonly object _lock = new();
        private ModbusConfig _config;
        private IModbusMaster? _master;
        private TcpClient? _tcpClient;
        private SerialPort? _serialPort;
        private bool _isConnected;
        private CancellationTokenSource _cts = new();
        public event Action<string>? LogMessage;

        // 🆕 设备 ID（由 DeviceManager 在 AddDevice 时赋值，用于报文监视器归类）
        public string DeviceId { get; set; } = "";

        public class ModbusReadResult
        {
            public ushort[] RawRegisters { get; set; } = Array.Empty<ushort>();
            public object? ConvertedValue { get; set; }
        }

        public bool IsConnected => _isConnected;
        public event EventHandler<ModbusConnectionEventArgs>? ConnectionStateChanged;

        public ModbusClient(ModbusConfig config) => _config = config;

        private void Log(string msg, LogLevel level = LogLevel.Info)
        {
            // 直接调用全局日志系统（内部会触发 OnLogReceived 并写文件）
            AppLogger.Log(msg, level, "Modbus");
        }

        // ================================================================
        // 🆕 报文监视器上报（零侵入，绝不抛异常）
        // ================================================================
        private void ReportFrame(FrameDirection dir, string summary, string payload,
                                 bool isError = false, string? errorMsg = null)
        {
            try
            {
                FrameMonitorHub.Instance.Publish(new FrameLogEntry
                {
                    Protocol = _config.Protocol == ModbusProtocol.Tcp
                        ? FrameProtocol.ModbusTcp
                        : FrameProtocol.ModbusRtu,
                    DeviceId = DeviceId,
                    Direction = dir,
                    Summary = summary,
                    Payload = payload,
                    IsError = isError,
                    ErrorMessage = errorMsg
                });
            }
            catch { /* 监视器异常绝不能打断主业务 */ }
        }

        // ================================================================
        // 连接管理
        // ================================================================
        public bool Connect()
        {
            lock (_lock)
            {
                DisconnectInternal();
                return ConnectInternal();
            }
        }

        public bool Reconnect(ModbusConfig newConfig)
        {
            lock (_lock)
            {
                _config = newConfig;
                DisconnectInternal();
                return ConnectInternal();
            }
        }

        private bool ConnectInternal()
        {
            try
            {
                if (_config.Protocol == ModbusProtocol.Tcp)
                {
                    _tcpClient = new TcpClient
                    {
                        ReceiveTimeout = _config.TimeoutMs,
                        SendTimeout = _config.TimeoutMs
                    };
                    var ar = _tcpClient.BeginConnect(_config.IpAddress, _config.Port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(_config.TimeoutMs))
                    {
                        _tcpClient.Close();
                        throw new TimeoutException($"TCP连接超时 ({_config.IpAddress}:{_config.Port})");
                    }
                    _tcpClient.EndConnect(ar);
                    _master = new ModbusFactory().CreateMaster(_tcpClient);
                }
                else // RTU
                {
                    _serialPort = new SerialPort(_config.PortName, _config.BaudRate,
                        _config.Parity, _config.DataBits, _config.StopBits)
                    {
                        ReadTimeout = _config.TimeoutMs,
                        WriteTimeout = _config.TimeoutMs
                    };
                    _serialPort.Open();
                    var streamResource = new SerialPortAdapter(_serialPort);
                    _master = new ModbusFactory().CreateRtuMaster(streamResource);
                }

                _isConnected = true;
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(true));

                // 🆕 上报连接事件
                ReportFrame(FrameDirection.Info,
                    ModbusFrameFormatter.FormatConnectionEvent(true, $"{_config.IpAddress}:{_config.Port}"),
                    "");

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                string err = $"连接失败: {ex.Message}";
                Log($"❌ Modbus错误: {err}", LogLevel.Error);
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(false, err));

                // 🆕 上报连接失败
                ReportFrame(FrameDirection.Error, $"连接失败: {err}", "", isError: true, errorMsg: ex.Message);

                return false;
            }
        }

        private void DisconnectInternal(bool raiseEvent = true)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _tcpClient?.Close();
            _tcpClient = null;
            _serialPort?.Close();
            _serialPort = null;
            _master = null;
            _isConnected = false;

            if (raiseEvent)
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(false, $"从机={_config.SlaveAddress} " + "Modbus连接已断开"));
        }

        // ================================================================
        // 读保持寄存器（带类型转换）
        // ================================================================
        public object? ReadHoldingRegisters(ushort startAddress, ushort count, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null) return null;

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatReadHoldingRequest(_config.SlaveAddress, startAddress, count);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                ushort[] raw = _master.ReadHoldingRegisters(_config.SlaveAddress, startAddress, count);
                var (rspSummary, rspPayload) = ModbusFrameFormatter.FormatReadHoldingResponse(_config.SlaveAddress, raw);
                ReportFrame(FrameDirection.RX, rspSummary, rspPayload);
                return ConvertRawToType(raw, _config.DataType);
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x03, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);

                Log($"❌ Modbus读取失败: {ex.Message}", LogLevel.Warn);
                _isConnected = false;
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(false, $"从机={_config.SlaveAddress} " + "读取超时，链路中断"));
                return null;
            }
        }

        // ================================================================
        // 读保持寄存器（同时返回原始 + 转换值）
        // ================================================================
        public ModbusReadResult? ReadHoldingRegistersWithRaw(ushort startAddress, ushort count, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null) return null;

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatReadHoldingRequest(_config.SlaveAddress, startAddress, count);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                ushort[] raw = _master.ReadHoldingRegisters(_config.SlaveAddress, startAddress, count);
                object? converted = ConvertRawToType(raw, _config.DataType);

                var (rspSummary, rspPayload) = ModbusFrameFormatter.FormatReadHoldingResponse(_config.SlaveAddress, raw);
                ReportFrame(FrameDirection.RX, rspSummary, rspPayload);

                return new ModbusReadResult
                {
                    RawRegisters = raw,
                    ConvertedValue = converted
                };
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x03, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);

                Log($"❌ Modbus读取转换指定类型失败:从机={_config.SlaveAddress} {ex.Message}", LogLevel.Warn);
                _isConnected = false;
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(false, $"从机={_config.SlaveAddress} " + "读取超时，链路中断"));
                return null;
            }
        }

        // ================================================================
        // 按地址类型读数据
        // ================================================================
        public ModbusReadResult? ReadDataByType(ushort startAddress, ushort count)
        {
            if (!_isConnected || _master == null) return null;

            try
            {
                switch (_config.AddressType)
                {
                    case AddressType.HoldingRegister:
                        {
                            var (s, p) = ModbusFrameFormatter.FormatReadHoldingRequest(_config.SlaveAddress, startAddress, count);
                            ReportFrame(FrameDirection.TX, s, p);

                            ushort[] hr = _master.ReadHoldingRegisters(_config.SlaveAddress, startAddress, count);
                            var (rs, rp) = ModbusFrameFormatter.FormatReadHoldingResponse(_config.SlaveAddress, hr);
                            ReportFrame(FrameDirection.RX, rs, rp);

                            return new ModbusReadResult
                            {
                                RawRegisters = hr,
                                ConvertedValue = ConvertRawToType(hr, _config.DataType)
                            };
                        }

                    case AddressType.Coil:
                        {
                            var (s, p) = ModbusFrameFormatter.FormatReadCoilsRequest(_config.SlaveAddress, startAddress, count);
                            ReportFrame(FrameDirection.TX, s, p);

                            bool[] coils = _master.ReadCoils(_config.SlaveAddress, startAddress, count);
                            ushort[] coilRaw = coils.Select(b => (ushort)(b ? 1 : 0)).ToArray();
                            ReportFrame(FrameDirection.RX, $"从机={_config.SlaveAddress:D2} 读线圈响应 [{string.Join(",", coils)}]", "");

                            return new ModbusReadResult
                            {
                                RawRegisters = coilRaw,
                                ConvertedValue = coils
                            };
                        }

                    case AddressType.InputRegister:
                        {
                            var (s, p) = ModbusFrameFormatter.FormatReadInputRequest(_config.SlaveAddress, startAddress, count);
                            ReportFrame(FrameDirection.TX, s, p);

                            ushort[] ir = _master.ReadInputRegisters(_config.SlaveAddress, startAddress, count);
                            var (rs, rp) = ModbusFrameFormatter.FormatReadHoldingResponse(_config.SlaveAddress, ir);
                            ReportFrame(FrameDirection.RX, rs, rp);

                            return new ModbusReadResult
                            {
                                RawRegisters = ir,
                                ConvertedValue = ConvertRawToType(ir, _config.DataType)
                            };
                        }

                    case AddressType.DiscreteInput:
                        {
                            var (s, p) = ModbusFrameFormatter.FormatReadDiscreteRequest(_config.SlaveAddress, startAddress, count);
                            ReportFrame(FrameDirection.TX, s, p);

                            bool[] dis = _master.ReadInputs(_config.SlaveAddress, startAddress, count);
                            ushort[] disRaw = dis.Select(b => (ushort)(b ? 1 : 0)).ToArray();
                            ReportFrame(FrameDirection.RX, $"从机={_config.SlaveAddress:D2} 读离散输入响应 [{string.Join(",", dis)}]", "");

                            return new ModbusReadResult
                            {
                                RawRegisters = disRaw,
                                ConvertedValue = dis
                            };
                        }

                    default:
                        throw new NotSupportedException($"不支持的地址类型: {_config.AddressType}");
                }
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatError(
                    $"读取 (类型={_config.AddressType})", ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);

                Log($"❌ Modbus读取失败 (类型={_config.AddressType}, 从机={_config.SlaveAddress}): {ex.Message}", LogLevel.Warn);
                _isConnected = false;
                ConnectionStateChanged?.Invoke(this, new ModbusConnectionEventArgs(false, $"从机={_config.SlaveAddress} 读取超时，链路中断"));
                return null;
            }
        }

        // ================================================================
        // 原始寄存器 → 目标类型
        // ================================================================
        private object ConvertRawToType(ushort[] raw, DataType type)
        {
            if (raw == null || raw.Length == 0) return null!;

            if (type == DataType.Int16)
            {
                short[] result = new short[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    result[i] = (short)((raw[i] << 8) | (raw[i] >> 8)); // 大端转换
                return result;
            }
            if (type == DataType.UInt16)
            {
                ushort[] result = new ushort[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                    result[i] = (ushort)((raw[i] << 8) | (raw[i] >> 8));
                return result;
            }

            byte[] bytes = new byte[raw.Length * 2];
            for (int i = 0; i < raw.Length; i++)
            {
                bytes[i * 2] = (byte)(raw[i] >> 8);
                bytes[i * 2 + 1] = (byte)(raw[i] & 0xFF);
            }

            if (type == DataType.Int32 && raw.Length >= 2)
                return BitConverter.ToInt32(bytes, 0);
            if (type == DataType.UInt32 && raw.Length >= 2)
                return BitConverter.ToUInt32(bytes, 0);
            if (type == DataType.Float && raw.Length >= 2)
                return BitConverter.ToSingle(bytes, 0);
            if (type == DataType.Double && raw.Length >= 4)
                return BitConverter.ToDouble(bytes, 0);

            return null!;
        }

        public void Disconnect() => DisconnectInternal(true);

        public void Dispose()
        {
            DisconnectInternal(false);
            _cts.Dispose();
        }

        // ================================================================
        // 写单个线圈 (功能码 0x05)
        // ================================================================
        public bool WriteSingleCoil(ushort address, bool value, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null)
            {
                Log($"❌ 写单个线圈失败：从机={_config.SlaveAddress} Modbus 未连接", LogLevel.Error);
                return false;
            }

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatWriteSingleCoil(_config.SlaveAddress, address, value);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                _master.WriteSingleCoil(_config.SlaveAddress, address, value);
                ReportFrame(FrameDirection.RX, $"{reqSummary} 执行成功", reqPayload);
                Log($"✅ 写单个线圈成功: 从机={_config.SlaveAddress} 地址={address}  值={value}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x05, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);
                Log($"❌ 写单个线圈异常: 从机={_config.SlaveAddress}  地址={address}  值={value} {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        // ================================================================
        // 写多个线圈 (功能码 0x0F)
        // ================================================================
        public bool WriteMultipleCoils(ushort address, bool[] values, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null)
            {
                Log($"❌ 写多个线圈失败： 从机={_config.SlaveAddress} Modbus 未连接", LogLevel.Error);
                return false;
            }

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatWriteMultipleCoils(_config.SlaveAddress, address, values);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                _master.WriteMultipleCoils(_config.SlaveAddress, address, values);
                ReportFrame(FrameDirection.RX, $"{reqSummary} 执行成功", reqPayload);
                Log($"✅ 写多个线圈成功: 从机={_config.SlaveAddress}  起始地址={address} 数量={values.Length}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x0F, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);
                Log($"❌ 写多个线圈异常: 从机={_config.SlaveAddress} {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        // ================================================================
        // 写单个保持寄存器 (功能码 0x06)
        // ================================================================
        public bool WriteSingleRegister(ushort address, ushort value, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null)
            {
                Log($"❌ 写单个寄存器失败： 从机={_config.SlaveAddress} Modbus 未连接", LogLevel.Error);
                return false;
            }

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatWriteSingleRegister(_config.SlaveAddress, address, value);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                _master.WriteSingleRegister(_config.SlaveAddress, address, value);
                ReportFrame(FrameDirection.RX, $"{reqSummary} 执行成功", reqPayload);
                Log($"✅ 写单个寄存器成功: 从机={_config.SlaveAddress} 地址={address} 值=0x{value:X4}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x06, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);
                Log($"❌ 写单个寄存器异常: 从机={_config.SlaveAddress} {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        // ================================================================
        // 写多个保持寄存器 (功能码 0x10)
        // ================================================================
        public bool WriteMultipleRegisters(ushort address, ushort[] values, byte slaveAddress = 1)
        {
            if (!_isConnected || _master == null)
            {
                Log($"❌ 写多个寄存器失败： 从机={_config.SlaveAddress} Modbus 未连接", LogLevel.Error);
                return false;
            }

            var (reqSummary, reqPayload) = ModbusFrameFormatter.FormatWriteMultipleRegisters(_config.SlaveAddress, address, values);
            ReportFrame(FrameDirection.TX, reqSummary, reqPayload);

            try
            {
                _master.WriteMultipleRegisters(_config.SlaveAddress, address, values);
                ReportFrame(FrameDirection.RX, $"{reqSummary} 执行成功", reqPayload);
                Log($"✅ 写多个寄存器成功:  从机={_config.SlaveAddress} 起始地址={address} 数量={values.Length}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                var (errSummary, errPayload) = ModbusFrameFormatter.FormatException(_config.SlaveAddress, 0x10, ex.Message);
                ReportFrame(FrameDirection.Error, errSummary, errPayload, isError: true, errorMsg: ex.Message);
                Log($"❌ 写多个寄存器异常: 从机={_config.SlaveAddress} {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        // ================================================================
        // 编码工具
        // ================================================================
        public static ushort[] EncodeValue(object value, DataType type, ByteOrder order)
        {
            if (value == null) return Array.Empty<ushort>();

            if (value is Array arr)
            {
                var result = new List<ushort>();
                foreach (var item in arr)
                    result.AddRange(EncodeValue(item, type, order));
                return result.ToArray();
            }

            byte[] bytes;
            switch (type)
            {
                case DataType.Int16:
                    bytes = BitConverter.GetBytes(Convert.ToInt16(value));
                    break;
                case DataType.UInt16:
                    bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
                    break;
                case DataType.Int32:
                    bytes = BitConverter.GetBytes(Convert.ToInt32(value));
                    break;
                case DataType.UInt32:
                    bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
                    break;
                case DataType.Float:
                    bytes = BitConverter.GetBytes(Convert.ToSingle(value));
                    break;
                case DataType.Double:
                    bytes = BitConverter.GetBytes(Convert.ToDouble(value));
                    break;
                default:
                    throw new NotSupportedException($"不支持的数据类型: {type}");
            }

            if (order == ByteOrder.BigEndian)
            {
                if (bytes.Length == 2) Array.Reverse(bytes);
                else if (bytes.Length == 4) { Array.Reverse(bytes, 0, 2); Array.Reverse(bytes, 2, 2); }
                else if (bytes.Length == 8)
                {
                    Array.Reverse(bytes, 0, 2);
                    Array.Reverse(bytes, 2, 2);
                    Array.Reverse(bytes, 4, 2);
                    Array.Reverse(bytes, 6, 2);
                }
            }

            ushort[] registers = new ushort[bytes.Length / 2];
            for (int i = 0; i < registers.Length; i++)
                registers[i] = (ushort)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
            return registers;
        }

        // ================================================================
        // 便捷方法
        // ================================================================
        public bool WriteHoldingRegister(ushort address, ushort value, byte slaveAddress = 1)
            => WriteSingleRegister(address, value, slaveAddress);

        public bool WriteHoldingRegisters(ushort address, ushort[] values, byte slaveAddress = 1)
            => WriteMultipleRegisters(address, values, slaveAddress);
    }
}