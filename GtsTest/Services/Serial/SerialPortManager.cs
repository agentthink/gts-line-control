using GtsTest.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace GtsTest.Services.Serial
{
    /// <summary>
    /// 多串口管理器（单例）
    /// </summary>
    public class SerialPortManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ISerialPortDriver> _ports = new();
        private bool _disposed;

        /// <summary>打开一个串口</summary>
        public ISerialPortDriver? OpenPort(SerialPortConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.PortName))
                return null;

            if (_ports.ContainsKey(config.PortName))
            {
                AppLogger.Warn($"⚠️ 串口 {config.PortName} 已存在，先关闭旧连接", "SerialPortMgr");
                ClosePort(config.PortName);
            }

            var driver = new SerialPortDriver(config);
            if (driver.Open())
            {
                _ports[config.PortName] = driver;
                return driver;
            }

            driver.Dispose();
            return null;
        }

        /// <summary>获取串口驱动</summary>
        public ISerialPortDriver? GetPort(string portName)
        {
            return _ports.TryGetValue(portName, out var driver) ? driver : null;
        }

        /// <summary>关闭指定串口</summary>
        public void ClosePort(string portName)
        {
            if (_ports.TryRemove(portName, out var driver))
            {
                try { driver.Close(); } catch { }
                try { driver.Dispose(); } catch { }
            }
        }

        /// <summary>关闭所有串口</summary>
        public void CloseAll()
        {
            foreach (var kv in _ports)
            {
                try { kv.Value.Close(); } catch { }
                try { kv.Value.Dispose(); } catch { }
            }
            _ports.Clear();
        }

        /// <summary>枚举系统可用串口</summary>
        public static string[] GetAvailablePorts()
        {
            try { return SerialPort.GetPortNames(); }
            catch { return Array.Empty<string>(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CloseAll();
            GC.SuppressFinalize(this);
        }
    }
}