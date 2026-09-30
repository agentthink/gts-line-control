using GtsTest.Commands;
using GtsTest.Modbus;
using GtsTest.Models;
using GtsTest.Services.Alarm;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using GtsTest.Services.Mes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GtsTest.Core
{
    /// <summary>
    /// 设备管理器：管理多台设备的并行控制与数据采集
    /// </summary>
    public class DeviceManager : IDisposable
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ConcurrentDictionary<string, DeviceRuntime> _devices = new();
        private readonly GtsModel _model;
        private readonly IDataRepository? _repository;
        private readonly IAlarmManager _alarmManager;
        private readonly ILogger _logger;
        private readonly IWorkflowEngine _workflowEngine;
        private bool _disposed = false;

        private MesReportService? _mesService;
        public MesReportService? MesService
        {
            get => _mesService;
            set => _mesService = value;
        }

        // ================================================================
        // 事件
        // ================================================================
        public event Action<string, bool>? OnDeviceOnlineChanged;
        public event Action<string, ushort[], object?>? OnDeviceDataUpdated;
        public event Action<string, string>? OnDeviceStepChanged;
        public event Action<string, int, int>? OnDeviceProductionUpdated;
        public event Action<string, DeviceState, string>? OnDeviceStateChanged;
        public IAlarmManager AlarmManager => _alarmManager;

        // ================================================================
        // 构造函数
        // ================================================================
        public DeviceManager(
            GtsModel model,
            IDataRepository? repository,
            IAlarmManager? alarmManager,
            ILogger? logger)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _repository = repository;
            _logger = logger ?? new AppLoggerWrapper();
            _alarmManager = alarmManager ?? new AlarmManager(null, _logger);

            _workflowEngine = new WorkflowEngine(_model, this, _logger);
        }

        public DeviceManager(GtsModel model, IDataRepository? repository = null)
            : this(model, repository, null, null) { }

        public GtsModel Model => _model;

        // ================================================================
        // 添加 / 移除设备
        // ================================================================
        public bool AddDevice(DeviceConfig config)
        {
            var runtime = new DeviceRuntime
            {
                Config = config,
                ModbusClient = new ModbusClient(config.Modbus) { DeviceId = config.DeviceId }   // ← 加 { DeviceId = ... }
            };

            runtime.ConnectionStateChangedHandler = (s, e) =>
            {
                try
                {
                    runtime.IsOnline = e.IsConnected;
                    OnDeviceOnlineChanged?.Invoke(config.DeviceId, e.IsConnected);
                    if (!e.IsConnected)
                    {
                        runtime.LastError = e.ErrorMessage ?? "连接断开";
                        runtime.StateMachine.ForceSet(DeviceState.Disconnected, "Modbus 断开");
                    }
                    else if (runtime.StateMachine.CurrentState == DeviceState.Disconnected)
                    {
                        runtime.StateMachine.ForceSet(DeviceState.Idle, "Modbus 恢复");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"⚠️ ConnectionStateChanged 处理异常: {ex}", "DeviceManager");
                }
            };
            runtime.ModbusClient.ConnectionStateChanged += runtime.ConnectionStateChangedHandler;

            EventHandler<DeviceStateChangedEventArgs> stateHandler = (s, e) =>
            {
                try
                {
                    OnDeviceStateChanged?.Invoke(config.DeviceId, e.NewState, e.Reason);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"OnDeviceStateChanged 处理异常: {ex.Message}", "DeviceManager");
                }
            };
            runtime.StateMachine.StateChanged += stateHandler;
            runtime.StateChangedHandler = stateHandler;

            if (!_devices.TryAdd(config.DeviceId, runtime))
            {
                _logger.Warn($"⚠️ 添加设备失败（已存在）: {config.DeviceId}", "DeviceManager");
                try { runtime.ModbusClient.Dispose(); } catch { }
                try { runtime.Watchdog.Dispose(); } catch { }
                return false;
            }

            _logger.Info($"✅ 设备 [{config.Name}] 已添加", "DeviceManager");
            return true;
        }

        public async Task<bool> RemoveDeviceAsync(string deviceId, int stopTimeoutMs = 1000)
        {
            if (!_devices.TryRemove(deviceId, out var runtime) || runtime == null)
                return false;

            try
            {
                if (runtime.ConnectionStateChangedHandler != null && runtime.ModbusClient != null)
                    runtime.ModbusClient.ConnectionStateChanged -= runtime.ConnectionStateChangedHandler;

                if (runtime.StateChangedHandler != null && runtime.StateMachine != null)
                    runtime.StateMachine.StateChanged -= runtime.StateChangedHandler;

                try { runtime.WorkflowCts?.Cancel(); } catch { }
                try { runtime.CurrentCommand?.Stop(); } catch { }

                var task = runtime.WorkflowTask ?? Task.CompletedTask;
                if (!task.IsCompleted)
                {
                    var finished = await Task.WhenAny(task, Task.Delay(stopTimeoutMs));
                    if (finished != task)
                        _logger.Warn($"⚠️ 设备 [{deviceId}] 停止超时 (>{stopTimeoutMs}ms)", "DeviceManager");
                }

                try { runtime.ModbusClient.Disconnect(); } catch { }
                try { runtime.ModbusClient.Dispose(); } catch { }
                try { runtime.Watchdog.Dispose(); } catch { }

                _logger.Info($"✅ 设备 [{deviceId}] 已移除", "DeviceManager");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ 移除设备 [{deviceId}] 异常: {ex}", "DeviceManager");
                return false;
            }
        }

        public bool RemoveDevice(string deviceId, int stopTimeoutMs = 1000)
            => RemoveDeviceAsync(deviceId, stopTimeoutMs).GetAwaiter().GetResult();

        // ================================================================
        // 启动 / 停止
        // ================================================================
        public bool StartDevice(string deviceId, string? workflowName = null)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null) return false;

            lock (runtime)
            {
                // 如果已经有一个后台循环在跑，先等它退出（避免多循环并行）
                var existingTask = runtime.WorkflowTask;
                if (existingTask != null && !existingTask.IsCompleted)
                {
                    if (runtime.StateMachine.CurrentState == DeviceState.Running)
                        return true;

                    // 状态不是 Running 但有活循环 → 先取消等它退出
                    try { runtime.WorkflowCts?.Cancel(); } catch { }
                    try { existingTask.Wait(1500); } catch { }
                }

                if (!runtime.ModbusClient.Connect())
                {
                    _logger.Error($"❌ 设备 [{runtime.Config.Name}] Modbus 连接失败，无法启动", "DeviceManager");
                    runtime.StateMachine.ForceSet(DeviceState.Disconnected, "启动时连接失败");
                    return false;
                }

                if (!string.IsNullOrEmpty(workflowName))
                    runtime.CurrentWorkflowName = workflowName;
                else if (string.IsNullOrEmpty(runtime.CurrentWorkflowName))
                {
                    _logger.Error($"❌ 设备 [{runtime.Config.Name}] 未指定工作流，无法启动", "DeviceManager");
                    return false;
                }

                runtime.WorkflowCts = new CancellationTokenSource();
                var token = runtime.WorkflowCts.Token;
                runtime.WorkflowTask = Task.Run(() => DeviceLoopAsync(runtime, token), token);

                runtime.StateMachine.ForceSet(DeviceState.Running, "启动");
                _logger.Info($"▶️ 设备 [{runtime.Config.Name}] 已启动，工作流: {runtime.CurrentWorkflowName}", "DeviceManager");
                return true;
            }
        }

        public bool StartDevice(string deviceId) => StartDevice(deviceId, null);

        public bool StopDevice(string deviceId, int stopTimeoutMs = 1000)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null) return false;

            lock (runtime)
            {
                // ★ 修复点 3：无论当前是什么状态，只要不是 Idle 就执行停止流程
                if (runtime.StateMachine.CurrentState == DeviceState.Idle) return true;

                try { runtime.WorkflowCts?.Cancel(); } catch { }
                try { runtime.CurrentCommand?.Stop(); } catch { }

                var task = runtime.WorkflowTask ?? Task.CompletedTask;
                if (!task.IsCompleted)
                {
                    try
                    {
                        var finished = Task.WhenAny(task, Task.Delay(stopTimeoutMs)).GetAwaiter().GetResult();
                        if (finished != task)
                            _logger.Warn($"⚠️ 设备 [{deviceId}] 停止超时 (>{stopTimeoutMs}ms)", "DeviceManager");
                    }
                    catch { }
                }

                runtime.CurrentCommand = null;
                runtime.StateMachine.ForceSet(DeviceState.Idle, "停止");
                try { runtime.Watchdog.Stop(); } catch { }
                _logger.Info($"⏹ 设备 [{runtime.Config.Name}] 已停止", "DeviceManager");
                return true;
            }
        }

        public void StartAllDevices()
        {
            int started = 0;
            int skipped = 0;
            foreach (var kv in _devices)
            {
                var runtime = kv.Value;
                if (!runtime.IsOnline) continue;
                if (runtime.StateMachine.CurrentState == DeviceState.Running) continue;

                string workflow = runtime.BoundWorkflowName;
                if (string.IsNullOrEmpty(workflow))
                {
                    _logger.Warn($"设备 [{runtime.Config.Name}] 未绑定工作流，跳过启动", "DeviceManager");
                    skipped++;
                    continue;
                }

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows", workflow + ".json");
                if (!File.Exists(filePath))
                {
                    _logger.Warn($"设备 [{runtime.Config.Name}] 绑定的工作流文件不存在: {workflow}.json，跳过启动", "DeviceManager");
                    skipped++;
                    continue;
                }

                if (StartDevice(kv.Key, workflow))
                    started++;
            }

            if (skipped > 0)
                _logger.Warn($"全部启动完成：{started} 台成功，{skipped} 台跳过（未绑定或文件缺失）", "DeviceManager");
            else if (started > 0)
                _logger.Info($"全部启动完成：{started} 台设备已启动", "DeviceManager");
            else
                _logger.Warn("没有设备可启动", "DeviceManager");
        }

        public void StopAllDevices(int stopTimeoutMs = 1000)
        {
            var keys = _devices.Keys.ToList();
            foreach (var id in keys)
                StopDevice(id, stopTimeoutMs);
        }

        // ================================================================
        // 工作流绑定 / 切换
        // ================================================================
        public bool SetBoundWorkflow(string deviceId, string workflowName)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null)
                return false;
            runtime.BoundWorkflowName = workflowName;
            _logger.Info($"设备 [{runtime.Config.Name}] 已绑定工作流: {workflowName}", "DeviceManager");
            return true;
        }

        public string? GetBoundWorkflow(string deviceId)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null)
                return null;
            return runtime.BoundWorkflowName;
        }

        public bool SetDeviceWorkflow(string deviceId, string workflowName)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null) return false;
            if (runtime.StateMachine.CurrentState == DeviceState.Running)
            {
                _logger.Warn($"设备 [{runtime.Config.Name}] 正在运行，请先停止再切换工作流", "DeviceManager");
                return false;
            }
            runtime.CurrentWorkflowName = workflowName;
            runtime.CurrentWorkflow = null;
            _logger.Info($"设备 [{runtime.Config.Name}] 已设置工作流: {workflowName}", "DeviceManager");
            return true;
        }

        public string? GetDeviceWorkflow(string deviceId)
        {
            if (!_devices.TryGetValue(deviceId, out var runtime) || runtime == null)
                return null;
            return runtime.CurrentWorkflowName;
        }

        public DeviceRuntime? GetDevice(string deviceId)
        {
            _devices.TryGetValue(deviceId, out var runtime);
            return runtime;
        }

        public List<DeviceRuntime> GetAllDevices() => _devices.Values.ToList();

        // ================================================================
        // Modbus 信号读写（跨设备）
        // ================================================================
        public bool WriteRegisterSignal(string targetDeviceId, int address, ushort value)
        {
            var device = GetDevice(targetDeviceId);
            if (device == null || !device.IsOnline) return false;
            try { return device.ModbusClient.WriteSingleRegister((ushort)address, value); }
            catch { return false; }
        }

        public bool WriteRegisterSignal(string targetDeviceId, int address, ushort[] values)
        {
            var device = GetDevice(targetDeviceId);
            if (device == null || !device.IsOnline) return false;
            try { return device.ModbusClient.WriteMultipleRegisters((ushort)address, values); }
            catch { return false; }
        }

        public ushort[]? ReadRegisterSignal(string targetDeviceId, int address, int count = 1)
        {
            var device = GetDevice(targetDeviceId);
            if (device == null || !device.IsOnline) return null;
            try { return device.ModbusClient.ReadHoldingRegistersWithRaw((ushort)address, (ushort)count)?.RawRegisters; }
            catch { return null; }
        }

        public bool WriteSignal(string targetDeviceId, int address, bool value)
        {
            var device = GetDevice(targetDeviceId);
            if (device == null || !device.IsOnline) return false;
            try { return device.ModbusClient.WriteSingleCoil((ushort)address, value); }
            catch { return false; }
        }

        public bool? ReadSignal(string targetDeviceId, int address)
        {
            var device = GetDevice(targetDeviceId);
            if (device == null || !device.IsOnline) return null;
            try
            {
                var result = device.ModbusClient.ReadDataByType((ushort)address, 1);
                if (result == null || result.RawRegisters.Length == 0) return null;
                return (result.RawRegisters[0] & 0x0001) != 0;
            }
            catch { return null; }
        }

        // ================================================================
        // 配置导出 / 导入
        // ================================================================
        public string ExportConfig()
        {
            var list = _devices.Values.Select(r => r.Config).ToList();
            return JsonSerializer.Serialize(list, _jsonOptions);
        }

        public bool ImportConfig(string json)
        {
            try
            {
                var configs = JsonSerializer.Deserialize<List<DeviceConfig>>(json);
                if (configs == null) return false;

                StopAllDevices();
                foreach (var kv in _devices.ToList())
                {
                    try { kv.Value.ModbusClient?.Disconnect(); } catch { }
                    try { kv.Value.Watchdog?.Dispose(); } catch { }
                    _devices.TryRemove(kv.Key, out _);
                }

                foreach (var config in configs)
                    AddDevice(config);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ 导入配置失败: {ex.Message}", "DeviceManager");
                return false;
            }
        }

        // ================================================================
        // 复位
        // ================================================================
        public enum ResetMode { HardReset, SoftReset, FullReset }

        public async Task<bool> ResetWorkflowAsync(string deviceId, ResetMode mode = ResetMode.SoftReset, string triggeredBy = "系统")
        {
            if (!_devices.TryGetValue(deviceId, out var runtime))
            {
                _logger.Warn($"复位失败：设备 [{deviceId}] 不存在", "DeviceManager");
                return false;
            }

            lock (runtime)
            {
                if (runtime.StateMachine.CurrentState == DeviceState.Running)
                {
                    try { runtime.WorkflowCts?.Cancel(); } catch { }
                    try { runtime.CurrentCommand?.Stop(); } catch { }

                    if (runtime.WorkflowTask != null && !runtime.WorkflowTask.IsCompleted)
                    {
                        try
                        {
                            var finished = Task.WhenAny(runtime.WorkflowTask, Task.Delay(2000)).GetAwaiter().GetResult();
                            if (finished != runtime.WorkflowTask)
                                _logger.Warn($"设备 [{runtime.Config.Name}] 复位超时 (2s)，强制重置状态", "DeviceManager");
                        }
                        catch { }
                    }
                }

                switch (mode)
                {
                    case ResetMode.HardReset:
                        runtime.Config.CurrentCount = 0;
                        runtime.CurrentStepIndex = 0;
                        runtime.WorkflowContext.Clear();
                        runtime.StateMachine.ForceSet(DeviceState.Idle, "硬重置");
                        _logger.Info($"设备 [{runtime.Config.Name}] 硬重置：产量归零，从头开始", "DeviceManager");
                        break;
                    case ResetMode.SoftReset:
                        runtime.LastError = "";
                        runtime.StateMachine.ForceSet(DeviceState.Idle, "软重置");
                        _logger.Info($"设备 [{runtime.Config.Name}] 软重置：从步骤 {runtime.CurrentStepIndex + 1} 继续", "DeviceManager");
                        break;
                    case ResetMode.FullReset:
                        runtime.Config.CurrentCount = 0;
                        runtime.CurrentStepIndex = 0;
                        runtime.CurrentWorkflow = null;
                        runtime.WorkflowContext.Clear();
                        runtime.StateMachine.ForceSet(DeviceState.Idle, "完全重置");
                        _logger.Info($"设备 [{runtime.Config.Name}] 完全重置：产量归零，重新加载工作流", "DeviceManager");
                        break;
                }

                runtime.LastError = "";
                runtime.Watchdog.Feed();

                var user = SessionManager.CurrentUser;
                AuditService.Log(
                    userId: user?.Id ?? 0,
                    username: user?.Username ?? triggeredBy,
                    actionType: "WorkflowReset",
                    detail: $"设备 [{runtime.Config.Name}] {mode} 触发，当前产量 {runtime.Config.CurrentCount}，步骤索引 {runtime.CurrentStepIndex}",
                    repo: _repository
                );

                OnDeviceStepChanged?.Invoke(deviceId, "空闲");
                return true;
            }
        }

        // ================================================================
        // 🔄 核心循环（状态机驱动）
        // ================================================================
        private async Task DeviceLoopAsync(DeviceRuntime runtime, CancellationToken ct)
        {
            var config = runtime.Config;
            var watchdog = runtime.Watchdog;
            var stateMachine = runtime.StateMachine;

            try
            {
                _workflowEngine.EnsureWorkflowLoaded(runtime);
                var workflow = runtime.CurrentWorkflow;

                if (workflow == null || workflow.Commands.Count == 0)
                {
                    _logger.Warn($"设备 [{config.Name}] 未配置有效工作流，进入空闲保活模式", "DeviceManager");
                    await RunIdleKeepAliveAsync(runtime, ct);
                    return;
                }

                if (!runtime.ModbusClient.IsConnected && !runtime.ModbusClient.Reconnect(config.Modbus))
                {
                    _logger.Error($"设备 [{config.Name}] Modbus 初始连接失败，循环退出", "DeviceManager");
                    stateMachine.ForceSet(DeviceState.Disconnected, "初始连接失败");
                    return;
                }

                watchdog.Start();
                _logger.Info($"✅ 设备 [{config.Name}] 开始执行工作流: {workflow.Name}，目标产量 {config.TargetCount}", "DeviceManager");

                while (!ct.IsCancellationRequested && config.CurrentCount < config.TargetCount)
                {
                    try
                    {
                        var action = await PreFlightCheckAsync(runtime, ct);
                        if (action == PreFlightAction.Exit) break;
                        if (action == PreFlightAction.Continue) continue;

                        var state = stateMachine.CurrentState;
                        if (state != DeviceState.Running && state != DeviceState.Idle)
                        {
                            await Task.Delay(500, ct);
                            continue;
                        }

                        stateMachine.ForceSet(DeviceState.Running, "开始周期");

                        var cycleResult = _workflowEngine.ExecuteCycle(
                            runtime, ct,
                            step => OnDeviceStepChanged?.Invoke(config.DeviceId, step));

                        // ================================================================
                        // ★ 修复点 4：失败后不再 2 秒自动重试，改为阻塞等待人工干预
                        // ================================================================
                        if (cycleResult.IsFaulted)
                        {
                            runtime.CurrentStepIndex = cycleResult.StepIndex;
                            stateMachine.ForceSet(DeviceState.Error, $"工作流失败: {cycleResult.FaultReason}");
                            _logger.Error($"设备 [{config.Name}] 工作流执行失败，已暂停于步骤 {cycleResult.StepIndex + 1}: {cycleResult.FaultReason}", "DeviceManager");
                            _alarmManager.TriggerAlarm(config.DeviceId, $"工作流暂停: {cycleResult.FaultReason}", AlarmSeverity.Error);

                            // 🆕 阻塞等待外部干预：
                            //   - 用户点"复位" → 状态变 Idle → 唤醒继续
                            //   - 用户点"停止" → ct 取消 → 退出循环
                            _logger.Info($"设备 [{config.Name}] 进入故障暂停，等待人工复位或停止...", "DeviceManager");
                            while (!ct.IsCancellationRequested
                                   && stateMachine.CurrentState != DeviceState.Idle
                                   && stateMachine.CurrentState != DeviceState.Running)
                            {
                                try { await Task.Delay(500, ct); }
                                catch (OperationCanceledException) { break; }
                            }

                            if (ct.IsCancellationRequested)
                            {
                                _logger.Info($"设备 [{config.Name}] 故障暂停期间收到停止信号", "DeviceManager");
                                break;
                            }

                            _logger.Info($"设备 [{config.Name}] 检测到状态恢复，重新进入周期", "DeviceManager");
                            continue;
                        }

                        // 成功：产量 +1
                        runtime.CurrentStepIndex = 0;
                        if (config.CurrentCount < config.TargetCount)
                        {
                            config.CurrentCount++;
                            OnDeviceProductionUpdated?.Invoke(config.DeviceId, config.CurrentCount, config.TargetCount);
                            ProductionService.RecordProduction(config.DeviceId, config.CurrentCount, config.TargetCount);
                            _logger.Info($"📈 设备 [{config.Name}] 产量 +1，当前 {config.CurrentCount}/{config.TargetCount}", "DeviceManager");
                        }

                        watchdog.Feed();
                        await Task.Delay(100, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Info($"设备 [{config.Name}] 循环被取消", "DeviceManager");
                        break;
                    }
                    catch (Exception ex)
                    {
                        runtime.LastError = ex.Message;
                        _logger.Error($"设备 [{config.Name}] 循环异常: {ex.Message}", "DeviceManager");
                        stateMachine.ForceSet(DeviceState.Error, ex.Message);
                        _alarmManager.TriggerAlarm(config.DeviceId, $"循环异常: {ex.Message}", AlarmSeverity.Error);

                        // 🆕 与上面失败处理一样：阻塞等待人工干预
                        _logger.Info($"设备 [{config.Name}] 进入故障暂停（异常），等待人工复位或停止...", "DeviceManager");
                        while (!ct.IsCancellationRequested
                               && stateMachine.CurrentState != DeviceState.Idle
                               && stateMachine.CurrentState != DeviceState.Running)
                        {
                            try { await Task.Delay(500, ct); }
                            catch (OperationCanceledException) { break; }
                        }

                        if (ct.IsCancellationRequested) break;
                        continue;
                    }
                    finally
                    {
                        runtime.CurrentCommand = null;
                    }
                }
            }
            finally
            {
                watchdog.Stop();
                runtime.CurrentCommand = null;
                stateMachine.ForceSet(DeviceState.Idle, "循环结束");
                _logger.Info($"⏹ 设备 [{config.Name}] 工作流循环已退出 (产量 {config.CurrentCount}/{config.TargetCount})", "DeviceManager");
            }
        }

        private enum PreFlightAction { Proceed, Continue, Exit }

        private async Task<PreFlightAction> PreFlightCheckAsync(DeviceRuntime runtime, CancellationToken ct)
        {
            var config = runtime.Config;
            var modbus = runtime.ModbusClient;
            var watchdog = runtime.Watchdog;
            var stateMachine = runtime.StateMachine;

            if (_model.IsEmergencyStopPressed())
            {
                _logger.Warn($"⚠️ 检测到硬件急停信号！设备 [{config.Name}] 强制暂停", "DeviceManager");
                stateMachine.ForceSet(DeviceState.EmergencyStop, "硬件急停");

                while (_model.IsEmergencyStopPressed() && !ct.IsCancellationRequested)
                {
                    await Task.Delay(200, ct);
                }

                _logger.Info($"设备 [{config.Name}] 急停已复位，恢复运行", "DeviceManager");
                stateMachine.ForceSet(DeviceState.Idle, "急停复位");
                return PreFlightAction.Continue;
            }

            if (watchdog.IsTimeout)
            {
                _logger.Warn($"设备 [{config.Name}] 看门狗超时，尝试恢复", "DeviceManager");
                if (!modbus.IsConnected)
                {
                    if (modbus.Reconnect(config.Modbus))
                    {
                        watchdog.Feed();
                        _logger.Info($"设备 [{config.Name}] 重连成功，看门狗重置", "DeviceManager");
                    }
                    else
                    {
                        _logger.Error($"设备 [{config.Name}] 重连失败，循环退出", "DeviceManager");
                        stateMachine.ForceSet(DeviceState.Disconnected, "重连失败");
                        return PreFlightAction.Exit;
                    }
                }
                else
                {
                    watchdog.Feed();
                    _logger.Warn($"设备 [{config.Name}] 看门狗已重置（连接正常）", "DeviceManager");
                }
            }

            if (!modbus.IsConnected)
            {
                _logger.Warn($"设备 [{config.Name}] Modbus 连接断开，尝试重连", "DeviceManager");
                if (modbus.Reconnect(config.Modbus))
                {
                    watchdog.Feed();
                    _logger.Info($"设备 [{config.Name}] 重连成功", "DeviceManager");
                }
                else
                {
                    await Task.Delay(2000, ct);
                    return PreFlightAction.Continue;
                }
            }

            return PreFlightAction.Proceed;
        }

        private async Task RunIdleKeepAliveAsync(DeviceRuntime runtime, CancellationToken ct)
        {
            var config = runtime.Config;
            var modbus = runtime.ModbusClient;
            var watchdog = runtime.Watchdog;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!modbus.IsConnected)
                    {
                        if (modbus.Reconnect(config.Modbus))
                            _logger.Info($"设备 [{config.Name}] 空闲重连成功", "DeviceManager");
                        else
                            await Task.Delay(2000, ct);
                    }
                    else
                    {
                        watchdog.Feed();
                        await Task.Delay(1000, ct);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.Error($"设备 [{config.Name}] 空闲保活异常: {ex.Message}", "DeviceManager");
                    await Task.Delay(2000, ct);
                }
            }
        }

        // ================================================================
        // 资源释放
        // ================================================================
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopAllDevices();
            foreach (var runtime in _devices.Values)
            {
                if (runtime.ConnectionStateChangedHandler != null && runtime.ModbusClient != null)
                    runtime.ModbusClient.ConnectionStateChanged -= runtime.ConnectionStateChangedHandler;
                if (runtime.StateChangedHandler != null && runtime.StateMachine != null)
                    runtime.StateMachine.StateChanged -= runtime.StateChangedHandler;
                try { runtime.ModbusClient?.Dispose(); } catch { }
                try { runtime.Watchdog?.Dispose(); } catch { }
            }
            _devices.Clear();
            _logger.Info("🗑️ DeviceManager 已释放", "DeviceManager");
        }
    }
}