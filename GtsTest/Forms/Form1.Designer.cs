using System.Drawing;
using System.Windows.Forms;
using GtsTest.Controls;
using GtsTest.Presenters;

namespace GtsTest
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // ==================== 控件声明 ====================

        // ---- 根布局 ----
        private TableLayoutPanel rootLayout;

        // ---- 顶栏 ----
        private Panel topPanel;
        private Label lblTitle;
        private Label lblUserInfo;
        private Button btnLogin;
        private Button btnEmergencyStop;
        private Button btnResetAlarm;
        private Button btnSystemConfig;

        // ⭐ MES 相关控件
        private Label lblMesPendingCount;
        private Button btnMesRetry;

        // ---- 主内容 ----
        private TableLayoutPanel mainContent;
        private Panel leftPanel;
        private Panel centerPanel;

        // ---- 左侧：设备列表 ----
        private Label lblDeviceListTitle;
        private ListBox listBoxDevices;
        private FlowLayoutPanel deviceButtonPanel;
        private Button btnAddDevice;
        private Button btnRemoveDevice;
        private Button btnStartAll;
        private Button btnStopAll;
        private Button btnStartSelected;
        private Button btnStopSelected;
        private Button btnResetDevice;

        // ---- 中间：TabControl ----
        private TabControl tabMain;
        private TabPage tabPageOverview;
        private TabPage tabPageProduction;

        // ---- 中间：控件 ----
        private ProductionOverviewControl overviewControl;
        private WorkflowExecutionControl workflowExecutionControl;

        // ---- 底部日志 ----
        private TabControl tabLog;
        private TabPage tabPageOpLog;
        private TabPage tabPageMonLog;
        private TextBox txtOperationLog;
        private TextBox txtMonitorLog;

        // ---- 状态栏 ----
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDeviceStatus;
        private ToolStripStatusLabel lblServoStatus;
        private ToolStripStatusLabel lblLimitStatus;
        private ToolStripStatusLabel lblWatchdogStatus;
        private ToolStripStatusLabel lblModbusStatus;
        private ToolStripStatusLabel lblCurrentCmd;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ================================================
            //  窗体
            // ================================================
            this.ClientSize = new Size(1500, 850);
            this.Text = "Gts_Test - 多设备产线控制软件";
            this.MinimumSize = new Size(1300, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.Font = new Font("Segoe UI", 9F);

            // ================================================
            //  根布局
            // ================================================
            rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            // ================================================
            //  顶栏
            // ================================================
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.BackColor = Color.FromArgb(30, 30, 46);
            topPanel.Padding = new Padding(12, 0, 12, 0);

            // 标题
            lblTitle = new Label();
            lblTitle.Text = "🏭 Gts_Test - 多设备产线控制软件";
            lblTitle.ForeColor = Color.White;
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 12);
            lblTitle.AutoSize = true;

            // 用户信息
            lblUserInfo = new Label();
            lblUserInfo.Text = "未登录";
            lblUserInfo.ForeColor = Color.FromArgb(180, 180, 200);
            lblUserInfo.Font = new Font("Segoe UI", 9F);
            lblUserInfo.Location = new Point(340, 16);
            lblUserInfo.AutoSize = true;

            // 登录按钮
            btnLogin = new Button();
            btnLogin.Text = "登录";
            btnLogin.Size = new Size(70, 30);
            btnLogin.Location = new Point(490, 10);
            btnLogin.BackColor = Color.FromArgb(60, 60, 80);
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Cursor = Cursors.Hand;

            // 急停
            btnEmergencyStop = new Button();
            btnEmergencyStop.Text = "🔴 急停";
            btnEmergencyStop.Size = new Size(110, 34);
            btnEmergencyStop.Location = new Point(580, 8);
            btnEmergencyStop.BackColor = Color.FromArgb(220, 50, 50);
            btnEmergencyStop.ForeColor = Color.White;
            btnEmergencyStop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnEmergencyStop.FlatStyle = FlatStyle.Flat;
            btnEmergencyStop.Cursor = Cursors.Hand;

            // 复位报警
            btnResetAlarm = new Button();
            btnResetAlarm.Text = "🔕 复位报警";
            btnResetAlarm.Size = new Size(100, 30);
            btnResetAlarm.Location = new Point(700, 10);
            btnResetAlarm.BackColor = Color.FromArgb(60, 60, 80);
            btnResetAlarm.ForeColor = Color.White;
            btnResetAlarm.FlatStyle = FlatStyle.Flat;
            btnResetAlarm.FlatAppearance.BorderSize = 0;
            btnResetAlarm.Cursor = Cursors.Hand;

            // 系统管理
            btnSystemConfig = new Button();
            btnSystemConfig.Text = "🔧 系统管理";
            btnSystemConfig.Size = new Size(100, 30);
            btnSystemConfig.Location = new Point(810, 10);
            btnSystemConfig.BackColor = Color.FromArgb(60, 60, 80);
            btnSystemConfig.ForeColor = Color.White;
            btnSystemConfig.FlatStyle = FlatStyle.Flat;
            btnSystemConfig.FlatAppearance.BorderSize = 0;
            btnSystemConfig.Cursor = Cursors.Hand;
            btnSystemConfig.Visible = false;

            // ⭐ MES 待重传数量标签
            lblMesPendingCount = new Label();
            lblMesPendingCount.Text = "MES待重传: 0";
            lblMesPendingCount.ForeColor = Color.LightGreen;
            lblMesPendingCount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMesPendingCount.Location = new Point(1000, 16);
            lblMesPendingCount.AutoSize = true;

            // ⭐ MES 手动重传按钮
            btnMesRetry = new Button();
            btnMesRetry.Text = "📤 重传MES";
            btnMesRetry.Size = new Size(110, 30);
            btnMesRetry.Location = new Point(1180, 10);
            btnMesRetry.BackColor = Color.FromArgb(60, 60, 80);
            btnMesRetry.ForeColor = Color.White;
            btnMesRetry.FlatStyle = FlatStyle.Flat;
            btnMesRetry.FlatAppearance.BorderSize = 0;
            btnMesRetry.Cursor = Cursors.Hand;

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblUserInfo);
            topPanel.Controls.Add(btnLogin);
            topPanel.Controls.Add(btnEmergencyStop);
            topPanel.Controls.Add(btnResetAlarm);
            topPanel.Controls.Add(btnSystemConfig);
            topPanel.Controls.Add(lblMesPendingCount);
            topPanel.Controls.Add(btnMesRetry);

            // ================================================
            //  主内容
            // ================================================
            mainContent = new TableLayoutPanel();
            mainContent.Dock = DockStyle.Fill;
            mainContent.ColumnCount = 2;
            mainContent.RowCount = 1;
            mainContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            mainContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainContent.Padding = new Padding(6);

            // ---- 左侧 ----
            leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.BackColor = Color.White;
            leftPanel.Padding = new Padding(4);

            lblDeviceListTitle = new Label();
            lblDeviceListTitle.Text = "🖥️ 设备列表";
            lblDeviceListTitle.Dock = DockStyle.Top;
            lblDeviceListTitle.Height = 30;
            lblDeviceListTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDeviceListTitle.BackColor = Color.FromArgb(245, 245, 250);
            lblDeviceListTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblDeviceListTitle.Padding = new Padding(8, 0, 0, 0);

            listBoxDevices = new ListBox();
            listBoxDevices.Dock = DockStyle.Fill;
            listBoxDevices.DrawMode = DrawMode.OwnerDrawVariable;
            listBoxDevices.IntegralHeight = false;

            deviceButtonPanel = new FlowLayoutPanel();
            deviceButtonPanel.Dock = DockStyle.Bottom;
            deviceButtonPanel.Height = 140;
            deviceButtonPanel.FlowDirection = FlowDirection.LeftToRight;
            deviceButtonPanel.WrapContents = true;
            deviceButtonPanel.Padding = new Padding(4);
            deviceButtonPanel.BackColor = Color.FromArgb(248, 248, 250);

            btnAddDevice = new Button { Text = "➕ 添加", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat };
            btnRemoveDevice = new Button { Text = "➖ 移除", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat };
            btnStartAll = new Button { Text = "▶ 全部启动", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
            btnStopAll = new Button { Text = "⏹ 全部停止", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.LightCoral };
            btnStartSelected = new Button { Text = "▶ 启动选中", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
            btnStopSelected = new Button { Text = "⏹ 停止选中", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.Orange };
            btnResetDevice = new Button { Text = "🔄 复位", Size = new Size(95, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.Gold };

            deviceButtonPanel.Controls.Add(btnAddDevice);
            deviceButtonPanel.Controls.Add(btnRemoveDevice);
            deviceButtonPanel.Controls.Add(btnStartAll);
            deviceButtonPanel.Controls.Add(btnStopAll);
            deviceButtonPanel.Controls.Add(btnStartSelected);
            deviceButtonPanel.Controls.Add(btnStopSelected);
            deviceButtonPanel.Controls.Add(btnResetDevice);

            leftPanel.Controls.Add(listBoxDevices);
            leftPanel.Controls.Add(deviceButtonPanel);
            leftPanel.Controls.Add(lblDeviceListTitle);

            // ---- 中间 ----
            centerPanel = new Panel();
            centerPanel.Dock = DockStyle.Fill;
            centerPanel.Padding = new Padding(0);

            tabMain = new TabControl();
            tabMain.Dock = DockStyle.Fill;

            tabPageOverview = new TabPage();
            tabPageOverview.Text = "📊 产线监控";
            tabPageOverview.Padding = new Padding(4);

            overviewControl = new ProductionOverviewControl();
            overviewControl.Dock = DockStyle.Fill;
            tabPageOverview.Controls.Add(overviewControl);

            tabPageProduction = new TabPage();
            tabPageProduction.Text = "⚙️ 生产执行";
            tabPageProduction.Padding = new Padding(4);

            workflowExecutionControl = new WorkflowExecutionControl();
            workflowExecutionControl.Dock = DockStyle.Fill;
            tabPageProduction.Controls.Add(workflowExecutionControl);

            tabMain.Controls.Add(tabPageOverview);
            tabMain.Controls.Add(tabPageProduction);

            centerPanel.Controls.Add(tabMain);

            mainContent.Controls.Add(leftPanel, 0, 0);
            mainContent.Controls.Add(centerPanel, 1, 0);

            // ================================================
            //  底部日志
            // ================================================
            tabLog = new TabControl();
            tabLog.Dock = DockStyle.Fill;

            tabPageOpLog = new TabPage();
            tabPageOpLog.Text = "📋 操作日志";
            txtOperationLog = new TextBox();
            txtOperationLog.Dock = DockStyle.Fill;
            txtOperationLog.Multiline = true;
            txtOperationLog.ReadOnly = true;
            txtOperationLog.ScrollBars = ScrollBars.Vertical;
            txtOperationLog.BackColor = Color.Black;
            txtOperationLog.ForeColor = Color.Lime;
            txtOperationLog.Font = new Font("Consolas", 9F);
            tabPageOpLog.Controls.Add(txtOperationLog);

            tabPageMonLog = new TabPage();
            tabPageMonLog.Text = "📊 监控日志";
            txtMonitorLog = new TextBox();
            txtMonitorLog.Dock = DockStyle.Fill;
            txtMonitorLog.Multiline = true;
            txtMonitorLog.ReadOnly = true;
            txtMonitorLog.ScrollBars = ScrollBars.Vertical;
            txtMonitorLog.BackColor = Color.Black;
            txtMonitorLog.ForeColor = Color.Cyan;
            txtMonitorLog.Font = new Font("Consolas", 9F);
            tabPageMonLog.Controls.Add(txtMonitorLog);

            tabLog.Controls.Add(tabPageOpLog);
            tabLog.Controls.Add(tabPageMonLog);

            // ================================================
            //  状态栏
            // ================================================
            statusStrip = new StatusStrip();
            statusStrip.Items.Add(new ToolStripStatusLabel("设备: 未选择") { Name = "lblDeviceStatus" });
            statusStrip.Items.Add(new ToolStripStatusLabel("伺服: --") { Name = "lblServoStatus" });
            statusStrip.Items.Add(new ToolStripStatusLabel("限位: 正常") { Name = "lblLimitStatus" });
            statusStrip.Items.Add(new ToolStripStatusLabel("看门狗: 正常") { Name = "lblWatchdogStatus" });
            statusStrip.Items.Add(new ToolStripStatusLabel("Modbus: 未连接") { Name = "lblModbusStatus" });
            statusStrip.Items.Add(new ToolStripStatusLabel("当前指令: 空闲") { Name = "lblCurrentCmd" });

            // ================================================
            //  组装
            // ================================================
            rootLayout.Controls.Add(topPanel, 0, 0);
            rootLayout.Controls.Add(mainContent, 0, 1);
            rootLayout.Controls.Add(tabLog, 0, 2);
            rootLayout.Controls.Add(statusStrip, 0, 3);

            this.Controls.Add(rootLayout);

            // ================================================
            //  绑定事件
            // ================================================
            this.Load += (s, e) => LoadView?.Invoke(s, e);

            btnAddDevice.Click += (s, e) => AddDeviceClicked?.Invoke(s, e);
            btnRemoveDevice.Click += (s, e) => RemoveDeviceClicked?.Invoke(s, e);
            btnStartAll.Click += (s, e) => StartAllClicked?.Invoke(s, e);
            btnStopAll.Click += (s, e) => StopAllClicked?.Invoke(s, e);
            btnStartSelected.Click += (s, e) => StartSelectedClicked?.Invoke(s, e);
            btnStopSelected.Click += (s, e) => StopSelectedClicked?.Invoke(s, e);
            btnResetDevice.Click += (s, e) => ResetDeviceClicked?.Invoke(s, e);

            btnEmergencyStop.Click += (s, e) => EmergencyStopClicked?.Invoke(s, e);
            btnResetAlarm.Click += (s, e) => AlarmResetClicked?.Invoke(s, e);
            btnSystemConfig.Click += (s, e) => SystemConfigClicked?.Invoke(s, e);
            btnLogin.Click += (s, e) => LoginClicked?.Invoke(s, e);

            listBoxDevices.SelectedIndexChanged += (s, e) => DeviceSelected?.Invoke(s, e);

            listBoxDevices.DisplayMember = "Name";

            this.ResumeLayout(false);
        }
    }
}