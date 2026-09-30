namespace GtsTest.Commands
{
    /// <summary>
    /// 单个命令的配置（相当于一块积木的说明书）
    /// </summary>
    public class CommandConfig
    {
        public string Type { get; set; } = "";          // 命令类型: "Home", "MoveAbs", "WaitIO", "Delay"
        public int Axis { get; set; } = 1;              // 轴号（Home/MoveAbs 用）
        public int TargetPos { get; set; } = 0;         // 目标位置（MoveAbs 用）
        public int HomePos { get; set; } = 0;           // 回零位置（Home 用）
        public int IoIndex { get; set; } = 0;           // IO 索引（WaitIO 用）
        public bool ExpectValue { get; set; } = true;   // 期望 IO 值（WaitIO 用）
        public int DelayMs { get; set; } = 500;         // 延时毫秒（Delay 用）
        public double Vel { get; set; } = 10;           // 速度
        public double Acc { get; set; } = 5;            // 加速度+

        // ========== 跨设备信号交互字段 ==========
        public string TargetDevice { get; set; } = "";   // 目标设备 ID（如 "dev-002"）
        public int SignalAddress { get; set; } = 0;      // 线圈地址
        public bool SignalValue { get; set; } = true;    // 期望值 / 写入值

        // ========== 视觉命令配置 ==========
        public string VisionServerIp { get; set; } = "127.0.0.1";
        public int VisionServerPort { get; set; } = 503;
        public int TriggerCoilAddress { get; set; } = 100;
        public int BusyCoilAddress { get; set; } = 101;
        public int ResultCoilAddress { get; set; } = 102;
        public int ResultCodeRegister { get; set; } = 1000;
        public int FileNameRegisterStart { get; set; } = 1001;
        public int VisionTimeoutMs { get; set; } = 10000;

        // ⭐ 新增：数据寄存器地址（与 HalconVisionServer 的 Modbus 协议对齐）
        public int DiameterRegister { get; set; } = 1003;      // 直径寄存器（× 100 存储）
        public int DefectCountRegister { get; set; } = 1004;   // 缺陷数寄存器
        public int XRegister { get; set; } = 1005;             // X 坐标寄存器（× 100 存储，有符号）
        public int YRegister { get; set; } = 1006;             // Y 坐标寄存器（× 100 存储，有符号）
    }
}