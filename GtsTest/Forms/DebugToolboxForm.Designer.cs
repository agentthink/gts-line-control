using System.Drawing;
using System.Windows.Forms;
using GtsTest.Modbus;

namespace GtsTest.Forms
{
    partial class DebugToolboxForm
    {
        private System.ComponentModel.IContainer components = null;

        // ==================== 顶部设备选择 ====================
        private ComboBox cmbDevice;
        private Label lblDeviceName;
        private Label lblDeviceStatus;
        private Label lblProdInfo;

        // ==================== Tab 控件 ====================
        private TabControl tabMain;
        private TabPage tabAxis;
        private TabPage tabModbus;
        private TabPage tabDevice;
        private TabPage tabVision;
        private TabPage tabSerial;
        private TabPage tabPlc;

        // ==================== 轴控制 ====================
        private Label lblAxisValue;
        private Label lblAxisStatus;
        private Label lblAxisPos;
        private Label lblAxisVel;
        private TextBox txtTargetPos;
        private TextBox txtJogSpeed;
        private TextBox txtAcc;
        private TextBox txtDec;
        private Button btnHome;
        private Button btnMoveAbs;
        private Button btnJogP;
        private Button btnJogN;
        private Button btnStopAxis;
        private Button btnServoOn;
        private Button btnServoOff;
        private Button btnAxisAlarmReset;

        // ==================== Modbus ====================
        private NumericUpDown numRegAddress;
        private NumericUpDown numCoilAddress;
        private NumericUpDown numReadAddress;
        private NumericUpDown numReadCount;
        private ComboBox cmbDataType;
        private ComboBox cmbByteOrder;
        private ComboBox cmbCoilValue;
        private TextBox txtRegValues;
        private TextBox txtReadResult;
        private Button btnWriteRegister;
        private Button btnWriteCoil;
        private Button btnReadRegister;

        // ==================== 设备控制 ====================
        private Label lblDevTitle;
        private Button btnStartDevice;
        private Button btnStopDevice;
        private Button btnDeviceConfig;
        private Button btnConnectModbus;
        private Button btnDisconnectModbus;
        private Label lblCurrentDevice;

        // ==================== 🆕 IO 强制模拟 ====================
        private GroupBox grpForceIO;
        private NumericUpDown numForceIOIndex;
        private CheckBox chkForceIOValue;
        private Button btnForceIO;
        private Button btnClearForceIO;

        // ==================== 视觉触发 ====================
        private Panel visionPanel;
        private Label lblVisionTitle;
        private Label lblVisionIp;
        private TextBox txtVisionIp;
        private Label lblVisionPort;
        private NumericUpDown numVisionPort;
        private Label lblVisionTimeout;
        private NumericUpDown numVisionTimeout;
        private Button btnTriggerVision;
        private GroupBox grpVisionConfig;
        private GroupBox grpVisionStatus;
        private Label lblVisionStatus;

        // ==================== 串口调试 ====================
        private ComboBox cmbSerialPort;
        private ComboBox cmbBaudRate;
        private ComboBox cmbDataBits;
        private ComboBox cmbStopBits;
        private ComboBox cmbParity;
        private ComboBox cmbFrameType;
        private TextBox txtDelimiterHex;
        private Button btnSerialRefresh;
        private Button btnSerialOpen;
        private Button btnSerialClose;
        private Label lblSerialStatus;
        private Label lblSerialRecvCount;
        private TextBox txtSerialSend;
        private TextBox txtSerialRecv;
        private CheckBox chkSerialSendHex;
        private CheckBox chkSerialAppendCRLF;
        private CheckBox chkSerialRecvHex;
        private CheckBox chkSerialAutoScroll;
        private Button btnSerialSend;
        private Button btnSerialClearRecv;

        // ==================== PLC 调试 ====================
        private ComboBox cmbPlcType;
        private TextBox txtPlcIp;
        private NumericUpDown numPlcPort;
        private NumericUpDown numPlcRack;
        private NumericUpDown numPlcSlot;
        private TextBox txtPlcName;
        private Button btnPlcConnect;
        private Button btnPlcDisconnect;
        private Label lblPlcStatus;
        private TextBox txtPlcReadAddr;
        private ComboBox cmbPlcReadType;
        private Button btnPlcRead;
        private TextBox txtPlcWriteAddr;
        private ComboBox cmbPlcWriteType;
        private TextBox txtPlcWriteValue;
        private Button btnPlcWrite;
        private TextBox txtPlcLog;
        private Button btnPlcClearLog;

        // ==================== 定时器 ====================
        private System.Windows.Forms.Timer timerRefresh;

        // ==================== 布局面板 ====================
        private Panel axisPanel;
        private Panel topPanel;
        private Panel modbusPanel;
        private Panel devicePanel;

        // ==================== 标签 ====================
        private Label lblDev;
        private Label lblName;
        private Label lblStatus;
        private Label lblProd;
        private Label lblA1;
        private Label lblA2;
        private Label lblPos;
        private Label lblVel;
        private Label lblTarget;
        private Label lblJog;
        private Label lblAccLabel;
        private Label lblDecLabel;

        // ==================== Modbus 分组 ====================
        private GroupBox grpWriteReg;
        private GroupBox grpCoil;
        private GroupBox grpRead;

        // ==================== Modbus 标签 ====================
        private Label lblRegAddr;
        private Label lblRegType;
        private Label lblRegByte;
        private Label lblRegVal;
        private Label lblCoilAddr;
        private Label lblCoilVal;
        private Label lblReadAddr;
        private Label lblReadCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            this.ClientSize = new Size(1150, 640);
            this.Text = "🔧 调试工具箱";
            this.MinimumSize = new Size(1000, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormClosing += DebugToolboxForm_FormClosing;

            timerRefresh = new System.Windows.Forms.Timer(components);
            timerRefresh.Interval = 500;

            tabMain = new TabControl();
            tabMain.Dock = DockStyle.Fill;

            BuildAxisTab();
            BuildModbusTab();
            BuildDeviceTab();
            BuildVisionTab();
            BuildSerialTab();
            BuildPlcTab();

            tabMain.Controls.Add(tabAxis);
            tabMain.Controls.Add(tabModbus);
            tabMain.Controls.Add(tabDevice);
            tabMain.Controls.Add(tabVision);
            tabMain.Controls.Add(tabSerial);
            tabMain.Controls.Add(tabPlc);

            this.Controls.Add(tabMain);

            // 填充 ComboBox
            cmbDataType.Items.Clear();
            cmbDataType.Items.AddRange(Enum.GetNames(typeof(DataType)));
            cmbDataType.SelectedIndex = 0;

            cmbByteOrder.Items.Clear();
            cmbByteOrder.Items.AddRange(Enum.GetNames(typeof(ByteOrder)));
            cmbByteOrder.SelectedIndex = 0;

            cmbCoilValue.Items.Clear();
            cmbCoilValue.Items.Add("ON (1)");
            cmbCoilValue.Items.Add("OFF (0)");
            cmbCoilValue.SelectedIndex = 0;

            this.ResumeLayout(false);
        }

        // ================================================================
        //  轴控制 Tab
        // ================================================================
        private void BuildAxisTab()
        {
            tabAxis = new TabPage();
            tabAxis.Text = "🎯 轴控制";
            tabAxis.Size = new Size(1142, 610);

            axisPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            topPanel = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.WhiteSmoke };

            lblDev = new Label { Location = new Point(3, 10), Size = new Size(64, 20), Text = "当前设备:", TextAlign = ContentAlignment.MiddleRight };
            cmbDevice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(70, 8), Size = new Size(150, 23) };
            lblName = new Label { Location = new Point(230, 10), Size = new Size(45, 20), Text = "名称:", TextAlign = ContentAlignment.MiddleRight };
            lblDeviceName = new Label { Location = new Point(278, 10), Size = new Size(80, 20), Text = "--", TextAlign = ContentAlignment.MiddleLeft };
            lblStatus = new Label { Location = new Point(370, 10), Size = new Size(40, 20), Text = "状态:", TextAlign = ContentAlignment.MiddleRight };
            lblDeviceStatus = new Label { Location = new Point(412, 10), Size = new Size(60, 20), Text = "未连接", ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft };
            lblProd = new Label { Location = new Point(480, 10), Size = new Size(40, 20), Text = "产量:", TextAlign = ContentAlignment.MiddleRight };
            lblProdInfo = new Label { Location = new Point(522, 10), Size = new Size(80, 20), Text = "0/0", TextAlign = ContentAlignment.MiddleLeft };

            topPanel.Controls.AddRange(new Control[] { lblDev, cmbDevice, lblName, lblDeviceName, lblStatus, lblDeviceStatus, lblProd, lblProdInfo });

            lblA1 = new Label { Location = new Point(20, 45), Size = new Size(50, 20), Text = "轴号:", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblAxisValue = new Label { Location = new Point(72, 45), Size = new Size(60, 20), Text = "--" };
            lblA2 = new Label { Location = new Point(160, 45), Size = new Size(60, 20), Text = "轴状态:", Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblAxisStatus = new Label { Location = new Point(225, 45), Size = new Size(80, 20), Text = "--" };
            lblPos = new Label { Location = new Point(20, 70), Size = new Size(60, 20), Text = "当前位置:" };
            lblAxisPos = new Label { Location = new Point(85, 70), Size = new Size(80, 20), Text = "--" };
            lblVel = new Label { Location = new Point(180, 70), Size = new Size(60, 20), Text = "当前速度:" };
            lblAxisVel = new Label { Location = new Point(245, 70), Size = new Size(80, 20), Text = "--" };

            lblTarget = new Label { Location = new Point(20, 100), Size = new Size(60, 20), Text = "目标位置:" };
            txtTargetPos = new TextBox { Location = new Point(85, 97), Size = new Size(80, 23), Text = "10000" };
            lblJog = new Label { Location = new Point(180, 100), Size = new Size(60, 20), Text = "点动速度:" };
            txtJogSpeed = new TextBox { Location = new Point(245, 97), Size = new Size(80, 23), Text = "10" };
            lblAccLabel = new Label { Location = new Point(20, 130), Size = new Size(60, 20), Text = "加速度:" };
            txtAcc = new TextBox { Location = new Point(85, 127), Size = new Size(80, 23), Text = "5" };
            lblDecLabel = new Label { Location = new Point(180, 130), Size = new Size(60, 20), Text = "减速度:" };
            txtDec = new TextBox { Location = new Point(245, 127), Size = new Size(80, 23), Text = "5" };

            btnHome = new Button { Location = new Point(20, 170), Size = new Size(70, 34), Text = "回零" };
            btnMoveAbs = new Button { Location = new Point(100, 170), Size = new Size(70, 34), Text = "定位" };
            btnJogP = new Button { Location = new Point(180, 170), Size = new Size(70, 34), Text = "点动+" };
            btnJogN = new Button { Location = new Point(260, 170), Size = new Size(70, 34), Text = "点动-" };
            btnStopAxis = new Button { Location = new Point(340, 170), Size = new Size(70, 34), Text = "停止轴", BackColor = Color.LightCoral };
            btnServoOn = new Button { Location = new Point(420, 170), Size = new Size(70, 34), Text = "使能", BackColor = Color.LightGreen };
            btnServoOff = new Button { Location = new Point(500, 170), Size = new Size(70, 34), Text = "去使能" };
            btnAxisAlarmReset = new Button { Location = new Point(580, 170), Size = new Size(70, 34), Text = "复位报警", BackColor = Color.Gold };

            axisPanel.Controls.AddRange(new Control[] {
                topPanel, lblA1, lblAxisValue, lblA2, lblAxisStatus, lblPos, lblAxisPos, lblVel, lblAxisVel,
                lblTarget, txtTargetPos, lblJog, txtJogSpeed, lblAccLabel, txtAcc, lblDecLabel, txtDec,
                btnHome, btnMoveAbs, btnJogP, btnJogN, btnStopAxis, btnServoOn, btnServoOff, btnAxisAlarmReset
            });

            tabAxis.Controls.Add(axisPanel);
        }

        // ================================================================
        //  Modbus Tab
        // ================================================================
        private void BuildModbusTab()
        {
            tabModbus = new TabPage();
            tabModbus.Text = "📡 Modbus";

            modbusPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            grpWriteReg = new GroupBox { Text = "写寄存器", Location = new Point(10, 10), Size = new Size(1100, 70) };
            lblRegAddr = new Label { Location = new Point(20, 27), Size = new Size(50, 20), Text = "地址:", TextAlign = ContentAlignment.MiddleRight };
            numRegAddress = new NumericUpDown { Location = new Point(75, 24), Maximum = 65535, Size = new Size(80, 23) };
            lblRegType = new Label { Location = new Point(170, 27), Size = new Size(40, 20), Text = "类型:", TextAlign = ContentAlignment.MiddleRight };
            cmbDataType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(215, 24), Size = new Size(90, 25) };
            lblRegByte = new Label { Location = new Point(320, 27), Size = new Size(50, 20), Text = "字节序:", TextAlign = ContentAlignment.MiddleRight };
            cmbByteOrder = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(375, 24), Size = new Size(100, 25) };
            lblRegVal = new Label { Location = new Point(490, 27), Size = new Size(90, 20), Text = "值(逗号分隔):", TextAlign = ContentAlignment.MiddleRight };
            txtRegValues = new TextBox { Location = new Point(585, 24), Size = new Size(120, 23), Text = "1" };
            btnWriteRegister = new Button { Location = new Point(730, 19), Size = new Size(120, 30), Text = "写入寄存器" };
            grpWriteReg.Controls.AddRange(new Control[] { lblRegAddr, numRegAddress, lblRegType, cmbDataType, lblRegByte, cmbByteOrder, lblRegVal, txtRegValues, btnWriteRegister });

            grpCoil = new GroupBox { Text = "写线圈", Location = new Point(10, 85), Size = new Size(1100, 65) };
            lblCoilAddr = new Label { Location = new Point(20, 27), Size = new Size(50, 20), Text = "地址:", TextAlign = ContentAlignment.MiddleRight };
            numCoilAddress = new NumericUpDown { Location = new Point(75, 24), Maximum = 65535, Size = new Size(80, 23) };
            lblCoilVal = new Label { Location = new Point(170, 27), Size = new Size(40, 20), Text = "值:", TextAlign = ContentAlignment.MiddleRight };
            cmbCoilValue = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(215, 24), Size = new Size(90, 25) };
            btnWriteCoil = new Button { Location = new Point(330, 19), Size = new Size(120, 30), Text = "写入线圈" };
            grpCoil.Controls.AddRange(new Control[] { lblCoilAddr, numCoilAddress, lblCoilVal, cmbCoilValue, btnWriteCoil });

            grpRead = new GroupBox { Text = "读寄存器", Location = new Point(10, 155), Size = new Size(1100, 130) };
            lblReadAddr = new Label { Location = new Point(20, 27), Size = new Size(50, 20), Text = "地址:", TextAlign = ContentAlignment.MiddleRight };
            numReadAddress = new NumericUpDown { Location = new Point(75, 24), Maximum = 65535, Size = new Size(80, 23) };
            lblReadCount = new Label { Location = new Point(170, 27), Size = new Size(50, 20), Text = "数量:", TextAlign = ContentAlignment.MiddleRight };
            numReadCount = new NumericUpDown { Location = new Point(225, 24), Maximum = 125, Minimum = 1, Value = 10, Size = new Size(80, 23) };
            btnReadRegister = new Button { Location = new Point(330, 19), Size = new Size(120, 30), Text = "读取" };
            txtReadResult = new TextBox { Location = new Point(20, 55), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Size = new Size(1060, 65) };
            grpRead.Controls.AddRange(new Control[] { lblReadAddr, numReadAddress, lblReadCount, numReadCount, btnReadRegister, txtReadResult });

            modbusPanel.Controls.AddRange(new Control[] { grpWriteReg, grpCoil, grpRead });
            tabModbus.Controls.Add(modbusPanel);
        }

        // ================================================================
        //  设备控制 Tab（IO 强制区：所有控件垂直居中对齐）
        // ================================================================
        private void BuildDeviceTab()
        {
            tabDevice = new TabPage { Text = "⚙️ 设备控制" };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20, 15, 20, 20),
                BackColor = Color.White
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));   // 当前设备
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));  // 单设备控制
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));  // IO 强制模拟
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // 空白

            // ---------- 区域 1：当前设备 ----------
            lblCurrentDevice = new Label
            {
                Dock = DockStyle.Fill,
                Text = "当前设备: 未选择",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblCurrentDevice, 0, 0);

            // ---------- 区域 2：单设备控制 ----------
            var grpDeviceCtrl = new GroupBox
            {
                Text = "🎛️ 单设备控制",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 5)
            };

            var deviceBtnLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(5)
            };
            deviceBtnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            deviceBtnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            deviceBtnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            deviceBtnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            deviceBtnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            btnStartDevice = new Button
            {
                Text = "▶ 启动设备",
                Dock = DockStyle.Fill,
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3)
            };
            btnStopDevice = new Button
            {
                Text = "⏹ 停止设备",
                Dock = DockStyle.Fill,
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3)
            };
            btnDeviceConfig = new Button
            {
                Text = "⚙️ 配置",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3)
            };
            btnConnectModbus = new Button
            {
                Text = "🔌 连接 Modbus",
                Dock = DockStyle.Fill,
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(3)
            };
            btnDisconnectModbus = new Button
            {
                Text = "🔌 断开 Modbus",
                Dock = DockStyle.Fill,
                BackColor = Color.LightPink,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(3)
            };

            deviceBtnLayout.Controls.Add(btnStartDevice, 0, 0);
            deviceBtnLayout.Controls.Add(btnStopDevice, 1, 0);
            deviceBtnLayout.Controls.Add(btnDeviceConfig, 2, 0);
            deviceBtnLayout.Controls.Add(btnConnectModbus, 0, 1);
            deviceBtnLayout.Controls.Add(btnDisconnectModbus, 1, 1);

            grpDeviceCtrl.Controls.Add(deviceBtnLayout);
            mainLayout.Controls.Add(grpDeviceCtrl, 0, 1);

            // ---------- 区域 3：IO 强制模拟 ----------
            grpForceIO = new GroupBox
            {
                Text = "🔧 IO 强制模拟（调试专用）",
                Size = new Size(650, 90),
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 5, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                Padding = new Padding(8, 5, 8, 5)
            };

            var ioLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(5)
            };
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75F));   // IO索引:
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));  // NumericUpDown
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));  // CheckBox
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));  // 应用强制
            ioLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));  // 清除全部
            ioLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ---- 标签：右对齐 + 垂直居中 ----
            var lblIOIndex = new Label
            {
                Text = "IO索引:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 5, 0)
            };

            // ---- NumericUpDown：Anchor=None 让它在单元格内垂直+水平居中 ----
            numForceIOIndex = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 127,
                Anchor = AnchorStyles.None,
                Size = new Size(90, 25),
                Margin = new Padding(0)
            };

            // ---- CheckBox：Anchor=None + AutoSize，垂直居中 ----
            chkForceIOValue = new CheckBox
            {
                Text = "强制为 True",
                Anchor = AnchorStyles.None,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            // ---- 应用按钮：Anchor=None + 固定尺寸，垂直居中 ----
            btnForceIO = new Button
            {
                Text = "✅ 应用强制",
                Anchor = AnchorStyles.None,
                Size = new Size(125, 34),
                BackColor = Color.LightYellow,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };

            // ---- 清除按钮：Anchor=None + 固定尺寸，垂直居中 ----
            btnClearForceIO = new Button
            {
                Text = "🗑️ 清除全部",
                Anchor = AnchorStyles.None,
                Size = new Size(135, 34),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };

            ioLayout.Controls.Add(lblIOIndex, 0, 0);
            ioLayout.Controls.Add(numForceIOIndex, 1, 0);
            ioLayout.Controls.Add(chkForceIOValue, 2, 0);
            ioLayout.Controls.Add(btnForceIO, 3, 0);
            ioLayout.Controls.Add(btnClearForceIO, 4, 0);

            grpForceIO.Controls.Add(ioLayout);
            mainLayout.Controls.Add(grpForceIO, 0, 2);

            tabDevice.Controls.Add(mainLayout);
        }

        // ================================================================
        //  视觉触发 Tab
        // ================================================================
        private void BuildVisionTab()
        {
            tabVision = new TabPage { Text = "📷 视觉触发" };

            visionPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            lblVisionTitle = new Label
            {
                Location = new Point(20, 15),
                Size = new Size(300, 28),
                Text = "📷 视觉服务器触发调试",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.DarkSlateGray
            };

            grpVisionConfig = new GroupBox
            {
                Text = "服务器配置",
                Location = new Point(20, 50),
                Size = new Size(500, 100),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var configLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(8)
            };
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            configLayout.Controls.Add(new Label { Text = "IP:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtVisionIp = new TextBox { Text = "127.0.0.1", Dock = DockStyle.Fill };
            configLayout.Controls.Add(txtVisionIp, 1, 0);
            configLayout.Controls.Add(new Label { Text = "端口:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            numVisionPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 503, Dock = DockStyle.Fill };
            configLayout.Controls.Add(numVisionPort, 3, 0);
            configLayout.Controls.Add(new Label { Text = "超时:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            numVisionTimeout = new NumericUpDown { Minimum = 1000, Maximum = 60000, Value = 10000, Increment = 1000, Dock = DockStyle.Fill };
            configLayout.Controls.Add(numVisionTimeout, 1, 1);

            btnTriggerVision = new Button
            {
                Text = "📸 触发拍照",
                Dock = DockStyle.Fill,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            configLayout.Controls.Add(btnTriggerVision, 2, 1);
            configLayout.SetColumnSpan(btnTriggerVision, 2);

            grpVisionConfig.Controls.Add(configLayout);

            grpVisionStatus = new GroupBox
            {
                Text = "状态",
                Location = new Point(20, 165),
                Size = new Size(500, 70),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblVisionStatus = new Label
            {
                Location = new Point(15, 30),
                Size = new Size(470, 25),
                Text = "就绪，等待触发...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            grpVisionStatus.Controls.Add(lblVisionStatus);

            visionPanel.Controls.AddRange(new Control[] { lblVisionTitle, grpVisionConfig, grpVisionStatus });
            tabVision.Controls.Add(visionPanel);
        }

        // ================================================================
        //  串口调试 Tab
        // ================================================================
        private void BuildSerialTab()
        {
            tabSerial = new TabPage();
            tabSerial.Text = "🔌 串口调试";
            tabSerial.Padding = new Padding(4);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(6)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grpConn = new GroupBox
            {
                Text = "🔗 连接配置",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var connLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 4,
                Padding = new Padding(4)
            };
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int i = 0; i < 4; i++)
                connLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            connLayout.Controls.Add(new Label { Text = "串口:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            cmbSerialPort = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            connLayout.Controls.Add(cmbSerialPort, 1, 0);

            connLayout.Controls.Add(new Label { Text = "波特率:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 0);
            cmbBaudRate = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
            cmbBaudRate.Items.AddRange(new object[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 });
            cmbBaudRate.SelectedItem = 9600;
            connLayout.Controls.Add(cmbBaudRate, 3, 0);

            connLayout.Controls.Add(new Label { Text = "数据位:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 0);
            cmbDataBits = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
            cmbDataBits.SelectedItem = 8;
            connLayout.Controls.Add(cmbDataBits, 5, 0);

            connLayout.Controls.Add(new Label { Text = "停止位:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            cmbStopBits = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStopBits.Items.AddRange(new object[] { "1", "1.5", "2" });
            cmbStopBits.SelectedIndex = 0;
            connLayout.Controls.Add(cmbStopBits, 1, 1);

            connLayout.Controls.Add(new Label { Text = "校验位:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 1);
            cmbParity = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
            cmbParity.SelectedIndex = 0;
            connLayout.Controls.Add(cmbParity, 3, 1);

            connLayout.Controls.Add(new Label { Text = "帧格式:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 1);
            cmbFrameType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFrameType.Items.AddRange(new object[] { "原始流", "定长", "头+长度", "分隔符" });
            cmbFrameType.SelectedIndex = 3;
            connLayout.Controls.Add(cmbFrameType, 5, 1);

            connLayout.Controls.Add(new Label { Text = "分隔符HEX:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            txtDelimiterHex = new TextBox { Dock = DockStyle.Fill, Text = "0D 0A" };
            connLayout.Controls.Add(txtDelimiterHex, 1, 2);
            connLayout.SetColumnSpan(txtDelimiterHex, 2);

            btnSerialRefresh = new Button { Text = "🔄 刷新", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            connLayout.Controls.Add(btnSerialRefresh, 3, 2);

            btnSerialOpen = new Button { Text = "🔌 打开", Dock = DockStyle.Fill, BackColor = Color.LightGreen, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            connLayout.Controls.Add(btnSerialOpen, 4, 2);

            btnSerialClose = new Button { Text = "❌ 关闭", Dock = DockStyle.Fill, BackColor = Color.LightCoral, FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(3) };
            connLayout.Controls.Add(btnSerialClose, 5, 2);

            lblSerialStatus = new Label
            {
                Text = "● 未连接",
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            connLayout.Controls.Add(lblSerialStatus, 0, 3);
            connLayout.SetColumnSpan(lblSerialStatus, 6);

            grpConn.Controls.Add(connLayout);
            mainLayout.Controls.Add(grpConn, 0, 0);

            var grpSend = new GroupBox
            {
                Text = "📤 发送",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var sendLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(4)
            };
            sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

            txtSerialSend = new TextBox { Dock = DockStyle.Fill, Text = "Hello" };
            sendLayout.Controls.Add(txtSerialSend, 0, 0);

            chkSerialSendHex = new CheckBox { Text = "HEX", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            sendLayout.Controls.Add(chkSerialSendHex, 1, 0);

            chkSerialAppendCRLF = new CheckBox { Text = "追加\\r\\n", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Checked = true };
            sendLayout.Controls.Add(chkSerialAppendCRLF, 2, 0);

            btnSerialSend = new Button
            {
                Text = "发送",
                Dock = DockStyle.Fill,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            sendLayout.Controls.Add(btnSerialSend, 3, 0);

            grpSend.Controls.Add(sendLayout);
            mainLayout.Controls.Add(grpSend, 0, 1);

            var grpRecv = new GroupBox
            {
                Text = "📥 接收",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var recvLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            recvLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            recvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var recvToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };

            chkSerialRecvHex = new CheckBox { Text = "HEX 显示", AutoSize = true, Margin = new Padding(3, 5, 8, 0) };
            recvToolbar.Controls.Add(chkSerialRecvHex);

            chkSerialAutoScroll = new CheckBox { Text = "自动滚动", AutoSize = true, Checked = true, Margin = new Padding(3, 5, 8, 0) };
            recvToolbar.Controls.Add(chkSerialAutoScroll);

            btnSerialClearRecv = new Button { Text = "清空接收区", FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            recvToolbar.Controls.Add(btnSerialClearRecv);

            lblSerialRecvCount = new Label { Text = "已收: 0 字节", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(15, 8, 0, 0) };
            recvToolbar.Controls.Add(lblSerialRecvCount);

            recvLayout.Controls.Add(recvToolbar, 0, 0);

            txtSerialRecv = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9.5F),
                WordWrap = false
            };
            recvLayout.Controls.Add(txtSerialRecv, 0, 1);

            grpRecv.Controls.Add(recvLayout);
            mainLayout.Controls.Add(grpRecv, 0, 2);

            tabSerial.Controls.Add(mainLayout);
        }

        // ================================================================
        //  PLC 调试 Tab
        // ================================================================
        private void BuildPlcTab()
        {
            tabPlc = new TabPage();
            tabPlc.Text = "🔧 PLC 调试";
            tabPlc.Padding = new Padding(6);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(4)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ==================== 1. 连接配置 ====================
            var grpConn = new GroupBox
            {
                Text = "🔗 PLC 连接",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var connGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(2)
            };
            connGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            connGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            // ---- Row 0: 类型 / IP / 名称 ----
            var connRow0 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            connRow0.Controls.Add(new Label { Text = "类型:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            cmbPlcType = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 15, 0) };
            cmbPlcType.Items.AddRange(new object[] {
                "模拟 (Simulated)",
                "西门子 S7-1200",
                "西门子 S7-1500",
                "西门子 S7-300",
                "西门子 S7-400",
                "西门子 S7-200 Smart",
                "三菱 MC"
            });
            cmbPlcType.SelectedIndex = 0;
            connRow0.Controls.Add(cmbPlcType);

            connRow0.Controls.Add(new Label { Text = "IP:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            txtPlcIp = new TextBox { Width = 140, Text = "192.168.1.10", Margin = new Padding(0, 4, 15, 0) };
            connRow0.Controls.Add(txtPlcIp);

            connRow0.Controls.Add(new Label { Text = "名称:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            txtPlcName = new TextBox { Width = 120, Text = "PLC1", Margin = new Padding(0, 4, 0, 0) };
            connRow0.Controls.Add(txtPlcName);

            connGrid.Controls.Add(connRow0, 0, 0);

            // ---- Row 1: 端口 / Rack / Slot / 按钮 ----
            var connRow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            connRow1.Controls.Add(new Label { Text = "端口:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            numPlcPort = new NumericUpDown { Width = 70, Minimum = 1, Maximum = 65535, Value = 102, Margin = new Padding(0, 4, 15, 0) };
            connRow1.Controls.Add(numPlcPort);

            connRow1.Controls.Add(new Label { Text = "Rack:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            numPlcRack = new NumericUpDown { Width = 55, Minimum = 0, Maximum = 7, Value = 0, Margin = new Padding(0, 4, 15, 0) };
            connRow1.Controls.Add(numPlcRack);

            connRow1.Controls.Add(new Label { Text = "Slot:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            numPlcSlot = new NumericUpDown { Width = 55, Minimum = 0, Maximum = 31, Value = 1, Margin = new Padding(0, 4, 20, 0) };
            connRow1.Controls.Add(numPlcSlot);

            btnPlcConnect = new Button
            {
                Text = "🔌 连接",
                Width = 90,
                Height = 26,
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3, 3, 5, 0)
            };
            connRow1.Controls.Add(btnPlcConnect);

            btnPlcDisconnect = new Button
            {
                Text = "❌ 断开",
                Width = 90,
                Height = 26,
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            connRow1.Controls.Add(btnPlcDisconnect);

            connGrid.Controls.Add(connRow1, 0, 1);

            grpConn.Controls.Add(connGrid);
            mainLayout.Controls.Add(grpConn, 0, 0);

            // ==================== 2. 读取 ====================
            var grpRead = new GroupBox
            {
                Text = "📥 读取",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var readFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            readFlow.Controls.Add(new Label { Text = "地址:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            txtPlcReadAddr = new TextBox { Width = 180, Text = "DB1.DBW0", Margin = new Padding(0, 4, 25, 0) };
            readFlow.Controls.Add(txtPlcReadAddr);

            readFlow.Controls.Add(new Label { Text = "类型:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            cmbPlcReadType = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 25, 0) };
            cmbPlcReadType.Items.AddRange(new object[] { "Bool", "Short", "UShort", "Int", "UInt", "Float", "Double", "String" });
            cmbPlcReadType.SelectedIndex = 1;
            readFlow.Controls.Add(cmbPlcReadType);

            btnPlcRead = new Button
            {
                Text = "📥 读取",
                Width = 120,
                Height = 26,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3, 3, 0, 0)
            };
            readFlow.Controls.Add(btnPlcRead);

            grpRead.Controls.Add(readFlow);
            mainLayout.Controls.Add(grpRead, 0, 1);

            // ==================== 3. 写入 ====================
            var grpWrite = new GroupBox
            {
                Text = "📤 写入",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var writeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            writeFlow.Controls.Add(new Label { Text = "地址:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            txtPlcWriteAddr = new TextBox { Width = 150, Text = "DB1.DBW0", Margin = new Padding(0, 4, 20, 0) };
            writeFlow.Controls.Add(txtPlcWriteAddr);

            writeFlow.Controls.Add(new Label { Text = "类型:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            cmbPlcWriteType = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 20, 0) };
            cmbPlcWriteType.Items.AddRange(new object[] { "Bool", "Short", "UShort", "Int", "UInt", "Float", "Double", "String" });
            cmbPlcWriteType.SelectedIndex = 1;
            writeFlow.Controls.Add(cmbPlcWriteType);

            writeFlow.Controls.Add(new Label { Text = "值:", AutoSize = true, Margin = new Padding(3, 8, 2, 0) });
            txtPlcWriteValue = new TextBox { Width = 140, Text = "100", Margin = new Padding(0, 4, 20, 0) };
            writeFlow.Controls.Add(txtPlcWriteValue);

            btnPlcWrite = new Button
            {
                Text = "📤 写入",
                Width = 120,
                Height = 26,
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3, 3, 0, 0)
            };
            writeFlow.Controls.Add(btnPlcWrite);

            grpWrite.Controls.Add(writeFlow);
            mainLayout.Controls.Add(grpWrite, 0, 2);

            // ==================== 4. 日志 ====================
            var grpLog = new GroupBox
            {
                Text = "📋 操作日志",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var logLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            txtPlcLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9.5F),
                WordWrap = false
            };
            logLayout.Controls.Add(txtPlcLog, 0, 0);

            btnPlcClearLog = new Button
            {
                Text = "清空",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 0, 0, 0)
            };
            logLayout.Controls.Add(btnPlcClearLog, 1, 0);

            grpLog.Controls.Add(logLayout);
            mainLayout.Controls.Add(grpLog, 0, 3);

            // ==================== 5. 状态栏 ====================
            lblPlcStatus = new Label
            {
                Text = "● 未连接",
                ForeColor = Color.Gray,
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            tabPlc.Controls.Add(mainLayout);
            tabPlc.Controls.Add(lblPlcStatus);
        }
    }
}