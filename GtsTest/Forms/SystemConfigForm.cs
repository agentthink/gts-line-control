using GtsTest.Controls;
using GtsTest.Core;
using GtsTest.Diagnostics;
using GtsTest.Forms;
using GtsTest.Models;
using GtsTest.Presenters;
using GtsTest.Services;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Plc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GtsTest
{
    public partial class SystemConfigForm : Form
    {
        private readonly DeviceManager _deviceManager;
        private readonly GtsModel _model;
        private readonly IDataRepository _repo;
        private readonly IAuthenticationService _authService;
        private readonly User _currentUser;
        private readonly PlcManager _plcManager;

        // ---- 子控件 ----
        private WorkflowControl workflowControl;
        private CommunicationControl commControl;
        private MqttControl mqttControl;
        private MqttPresenter mqttPresenter;
        private Panel debugPanel;
        private DebugToolboxForm debugControl;

        // ---- 用户管理控件 ----
        private ListView listViewUsers;
        private Button btnAddUser;
        private Button btnEditUser;
        private Button btnDeleteUser;
        private Button btnResetPassword;
        private Button btnToggleStatus;
        private CheckBox chkShowDeleted;
        private Label lblUserStatus;

        // ================================================================
        // 构造函数
        // ================================================================
        public SystemConfigForm(
            DeviceManager deviceManager,
            GtsModel model,
            IDataRepository repo,
            IAuthenticationService authService,
            User currentUser,
            PlcManager plcManager)
        {
            _deviceManager = deviceManager;
            _model = model;
            _repo = repo;
            _authService = authService;
            _currentUser = currentUser;
            _plcManager = plcManager;

            InitializeComponent();

            BindEvents();
            ApplyPermissions();
            LoadControls();
            LoadUserManagement();

            this.Text = $"🔧 系统配置中心 - {currentUser?.FullName ?? "工程师"}";
            this.Icon = SystemIcons.Application;

            btnToggleMode.Text = GtsModel.UseSimulation ? "切换到真实" : "切换到模拟";

            AppLogger.Info($"SystemConfigForm 初始化: 用户={currentUser?.Username}, 角色={currentUser?.Role}", "SystemConfig");
        }

        // ================================================================
        // 事件绑定
        // ================================================================
        private void BindEvents()
        {
            // ---- 系统工具 ----
            btnInit.Click += BtnInit_Click;
            btnToggleMode.Click += BtnToggleMode_Click;
            btnHotReload.Click += BtnHotReload_Click;
            btnSaveConfig.Click += BtnSaveConfig_Click;
            btnDumpBlackBox.Click += BtnDumpBlackBox_Click;
            btnClearLogs.Click += BtnClearLogs_Click;
            btnDiagnostics.Click += BtnDiagnostics_Click;
            btnExportDiagnostic.Click += BtnExportDiagnostic_Click;
            btnOpenFrameMonitor.Click += BtnOpenFrameMonitor_Click;

            // ---- 日志级别 ----
            btnApplyLogLevel.Click += BtnApplyLogLevel_Click;
        }

        // ================================================================
        // 加载子控件
        // ================================================================
        private void LoadControls()
        {
            // ---- 1. 工作流 ----
            try
            {
                workflowControl = new WorkflowControl(_model, _deviceManager);
                workflowControl.Dock = DockStyle.Fill;
                tabWorkflow.Controls.Add(workflowControl);
                AppLogger.Info("✅ 工作流控件加载成功", "SystemConfig");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ 工作流控件加载失败: {ex.Message}", "SystemConfig");
                tabWorkflow.Controls.Add(new Label
                {
                    Text = $"加载失败: {ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                });
            }

            // ---- 2. OPC UA ----
            try
            {
                commControl = new CommunicationControl();
                commControl.Dock = DockStyle.Fill;
                tabComm.Controls.Add(commControl);
                AppLogger.Info("✅ OPC UA 控件加载成功", "SystemConfig");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ OPC UA 控件加载失败: {ex.Message}", "SystemConfig");
                tabComm.Controls.Add(new Label
                {
                    Text = $"加载失败: {ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                });
            }

            // ---- 3. MQTT ----
            try
            {
                mqttControl = new MqttControl();
                mqttControl.Dock = DockStyle.Fill;
                tabMqtt.Controls.Add(mqttControl);

                var mqttService = new MqttService();
                mqttPresenter = new MqttPresenter(mqttControl, mqttService);

                AppLogger.Info("✅ MQTT 控件加载成功", "SystemConfig");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ MQTT 控件加载失败: {ex.Message}", "SystemConfig");
                tabMqtt.Controls.Add(new Label
                {
                    Text = $"加载失败: {ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                });
            }

            // ---- 4. 调试工具箱 ----
            try
            {
                debugPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
                debugControl = new DebugToolboxForm(
                    _deviceManager,
                    _model,
                    _deviceManager.AlarmManager,
                    _repo,
                    _authService,
                    _plcManager);
                debugControl.TopLevel = false;
                debugControl.FormBorderStyle = FormBorderStyle.None;
                debugControl.Dock = DockStyle.Fill;
                debugControl.Visible = true;
                debugPanel.Controls.Add(debugControl);
                tabDebug.Controls.Add(debugPanel);
                AppLogger.Info("✅ 调试工具控件加载成功", "SystemConfig");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"❌ 调试工具控件加载失败: {ex.Message}", "SystemConfig");
                tabDebug.Controls.Add(new Label
                {
                    Text = $"加载失败: {ex.Message}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                });
            }
        }

        // ================================================================
        // 用户管理
        // ================================================================
        private void LoadUserManagement()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // ---- 顶部按钮条 ----
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 0, 0, 5)
            };

            btnAddUser = new Button { Text = "➕ 添加用户", Size = new Size(90, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
            btnEditUser = new Button { Text = "✏️ 编辑用户", Size = new Size(90, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.LightYellow, Enabled = false };
            btnToggleStatus = new Button { Text = "🔄 切换状态", Size = new Size(90, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.LightBlue, Enabled = false };
            btnDeleteUser = new Button { Text = "🗑️ 删除用户", Size = new Size(90, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.LightCoral, Enabled = false };
            btnResetPassword = new Button { Text = "🔑 重置密码", Size = new Size(90, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.LightBlue, Enabled = false };

            btnAddUser.Click += BtnAddUser_Click;
            btnEditUser.Click += BtnEditUser_Click;
            btnToggleStatus.Click += BtnToggleStatus_Click;
            btnDeleteUser.Click += BtnDeleteUser_Click;
            btnResetPassword.Click += BtnResetPassword_Click;

            btnPanel.Controls.Add(btnAddUser);
            btnPanel.Controls.Add(btnEditUser);
            btnPanel.Controls.Add(btnToggleStatus);
            btnPanel.Controls.Add(btnDeleteUser);
            btnPanel.Controls.Add(btnResetPassword);

            chkShowDeleted = new CheckBox
            {
                Text = "显示已删除用户",
                Dock = DockStyle.Right,
                AutoSize = true,
                Checked = false,
                Margin = new Padding(5, 8, 0, 0)
            };
            chkShowDeleted.CheckedChanged += (s, e) => RefreshUserList();
            btnPanel.Controls.Add(chkShowDeleted);

            // ---- 底部状态栏 ----
            lblUserStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                Text = "就绪",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9F)
            };

            // ---- 中间列表 ----
            listViewUsers = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            listViewUsers.Columns.Add("用户名", 120);
            listViewUsers.Columns.Add("全名", 120);
            listViewUsers.Columns.Add("角色", 100);
            listViewUsers.Columns.Add("状态", 80);
            listViewUsers.Columns.Add("创建时间", 150);
            listViewUsers.Columns.Add("失败次数", 80);
            listViewUsers.Columns.Add("删除标记", 80);

            listViewUsers.SelectedIndexChanged += (s, e) =>
            {
                bool hasSelected = listViewUsers.SelectedItems.Count > 0;
                User? selectedUser = hasSelected ? listViewUsers.SelectedItems[0].Tag as User : null;

                btnEditUser.Enabled = hasSelected;
                btnToggleStatus.Enabled = hasSelected && selectedUser?.IsDeleted == 0;
                btnResetPassword.Enabled = hasSelected && selectedUser?.IsDeleted == 0;

                if (hasSelected)
                {
                    var user = selectedUser;
                    btnDeleteUser.Enabled = user != null && user.Username != "admin" && user.IsDeleted == 0;

                    if (user?.IsDeleted == 1)
                    {
                        btnToggleStatus.Text = "已删除，无法切换";
                        btnToggleStatus.Enabled = false;
                    }
                    else
                    {
                        btnToggleStatus.Text = (user != null && user.IsActive == 1) ? "🔴 禁用" : "🟢 启用";
                        btnToggleStatus.Enabled = true;
                    }
                }
                else
                {
                    btnDeleteUser.Enabled = false;
                    btnToggleStatus.Text = "🔄 切换状态";
                    btnToggleStatus.Enabled = false;
                }
            };

            panel.Controls.Add(listViewUsers);
            panel.Controls.Add(lblUserStatus);
            panel.Controls.Add(btnPanel);

            tabAdmin.Controls.Add(panel);
            RefreshUserList();
        }

        private void RefreshUserList()
        {
            try
            {
                listViewUsers.Items.Clear();
                bool showDeleted = chkShowDeleted?.Checked ?? false;
                var users = _repo.GetAllUsers(showDeleted);

                foreach (var user in users)
                {
                    var item = new ListViewItem(user.Username);
                    item.SubItems.Add(user.FullName);
                    item.SubItems.Add(user.Role);

                    if (user.IsDeleted == 1)
                        item.SubItems.Add("已删除");
                    else
                        item.SubItems.Add(user.IsActive == 1 ? "✅ 启用" : "❌ 禁用");

                    item.SubItems.Add(user.CreatedTime);
                    item.SubItems.Add(user.FailedAttempts.ToString());

                    if (user.IsDeleted == 1)
                    {
                        item.ForeColor = Color.Gray;
                        item.SubItems.Add("已删除");
                    }
                    else
                    {
                        item.SubItems.Add("正常");
                    }

                    item.Tag = user;
                    listViewUsers.Items.Add(item);
                }

                lblUserStatus.Text = $"共 {users.Count} 个用户{(showDeleted ? "（含已删除）" : "")}";
            }
            catch (Exception ex)
            {
                lblUserStatus.Text = $"加载用户列表失败: {ex.Message}";
                AppLogger.Error($"加载用户列表失败: {ex.Message}", "SystemConfig");
            }
        }

        // ================================================================
        // 用户管理事件
        // ================================================================
        private void BtnAddUser_Click(object? sender, EventArgs e)
        {
            using (var dialog = new UserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    bool success = _authService.RegisterUser(
                        dialog.Username,
                        dialog.FullName,
                        dialog.Password,
                        dialog.Role);

                    if (success)
                    {
                        RefreshUserList();
                        MessageBox.Show($"用户 {dialog.Username} 创建成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        AppLogger.Info($"管理员 {_currentUser.Username} 创建了用户 {dialog.Username}", "SystemConfig");
                    }
                    else
                    {
                        MessageBox.Show($"创建用户失败，用户名可能已存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (listViewUsers.SelectedItems.Count == 0) return;
            var user = listViewUsers.SelectedItems[0].Tag as User;
            if (user == null) return;

            using (var dialog = new UserDialog(user))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    user.FullName = dialog.FullName;
                    user.Role = dialog.Role;
                    user.IsActive = dialog.IsActive ? 1 : 0;

                    if (_repo.UpdateUser(user))
                    {
                        RefreshUserList();
                        MessageBox.Show($"用户 {user.Username} 信息已更新", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        AppLogger.Info($"管理员 {_currentUser.Username} 编辑了用户 {user.Username}", "SystemConfig");
                    }
                    else
                    {
                        MessageBox.Show("更新用户信息失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDeleteUser_Click(object? sender, EventArgs e)
        {
            if (listViewUsers.SelectedItems.Count == 0) return;
            var user = listViewUsers.SelectedItems[0].Tag as User;
            if (user == null) return;

            if (user.Username == "admin")
            {
                MessageBox.Show("不能删除管理员账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (user.IsDeleted == 1)
            {
                MessageBox.Show("该用户已被删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"确定要删除用户 {user.Username} 吗？\n（用户数据将保留但标记为已删除）", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_repo.SoftDeleteUser(user.Id, _currentUser?.Username ?? "系统"))
                {
                    RefreshUserList();
                    MessageBox.Show($"用户 {user.Username} 已标记为删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppLogger.Info($"管理员 {_currentUser?.Username} 删除了用户 {user.Username}", "SystemConfig");
                    AuditService.Log(_currentUser?.Id ?? 0, _currentUser?.Username ?? "系统", "DeleteUser", $"逻辑删除用户 {user.Username}", _repo);
                }
                else
                {
                    MessageBox.Show("删除失败，请重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnResetPassword_Click(object? sender, EventArgs e)
        {
            if (listViewUsers.SelectedItems.Count == 0) return;
            var user = listViewUsers.SelectedItems[0].Tag as User;
            if (user == null) return;

            using (var dialog = new ResetPasswordDialog(user.Username))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(dialog.NewPassword) || dialog.NewPassword.Length < 6)
                        {
                            MessageBox.Show("密码至少6位", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string newHash = AuthenticationHelper.HashPassword(dialog.NewPassword);
                        user.PasswordHash = newHash;
                        user.Salt = null;

                        if (_repo.UpdateUser(user))
                        {
                            RefreshUserList();
                            MessageBox.Show($"用户 {user.Username} 的密码已重置成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            AppLogger.Info($"管理员 {_currentUser.Username} 重置了用户 {user.Username} 的密码", "SystemConfig");
                            AuditService.Log(_currentUser?.Id ?? 0, _currentUser?.Username ?? "系统", "ResetPassword", $"管理员重置了用户 {user.Username} 的密码", _repo);
                        }
                        else
                        {
                            MessageBox.Show("重置密码失败，请重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重置密码失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AppLogger.Error($"重置密码异常: {ex.Message}", "SystemConfig");
                    }
                }
            }
        }

        private void BtnToggleStatus_Click(object? sender, EventArgs e)
        {
            if (listViewUsers.SelectedItems.Count == 0) return;
            var user = listViewUsers.SelectedItems[0].Tag as User;
            if (user == null) return;

            string action = user.IsActive == 1 ? "禁用" : "启用";
            if (MessageBox.Show($"确定要{action}用户 {user.Username} 吗？", $"确认{action}", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                user.IsActive = user.IsActive == 1 ? 0 : 1;
                if (_repo.UpdateUser(user))
                {
                    RefreshUserList();
                    MessageBox.Show($"用户 {user.Username} 已{action}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppLogger.Info($"管理员 {_currentUser.Username} {action}了用户 {user.Username}", "SystemConfig");
                    AuditService.Log(_currentUser?.Id ?? 0, _currentUser?.Username ?? "系统", "ToggleUserStatus", $"{action}用户 {user.Username}", _repo);
                }
                else
                {
                    MessageBox.Show("操作失败，请重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ================================================================
        // 权限
        // ================================================================
        private void ApplyPermissions()
        {
            if (_currentUser == null)
            {
                AppLogger.Warn("SystemConfigForm: 当前用户为 null，仅显示基础功能", "SystemConfig");
                return;
            }

            bool isAdmin = string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isEngineer = string.Equals(_currentUser.Role, "Engineer", StringComparison.OrdinalIgnoreCase) || isAdmin;

            AppLogger.Info($"SystemConfigForm 权限判断: IsAdmin={isAdmin}, IsEngineer={isEngineer}, Role={_currentUser.Role}", "SystemConfig");

            tabMain.TabPages.Clear();

            if (isEngineer)
            {
                tabMain.TabPages.Add(tabWorkflow);
                tabMain.TabPages.Add(tabComm);
                tabMain.TabPages.Add(tabMqtt);
                tabMain.TabPages.Add(tabDebug);
                tabMain.TabPages.Add(tabSystemTools);
            }
            if (isAdmin)
            {
                tabMain.TabPages.Add(tabAdmin);
            }

            if (tabMain.TabPages.Count == 0)
            {
                var page = new TabPage("无权限");
                page.Controls.Add(new Label
                {
                    Text = "您没有权限查看任何配置项",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red
                });
                tabMain.TabPages.Add(page);
            }

            tabMain.SelectedIndex = 0;
        }

        // ================================================================
        // 系统工具事件
        // ================================================================
        private void BtnInit_Click(object? sender, EventArgs e)
        {
            try
            {
                short result = _model.OpenDevice(0, 0);
                if (result != 0)
                {
                    MessageBox.Show($"初始化失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _model.CloseDevice();
                MessageBox.Show("运动控制卡初始化成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info("运动控制卡初始化成功", "SystemConfig");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"初始化异常: {ex.Message}", "SystemConfig");
            }
        }

        private void BtnToggleMode_Click(object? sender, EventArgs e)
        {
            bool isSim = GtsModel.UseSimulation;
            try
            {
                if (!isSim)
                {
                    if (!GtsModel.CheckHardwareAvailable())
                    {
                        MessageBox.Show("未检测到硬件，无法切换到真实模式", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    short result = _model.OpenDevice(0, 0);
                    if (result != 0)
                    {
                        MessageBox.Show($"打开卡失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _model.CloseDevice();
                        return;
                    }
                    _model.CloseDevice();
                }

                GtsModel.UseSimulation = !isSim;
                btnToggleMode.Text = GtsModel.UseSimulation ? "切换到真实" : "切换到模拟";
                UpdateModeStatusLabel();
                MessageBox.Show($"已切换到 {(GtsModel.UseSimulation ? "模拟" : "真实")} 模式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info($"系统模式已切换: {(GtsModel.UseSimulation ? "模拟" : "真实")}", "SystemConfig");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"切换异常: {ex.Message}", "SystemConfig");
            }
        }

        /// <summary>刷新"当前模式"标签（动态查找）</summary>
        private void UpdateModeStatusLabel()
        {
            foreach (Control ctrl in tabSystemTools.Controls)
            {
                if (ctrl is Panel panel)
                {
                    foreach (Control inner in panel.Controls)
                    {
                        if (inner is TableLayoutPanel table)
                        {
                            foreach (Control sub in table.Controls)
                            {
                                if (sub is GroupBox gb && gb.Text.Contains("模拟模式"))
                                {
                                    foreach (Control gbCtrl in gb.Controls)
                                    {
                                        if (gbCtrl is TableLayoutPanel modeTable)
                                        {
                                            foreach (Control modeCtrl in modeTable.Controls)
                                            {
                                                if (modeCtrl is Label lbl && lbl.Text.StartsWith("当前模式:"))
                                                {
                                                    lbl.Text = $"当前模式: {(GtsModel.UseSimulation ? "模拟模式" : "真实模式")}";
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // ================================================================
        // 🔄 热加载配置（带详细 diff 报告）
        // ================================================================
        private void BtnHotReload_Click(object? sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json");
            if (!File.Exists(path))
            {
                MessageBox.Show($"配置文件不存在:\n{path}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ---- 1. 读取并解析 JSON ----
            List<DeviceConfig>? newConfigs;
            try
            {
                string json = File.ReadAllText(path);
                newConfigs = System.Text.Json.JsonSerializer.Deserialize<List<DeviceConfig>>(json);
                if (newConfigs == null)
                {
                    MessageBox.Show("配置文件解析失败：内容为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"配置文件解析失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"热加载解析失败: {ex.Message}", "SystemConfig");
                return;
            }

            // ---- 2. 与内存中的设备做 Diff ----
            var currentRuntimes = _deviceManager.GetAllDevices();
            var currentDict = currentRuntimes.ToDictionary(d => d.Config.DeviceId, d => d.Config);

            var added = new List<DeviceConfig>();
            var removed = new List<DeviceConfig>();
            var modified = new List<(DeviceConfig Old, DeviceConfig New, List<string> Changes)>();
            var unchanged = new List<DeviceConfig>();

            // 遍历新配置：识别新增 / 修改 / 未变
            var newIds = new HashSet<string>();
            foreach (var newCfg in newConfigs)
            {
                if (string.IsNullOrEmpty(newCfg.DeviceId)) continue;
                newIds.Add(newCfg.DeviceId);

                if (!currentDict.TryGetValue(newCfg.DeviceId, out var oldCfg))
                {
                    added.Add(newCfg);
                }
                else
                {
                    var changes = DiffDeviceConfig(oldCfg, newCfg);
                    if (changes.Count > 0)
                        modified.Add((oldCfg, newCfg, changes));
                    else
                        unchanged.Add(newCfg);
                }
            }

            // 遍历当前内存：识别移除
            foreach (var oldCfg in currentDict.Values)
            {
                if (!newIds.Contains(oldCfg.DeviceId))
                    removed.Add(oldCfg);
            }

            // ---- 3. 生成变更报告 ----
            int totalChanges = added.Count + removed.Count + modified.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"📄 配置文件: {Path.GetFileName(path)}");
            sb.AppendLine($"🕐 修改时间: {File.GetLastWriteTime(path):yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('─', 62));
            sb.AppendLine();

            if (totalChanges == 0)
            {
                sb.AppendLine("✅ 配置文件与当前运行状态完全一致，无需更新");
                sb.AppendLine();
                sb.AppendLine($"设备总数: {newConfigs.Count}（全部未变化）");
                MessageBox.Show(sb.ToString(), "热加载 - 无变化", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info("热加载检查: 配置文件与当前状态一致，无变更", "SystemConfig");
                return;
            }

            sb.AppendLine($"📊 变更总览: 新增 {added.Count} 台 / 移除 {removed.Count} 台 / 修改 {modified.Count} 台 / 未变 {unchanged.Count} 台");
            sb.AppendLine();

            // ---- 新增 ----
            if (added.Count > 0)
            {
                sb.AppendLine($"【➕ 新增 {added.Count} 台】");
                foreach (var cfg in added)
                    sb.AppendLine($"   • {cfg.Name} ({cfg.DeviceId})  →  {cfg.Modbus?.IpAddress}:{cfg.Modbus?.Port}, 轴={cfg.Axis}");
                sb.AppendLine();
            }

            // ---- 移除 ----
            if (removed.Count > 0)
            {
                sb.AppendLine($"【➖ 移除 {removed.Count} 台】");
                foreach (var cfg in removed)
                    sb.AppendLine($"   • {cfg.Name} ({cfg.DeviceId})  ⚠️ 当前产量: {cfg.CurrentCount}/{cfg.TargetCount}");
                sb.AppendLine();
            }

            // ---- 修改 ----
            if (modified.Count > 0)
            {
                sb.AppendLine($"【✏️ 修改 {modified.Count} 台】");
                foreach (var (oldCfg, newCfg, changes) in modified)
                {
                    sb.AppendLine($"   • {newCfg.Name} ({newCfg.DeviceId})");
                    foreach (var c in changes)
                        sb.AppendLine($"        {c}");
                }
                sb.AppendLine();
            }

            // ---- 未变 ----
            if (unchanged.Count > 0)
            {
                sb.AppendLine($"【✔ 未变化 {unchanged.Count} 台】");
                foreach (var cfg in unchanged)
                    sb.AppendLine($"   • {cfg.Name} ({cfg.DeviceId})");
                sb.AppendLine();
            }

            // ---- 4. 用户确认 ----
            sb.AppendLine(new string('─', 62));
            sb.AppendLine("⚠️ 注意：");
            sb.AppendLine("   • 正在运行的设备需先停止才能应用新配置");
            sb.AppendLine("   • 当前产量会被保留，不会被 JSON 覆盖");
            sb.AppendLine();
            sb.AppendLine("是否应用这些变更？");

            var result = MessageBox.Show(sb.ToString(), "热加载 - 变更预览",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (result != DialogResult.OK)
            {
                AppLogger.Info("用户取消了热加载", "SystemConfig");
                return;
            }

            // ---- 5. 应用变更（保留当前产量）----
            try
            {
                // 快照当前产量
                var productionSnapshot = currentRuntimes
                    .ToDictionary(d => d.Config.DeviceId, d => d.Config.CurrentCount);

                if (_deviceManager.ImportConfig(File.ReadAllText(path)))
                {
                    // 恢复产量
                    foreach (var dev in _deviceManager.GetAllDevices())
                    {
                        if (productionSnapshot.TryGetValue(dev.Config.DeviceId, out int savedCount))
                            dev.Config.CurrentCount = savedCount;
                    }

                    AppLogger.Info($"✅ 热加载成功: +{added.Count} / -{removed.Count} / ~{modified.Count}", "SystemConfig");
                    AuditService.Log(
                        _currentUser?.Id ?? 0,
                        _currentUser?.Username ?? "系统",
                        "HotReload",
                        $"热加载 devices.json: 新增{added.Count}/移除{removed.Count}/修改{modified.Count}",
                        _repo);

                    MessageBox.Show(
                        $"✅ 配置已应用\n\n➕ 新增 {added.Count} 台\n➖ 移除 {removed.Count} 台\n✏️ 修改 {modified.Count} 台",
                        "热加载成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("配置应用失败，请查看日志", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用配置异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"热加载异常: {ex.Message}", "SystemConfig");
            }
        }

        /// <summary>
        /// 对比两个 DeviceConfig，返回人类可读的差异列表（空列表 = 完全一致）
        /// </summary>
        private List<string> DiffDeviceConfig(DeviceConfig oldCfg, DeviceConfig newCfg)
        {
            var diffs = new List<string>();

            void Check<T>(string label, T oldVal, T newVal)
            {
                if (!EqualityComparer<T>.Default.Equals(oldVal, newVal))
                    diffs.Add($"{label}: {oldVal} → {newVal}");
            }

            // 顶层字段
            Check("名称", oldCfg.Name, newCfg.Name);
            Check("启用状态", oldCfg.Enabled, newCfg.Enabled);
            Check("轴号", oldCfg.Axis, newCfg.Axis);
            Check("目标产量", oldCfg.TargetCount, newCfg.TargetCount);
            Check("信号起始地址", oldCfg.SignalStartAddress, newCfg.SignalStartAddress);
            Check("工位位置", oldCfg.WorkPosition, newCfg.WorkPosition);
            Check("回零位置", oldCfg.HomePosition, newCfg.HomePosition);
            Check("移动速度", oldCfg.MoveSpeed, newCfg.MoveSpeed);
            Check("移动加速度", oldCfg.MoveAcc, newCfg.MoveAcc);
            Check("循环延时(ms)", oldCfg.CycleDelayMs, newCfg.CycleDelayMs);

            // Modbus 子配置
            if (oldCfg.Modbus != null && newCfg.Modbus != null)
            {
                Check("Modbus协议", oldCfg.Modbus.Protocol, newCfg.Modbus.Protocol);
                Check("Modbus IP", oldCfg.Modbus.IpAddress, newCfg.Modbus.IpAddress);
                Check("Modbus端口", oldCfg.Modbus.Port, newCfg.Modbus.Port);
                Check("从站地址", oldCfg.Modbus.SlaveAddress, newCfg.Modbus.SlaveAddress);
                Check("起始地址", oldCfg.Modbus.StartAddress, newCfg.Modbus.StartAddress);
                Check("寄存器数量", oldCfg.Modbus.RegisterCount, newCfg.Modbus.RegisterCount);
            }
            else if ((oldCfg.Modbus == null) != (newCfg.Modbus == null))
            {
                diffs.Add($"Modbus 配置: {(oldCfg.Modbus == null ? "无 → 有" : "有 → 无")}");
            }

            return diffs;
        }

        private void BtnSaveConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                string json = _deviceManager.ExportConfig();
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json");
                File.WriteAllText(path, json);
                MessageBox.Show($"配置已保存到 {path}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info($"配置已保存: {path}", "SystemConfig");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"保存异常: {ex.Message}", "SystemConfig");
            }
        }

        private void BtnDumpBlackBox_Click(object? sender, EventArgs e)
        {
            try
            {
                var file = CyclicMonitorBuffer.DumpToFile("系统配置手动导出");
                if (file != null)
                {
                    MessageBox.Show($"黑匣子已导出到 {file}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppLogger.Info($"黑匣子已导出: {file}", "SystemConfig");
                }
                else
                {
                    MessageBox.Show("黑匣子导出失败，缓冲区为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AppLogger.Warn("黑匣子导出失败，缓冲区为空", "SystemConfig");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"导出异常: {ex.Message}", "SystemConfig");
            }
        }

        private void BtnClearLogs_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空操作日志和监控日志吗？（不影响日志文件）", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (this.Owner is Form1 mainForm)
                {
                    mainForm.ClearLogs();
                }
                MessageBox.Show("日志已清空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info("界面日志已清空", "SystemConfig");
            }
        }

        private void BtnDiagnostics_Click(object? sender, EventArgs e)
        {
            var devices = _deviceManager.GetAllDevices();
            string info = $"设备总数: {devices.Count}\n";
            info += $"在线设备: {devices.Count(d => d.IsOnline)}\n";
            info += $"总产量: {devices.Sum(d => d.Config.CurrentCount)}\n";
            info += $"模拟模式: {GtsModel.UseSimulation}\n";
            info += $"时间: {DateTime.Now}\n";
            info += $"用户: {_currentUser?.Username} ({_currentUser?.Role})";
            MessageBox.Show(info, "系统诊断", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AppLogger.Info("执行了系统诊断", "SystemConfig");
        }

        private void BtnExportDiagnostic_Click(object? sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnExportDiagnostic.Enabled = false;

                Form? mainForm = this.Owner as Form;
                string? zipPath = DiagnosticPackageBuilder.Build(_deviceManager, mainForm);

                this.Cursor = Cursors.Default;
                btnExportDiagnostic.Enabled = true;

                if (string.IsNullOrEmpty(zipPath))
                {
                    MessageBox.Show("诊断包导出失败，请查看日志文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                AppLogger.Info($"诊断包已导出: {zipPath}", "Diagnostic");
                AuditService.Log(
                    _currentUser?.Id ?? 0,
                    _currentUser?.Username ?? "系统",
                    "ExportDiagnostic",
                    $"导出诊断包: {Path.GetFileName(zipPath)}",
                    _repo);

                var result = MessageBox.Show(
                    $"✅ 诊断包已导出：\n\n{zipPath}\n\n是否立即打开所在文件夹？",
                    "导出成功",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start("explorer.exe", $"/select,\"{zipPath}\"");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warn($"打开文件夹失败: {ex.Message}", "Diagnostic");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                btnExportDiagnostic.Enabled = true;
                MessageBox.Show($"导出诊断包异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"导出诊断包异常: {ex}", "Diagnostic");
            }
        }

        private void BtnOpenFrameMonitor_Click(object? sender, EventArgs e)
        {
            try
            {
                var form = new GtsTest.Diagnostics.FrameMonitorForm();
                form.Show(this);
                AppLogger.Info("打开报文监视器", "Diagnostic");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开报文监视器失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"打开报文监视器失败: {ex}", "Diagnostic");
            }
        }

        private void BtnApplyLogLevel_Click(object? sender, EventArgs e)
        {
            try
            {
                string? levelName = cmbLogLevel.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(levelName))
                {
                    MessageBox.Show("请选择日志级别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!Enum.TryParse<LogLevel>(levelName, true, out var level))
                {
                    MessageBox.Show($"无效的日志级别: {levelName}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                AppLogger.GlobalLogLevel = level;
                lblCurrentLogLevel.Text = $"当前级别: {level}";

                AppLogger.Info($"🔄 日志级别已切换为: {level}", "SystemConfig");
                AuditService.Log(
                    _currentUser?.Id ?? 0,
                    _currentUser?.Username ?? "系统",
                    "SetLogLevel",
                    $"设置日志级别为 {level}",
                    _repo);

                MessageBox.Show($"日志级别已切换为 {level}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用日志级别异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"应用日志级别异常: {ex.Message}", "SystemConfig");
            }
        }

        // ================================================================
        // 生命周期
        // ================================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (debugControl != null && !debugControl.IsDisposed)
            {
                debugControl.Close();
            }
            mqttPresenter?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                if (debugControl != null && !debugControl.IsDisposed)
                {
                    debugControl.Dispose();
                }
                workflowControl?.Dispose();
                commControl?.Dispose();
                mqttControl?.Dispose();
                mqttPresenter?.Dispose();
                listViewUsers?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}