namespace GtsTest.Services.Serial
{
    /// <summary>
    /// 串口帧格式类型
    /// </summary>
    public enum SerialFrameType
    {
        /// <summary>定长帧：每帧固定 N 字节</summary>
        FixedLength = 0,

        /// <summary>变长帧：起始头 + 长度字段 + 载荷 + 校验</summary>
        LengthField = 1,

        /// <summary>分隔符帧：以特定字节（如 \r\n）作为结尾</summary>
        Delimiter = 2,

        /// <summary>原始流：不做解析，收到什么就抛什么</summary>
        Raw = 3,
    }
}