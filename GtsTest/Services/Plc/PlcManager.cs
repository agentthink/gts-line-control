using GtsTest.Core;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace GtsTest.Services.Plc
{
    /// <summary>
    /// 多 PLC 管理器（单例）
    /// </summary>
    public class PlcManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IPlcClient> _clients = new();
        private bool _disposed;

        /// <summary>创建一个 PLC 客户端</summary>
        public IPlcClient? CreateClient(PlcConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.Name))
                return null;

            if (_clients.ContainsKey(config.Name))
            {
                AppLogger.Warn($"⚠️ PLC [{config.Name}] 已存在，先移除旧的", "PlcMgr");
                RemoveClient(config.Name);
            }

            IPlcClient client = config.Type switch
            {
                PlcType.Simulated => new SimulatedPlcClient(config),
                PlcType.SiemensS1200 or PlcType.SiemensS1500 or PlcType.SiemensS300
                    or PlcType.SiemensS400 or PlcType.SiemensS200Smart
                    => new SiemensS7Client(config),
                PlcType.MitsubishiMc => new MitsubishiPlcClient(config), // 🆕 新增三菱支持
                _ => new SimulatedPlcClient(config)
            };

            _clients[config.Name] = client;

            if (client is SimulatedPlcClient sim)
                sim.SeedTestData();

            client.Connect();
            return client;
        }

        /// <summary>获取 PLC 客户端</summary>
        public IPlcClient? GetClient(string name)
        {
            return _clients.TryGetValue(name, out var c) ? c : null;
        }

        /// <summary>获取所有 PLC 名称</summary>
        public string[] GetAllNames()
        {
            return _clients.Keys.ToArray();
        }

        /// <summary>移除 PLC</summary>
        public void RemoveClient(string name)
        {
            if (_clients.TryRemove(name, out var c))
            {
                try { c.Disconnect(); } catch { }
                try { c.Dispose(); } catch { }
            }
        }

        /// <summary>关闭所有 PLC</summary>
        public void CloseAll()
        {
            foreach (var kv in _clients)
            {
                try { kv.Value.Disconnect(); } catch { }
                try { kv.Value.Dispose(); } catch { }
            }
            _clients.Clear();
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