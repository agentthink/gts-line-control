using GtsTest.Core;
using GtsTest.Services;
using GtsTest.Services.OpcUa;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GtsTest.Controls
{
    public class CommunicationControl : UserControl
    {
        private readonly IOpcUaClient _opcUaClient;
        private System.Windows.Forms.Timer _statusTimer;

        // UI 控件
        private TextBox txtServerUrl;
        private Button btnConnect, btnDisconnect;
        private Label lblStatus, lblStatusValue;
        private TextBox txtSubscribeNode;
        private Button btnSubscribe, btnUnsubscribe;
        private TextBox txtLog;
        private GroupBox grpConnection, grpSubscription, grpData;

        // ✅ 保存事件处理程序引用（用于取消订阅）
        private EventHandler<bool> _connHandler;
        private EventHandler<string> _dataHandler;
        private EventHandler<string> _errorHandler;

        public CommunicationControl()
        {
            BuildUI();
            _opcUaClient = new OpcUaClient();
            SubscribeEvents();
            InitTimer();
            txtServerUrl.Text = "opc.tcp://DESKTOP-S793UAV:53530/OPCUA/SimulationServer";
        }

        private void BuildUI()
        {
            this.SuspendLayout();

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 连接配置
            grpConnection = new GroupBox
            {
                Text = "🔗 OPC UA 连接",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            TableLayoutPanel connLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(5)
            };
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Label lblUrl = new Label { Text = "服务器:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            txtServerUrl = new TextBox { Dock = DockStyle.Fill, Text = "opc.tcp://DESKTOP-S793UAV:53530/OPCUA/SimulationServer" };

            btnConnect = new Button { Text = "连接", Size = new Size(80, 30), BackColor = Color.LightGreen };
            btnConnect.Click += BtnConnect_Click;
            btnDisconnect = new Button { Text = "断开", Size = new Size(80, 30), BackColor = Color.LightCoral, Enabled = false };
            btnDisconnect.Click += BtnDisconnect_Click;

            lblStatus = new Label { Text = "状态:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            lblStatusValue = new Label { Text = "● 未连接", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            connLayout.Controls.Add(lblUrl, 0, 0);
            connLayout.Controls.Add(txtServerUrl, 1, 0);
            connLayout.SetColumnSpan(txtServerUrl, 3);
            connLayout.Controls.Add(btnConnect, 0, 1);
            connLayout.Controls.Add(btnDisconnect, 1, 1);
            connLayout.Controls.Add(lblStatus, 2, 1);
            connLayout.Controls.Add(lblStatusValue, 3, 1);

            grpConnection.Controls.Add(connLayout);

            // 订阅配置
            grpSubscription = new GroupBox
            {
                Text = "📡 数据订阅",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            TableLayoutPanel subLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(5)
            };
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            Label lblNode = new Label { Text = "节点 ID:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            txtSubscribeNode = new TextBox { Dock = DockStyle.Fill, Text = "ns=3;i=1001" };
            btnSubscribe = new Button { Text = "订阅", Size = new Size(80, 30), BackColor = Color.LightBlue, Enabled = false };
            btnSubscribe.Click += BtnSubscribe_Click;
            btnUnsubscribe = new Button { Text = "取消订阅", Size = new Size(80, 30), BackColor = Color.LightGray, Enabled = false };
            btnUnsubscribe.Click += BtnUnsubscribe_Click;

            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            btnPanel.Controls.Add(btnSubscribe);
            btnPanel.Controls.Add(btnUnsubscribe);

            subLayout.Controls.Add(lblNode, 0, 0);
            subLayout.Controls.Add(txtSubscribeNode, 1, 0);
            subLayout.Controls.Add(btnPanel, 2, 0);

            grpSubscription.Controls.Add(subLayout);

            // 数据日志
            grpData = new GroupBox
            {
                Text = "📊 实时数据日志",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9F),
                WordWrap = false
            };
            grpData.Controls.Add(txtLog);

            mainLayout.Controls.Add(grpConnection, 0, 0);
            mainLayout.SetColumnSpan(grpConnection, 2);
            mainLayout.Controls.Add(grpSubscription, 0, 1);
            mainLayout.SetColumnSpan(grpSubscription, 2);
            mainLayout.Controls.Add(grpData, 0, 2);
            mainLayout.SetColumnSpan(grpData, 2);

            this.Controls.Add(mainLayout);
            this.ResumeLayout(false);
        }

        // ---------- 事件订阅（保存引用便于取消） ----------
        private void SubscribeEvents()
        {
            _connHandler = (s, connected) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    lblStatusValue.Text = connected ? "● 已连接" : "● 未连接";
                    lblStatusValue.ForeColor = connected ? Color.Green : Color.Gray;
                    btnConnect.Enabled = !connected;
                    btnDisconnect.Enabled = connected;
                    btnSubscribe.Enabled = connected;
                    if (!connected)
                    {
                        btnUnsubscribe.Enabled = false;
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 连接已断开{Environment.NewLine}");
                    }
                    else
                    {
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 连接成功{Environment.NewLine}");
                    }
                }));
            };

            _dataHandler = (s, data) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    AppLogger.Info($"📢 UI 收到数据事件: {data}", "Communication");
                    var parts = data.Split('|');
                    if (parts.Length == 2)
                    {
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {parts[0]} = {parts[1]}{Environment.NewLine}");
                        if (txtLog.Lines.Length > 100)
                        {
                            var lines = txtLog.Lines;
                            txtLog.Lines = lines[10..];
                        }
                    }
                }));
            };

            _errorHandler = (s, error) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ {error}{Environment.NewLine}");
                }));
            };

            _opcUaClient.ConnectionStateChanged += _connHandler;
            _opcUaClient.DataValueChanged += _dataHandler;
            _opcUaClient.ErrorOccurred += _errorHandler;
        }

        private void InitTimer()
        {
            _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _statusTimer.Tick += (s, e) => { };
            _statusTimer.Start();
        }

        // ---------- 按钮事件 ----------
        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            string url = txtServerUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入 OPC UA 服务器地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!url.StartsWith("opc.tcp://"))
                url = "opc.tcp://" + url;

            bool success = await _opcUaClient.ConnectAsync(url);
            if (!success)
            {
                MessageBox.Show("OPC UA 连接失败，请检查服务器地址和网络", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _opcUaClient.Disconnect();
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 手动断开连接{Environment.NewLine}");
        }

        private void BtnSubscribe_Click(object sender, EventArgs e)
        {
            string nodeId = txtSubscribeNode.Text.Trim();
            if (string.IsNullOrEmpty(nodeId))
            {
                MessageBox.Show("请输入节点 ID", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _opcUaClient.Subscribe(nodeId);
            btnUnsubscribe.Enabled = true;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 订阅节点: {nodeId}{Environment.NewLine}");
        }

        private void BtnUnsubscribe_Click(object sender, EventArgs e)
        {
            string nodeId = txtSubscribeNode.Text.Trim();
            if (!string.IsNullOrEmpty(nodeId))
            {
                _opcUaClient.Unsubscribe(nodeId);
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 取消订阅节点: {nodeId}{Environment.NewLine}");
                btnUnsubscribe.Enabled = false;
            }
        }

        private void InitializeComponent() { }

        // ================================================================
        // ✅ 修复：释放资源并取消事件订阅
        // ================================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 停止定时器
                _statusTimer?.Stop();
                _statusTimer?.Dispose();

                // ✅ 取消 OPC UA 事件订阅
                if (_opcUaClient != null)
                {
                    _opcUaClient.ConnectionStateChanged -= _connHandler;
                    _opcUaClient.DataValueChanged -= _dataHandler;
                    _opcUaClient.ErrorOccurred -= _errorHandler;
                    _opcUaClient.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}