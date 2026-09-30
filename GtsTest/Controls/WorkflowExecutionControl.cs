using GtsTest.Commands;
using GtsTest.Core;
using GtsTest.Presenters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace GtsTest.Controls
{
    public partial class WorkflowExecutionControl : UserControl
    {
        private readonly DeviceManager _deviceManager;
        private List<WorkflowConfig> _workflows = new();
        private string _currentWorkflowName = "";

        // 控件
        private ComboBox cmbDevice;
        private ComboBox cmbWorkflow;
        private Button btnBind;
        private Button btnExecute;
        private Button btnStop;
        private Button btnReset;
        private Button btnClearProduction;
        private ListView listViewSteps;
        private Label lblStatus;
        private Label lblProgress;
        private Label lblCurrentStep;
        private Label lblStepTime;
        private TextBox txtExecutionLog;

        // 事件
        public event EventHandler<string> ExecuteClicked;
        public event EventHandler StopClicked;
        public event EventHandler ResetClicked;
        public event EventHandler ProductionResetClicked;
        public event EventHandler<string> DeviceSelected;
        public event EventHandler BindClicked;

        public WorkflowExecutionControl()
        {
            InitializeComponent();
        }

        public WorkflowExecutionControl(DeviceManager deviceManager) : this()
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.Padding = new Padding(10);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // ---- 第一行：选择和按钮（9列） ----
            var controlPanel = new TableLayoutPanel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.ColumnCount = 9;           // ★★★ 改为9列 ★★★
            controlPanel.RowCount = 1;
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 设备
            controlPanel.Controls.Add(new Label { Text = "设备:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            cmbDevice = new ComboBox();
            cmbDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDevice.SelectedIndexChanged += (s, e) =>
            {
                var selected = cmbDevice.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selected))
                    DeviceSelected?.Invoke(this, selected);
            };
            controlPanel.Controls.Add(cmbDevice, 1, 0);

            // 工作流
            controlPanel.Controls.Add(new Label { Text = "工作流:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            cmbWorkflow = new ComboBox();
            cmbWorkflow.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWorkflow.SelectedIndexChanged += (s, e) =>
            {
                _currentWorkflowName = cmbWorkflow.SelectedItem?.ToString() ?? "";
                LoadWorkflowPreview();
            };
            controlPanel.Controls.Add(cmbWorkflow, 3, 0);

            // 绑定按钮 (列 4)
            btnBind = new Button();
            btnBind.Text = "🔗 绑定";
            btnBind.BackColor = Color.LightBlue;
            btnBind.FlatStyle = FlatStyle.Flat;
            btnBind.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBind.Click += (s, e) => BindClicked?.Invoke(this, EventArgs.Empty);
            controlPanel.Controls.Add(btnBind, 4, 0);

            // 执行按钮 (列 5)
            btnExecute = new Button();
            btnExecute.Text = "▶ 执行";
            btnExecute.BackColor = Color.LightGreen;
            btnExecute.FlatStyle = FlatStyle.Flat;
            btnExecute.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExecute.Click += (s, e) => ExecuteClicked?.Invoke(this, _currentWorkflowName);
            controlPanel.Controls.Add(btnExecute, 5, 0);

            // 停止按钮 (列 6)
            btnStop = new Button();
            btnStop.Text = "⏹ 停止";
            btnStop.BackColor = Color.LightCoral;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStop.Click += (s, e) => StopClicked?.Invoke(this, EventArgs.Empty);
            controlPanel.Controls.Add(btnStop, 6, 0);

            // 复位按钮 (列 7)
            btnReset = new Button();
            btnReset.Text = "🔄 复位";
            btnReset.BackColor = Color.Gold;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnReset.Click += (s, e) => ResetClicked?.Invoke(this, EventArgs.Empty);
            controlPanel.Controls.Add(btnReset, 7, 0);

            // 产量清零按钮 (列 8)
            btnClearProduction = new Button();
            btnClearProduction.Text = "📊 产量清零";
            btnClearProduction.BackColor = Color.Orange;
            btnClearProduction.FlatStyle = FlatStyle.Flat;
            btnClearProduction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnClearProduction.Click += (s, e) => ProductionResetClicked?.Invoke(this, EventArgs.Empty);
            controlPanel.Controls.Add(btnClearProduction, 8, 0);

            layout.Controls.Add(controlPanel, 0, 0);
            layout.SetColumnSpan(controlPanel, 2);

            // ---- 第二行：工作流详情 + 状态 ----
            var detailPanel = new TableLayoutPanel();
            detailPanel.Dock = DockStyle.Fill;
            detailPanel.ColumnCount = 2;
            detailPanel.RowCount = 1;
            detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            detailPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            listViewSteps = new ListView();
            listViewSteps.Dock = DockStyle.Fill;
            listViewSteps.View = View.Details;
            listViewSteps.FullRowSelect = true;
            listViewSteps.Columns.Add(new ColumnHeader { Text = "步骤", Width = 60 });
            listViewSteps.Columns.Add(new ColumnHeader { Text = "命令", Width = 120 });
            listViewSteps.Columns.Add(new ColumnHeader { Text = "状态", Width = 80 });
            listViewSteps.Columns.Add(new ColumnHeader { Text = "参数", Width = 250 });
            detailPanel.Controls.Add(listViewSteps, 0, 0);

            var statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.Padding = new Padding(10);
            statusPanel.BackColor = Color.FromArgb(248, 248, 250);

            var statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.ColumnCount = 1;
            statusLayout.RowCount = 4;

            lblStatus = new Label();
            lblStatus.Text = "🟢 空闲";
            lblStatus.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Green;
            statusLayout.Controls.Add(lblStatus, 0, 0);

            lblCurrentStep = new Label();
            lblCurrentStep.Text = "当前步骤: --";
            lblCurrentStep.Font = new Font("Segoe UI", 10F);
            statusLayout.Controls.Add(lblCurrentStep, 0, 1);

            lblProgress = new Label();
            lblProgress.Text = "进度: 0%";
            lblProgress.Font = new Font("Segoe UI", 10F);
            statusLayout.Controls.Add(lblProgress, 0, 2);

            lblStepTime = new Label();
            lblStepTime.Text = "耗时: 0.0s";
            lblStepTime.Font = new Font("Segoe UI", 10F);
            lblStepTime.ForeColor = Color.Gray;
            statusLayout.Controls.Add(lblStepTime, 0, 3);

            statusPanel.Controls.Add(statusLayout);
            detailPanel.Controls.Add(statusPanel, 1, 0);

            layout.Controls.Add(detailPanel, 0, 1);
            layout.SetColumnSpan(detailPanel, 2);

            // ---- 第三行：执行日志 ----
            txtExecutionLog = new TextBox();
            txtExecutionLog.Dock = DockStyle.Fill;
            txtExecutionLog.Multiline = true;
            txtExecutionLog.ReadOnly = true;
            txtExecutionLog.ScrollBars = ScrollBars.Vertical;
            txtExecutionLog.BackColor = Color.Black;
            txtExecutionLog.ForeColor = Color.Lime;
            txtExecutionLog.Font = new Font("Consolas", 9F);
            layout.Controls.Add(txtExecutionLog, 0, 2);
            layout.SetColumnSpan(txtExecutionLog, 2);

            this.Controls.Add(layout);
            this.ResumeLayout(false);
        }

        // ================================================================
        //  公共方法
        // ================================================================

        public void SetDeviceList(IEnumerable<DeviceListItem> devices)
        {
            cmbDevice.Items.Clear();
            if (devices == null || !devices.Any())
            {
                cmbDevice.Items.Add("无设备");
                cmbDevice.SelectedIndex = 0;
                return;
            }

            foreach (var d in devices)
            {
                if (!string.IsNullOrEmpty(d.Name))
                    cmbDevice.Items.Add(d.Name);
                else
                    cmbDevice.Items.Add(d.DeviceId);
            }

            if (cmbDevice.Items.Count > 0)
                cmbDevice.SelectedIndex = 0;
        }

        public void SetWorkflowList(IEnumerable<string> workflowNames)
        {
            cmbWorkflow.Items.Clear();
            if (workflowNames == null || !workflowNames.Any())
            {
                cmbWorkflow.Items.Add("Default");
                cmbWorkflow.SelectedIndex = 0;
                return;
            }

            foreach (var name in workflowNames)
                cmbWorkflow.Items.Add(name);

            if (cmbWorkflow.Items.Count > 0)
                cmbWorkflow.SelectedIndex = 0;
        }

        public void SetPermissions(bool isLoggedIn, bool canEdit)
        {
            btnExecute.Enabled = isLoggedIn;
            btnStop.Enabled = isLoggedIn;
            btnReset.Enabled = isLoggedIn;
            btnClearProduction.Enabled = isLoggedIn && canEdit;
            btnBind.Enabled = isLoggedIn && canEdit;   // 绑定需要编辑权限
        }

        public void LoadWorkflowPreview()
        {
            if (string.IsNullOrEmpty(_currentWorkflowName))
            {
                listViewSteps.Items.Clear();
                return;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows", _currentWorkflowName + ".json");
            if (!File.Exists(path))
            {
                listViewSteps.Items.Clear();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<WorkflowConfig>(json);
                if (config == null) return;

                listViewSteps.Items.Clear();
                int idx = 1;
                foreach (var cmd in config.Commands)
                {
                    var item = new ListViewItem(idx.ToString());
                    item.SubItems.Add(cmd.Type);
                    item.SubItems.Add("⬜ 待执行");
                    string param = cmd.Type switch
                    {
                        "Home" => $"轴{cmd.Axis} 位置={cmd.HomePos}",
                        "MoveAbs" => $"轴{cmd.Axis} 目标={cmd.TargetPos} 速度={cmd.Vel}",
                        "Delay" => $"{cmd.DelayMs}ms",
                        "WaitIO" => $"IO{cmd.IoIndex} = {cmd.ExpectValue}",
                        "TriggerVision" => $"视觉 {cmd.VisionServerIp}:{cmd.VisionServerPort}",
                        "WriteSignal" => $"设备:{cmd.TargetDevice} 线圈{cmd.SignalAddress}={cmd.SignalValue}",
                        "WaitSignal" => $"等待设备:{cmd.TargetDevice} 线圈{cmd.SignalAddress}={cmd.ExpectValue}",
                        _ => ""
                    };
                    item.SubItems.Add(param);
                    listViewSteps.Items.Add(item);
                    idx++;
                }
            }
            catch { }
        }

        public string GetSelectedWorkflowName()
        {
            return cmbWorkflow.SelectedItem?.ToString() ?? "";
        }

        public string GetSelectedDeviceName()
        {
            return cmbDevice.SelectedItem?.ToString() ?? "";
        }

        public void UpdateExecutionStatus(string status, string step, int progress, string time)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateExecutionStatus(status, step, progress, time)));
                return;
            }

            lblStatus.Text = status;
            lblCurrentStep.Text = $"当前步骤: {step}";
            lblProgress.Text = $"进度: {progress}%";
            lblStepTime.Text = $"耗时: {time}";

            if (listViewSteps.Items.Count > 0)
            {
                int stepIndex = 0;
                if (int.TryParse(step.Replace("步骤", ""), out int idx))
                    stepIndex = idx - 1;

                for (int i = 0; i < listViewSteps.Items.Count; i++)
                {
                    if (i < stepIndex)
                        listViewSteps.Items[i].SubItems[2].Text = "✅ 完成";
                    else if (i == stepIndex)
                        listViewSteps.Items[i].SubItems[2].Text = "⏳ 执行中...";
                    else
                        listViewSteps.Items[i].SubItems[2].Text = "⬜ 待执行";
                }
            }
        }

        public void AppendLog(string message)
        {
            if (txtExecutionLog.InvokeRequired)
            {
                txtExecutionLog.BeginInvoke(new Action(() => AppendLog(message)));
                return;
            }
            txtExecutionLog.AppendText(message + Environment.NewLine);
            if (txtExecutionLog.Lines.Length > 200)
            {
                var lines = txtExecutionLog.Lines;
                txtExecutionLog.Lines = lines[50..];
            }
            txtExecutionLog.ScrollToCaret();
        }
    }
}