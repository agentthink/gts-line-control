namespace GtsTest.Services.Plc
{
    /// <summary>
    /// PLC 类型
    /// </summary>
    public enum PlcType
    {
        /// <summary>模拟 PLC（无硬件测试用）</summary>
        Simulated = 0,

        /// <summary>西门子 S7-1200</summary>
        SiemensS1200 = 1,

        /// <summary>西门子 S7-1500</summary>
        SiemensS1500 = 2,

        /// <summary>西门子 S7-300</summary>
        SiemensS300 = 3,

        /// <summary>西门子 S7-400</summary>
        SiemensS400 = 4,

        /// <summary>西门子 S7-200 Smart</summary>
        SiemensS200Smart = 5,

        /// <summary>三菱 MC 协议</summary>
        MitsubishiMc = 10,

        // 预留：欧姆龙
        // OmronFins = 20,
    }
}