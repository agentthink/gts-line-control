using System;

namespace GtsTest.Diagnostics
{
    /// <summary>报文方向</summary>
    public enum FrameDirection
    {
        TX,     // 发送
        RX,     // 接收
        Info,   // 信息
        Error   // 错误
    }

    /// <summary>协议类型</summary>
    public enum FrameProtocol
    {
        ModbusTcp,
        ModbusRtu,
        OpcUa,
        Mqtt,
        System
    }

    /// <summary>
    /// 一条通信报文记录
    /// </summary>
    public class FrameLogEntry
    {
        /// <summary>全局递增序号</summary>
        public long Seq { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public FrameProtocol Protocol { get; set; }

        /// <summary>设备 ID（如 dev-001）</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>设备名称（如 设备1-焊接）</summary>
        public string DeviceName { get; set; } = "";

        public FrameDirection Direction { get; set; }

        /// <summary>摘要（人可读，如 "从机=01 0x03 读保持寄存器 起始=0000 数量=000A"）</summary>
        public string Summary { get; set; } = "";

        /// <summary>报文数据（十六进制字符串，如 "01 03 00 00 00 0A"）</summary>
        public string Payload { get; set; } = "";

        public bool IsError { get; set; }

        public string? ErrorMessage { get; set; }

        // ---------- 派生显示字段 ----------

        public string TimeText => Timestamp.ToString("HH:mm:ss.fff");

        public string ProtocolText => Protocol switch
        {
            FrameProtocol.ModbusTcp => "Modbus TCP",
            FrameProtocol.ModbusRtu => "Modbus RTU",
            FrameProtocol.OpcUa => "OPC UA",
            FrameProtocol.Mqtt => "MQTT",
            _ => "System"
        };

        public string DirectionText => Direction switch
        {
            FrameDirection.TX => "→ TX",
            FrameDirection.RX => "← RX",
            FrameDirection.Info => "· 信息",
            FrameDirection.Error => "✗ 错误",
            _ => Direction.ToString()
        };
    }

    /// <summary>报文统计</summary>
    public class FrameStats
    {
        public long TotalTx { get; set; }
        public long TotalRx { get; set; }
        public long TotalError { get; set; }
        public long ModbusTcpCount { get; set; }
        public long ModbusRtuCount { get; set; }
        public long OpcUaCount { get; set; }
        public long MqttCount { get; set; }

        public DateTime LastResetTime { get; set; } = DateTime.Now;

        public void Reset()
        {
            TotalTx = TotalRx = TotalError = 0;
            ModbusTcpCount = ModbusRtuCount = OpcUaCount = MqttCount = 0;
            LastResetTime = DateTime.Now;
        }
    }
}