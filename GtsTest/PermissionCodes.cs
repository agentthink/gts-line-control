namespace GtsTest.Services.Authentication
{
    /// <summary>
    /// 权限码常量定义
    /// </summary>
    public static class PermissionCodes
    {
        // ---- 设备 ----
        public const string DeviceView = "Device.View";
        public const string DeviceStart = "Device.Start";
        public const string DeviceStop = "Device.Stop";
        public const string DeviceStartAll = "Device.StartAll";
        public const string DeviceStopAll = "Device.StopAll";
        public const string DeviceAdd = "Device.Add";
        public const string DeviceRemove = "Device.Remove";
        public const string DeviceReset = "Device.Reset";

        // ---- 报警 ----
        public const string AlarmReset = "Alarm.Reset";
        public const string AlarmAcknowledge = "Alarm.Acknowledge";
        public const string AlarmResolve = "Alarm.Resolve";

        // ---- 生产执行 ----
        public const string WorkflowRun = "Workflow.Run";
        public const string WorkflowStop = "Workflow.Stop";
        public const string ProductionReset = "Production.Reset";

        // ---- 系统配置 ----
        public const string SystemConfig = "System.Config";
        public const string WorkflowEdit = "Workflow.Edit";
        public const string WorkflowSave = "Workflow.Save";
        public const string WorkflowLoad = "Workflow.Load";
        public const string CommConfig = "Comm.Config";
        public const string DebugToolbox = "Debug.Toolbox";
        public const string SystemInit = "System.Init";
        public const string SystemToggleMode = "System.ToggleMode";
        public const string ConfigSave = "Config.Save";
        public const string ConfigHotReload = "Config.HotReload";
        public const string BlackBoxExport = "BlackBox.Export";

        // ---- 管理 ----
        public const string UserManage = "User.Manage";
        public const string AuditView = "Audit.View";

        // ---- 安全 ----
        public const string EmergencyStop = "EmergencyStop";
    }
}