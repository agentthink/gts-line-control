using GtsTest.Presenters;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GtsTest.Controls
{
    public class MqttControl : UserControl, IMqttView
    {
        // ---------- 事件 ----------
        public event Action ConnectClicked;
        public event Action DisconnectClicked;
        public event Action SubscribeClicked;
        public event Action UnsubscribeClicked;
        public event Action PublishClicked;

        // ---------- UI 控件 ----------
        private TextBox txtBroker, txtPort, txtUsername, txtPassword;
        private TextBox txtSubscribeTopic, txtPublishTopic, txtPublishPayload;
        private CheckBox chkRetain;
        private Button btnConnect, btnDisconnect, btnSubscribe, btnUnsubscribe, btnPublish;
        private Label lblStatus;
        private TextBox txtLog;

        public MqttControl()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.SuspendLayout();

            // 主布局：4 行，日志区域不再占用过多高度
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(6),
                BackColor = Color.WhiteSmoke
            };
            // 行高：连接 120，订阅 80，发布 110，剩余给日志
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

            // ---------- 1. 连接配置 ----------
            GroupBox grpConnection = new GroupBox
            {
                Text = "🔗 MQTT 连接",
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            TableLayoutPanel connLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 3,
                Padding = new Padding(3)
            };
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45));
            connLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            connLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            connLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            connLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            // Broker
            connLayout.Controls.Add(new Label { Text = "Broker:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtBroker = new TextBox { Dock = DockStyle.Fill, Text = "test.mosquitto.org" };
            connLayout.Controls.Add(txtBroker, 1, 0);
            connLayout.Controls.Add(new Label { Text = "端口:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            txtPort = new TextBox { Dock = DockStyle.Fill, Text = "1883" };
            connLayout.Controls.Add(txtPort, 3, 0);
            connLayout.Controls.Add(new Label { Text = "用户:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 4, 0);
            txtUsername = new TextBox { Dock = DockStyle.Fill };
            connLayout.Controls.Add(txtUsername, 5, 0);

            // 密码
            connLayout.Controls.Add(new Label { Text = "密码:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 4, 1);
            txtPassword = new TextBox { Dock = DockStyle.Fill, PasswordChar = '*' };
            connLayout.Controls.Add(txtPassword, 5, 1);

            // 按钮和状态
            btnConnect = new Button { Text = "连接", Size = new Size(70, 26), BackColor = Color.LightGreen };
            btnConnect.Click += (s, e) => ConnectClicked?.Invoke();
            btnDisconnect = new Button { Text = "断开", Size = new Size(70, 26), BackColor = Color.LightCoral, Enabled = false };
            btnDisconnect.Click += (s, e) => DisconnectClicked?.Invoke();
            lblStatus = new Label { Text = "● 未连接", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            connLayout.Controls.Add(btnConnect, 0, 2);
            connLayout.Controls.Add(btnDisconnect, 1, 2);
            connLayout.Controls.Add(lblStatus, 4, 2);
            connLayout.SetColumnSpan(lblStatus, 2);

            grpConnection.Controls.Add(connLayout);
            mainLayout.Controls.Add(grpConnection, 0, 0);

            // ---------- 2. 订阅 ----------
            GroupBox grpSubscription = new GroupBox
            {
                Text = "📥 订阅",
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            TableLayoutPanel subLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(3)
            };
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            subLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            subLayout.Controls.Add(new Label { Text = "主题:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtSubscribeTopic = new TextBox { Dock = DockStyle.Fill, Text = "test/topic" };
            subLayout.Controls.Add(txtSubscribeTopic, 1, 0);

            FlowLayoutPanel subBtnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(3, 1, 0, 0)
            };
            btnSubscribe = new Button { Text = "订阅", Size = new Size(65, 24), BackColor = Color.LightBlue, Enabled = false };
            btnSubscribe.Click += (s, e) => SubscribeClicked?.Invoke();
            btnUnsubscribe = new Button { Text = "取消订阅", Size = new Size(80, 24), BackColor = Color.LightGray, Enabled = false };
            btnUnsubscribe.Click += (s, e) => UnsubscribeClicked?.Invoke();
            subBtnPanel.Controls.Add(btnSubscribe);
            subBtnPanel.Controls.Add(btnUnsubscribe);
            subLayout.Controls.Add(subBtnPanel, 2, 0);

            grpSubscription.Controls.Add(subLayout);
            mainLayout.Controls.Add(grpSubscription, 0, 1);

            // ---------- 3. 发布 ----------
            GroupBox grpPublish = new GroupBox
            {
                Text = "📤 发布",
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            TableLayoutPanel pubLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(3)
            };
            pubLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            pubLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pubLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            pubLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // 增加高度，确保控件完全显示

            // 行0: 主题
            pubLayout.Controls.Add(new Label { Text = "主题:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtPublishTopic = new TextBox { Dock = DockStyle.Fill, Text = "test/topic" };
            pubLayout.Controls.Add(txtPublishTopic, 1, 0);

            // 行1: 消息 + 保留 + 发布
            TableLayoutPanel row1Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            row1Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            txtPublishPayload = new TextBox { Dock = DockStyle.Fill, Text = "Hello MQTT" };
            row1Layout.Controls.Add(txtPublishPayload, 0, 0);

            FlowLayoutPanel pubRightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5, 3, 0, 0)
            };
            chkRetain = new CheckBox { Text = "保留", AutoSize = true, CheckAlign = ContentAlignment.MiddleLeft };
            btnPublish = new Button { Text = "发布", Size = new Size(65, 24), BackColor = Color.LightGreen, Enabled = false };
            btnPublish.Click += (s, e) => PublishClicked?.Invoke();
            pubRightPanel.Controls.Add(chkRetain);
            pubRightPanel.Controls.Add(btnPublish);

            row1Layout.Controls.Add(pubRightPanel, 1, 0);

            pubLayout.Controls.Add(row1Layout, 1, 1);

            grpPublish.Controls.Add(pubLayout);
            mainLayout.Controls.Add(grpPublish, 0, 2);

            // ---------- 4. 消息日志 ----------
            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9F)
            };
            mainLayout.Controls.Add(txtLog, 0, 3);

            this.Controls.Add(mainLayout);
            this.ResumeLayout(false);
        }

        // ---------- 实现 IMqttView ----------
        public void UpdateConnectionStatus(bool connected, string brokerInfo)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateConnectionStatus(connected, brokerInfo)));
                return;
            }
            lblStatus.Text = connected ? $"● 已连接 ({brokerInfo})" : "● 未连接";
            lblStatus.ForeColor = connected ? Color.Green : Color.Gray;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            btnSubscribe.Enabled = connected;
            btnUnsubscribe.Enabled = connected;
            btnPublish.Enabled = connected;
        }

        public void AppendMessage(string topic, string payload)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendMessage(topic, payload)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] 📨 {topic} = {payload}{Environment.NewLine}");
        }

        public void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendLog(message)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
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

        public string GetBrokerAddress() => txtBroker.Text.Trim();
        public int GetBrokerPort() => int.TryParse(txtPort.Text, out int p) ? p : 1883;
        public string GetUsername() => txtUsername.Text.Trim();
        public string GetPassword() => txtPassword.Text.Trim();
        public string GetSubscribeTopic() => txtSubscribeTopic.Text.Trim();
        public string GetPublishTopic() => txtPublishTopic.Text.Trim();
        public string GetPublishPayload() => txtPublishPayload.Text.Trim();
        public bool GetRetainFlag() => chkRetain.Checked;

        protected override void Dispose(bool disposing)
        {
            if (disposing) { /* 外部释放 */ }
            base.Dispose(disposing);
        }
    }
}