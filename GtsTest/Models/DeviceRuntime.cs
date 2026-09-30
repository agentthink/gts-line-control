using GtsTest.Commands;
using GtsTest.Core;
using GtsTest.Modbus;

namespace GtsTest.Models
{
    /// <summary>
    /// 设备运行时状态
    /// </summary>
    public class DeviceRuntime
    {
        public DeviceConfig Config { get; set; } = null!;
        public ModbusClient ModbusClient { get; set; } = null!;
        public CancellationTokenSource? WorkflowCts { get; set; }
        public Task? WorkflowTask { get; set; }
        public string CurrentStep { get; set; } = "空闲";
        public DateTime LastUpdate { get; set; }
        public object? LastModbusData { get; set; }
        public bool IsOnline { get; set; }
        public string LastError { get; set; } = "";
        public Watchdog Watchdog { get; set; } = new Watchdog(timeoutMs: 6000, checkIntervalMs: 200);
        public HashSet<int> TriggeredSignalCounts { get; set; } = new HashSet<int>();

        // ========== 工作流断点恢复 ==========
        public WorkflowConfig? CurrentWorkflow { get; set; }
        public string? CurrentWorkflowName { get; set; }
        public IMotionCommand? CurrentCommand { get; set; }
        public int CurrentStepIndex { get; set; } = 0;
        public DateTime LastPauseTime { get; set; }
        public string? BoundWorkflowName { get; set; }
        public Dictionary<string, object> WorkflowContext { get; set; } = new Dictionary<string, object>();

        // ========== 用于取消 Modbus 事件订阅 ==========
        public EventHandler<ModbusConnectionEventArgs>? ConnectionStateChangedHandler { get; set; }

        // ========== 用于取消状态机事件订阅 ==========
        public EventHandler<DeviceStateChangedEventArgs>? StateChangedHandler { get; set; }

        // ========== 状态机（唯一真相源）==========
        /// <summary>设备状态机</summary>
        public IDeviceStateMachine StateMachine { get; } = new DeviceStateMachine(DeviceState.Idle);

        // ========== 兼容旧代码：计算属性 ==========
        /// <summary>是否正在运行（委托到状态机，只读）</summary>
        public bool IsRunning => StateMachine.CurrentState == DeviceState.Running;

        /// <summary>是否处于暂停状态（委托到状态机，只读）</summary>
        public bool IsPaused => StateMachine.CurrentState == DeviceState.Paused;

        /// <summary>是否处于故障状态（委托到状态机，只读）</summary>
        public bool IsError => StateMachine.CurrentState == DeviceState.Error;
        // 在类中找到合适的位置，添加以下属性：
        /// <summary>
        /// I/O 强制模拟字典：Key=IO索引，Value=强制值（null 表示取消强制）
        /// </summary>
        public Dictionary<int, bool> ForcedIOs { get; set; } = new Dictionary<int, bool>();
    }
}