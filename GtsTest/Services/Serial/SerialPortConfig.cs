using System.IO.Ports;

namespace GtsTest.Services.Serial
{
    /// <summary>
    /// 串口配置
    /// </summary>
    public class SerialPortConfig
    {
        /// <summary>端口名，如 COM1 / COM3</summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>波特率</summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>数据位 5~8</summary>
        public int DataBits { get; set; } = 8;

        /// <summary>停止位</summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>校验位</summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>读超时（毫秒）</summary>
        public int ReadTimeoutMs { get; set; } = 500;

        /// <summary>写超时（毫秒）</summary>
        public int WriteTimeoutMs { get; set; } = 500;

        /// <summary>帧解析类型</summary>
        public SerialFrameType FrameType { get; set; } = SerialFrameType.Raw;

        /// <summary>定长帧长度（FrameType=FixedLength 时有效）</summary>
        public int FixedFrameLength { get; set; } = 8;

        /// <summary>帧头字节（FrameType=LengthField 时有效，可 1~2 字节）</summary>
        public byte[] FrameHeader { get; set; } = new byte[] { 0xAA, 0x55 };

        /// <summary>帧头长度字段的位置（相对于帧头的偏移）</summary>
        public int LengthFieldOffset { get; set; } = 2;

        /// <summary>帧头长度字段的字节数（1 或 2）</summary>
        public int LengthFieldSize { get; set; } = 1;

        /// <summary>长度字段是否包含帧头与校验字节</summary>
        public bool LengthIncludesHeader { get; set; } = true;

        /// <summary>是否使用 2 字节校验（CRC16-Modbus）</summary>
        public bool UseCrc16 { get; set; } = false;

        /// <summary>帧尾分隔符（FrameType=Delimiter 时有效）</summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0x0D, 0x0A }; // \r\n

        /// <summary>自动重连间隔（毫秒），<=0 表示不自动重连</summary>
        public int AutoReconnectIntervalMs { get; set; } = 3000;

        /// <summary>串口两次读之间允许的最大空闲时间（用于分包）</summary>
        public int InterByteTimeoutMs { get; set; } = 50;
    }
}