using GtsTest.Controls;
using GtsTest.Core;
using GtsTest.Presenters;
using GtsTest.Services.Alarm;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Logging;
using GtsTest.Services.Plc; // 🆕 引入 PlcManager
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GtsTest
{
    public partial class Form1 : Form, IGtsView
    {
        // ---------- 事件 ----------
        public event EventHandler LoadView;
        public event EventHandler DeviceSelected;
        public event EventHandler AddDeviceClicked;
        public event EventHandler RemoveDeviceClicked;
        public event EventHandler StartAllClicked;
        public event EventHandler StopAllClicked;
        public event EventHandler StartSelectedClicked;
        public event EventHandler StopSelectedClicked;
        public event EventHandler ResetDeviceClicked;
        public event EventHandler AlarmResetClicked;
        public event EventHandler AlarmAcknowledgeClicked;
        public event EventHandler AlarmResolveClicked;
        public event EventHandler EmergencyStopClicked;
        public event EventHandler SystemConfigClicked;
        public event EventHandler LoginClicked;
        public event EventHandler<string> WorkflowRunClicked;
        public event EventHandler WorkflowStopClicked;
        public event EventHandler<string> DeviceForWorkflowSelected;
        public event EventHandler ProductionResetClicked;
        public event EventHandler BindDeviceWorkflowClicked;

        // ---------- 私有字段 ----------
        private GtsPresenter _presenter;
        private readonly IDataRepository _repo;
        private readonly IAuthenticationService _authService;
        private readonly DeviceManager _deviceManager;
        private readonly IAlarmManager _alarmManager;
        private readonly ILogger _logger;
        private bool _isSelectingDevice = false;

        // 🆕 构造函数增加 PlcManager 参数
        public Form1(
            DeviceManager deviceManager,
            IDataRepository repo,
            IAuthenticationService authService,
            IAlarmManager alarmManager,
            ILogger logger,
            PlcManager plcManager)
        {
            InitializeComponent();

            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _alarmManager = alarmManager ?? throw new ArgumentNullException(nameof(alarmManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var model = _deviceManager.Model;

            // ---- 注入到 overviewControl ----
            overviewControl.SetDeviceManager(_deviceManager, _alarmManager);

            // ---- 创建 Presenter 🆕 传入 plcManager ----
            _presenter = new GtsPresenter(
                this,
                model,
                _deviceManager,
                _logger,
                _repo,
                _alarmManager,
                _authService,
                plcManager
            );

            // ---- 初始化生产执行控件的下拉列表 ----
            string workflowsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
            if (Directory.Exists(workflowsDir))
            {
                var workflowFiles = Directory.GetFiles(workflowsDir, "*.json");
                var workflowNames = workflowFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                workflowExecutionControl.SetWorkflowList(workflowNames);
            }
            else
            {
                Directory.CreateDirectory(workflowsDir);
                workflowExecutionControl.SetWorkflowList(new List<string> { "Default" });
            }
            workflowExecutionControl.LoadWorkflowPreview();

            // 订阅生产执行控件事件
            workflowExecutionControl.ExecuteClicked += (s, workflowName) => WorkflowRunClicked?.Invoke(s, workflowName);
            workflowExecutionControl.StopClicked += (s, e) => WorkflowStopClicked?.Invoke(s, e);
            workflowExecutionControl.ResetClicked += (s, e) => ResetDeviceClicked?.Invoke(s, e);
            workflowExecutionControl.ProductionResetClicked += (s, e) => ProductionResetClicked?.Invoke(s, e);
            workflowExecutionControl.DeviceSelected += (s, deviceId) => DeviceForWorkflowSelected?.Invoke(s, deviceId);
            workflowExecutionControl.BindClicked += (s, e) => BindDeviceWorkflowClicked?.Invoke(s, e);

            // 设置 ListBox 绘制
            listBoxDevices.DrawItem += ListBoxDevices_DrawItem;

            // 键盘快捷键
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if ((e.Control && e.KeyCode == Keys.E) || e.KeyCode == Keys.Escape)
                {
                    EmergencyStopClicked?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                }
            };

            SetupToolTips();
        }

        // ==================== 实现 IGtsView ====================

        public void UpdateDeviceList(IEnumerable<DeviceListItem> items)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateDeviceList(items)));
                return;
            }

            listBoxDevices.Items.Clear();
            foreach (var item in items)
            {
                listBoxDevices.Items.Add(item);
            }

            workflowExecutionControl.SetDeviceList(items);
        }

        public void SelectDevice(string deviceId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SelectDevice(deviceId)));
                return;
            }

            _isSelectingDevice = true;
            try
            {
                for (int i = 0; i < listBoxDevices.Items.Count; i++)
                {
                    if (listBoxDevices.Items[i] is DeviceListItem item && item.DeviceId == deviceId)
                    {
                        listBoxDevices.SelectedIndex = i;
                        break;
                    }
                }
            }
            finally
            {
                _isSelectingDevice = false;
            }
        }

        public string GetSelectedDeviceId()
        {
            if (InvokeRequired)
                return (string)Invoke(new Func<string>(GetSelectedDeviceId));

            if (listBoxDevices.SelectedItem is DeviceListItem item)
                return item.DeviceId;
            return "";
        }

        public string GetSelectedDeviceName()
        {
            if (InvokeRequired)
                return (string)Invoke(new Func<string>(GetSelectedDeviceName));
            return workflowExecutionControl?.GetSelectedDeviceName() ?? "";
        }

        public string GetSelectedWorkflowName()
        {
            if (InvokeRequired)
                return (string)Invoke(new Func<string>(GetSelectedWorkflowName));
            return workflowExecutionControl?.GetSelectedWorkflowName() ?? "";
        }

        public void UpdateDeviceOnlineStatus(string deviceId, bool isOnline)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateDeviceOnlineStatus(deviceId, isOnline)));
                return;
            }

            for (int i = 0; i < listBoxDevices.Items.Count; i++)
            {
                if (listBoxDevices.Items[i] is DeviceListItem item && item.DeviceId == deviceId)
                {
                    item.IsOnline = isOnline;
                    listBoxDevices.Items[i] = item;
                    listBoxDevices.Invalidate();
                    break;
                }
            }
        }

        // ---------- 状态机状态更新 ----------
        public void UpdateDeviceState(string deviceId, DeviceState state, string reason)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateDeviceState(deviceId, state, reason)));
                return;
            }

            for (int i = 0; i < listBoxDevices.Items.Count; i++)
            {
                if (listBoxDevices.Items[i] is DeviceListItem item && item.DeviceId == deviceId)
                {
                    item.State = state;
                    listBoxDevices.Items[i] = item;
                    listBoxDevices.Invalidate();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(reason))
            {
                AppLogger.Debug($"[UI] 设备 {deviceId} 状态 → {state} ({reason})", "UI");
            }
        }

        public void UpdateDeviceProduction(string deviceId, int current, int target) { }
        public void UpdateDeviceStep(string deviceId, string step) { }
        public void UpdateDeviceData(string deviceId, object data) { }
        public void UpdateGlobalStats(int onlineCount, int totalCount, int totalProduction) { }

        // ================================================================
        // 状态栏（含 DeviceState）
        // ================================================================
        public void UpdateStatusBar(string deviceName, bool isOnline, DeviceState state, bool servoOn,
            string limitStatus, bool modbusConnected, string currentStep,
            int watchdogRemainingMs, bool watchdogTimeout)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatusBar(deviceName, isOnline, state, servoOn,
                    limitStatus, modbusConnected, currentStep, watchdogRemainingMs, watchdogTimeout)));
                return;
            }

            var items = statusStrip.Items;
            if (items.Count < 6) return;

            // ---- 0. 设备名 + 状态机状态 + 在线状态 ----
            string stateText = GetStateText(state);
            Color stateColor = GetStateColor(state);
            items[0].Text = $"设备: {deviceName} [{stateText}] {(isOnline ? "●在线" : "○离线")}";
            items[0].ForeColor = isOnline ? stateColor : Color.Red;

            // ---- 1. 伺服 ----
            items[1].Text = servoOn ? "伺服: 已使能" : "伺服: 未使能";
            items[1].ForeColor = servoOn ? Color.Green : Color.Orange;

            // ---- 2. 限位 ----
            items[2].Text = $"限位: {limitStatus}";
            items[2].ForeColor = limitStatus.Contains("限位") ? Color.Red : Color.Green;

            // ---- 3. 看门狗 ----
            if (watchdogTimeout)
            {
                items[3].Text = "看门狗: 超时!";
                items[3].ForeColor = Color.Red;
            }
            else if (watchdogRemainingMs <= 0)
            {
                items[3].Text = "看门狗: --";
                items[3].ForeColor = Color.Gray;
            }
            else
            {
                items[3].Text = $"看门狗: 正常 ({watchdogRemainingMs}ms)";
                items[3].ForeColor = Color.Green;
            }

            // ---- 4. Modbus ----
            items[4].Text = modbusConnected ? "Modbus: 已连接" : "Modbus: 未连接";
            items[4].ForeColor = modbusConnected ? Color.Green : Color.Red;

            // ---- 5. 当前指令 ----
            items[5].Text = $"当前指令: {currentStep}";
        }

        public void UpdateAlarmList(IEnumerable<AlarmRecord> alarms) { }
        public string GetSelectedAlarmId() => "";

        public void AppendOperationLog(string message)
        {
            if (txtOperationLog.InvokeRequired)
            {
                txtOperationLog.BeginInvoke(new Action(() => AppendOperationLog(message)));
                return;
            }
            txtOperationLog.AppendText(message + Environment.NewLine);
            if (txtOperationLog.Lines.Length > 500)
            {
                var lines = txtOperationLog.Lines;
                txtOperationLog.Lines = lines[100..];
            }
            txtOperationLog.ScrollToCaret();
        }

        public void AppendMonitorLog(string message)
        {
            if (txtMonitorLog.InvokeRequired)
            {
                txtMonitorLog.BeginInvoke(new Action(() => AppendMonitorLog(message)));
                return;
            }
            txtMonitorLog.AppendText(message + Environment.NewLine);
            if (txtMonitorLog.Lines.Length > 500)
            {
                var lines = txtMonitorLog.Lines;
                txtMonitorLog.Lines = lines[100..];
            }
            txtMonitorLog.ScrollToCaret();
        }

        public void ClearLogs()
        {
            if (txtOperationLog.InvokeRequired)
            {
                txtOperationLog.BeginInvoke(new Action(ClearLogs));
                return;
            }
            txtOperationLog.Clear();
            txtMonitorLog.Clear();
        }

        public void AppendExecutionLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendExecutionLog(message)));
                return;
            }
            workflowExecutionControl?.AppendLog(message);
        }

        public void SetSimulationMode(bool isSimulation) { }

        public void UpdateUIByPermissions(string role)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateUIByPermissions(role)));
                return;
            }

            bool isLoggedIn = !string.IsNullOrEmpty(role) && role != "未登录";
            bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isEngineer = string.Equals(role, "Engineer", StringComparison.OrdinalIgnoreCase) || isAdmin;

            btnAddDevice.Enabled = isLoggedIn && isEngineer;
            btnRemoveDevice.Enabled = isLoggedIn && isEngineer;

            btnStartAll.Enabled = isLoggedIn;
            btnStopAll.Enabled = isLoggedIn;
            btnStartSelected.Enabled = isLoggedIn;
            btnStopSelected.Enabled = isLoggedIn;
            btnResetDevice.Enabled = isLoggedIn;

            btnSystemConfig.Visible = isLoggedIn && isEngineer;
            btnSystemConfig.Enabled = isLoggedIn && isEngineer;

            btnResetAlarm.Enabled = isLoggedIn;
            btnEmergencyStop.Enabled = true;

            if (isLoggedIn)
            {
                lblUserInfo.Text = $"👤 {role}";
                btnLogin.Text = "登出";
            }
            else
            {
                lblUserInfo.Text = "未登录";
                btnLogin.Text = "登录";
            }

            workflowExecutionControl?.SetPermissions(isLoggedIn, isEngineer);
        }

        private void SetupToolTips()
        {
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(btnResetAlarm, "确认并解决所有设备的当前活动报警\n（报警已处理后的确认操作）");
            toolTip.SetToolTip(btnResetDevice, "重置选中设备的工作流状态\n• 软复位：从断点继续，保留产量\n• 硬复位：产量归零，从头开始");
            toolTip.SetToolTip(btnSystemConfig, "打开系统配置中心（工程师/管理员权限）");
        }

        public void ShowMessage(string text, string caption, MessageType type)
        {
            MessageBoxIcon icon = type switch
            {
                MessageType.Info => MessageBoxIcon.Information,
                MessageType.Warning => MessageBoxIcon.Warning,
                MessageType.Error => MessageBoxIcon.Error,
                _ => MessageBoxIcon.None
            };
            MessageBox.Show(text, caption, MessageBoxButtons.OK, icon);
        }

        public bool ShowConfirm(string text, string caption)
        {
            return MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        // ---------- 设备列表绘制：圆点 + 状态文本 ----------
        private void ListBoxDevices_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            if (listBoxDevices.Items[e.Index] is not DeviceListItem item) return;

            // 圆点颜色：在线/离线
            Color onlineColor = item.IsOnline ? Color.Green : Color.Red;

            // 状态机状态文本 + 颜色
            string stateText = GetStateText(item.State);
            Color stateColor = GetStateColor(item.State);

            // 1. 绘制在线/离线圆点
            using (var dotBrush = new SolidBrush(onlineColor))
            {
                e.Graphics.FillEllipse(dotBrush, e.Bounds.X + 5, e.Bounds.Y + 5, 10, 10);
            }

            // 2. 绘制设备名（默认色）
            float nameX = e.Bounds.X + 22;
            using (var nameBrush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(item.Name, e.Font, nameBrush, nameX, e.Bounds.Y + 2);
            }

            // 3. 在名字后面绘制状态文本（状态色）
            SizeF nameSize = e.Graphics.MeasureString(item.Name, e.Font);
            using (var stateBrush = new SolidBrush(stateColor))
            {
                e.Graphics.DrawString($" [{stateText}]", e.Font, stateBrush,
                    nameX + nameSize.Width, e.Bounds.Y + 2);
            }

            e.DrawFocusRectangle();
        }

        // ================================================================
        // 状态机状态 → 显示文本 / 颜色
        // ================================================================
        internal static string GetStateText(DeviceState state) => state switch
        {
            DeviceState.Idle => "空闲",
            DeviceState.Running => "运行中",
            DeviceState.Paused => "暂停",
            DeviceState.Error => "故障",
            DeviceState.EmergencyStop => "急停",
            DeviceState.Disconnected => "断开",
            _ => state.ToString()
        };

        internal static Color GetStateColor(DeviceState state) => state switch
        {
            DeviceState.Idle => Color.Gray,
            DeviceState.Running => Color.Green,
            DeviceState.Paused => Color.Orange,
            DeviceState.Error => Color.Red,
            DeviceState.EmergencyStop => Color.DarkRed,
            DeviceState.Disconnected => Color.DimGray,
            _ => Color.Black
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _presenter = null;
            }
            base.Dispose(disposing);
        }
    }
}