using GtsTest.Commands;
using GtsTest.Core;
using GtsTest.Modbus;
using GtsTest.Models;
using GtsTest.Services.Alarm;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using GtsTest.Services.Plc; // 🆕 引入 PlcManager
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GtsTest.Presenters
{
    public class GtsPresenter
    {
        private readonly IGtsView _view;
        private readonly GtsModel _model;
        private readonly DeviceManager _deviceManager;
        private readonly ILogger _logger;
        private readonly IDataRepository _repository;
        private readonly IAlarmManager _alarmManager;
        private readonly IAuthenticationService _authService;
        private readonly PlcManager _plcManager; // 🆕 新增字段

        private string _selectedDeviceId = "";
        private CancellationTokenSource _workflowCts;

        // 🆕 构造函数增加 PlcManager 参数
        public GtsPresenter(
            IGtsView view,
            GtsModel model,
            DeviceManager deviceManager,
            ILogger logger,
            IDataRepository repository,
            IAlarmManager alarmManager,
            IAuthenticationService authService,
            PlcManager plcManager)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository;
            _alarmManager = alarmManager;
            _authService = authService;
            _plcManager = plcManager; // 🆕 赋值

            _view.LoadView += OnLoadView;
            _view.DeviceSelected += OnDeviceSelected;

            _view.AddDeviceClicked += OnAddDevice;
            _view.RemoveDeviceClicked += OnRemoveDevice;
            _view.StartAllClicked += OnStartAll;
            _view.StopAllClicked += OnStopAll;
            _view.StartSelectedClicked += OnStartSelected;
            _view.StopSelectedClicked += OnStopSelected;
            _view.ResetDeviceClicked += OnResetDevice;

            _view.AlarmResetClicked += OnAlarmReset;
            _view.AlarmAcknowledgeClicked += OnAlarmAcknowledge;
            _view.AlarmResolveClicked += OnAlarmResolve;

            _view.EmergencyStopClicked += OnEmergencyStop;

            _view.SystemConfigClicked += OnSystemConfig;
            _view.LoginClicked += OnLogin;

            _view.WorkflowRunClicked += OnWorkflowRun;
            _view.WorkflowStopClicked += OnWorkflowStop;
            _view.DeviceForWorkflowSelected += OnDeviceForWorkflowSelected;
            _view.ProductionResetClicked += OnProductionReset;
            _view.BindDeviceWorkflowClicked += OnBindDeviceWorkflow;

            _view.DeviceForWorkflowSelected += (s, deviceId) =>
            {
                _selectedDeviceId = deviceId;
                UpdateStatusBar();
            };

            _deviceManager.OnDeviceOnlineChanged += (id, online) =>
            {
                RunOnUI(() =>
                {
                    _view.UpdateDeviceOnlineStatus(id, online);
                    if (id == _selectedDeviceId) UpdateStatusBar();
                });
            };

            _deviceManager.OnDeviceStepChanged += (id, step) =>
            {
                RunOnUI(() =>
                {
                    _view.UpdateDeviceStep(id, step);
                    if (id == _selectedDeviceId) UpdateStatusBar();
                });
            };

            _deviceManager.OnDeviceProductionUpdated += (id, current, target) =>
            {
                RunOnUI(() =>
                {
                    _view.UpdateDeviceProduction(id, current, target);
                    UpdateGlobalStats();
                });
            };

            _deviceManager.OnDeviceStateChanged += (id, state, reason) =>
            {
                RunOnUI(() =>
                {
                    _view.UpdateDeviceState(id, state, reason);
                    if (id == _selectedDeviceId) UpdateStatusBar();
                });
            };

            if (_alarmManager != null)
            {
                _alarmManager.AlarmAdded += (s, a) => RunOnUI(() => UpdateAlarmList());
                _alarmManager.AlarmAcknowledged += (s, id) => RunOnUI(() => UpdateAlarmList());
                _alarmManager.AlarmResolved += (s, id) => RunOnUI(() => UpdateAlarmList());
            }

            AppLogger.OnLogReceived += (level, logLine, category) =>
            {
                RunOnUI(() =>
                {
                    if (category == "Monitor" || category == "Modbus" || category == "DeviceManager")
                        _view.AppendMonitorLog(logLine);
                    else
                        _view.AppendOperationLog(logLine);
                });
            };

            SessionManager.OnUserChanged += OnUserChanged;
        }

        private void RunOnUI(Action action)
        {
            if (_view is Control control && control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }

        private bool CheckPermission(string permissionCode)
        {
            var user = SessionManager.CurrentUser;
            if (user == null)
            {
                _view.ShowMessage("请先登录！", "未登录", MessageType.Warning);
                return false;
            }
            if (!_authService.HasPermission(user, permissionCode))
            {
                _view.ShowMessage($"用户 {user.Username} 没有执行此操作的权限！", "权限不足", MessageType.Warning);
                return false;
            }
            return true;
        }

        private bool IsLoggedIn() => SessionManager.CurrentUser != null;

        private void OnUserChanged(User user)
        {
            RunOnUI(() =>
            {
                string role = user?.Role ?? "未登录";
                _view.UpdateUIByPermissions(role);
                if (user != null)
                    _view.ShowMessage($"欢迎 {user.FullName} ({user.Role})", "登录成功", MessageType.Info);
                UpdateDeviceList();
                UpdateStatusBar();
                UpdateGlobalStats();
                UpdateAlarmList();
            });
        }

        private void OnLoadView(object sender, EventArgs e)
        {
            LoadDefaultDevices();
            RunOnUI(() =>
            {
                _view.SetSimulationMode(GtsModel.UseSimulation);
                UpdateDeviceList();
                UpdateGlobalStats();
                UpdateStatusBar();
                UpdateAlarmList();
                var user = SessionManager.CurrentUser;
                _view.UpdateUIByPermissions(user?.Role ?? "未登录");
            });
        }

        private void LoadDefaultDevices()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json");
            bool hasDevices = false;

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (_deviceManager.ImportConfig(json))
                        hasDevices = _deviceManager.GetAllDevices().Count > 0;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"加载 devices.json 失败: {ex.Message}", "Init");
                }
            }

            if (!hasDevices)
            {
                var configs = new[]
                {
                    new DeviceConfig { DeviceId = "dev-001", Name = "设备1-焊接", Modbus = new ModbusConfig { IpAddress = "192.168.1.10", Port = 502, StartAddress = 0, RegisterCount = 10 }, Axis = 1, TargetCount = 100 },
                    new DeviceConfig { DeviceId = "dev-002", Name = "设备2-检测", Modbus = new ModbusConfig { IpAddress = "192.168.1.11", Port = 502, StartAddress = 100, RegisterCount = 5 }, Axis = 2, TargetCount = 100 },
                    new DeviceConfig { DeviceId = "dev-003", Name = "设备3-包装", Modbus = new ModbusConfig { IpAddress = "192.168.1.12", Port = 502, StartAddress = 200, RegisterCount = 8 }, Axis = 3, TargetCount = 100 }
                };
                foreach (var cfg in configs)
                    _deviceManager.AddDevice(cfg);

                _logger.Info("已加载默认设备（3台）", "Init");
            }
        }

        private void UpdateDeviceList()
        {
            var devices = _deviceManager.GetAllDevices();
            var items = devices.Select(d => new DeviceListItem
            {
                DeviceId = d.Config.DeviceId,
                Name = d.Config.Name,
                IsOnline = d.IsOnline,
                State = d.StateMachine.CurrentState
            });
            _view.UpdateDeviceList(items);

            if (!string.IsNullOrEmpty(_selectedDeviceId) && devices.Any(d => d.Config.DeviceId == _selectedDeviceId))
                _view.SelectDevice(_selectedDeviceId);
            else if (devices.Count > 0)
            {
                _selectedDeviceId = devices[0].Config.DeviceId;
                _view.SelectDevice(_selectedDeviceId);
            }
        }

        private void UpdateGlobalStats()
        {
            var devices = _deviceManager.GetAllDevices();
            int online = devices.Count(d => d.IsOnline);
            int total = devices.Sum(d => d.Config.CurrentCount);
            _view.UpdateGlobalStats(online, devices.Count, total);
        }

        private void UpdateStatusBar()
        {
            var device = _deviceManager.GetDevice(_selectedDeviceId);
            if (device == null)
            {
                _view.UpdateStatusBar("", false, DeviceState.Idle, false, "正常", false, "空闲", 0, false);
                return;
            }

            bool isOnline = device.IsOnline;
            bool servoOn = false;
            string limitStatus = "正常";

            if (isOnline)
            {
                uint clk;
                int status = 0;
                _model.GetAxisStatus(device.Config.Axis, out status, out clk);
                servoOn = (status & 0x200) != 0;
                if ((status & 0x01) != 0) limitStatus = "正向限位";
                else if ((status & 0x02) != 0) limitStatus = "负向限位";
            }

            bool modbusConnected = device.ModbusClient?.IsConnected ?? false;
            int remaining = device.Watchdog.RemainingMs;
            bool timeout = device.Watchdog.IsTimeout;

            _view.UpdateStatusBar(
                device.Config.Name,
                isOnline,
                device.StateMachine.CurrentState,
                servoOn,
                limitStatus,
                modbusConnected,
                device.CurrentStep,
                remaining,
                timeout
            );
        }

        private void UpdateAlarmList()
        {
            if (_alarmManager != null)
                _view.UpdateAlarmList(_alarmManager.GetActiveAlarms());
        }

        private void OnDeviceSelected(object sender, EventArgs e)
        {
            _selectedDeviceId = _view.GetSelectedDeviceId();
            UpdateStatusBar();
        }

        public void OnDeviceSelected(string deviceId)
        {
            _selectedDeviceId = deviceId;
            RunOnUI(() =>
            {
                UpdateStatusBar();
                _view.SelectDevice(deviceId);
            });
        }

        private void OnAddDevice(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.Add")) return;
            using (var form = new DeviceConfigForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _deviceManager.AddDevice(form.Config);
                    RunOnUI(() =>
                    {
                        UpdateDeviceList();
                        UpdateGlobalStats();
                        _view.ShowMessage($"设备 {form.Config.Name} 添加成功", "提示", MessageType.Info);
                    });
                }
            }
        }

        private void OnRemoveDevice(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.Remove")) return;
            string id = _view.GetSelectedDeviceId();
            if (string.IsNullOrEmpty(id)) return;
            var device = _deviceManager.GetDevice(id);
            if (device == null) return;

            if (_view.ShowConfirm($"确定移除设备 {device.Config.Name} 吗？", "确认删除"))
            {
                _deviceManager.StopDevice(id);
                _deviceManager.RemoveDevice(id);
                if (_selectedDeviceId == id)
                    _selectedDeviceId = "";
                RunOnUI(() =>
                {
                    UpdateDeviceList();
                    UpdateGlobalStats();
                    UpdateStatusBar();
                    _view.ShowMessage($"设备 {device.Config.Name} 已移除", "提示", MessageType.Info);
                });
            }
        }

        private void OnStartAll(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.StartAll")) return;
            var devices = _deviceManager.GetAllDevices();
            int offline = devices.Count(d => !d.IsOnline);
            if (offline > 0)
            {
                if (!_view.ShowConfirm($"有 {offline} 台设备未连接，是否跳过并启动已连接的？", "提示"))
                    return;
            }
            _deviceManager.StartAllDevices();
            var user = SessionManager.CurrentUser;
            AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "StartAllDevices", "启动全部设备", _repository);
            RunOnUI(() =>
            {
                UpdateDeviceList();
                UpdateStatusBar();
                _view.ShowMessage("全部设备已启动", "提示", MessageType.Info);
            });
        }

        private void OnStopAll(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.StopAll")) return;
            if (!_view.ShowConfirm("确定停止所有设备吗？", "确认停止"))
                return;
            _deviceManager.StopAllDevices();
            var user = SessionManager.CurrentUser;
            AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "StopAllDevices", "停止全部设备", _repository);
            RunOnUI(() =>
            {
                UpdateDeviceList();
                UpdateStatusBar();
                _view.ShowMessage("全部设备已停止", "提示", MessageType.Info);
            });
        }

        private void OnStartSelected(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.Start")) return;
            string deviceId = _selectedDeviceId;
            if (string.IsNullOrEmpty(deviceId))
            {
                _view.ShowMessage("请先选择一个设备", "提示", MessageType.Warning);
                return;
            }
            var device = _deviceManager.GetDevice(deviceId);
            if (device == null) return;
            if (!device.IsOnline)
            {
                _view.ShowMessage($"设备 {device.Config.Name} 不在线，无法启动", "警告", MessageType.Warning);
                return;
            }
            if (device.StateMachine.CurrentState == DeviceState.Running)
            {
                _view.ShowMessage($"设备 {device.Config.Name} 已在运行中", "提示", MessageType.Info);
                return;
            }

            string workflowName = _view.GetSelectedWorkflowName();
            if (string.IsNullOrEmpty(workflowName))
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
                var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json") : new string[0];
                var workflowNames = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                if (workflowNames.Count == 0)
                {
                    _view.ShowMessage("没有可用的工作流文件，请先在系统管理中创建工作流", "错误", MessageType.Error);
                    return;
                }
                using (var form = new Form())
                {
                    form.Text = "选择工作流";
                    form.Size = new Size(300, 150);
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Height = 30 };
                    combo.Items.AddRange(workflowNames.ToArray());
                    if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                    var btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 35 };
                    var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 35 };
                    var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
                    panel.Controls.Add(combo);
                    panel.Controls.Add(btnOK);
                    panel.Controls.Add(btnCancel);
                    form.Controls.Add(panel);
                    if (form.ShowDialog() != DialogResult.OK) return;
                    workflowName = combo.SelectedItem?.ToString() ?? "";
                }
                if (string.IsNullOrEmpty(workflowName))
                {
                    _view.ShowMessage("未选择工作流", "提示", MessageType.Warning);
                    return;
                }
            }

            if (!_deviceManager.SetDeviceWorkflow(deviceId, workflowName))
            {
                _view.ShowMessage($"设置工作流失败，设备可能正在运行", "错误", MessageType.Error);
                return;
            }

            if (_deviceManager.StartDevice(deviceId))
            {
                var user = SessionManager.CurrentUser;
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "DeviceStart", $"启动设备 {device.Config.Name}，工作流: {workflowName}", _repository);
                _view.ShowMessage($"设备 {device.Config.Name} 已启动，工作流: {workflowName}", "提示", MessageType.Info);
                UpdateStatusBar();
                UpdateDeviceList();
            }
            else
            {
                _view.ShowMessage($"设备 {device.Config.Name} 启动失败", "错误", MessageType.Error);
            }
        }

        private void OnBindDeviceWorkflow(object sender, EventArgs e)
        {
            if (!CheckPermission("Workflow.Bind")) return;

            string deviceName = _view.GetSelectedDeviceName();
            string workflowName = _view.GetSelectedWorkflowName();

            if (string.IsNullOrEmpty(deviceName))
            {
                _view.ShowMessage("请先选择设备", "提示", MessageType.Warning);
                return;
            }
            if (string.IsNullOrEmpty(workflowName))
            {
                _view.ShowMessage("请先选择工作流", "提示", MessageType.Warning);
                return;
            }

            var device = _deviceManager.GetAllDevices().FirstOrDefault(d => d.Config.Name == deviceName);
            if (device == null)
            {
                _view.ShowMessage("设备未找到", "错误", MessageType.Error);
                return;
            }

            if (_deviceManager.SetBoundWorkflow(device.Config.DeviceId, workflowName))
            {
                _view.ShowMessage($"设备 {deviceName} 已绑定工作流: {workflowName}", "绑定成功", MessageType.Info);
                var user = SessionManager.CurrentUser;
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "BindWorkflow", $"设备 {deviceName} 绑定工作流 {workflowName}", _repository);
            }
            else
            {
                _view.ShowMessage("绑定失败，请重试", "错误", MessageType.Error);
            }
        }

        // ✅ 修复点 1：OnStopSelected —— 只要不是 Idle 就允许停止
        private void OnStopSelected(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.Stop")) return;
            string deviceId = _selectedDeviceId;
            if (string.IsNullOrEmpty(deviceId))
            {
                _view.ShowMessage("请先选择一个设备", "提示", MessageType.Warning);
                return;
            }
            var device = _deviceManager.GetDevice(deviceId);
            if (device == null) return;

            if (device.StateMachine.CurrentState == DeviceState.Idle)
            {
                _view.ShowMessage($"设备 {device.Config.Name} 未在运行", "提示", MessageType.Info);
                return;
            }

            if (!_view.ShowConfirm($"确定停止设备 {device.Config.Name} 吗？", "确认停止"))
                return;

            if (_deviceManager.StopDevice(deviceId))
            {
                var user = SessionManager.CurrentUser;
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "DeviceStop", $"停止设备 {device.Config.Name}", _repository);
                _view.ShowMessage($"设备 {device.Config.Name} 已停止", "提示", MessageType.Info);
                UpdateStatusBar();
                UpdateDeviceList();
            }
            else
            {
                _view.ShowMessage($"设备 {device.Config.Name} 停止失败", "错误", MessageType.Error);
            }
        }

        private async void OnResetDevice(object sender, EventArgs e)
        {
            if (!CheckPermission("Device.Reset")) return;
            string deviceId = _selectedDeviceId;
            if (string.IsNullOrEmpty(deviceId))
            {
                _view.ShowMessage("请先选择一个设备", "提示", MessageType.Warning);
                return;
            }
            var device = _deviceManager.GetDevice(deviceId);
            if (device == null) return;

            var result = MessageBox.Show(
                $"请选择复位模式：\n\n" +
                $"• 点击「是」= 软复位（从断点继续，保留产量）\n" +
                $"• 点击「否」= 硬复位（产量归零，从头开始）",
                "选择复位模式",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            DeviceManager.ResetMode mode;
            if (result == DialogResult.Yes)
                mode = DeviceManager.ResetMode.SoftReset;
            else if (result == DialogResult.No)
                mode = DeviceManager.ResetMode.HardReset;
            else
                return;

            string confirmMsg = mode == DeviceManager.ResetMode.SoftReset
                ? $"确定要软复位设备 {device.Config.Name} 吗？"
                : $"⚠️ 硬复位将清零产量 {device.Config.Name}，确定要继续吗？";

            if (!_view.ShowConfirm(confirmMsg, "确认复位"))
                return;

            var user = SessionManager.CurrentUser;
            bool success = await _deviceManager.ResetWorkflowAsync(deviceId, mode, user?.Username ?? "操作员");

            if (success)
            {
                _view.ShowMessage($"设备 {device.Config.Name} 工作流已复位", "提示", MessageType.Info);
                UpdateStatusBar();
                UpdateDeviceList();
                UpdateGlobalStats();
            }
            else
            {
                _view.ShowMessage($"设备 {device.Config.Name} 复位失败", "错误", MessageType.Error);
            }
        }

        private void OnAlarmReset(object sender, EventArgs e)
        {
            if (!CheckPermission("Alarm.Reset")) return;
            if (_alarmManager == null) return;
            var user = SessionManager.CurrentUser;
            var alarms = _alarmManager.GetActiveAlarms().ToList();
            if (alarms.Count == 0)
            {
                _view.ShowMessage("没有活动报警需要复位", "提示", MessageType.Info);
                return;
            }
            int count = 0;
            foreach (var alarm in alarms)
            {
                if (!alarm.IsAcknowledged)
                {
                    if (_alarmManager.AcknowledgeAlarm(alarm.Id, user?.Username ?? "系统"))
                        count++;
                }
                if (!alarm.IsResolved)
                {
                    if (_alarmManager.ResolveAlarm(alarm.Id, user?.Username ?? "系统"))
                        count++;
                }
            }
            AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "ResetAlarms", $"复位了 {count} 个报警", _repository);
            _view.ShowMessage($"已复位 {count} 个报警", "提示", MessageType.Info);
        }

        private void OnAlarmAcknowledge(object sender, EventArgs e)
        {
            if (!CheckPermission("Alarm.Acknowledge")) return;
            string idStr = _view.GetSelectedAlarmId();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int alarmId))
            {
                _view.ShowMessage("请先选择一个报警", "提示", MessageType.Warning);
                return;
            }
            var user = SessionManager.CurrentUser;
            if (_alarmManager.AcknowledgeAlarm(alarmId, user?.Username ?? "系统"))
            {
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "AcknowledgeAlarm", $"确认报警 {alarmId}", _repository);
                _view.ShowMessage("报警已确认", "提示", MessageType.Info);
            }
            else
            {
                _view.ShowMessage("确认失败，可能该报警已确认或已解决", "错误", MessageType.Error);
            }
        }

        private void OnAlarmResolve(object sender, EventArgs e)
        {
            if (!CheckPermission("Alarm.Resolve")) return;
            string idStr = _view.GetSelectedAlarmId();
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int alarmId))
            {
                _view.ShowMessage("请先选择一个报警", "提示", MessageType.Warning);
                return;
            }
            var user = SessionManager.CurrentUser;
            if (_alarmManager.ResolveAlarm(alarmId, user?.Username ?? "系统"))
            {
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "ResolveAlarm", $"解决报警 {alarmId}", _repository);
                _view.ShowMessage("报警已解决", "提示", MessageType.Info);
            }
            else
            {
                _view.ShowMessage("解决失败，可能该报警已解决", "错误", MessageType.Error);
            }
        }

        private void OnEmergencyStop(object sender, EventArgs e)
        {
            _deviceManager.StopAllDevices();
            _model.GT_Stop(0xFF, 0);
            _logger.Warn("⚠️ 全局急停触发！", "Operation");
            var user = SessionManager.CurrentUser;
            AuditService.Log(
                user?.Id ?? 0,
                user?.Username ?? "未登录",
                "EmergencyStop",
                "全局急停触发（紧急停止）",
                _repository
            );
            _view.ShowMessage("全局急停已触发，所有运动停止", "急停", MessageType.Warning);
            RunOnUI(() =>
            {
                UpdateDeviceList();
                UpdateStatusBar();
            });
        }

        private void OnLogin(object sender, EventArgs e)
        {
            if (SessionManager.IsLoggedIn)
            {
                SessionManager.Logout(_repository);
                _view.ShowMessage("已注销", "提示", MessageType.Info);
                return;
            }
            using (var login = new LoginForm(_authService, _repository))
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    // SessionManager 已触发 OnUserChanged
                }
            }
        }

        private void OnSystemConfig(object sender, EventArgs e)
        {
            if (!CheckPermission("System.Config")) return;
            var user = SessionManager.CurrentUser;
            using (var form = new SystemConfigForm(
                _deviceManager,
                _model,
                _repository,
                _authService,
                user,
                _plcManager)) // 🆕 传入 _plcManager
            {
                form.ShowDialog(_view as Form);
            }
        }

        private void OnDeviceForWorkflowSelected(object sender, string deviceId)
        {
            _selectedDeviceId = deviceId;
            UpdateStatusBar();
        }

        private void OnWorkflowRun(object sender, string workflowName)
        {
            if (!CheckPermission("Workflow.Run")) return;
            if (string.IsNullOrEmpty(_selectedDeviceId))
            {
                _view.ShowMessage("请先选择一台设备", "提示", MessageType.Warning);
                return;
            }
            var device = _deviceManager.GetDevice(_selectedDeviceId);
            if (device == null || !device.IsOnline)
            {
                _view.ShowMessage("设备不在线，无法运行工作流", "提示", MessageType.Warning);
                return;
            }
            if (string.IsNullOrEmpty(workflowName))
            {
                _view.ShowMessage("请选择工作流", "提示", MessageType.Warning);
                return;
            }
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows", workflowName + ".json");
            if (!File.Exists(filePath))
            {
                _view.ShowMessage($"工作流文件 {workflowName}.json 不存在", "错误", MessageType.Error);
                return;
            }
            var config = LoadWorkflowFromJson(filePath);
            if (config == null)
            {
                _view.ShowMessage("工作流加载失败", "错误", MessageType.Error);
                return;
            }

            string logMsg = $"启动工作流: {workflowName} (文件: {filePath}, 共 {config.Commands.Count} 条命令)";
            _logger.Info(logMsg, "Operation");
            _view.AppendExecutionLog(logMsg);
            _view.AppendExecutionLog($"设备: {device.Config.Name}");

            if (device.StateMachine.CurrentState == DeviceState.Running)
            {
                _view.AppendExecutionLog("⏹ 设备正在运行，先停止...");
                _deviceManager.StopDevice(_selectedDeviceId);
                Thread.Sleep(500);
                _view.AppendExecutionLog("✅ 设备已停止");
            }

            if (_deviceManager.StartDevice(_selectedDeviceId, workflowName))
            {
                _view.ShowMessage($"设备 {device.Config.Name} 已启动，工作流: {workflowName}", "提示", MessageType.Info);
                _view.AppendExecutionLog($"✅ 设备 {device.Config.Name} 已启动，工作流: {workflowName}");
                var user = SessionManager.CurrentUser;
                AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "RunWorkflow", $"启动工作流 {workflowName} 在设备 {device.Config.Name}", _repository);
            }
            else
            {
                _view.AppendExecutionLog($"❌ 设备 {device.Config.Name} 启动失败");
                _view.ShowMessage($"设备 {device.Config.Name} 启动失败", "错误", MessageType.Error);
            }
        }

        // ✅ 修复点 2：OnWorkflowStop —— 只要不是 Idle 就停
        private void OnWorkflowStop(object sender, EventArgs e)
        {
            if (!CheckPermission("Workflow.Stop")) return;

            if (!string.IsNullOrEmpty(_selectedDeviceId))
            {
                var device = _deviceManager.GetDevice(_selectedDeviceId);
                if (device != null && device.StateMachine.CurrentState != DeviceState.Idle)
                {
                    _view.AppendExecutionLog($"⏹ 正在停止设备 {device.Config.Name}...");
                    _deviceManager.StopDevice(_selectedDeviceId);
                    _view.AppendExecutionLog($"✅ 设备 {device.Config.Name} 已停止");
                }
                else
                {
                    _view.AppendExecutionLog($"设备 {device?.Config.Name ?? ""} 未在运行，无需停止");
                }
            }

            _workflowCts?.Cancel();
            _logger.Info("工作流已停止", "Operation");
            _view.AppendExecutionLog("⏹ 工作流已停止");
            var user = SessionManager.CurrentUser;
            AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "StopWorkflow", "停止工作流", _repository);
            _view.ShowMessage("工作流已停止", "提示", MessageType.Info);

            RunOnUI(() =>
            {
                UpdateDeviceList();
                UpdateStatusBar();
            });
        }

        private void OnProductionReset(object sender, EventArgs e)
        {
            if (!CheckPermission("Production.Reset")) return;
            if (string.IsNullOrEmpty(_selectedDeviceId))
            {
                _view.ShowMessage("请先选择一台设备", "提示", MessageType.Warning);
                return;
            }
            var device = _deviceManager.GetDevice(_selectedDeviceId);
            if (device == null) return;
            if (!_view.ShowConfirm($"确定要清零设备 {device.Config.Name} 的产量吗？", "确认清零"))
                return;
            device.Config.CurrentCount = 0;
            _view.UpdateDeviceProduction(_selectedDeviceId, 0, device.Config.TargetCount);
            UpdateGlobalStats();
            var user = SessionManager.CurrentUser;
            AuditService.Log(user?.Id ?? 0, user?.Username ?? "系统", "ProductionReset", $"清零设备 {device.Config.Name} 产量", _repository);
            _view.ShowMessage($"设备 {device.Config.Name} 产量已清零", "提示", MessageType.Info);
        }

        private WorkflowConfig LoadWorkflowFromJson(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                return System.Text.Json.JsonSerializer.Deserialize<WorkflowConfig>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}