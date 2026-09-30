using GtsTest.Modbus;

namespace GtsTest.Models
{
    public class DeviceConfig
    {
        public string DeviceId { get; set; } = Guid.NewGuid().ToString();   // 设备唯一标识符
        public string Name { get; set; } = "设备1";       // 设备名称
        public bool Enabled { get; set; } = true;           // 设备是否启用
        public ModbusConfig Modbus { get; set; } = new ModbusConfig();  // Modbus 配置
        // public string WorkflowName { get; set; } = "";   // ★★★ 已移除 ★★★
        public short Axis { get; set; } = 1;            // 轴号（用于运动控制）
        public int TargetCount { get; set; } = 1000;    // 目标计数（用于循环次数或触发信号次数）
        public int CurrentCount { get; set; } = 0;      // 当前计数（用于循环次数或触发信号次数）
        public int SignalStartAddress { get; set; } = 100;  // 信号起始地址（用于触发信号）

        // 工作流参数（仅用于默认值，但不再引用工作流文件名）
        public int WorkPosition { get; set; } = 1000;
        public int HomePosition { get; set; } = 0;
        public double MoveSpeed { get; set; } = 20.0;
        public double MoveAcc { get; set; } = 10.0;
        public int CycleDelayMs { get; set; } = 500;
    }
}