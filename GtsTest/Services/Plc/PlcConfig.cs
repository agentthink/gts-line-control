namespace GtsTest.Services.Plc
{
    /// <summary>
    /// PLC 连接配置
    /// </summary>
    public class PlcConfig
    {
        /// <summary>PLC 类型</summary>
        public PlcType Type { get; set; } = PlcType.Simulated;

        /// <summary>IP 地址</summary>
        public string IpAddress { get; set; } = "192.168.1.10";

        /// <summary>端口（西门子 S7 默认 102）</summary>
        public int Port { get; set; } = 102;

        /// <summary>机架号（S7-1200/1500 默认 0）</summary>
        public short Rack { get; set; } = 0;

        /// <summary>槽号（S7-1200/1500 默认 1；S7-300/400 默认 2）</summary>
        public short Slot { get; set; } = 1;

        /// <summary>连接超时（毫秒）</summary>
        public int ConnectTimeoutMs { get; set; } = 3000;

        /// <summary>读写超时（毫秒）</summary>
        public int ReadWriteTimeoutMs { get; set; } = 2000;

        /// <summary>自动重连间隔（毫秒），<=0 表示不重连</summary>
        public int AutoReconnectIntervalMs { get; set; } = 5000;

        /// <summary>名称（用于日志和多 PLC 管理）</summary>
        public string Name { get; set; } = "PLC1";
    }
}