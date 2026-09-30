using GtsTest.Commands;
using GtsTest.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GtsTest.Controls
{
    public partial class WorkflowControl : UserControl
    {
        private List<CommandConfig> _steps = new List<CommandConfig>();
        private string _workflowName = "";
        private readonly GtsModel _model;
        private readonly DeviceManager _deviceManager;

        // 控件声明
        private TableLayoutPanel tableLayout;
        private Panel toolPanel, panelEdit;
        private ComboBox cmbWorkflow;
        private Button btnNew, btnSave, btnRun, btnStop;
        private Button btnAdd, btnEdit, btnDelete, btnMoveUp, btnMoveDown;
        private ListView listViewSteps;
        private ColumnHeader colStep, colType, colParams;

        public WorkflowControl(GtsModel model, DeviceManager deviceManager)
        {
            _model = model;
            _deviceManager = deviceManager;
            InitializeComponent();
            RefreshWorkflowDropdown(); // 初始填充下拉列表
            LoadWorkflow();            // 默认加载第一个工作流
        }

        private void InitializeComponent()
        {
            this.tableLayout = new TableLayoutPanel();
            this.toolPanel = new Panel();
            this.cmbWorkflow = new ComboBox();
            this.btnNew = new Button();
            this.btnSave = new Button();
            this.btnRun = new Button();
            this.btnStop = new Button();
            this.listViewSteps = new ListView();
            this.colStep = new ColumnHeader();
            this.colType = new ColumnHeader();
            this.colParams = new ColumnHeader();
            this.panelEdit = new Panel();
            this.btnAdd = new Button();
            this.btnEdit = new Button();
            this.btnDelete = new Button();
            this.btnMoveUp = new Button();
            this.btnMoveDown = new Button();

            this.SuspendLayout();

            // tableLayout
            this.tableLayout.Dock = DockStyle.Fill;
            this.tableLayout.ColumnCount = 1;
            this.tableLayout.RowCount = 3;
            this.tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this.tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // 工具栏
            this.toolPanel.Dock = DockStyle.Fill;
            this.cmbWorkflow.Location = new Point(10, 8);
            this.cmbWorkflow.Size = new Size(150, 25);
            this.cmbWorkflow.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbWorkflow.SelectedIndexChanged += CmbWorkflow_SelectedIndexChanged;

            this.btnNew.Location = new Point(170, 6);
            this.btnNew.Size = new Size(60, 28);
            this.btnNew.Text = "新建";
            this.btnNew.Click += BtnNew_Click;

            this.btnSave.Location = new Point(240, 6);
            this.btnSave.Size = new Size(60, 28);
            this.btnSave.Text = "保存";
            this.btnSave.Click += BtnSave_Click;

            this.btnRun.Location = new Point(310, 6);
            this.btnRun.Size = new Size(60, 28);
            this.btnRun.Text = "▶ 运行";
            this.btnRun.BackColor = Color.LightGreen;
            this.btnRun.Click += BtnRun_Click;

            this.btnStop.Location = new Point(380, 6);
            this.btnStop.Size = new Size(60, 28);
            this.btnStop.Text = "⏹ 停止";
            this.btnStop.BackColor = Color.LightCoral;
            this.btnStop.Click += BtnStop_Click;

            this.toolPanel.Controls.Add(this.cmbWorkflow);
            this.toolPanel.Controls.Add(this.btnNew);
            this.toolPanel.Controls.Add(this.btnSave);
            this.toolPanel.Controls.Add(this.btnRun);
            this.toolPanel.Controls.Add(this.btnStop);

            // 步骤列表
            this.listViewSteps.Dock = DockStyle.Fill;
            this.listViewSteps.View = View.Details;
            this.listViewSteps.FullRowSelect = true;
            this.listViewSteps.Columns.Add(this.colStep);
            this.listViewSteps.Columns.Add(this.colType);
            this.listViewSteps.Columns.Add(this.colParams);
            this.colStep.Text = "步骤";
            this.colStep.Width = 60;
            this.colType.Text = "类型";
            this.colType.Width = 120;
            this.colParams.Text = "参数";
            this.colParams.Width = 400;
            this.listViewSteps.SelectedIndexChanged += ListViewSteps_SelectedIndexChanged;

            // 编辑面板
            this.panelEdit.Dock = DockStyle.Fill;
            this.btnAdd.Location = new Point(10, 6);
            this.btnAdd.Size = new Size(60, 28);
            this.btnAdd.Text = "➕ 添加";
            this.btnAdd.Click += BtnAdd_Click;

            this.btnEdit.Location = new Point(80, 6);
            this.btnEdit.Size = new Size(60, 28);
            this.btnEdit.Text = "✏️ 编辑";
            this.btnEdit.Enabled = false;
            this.btnEdit.Click += BtnEdit_Click;

            this.btnDelete.Location = new Point(150, 6);
            this.btnDelete.Size = new Size(60, 28);
            this.btnDelete.Text = "🗑 删除";
            this.btnDelete.Enabled = false;
            this.btnDelete.Click += BtnDelete_Click;

            this.btnMoveUp.Location = new Point(220, 6);
            this.btnMoveUp.Size = new Size(60, 28);
            this.btnMoveUp.Text = "⬆ 上移";
            this.btnMoveUp.Enabled = false;
            this.btnMoveUp.Click += BtnMoveUp_Click;

            this.btnMoveDown.Location = new Point(290, 6);
            this.btnMoveDown.Size = new Size(60, 28);
            this.btnMoveDown.Text = "⬇ 下移";
            this.btnMoveDown.Enabled = false;
            this.btnMoveDown.Click += BtnMoveDown_Click;

            this.panelEdit.Controls.Add(this.btnAdd);
            this.panelEdit.Controls.Add(this.btnEdit);
            this.panelEdit.Controls.Add(this.btnDelete);
            this.panelEdit.Controls.Add(this.btnMoveUp);
            this.panelEdit.Controls.Add(this.btnMoveDown);

            this.tableLayout.Controls.Add(this.toolPanel, 0, 0);
            this.tableLayout.Controls.Add(this.listViewSteps, 0, 1);
            this.tableLayout.Controls.Add(this.panelEdit, 0, 2);

            this.Controls.Add(this.tableLayout);
            this.ResumeLayout(false);
        }

        // ---- 下拉选择工作流 ----
        private void CmbWorkflow_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = cmbWorkflow.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected) || selected == "(无工作流)") return;
            LoadWorkflowByName(selected);
        }

        private void LoadWorkflowByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _workflowName = name;
            LoadWorkflow();
        }

        private void ListViewSteps_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewSteps == null) return;
            bool hasSelected = this.listViewSteps.SelectedItems.Count > 0;
            this.btnEdit.Enabled = hasSelected;
            this.btnDelete.Enabled = hasSelected;
            this.btnMoveUp.Enabled = hasSelected && this.listViewSteps.SelectedIndices[0] > 0;
            this.btnMoveDown.Enabled = hasSelected && this.listViewSteps.SelectedIndices[0] < this.listViewSteps.Items.Count - 1;
        }

        private void LoadWorkflow()
        {
            if (listViewSteps == null) return;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows", _workflowName + ".json");
            if (!string.IsNullOrEmpty(_workflowName) && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var config = System.Text.Json.JsonSerializer.Deserialize<WorkflowConfig>(json);
                    if (config != null)
                    {
                        _steps = config.Commands;
                        RefreshStepList();
                        // 确保下拉框选中当前工作流
                        if (cmbWorkflow.SelectedItem as string != _workflowName)
                            cmbWorkflow.SelectedItem = _workflowName;
                        return;
                    }
                }
                catch { /* 忽略，回退到默认 */ }
            }

            // 默认示例（仅当 _steps 为空时加载）
            if (_steps.Count == 0)
            {
                _steps = new List<CommandConfig>
                {
                    new CommandConfig { Type = "Home", Axis = 1, HomePos = 0 },
                    new CommandConfig { Type = "MoveAbs", Axis = 1, TargetPos = 10000, Vel = 20, Acc = 10 },
                    new CommandConfig { Type = "Delay", DelayMs = 500 },
                    new CommandConfig { Type = "WaitIO", IoIndex = 0, ExpectValue = true },
                    new CommandConfig
                    {
                        Type = "TriggerVision",
                        VisionServerIp = "127.0.0.1",
                        VisionServerPort = 503,
                        TriggerCoilAddress = 100,
                        BusyCoilAddress = 101,
                        ResultCoilAddress = 102,
                        ResultCodeRegister = 1000,
                        VisionTimeoutMs = 10000
                    }
                };
            }
            RefreshStepList();
        }

        private void RefreshStepList()
        {
            if (listViewSteps == null) return;

            this.listViewSteps.Items.Clear();
            int idx = 1;
            foreach (var step in _steps)
            {
                var item = new ListViewItem(idx.ToString());
                item.SubItems.Add(step.Type);
                string param = step.Type switch
                {
                    "Home" => $"轴{step.Axis} 位置={step.HomePos}",
                    "MoveAbs" => $"轴{step.Axis} 目标={step.TargetPos} 速度={step.Vel} 加速度={step.Acc}",
                    "Delay" => $"{step.DelayMs}ms",
                    "WaitIO" => $"IO{step.IoIndex} = {step.ExpectValue}",
                    "WriteSignal" => $"设备:{step.TargetDevice} 线圈{step.SignalAddress} = {step.SignalValue}",
                    "WaitSignal" => $"设备:{step.TargetDevice} 等待线圈{step.SignalAddress} = {step.ExpectValue}",
                    "TriggerVision" => $"视觉服务器 {step.VisionServerIp}:{step.VisionServerPort}",
                    _ => "未知参数"
                };
                item.SubItems.Add(param);
                this.listViewSteps.Items.Add(item);
                idx++;
            }
            this.btnEdit.Enabled = false;
            this.btnDelete.Enabled = false;
            this.btnMoveUp.Enabled = false;
            this.btnMoveDown.Enabled = false;
        }

        // ---- 新建 ----
        private void BtnNew_Click(object sender, EventArgs e)
        {
            _steps.Clear();
            _workflowName = "";
            RefreshStepList();
            cmbWorkflow.SelectedIndex = -1;
        }

        // ---- 保存 ----
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string fileName = _workflowName;
            if (string.IsNullOrEmpty(fileName))
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "另存为工作流";
                    dialog.Filter = "JSON文件|*.json";
                    dialog.DefaultExt = "json";
                    dialog.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                var result = MessageBox.Show($"是否将当前工作流另存为新文件？\n（选择“是”另存为新文件，选择“否”覆盖现有文件）",
                    "保存选项", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                    return;
                if (result == DialogResult.Yes)
                {
                    using (var dialog = new SaveFileDialog())
                    {
                        dialog.Title = "另存为工作流";
                        dialog.Filter = "JSON文件|*.json";
                        dialog.DefaultExt = "json";
                        dialog.FileName = fileName + ".json";
                        dialog.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            var config = new WorkflowConfig { Name = fileName, Commands = _steps };
            string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName + ".json");
            File.WriteAllText(path, json);

            _workflowName = fileName;
            RefreshWorkflowDropdown();
            MessageBox.Show($"工作流已保存到 {path}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- 刷新下拉框 ----
        private void RefreshWorkflowDropdown()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var files = Directory.GetFiles(dir, "*.json");
            var names = new List<string>();
            foreach (var f in files)
                names.Add(Path.GetFileNameWithoutExtension(f));

            string previousSelected = cmbWorkflow.SelectedItem as string;
            cmbWorkflow.Items.Clear();
            if (names.Count > 0)
            {
                cmbWorkflow.Items.AddRange(names.ToArray());
                if (!string.IsNullOrEmpty(_workflowName) && cmbWorkflow.Items.Contains(_workflowName))
                    cmbWorkflow.SelectedItem = _workflowName;
                else if (!string.IsNullOrEmpty(previousSelected) && cmbWorkflow.Items.Contains(previousSelected))
                    cmbWorkflow.SelectedItem = previousSelected;
                else
                    cmbWorkflow.SelectedIndex = 0;
            }
            else
            {
                cmbWorkflow.Items.Add("(无工作流)");
                cmbWorkflow.SelectedIndex = 0;
            }
        }

        private void BtnRun_Click(object sender, EventArgs e) { /* 由主窗体处理 */ }
        private void BtnStop_Click(object sender, EventArgs e) { /* 由主窗体处理 */ }

        // ---- 添加/编辑/删除/移动 ----
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommandConfigDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _steps.Add(dialog.Config);
                    RefreshStepList();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (this.listViewSteps.SelectedIndices.Count == 0) return;
            int idx = this.listViewSteps.SelectedIndices[0];
            var config = _steps[idx];
            using (var dialog = new CommandConfigDialog(config))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _steps[idx] = dialog.Config;
                    RefreshStepList();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (this.listViewSteps.SelectedIndices.Count == 0) return;
            int idx = this.listViewSteps.SelectedIndices[0];
            if (MessageBox.Show($"确定删除步骤 {idx + 1} 吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _steps.RemoveAt(idx);
                RefreshStepList();
            }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (this.listViewSteps.SelectedIndices.Count == 0) return;
            int idx = this.listViewSteps.SelectedIndices[0];
            if (idx > 0)
            {
                var temp = _steps[idx - 1];
                _steps[idx - 1] = _steps[idx];
                _steps[idx] = temp;
                RefreshStepList();
                this.listViewSteps.Items[idx - 1].Selected = true;
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (this.listViewSteps.SelectedIndices.Count == 0) return;
            int idx = this.listViewSteps.SelectedIndices[0];
            if (idx < _steps.Count - 1)
            {
                var temp = _steps[idx + 1];
                _steps[idx + 1] = _steps[idx];
                _steps[idx] = temp;
                RefreshStepList();
                this.listViewSteps.Items[idx + 1].Selected = true;
            }
        }

        // ================================================================
        // 内部类：命令配置对话框（完整版）
        // ================================================================
        public class CommandConfigDialog : Form
        {
            public CommandConfig Config { get; private set; }

            private ComboBox cmbType;
            private NumericUpDown numAxis;
            private NumericUpDown numTarget;
            private NumericUpDown numHome;
            private NumericUpDown numDelay;
            private NumericUpDown numVel;
            private NumericUpDown numAcc;
            private Panel visionPanel;
            private TextBox txtVisionIp;
            private NumericUpDown numVisionPort;
            private NumericUpDown numTriggerCoil;
            private NumericUpDown numBusyCoil;
            private NumericUpDown numResultCoil;
            private NumericUpDown numResultCode;
            private NumericUpDown numTimeoutMs;
            private Button btnOK;
            private Button btnCancel;

            public CommandConfigDialog() : this(null) { }

            public CommandConfigDialog(CommandConfig existing)
            {
                Config = existing ?? new CommandConfig { Type = "Home", Axis = 1 };
                InitializeComponent();
                LoadConfig();
            }

            private void InitializeComponent()
            {
                this.Text = "编辑指令";
                this.Size = new Size(480, 420);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;

                TableLayoutPanel mainLayout = new TableLayoutPanel();
                mainLayout.Dock = DockStyle.Fill;
                mainLayout.ColumnCount = 2;
                mainLayout.RowCount = 10;
                mainLayout.Padding = new Padding(10);
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85F));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                // 类型
                mainLayout.Controls.Add(new Label { Text = "类型:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
                cmbType = new ComboBox();
                cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbType.Items.AddRange(new object[] { "Home", "MoveAbs", "Delay", "WaitIO", "WriteSignal", "WaitSignal", "TriggerVision" });
                cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;
                mainLayout.Controls.Add(cmbType, 1, 0);

                // 轴号
                mainLayout.Controls.Add(new Label { Text = "轴号:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
                numAxis = new NumericUpDown { Minimum = 1, Maximum = 8, Value = 1, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numAxis, 1, 1);

                // 目标位置
                mainLayout.Controls.Add(new Label { Text = "目标位置:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
                numTarget = new NumericUpDown { Maximum = 100000, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numTarget, 1, 2);

                // 回零位置
                mainLayout.Controls.Add(new Label { Text = "回零位置:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
                numHome = new NumericUpDown { Maximum = 100000, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numHome, 1, 3);

                // 延时
                mainLayout.Controls.Add(new Label { Text = "延时(ms):", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
                numDelay = new NumericUpDown { Maximum = 60000, Value = 500, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numDelay, 1, 4);

                // 速度
                mainLayout.Controls.Add(new Label { Text = "速度:", TextAlign = ContentAlignment.MiddleRight }, 0, 5);
                numVel = new NumericUpDown { DecimalPlaces = 1, Increment = 0.5m, Maximum = 100, Value = 10, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numVel, 1, 5);

                // 加速度
                mainLayout.Controls.Add(new Label { Text = "加速度:", TextAlign = ContentAlignment.MiddleRight }, 0, 6);
                numAcc = new NumericUpDown { DecimalPlaces = 1, Increment = 0.5m, Maximum = 50, Value = 5, Dock = DockStyle.Fill };
                mainLayout.Controls.Add(numAcc, 1, 6);

                // 视觉配置面板
                visionPanel = new Panel();
                visionPanel.Dock = DockStyle.Fill;
                visionPanel.Visible = false;
                visionPanel.Padding = new Padding(0, 5, 0, 5);
                visionPanel.BackColor = Color.FromArgb(248, 248, 255);
                visionPanel.BorderStyle = BorderStyle.FixedSingle;

                TableLayoutPanel visionLayout = new TableLayoutPanel();
                visionLayout.Dock = DockStyle.Fill;
                visionLayout.ColumnCount = 2;
                visionLayout.RowCount = 8;
                visionLayout.Padding = new Padding(8);
                visionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
                visionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                Label lblVisionTitle = new Label
                {
                    Text = "📷 视觉服务器配置",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.DarkBlue,
                    Dock = DockStyle.Fill
                };
                visionLayout.Controls.Add(lblVisionTitle, 0, 0);
                visionLayout.SetColumnSpan(lblVisionTitle, 2);

                visionLayout.Controls.Add(new Label { Text = "服务器 IP:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
                txtVisionIp = new TextBox { Text = "127.0.0.1", Dock = DockStyle.Fill };
                visionLayout.Controls.Add(txtVisionIp, 1, 1);

                visionLayout.Controls.Add(new Label { Text = "端口:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
                numVisionPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 503, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numVisionPort, 1, 2);

                visionLayout.Controls.Add(new Label { Text = "触发线圈:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
                numTriggerCoil = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = 100, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numTriggerCoil, 1, 3);

                visionLayout.Controls.Add(new Label { Text = "忙状态线圈:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
                numBusyCoil = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = 101, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numBusyCoil, 1, 4);

                visionLayout.Controls.Add(new Label { Text = "结果线圈:", TextAlign = ContentAlignment.MiddleRight }, 0, 5);
                numResultCoil = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = 102, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numResultCoil, 1, 5);

                visionLayout.Controls.Add(new Label { Text = "结果码寄存器:", TextAlign = ContentAlignment.MiddleRight }, 0, 6);
                numResultCode = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = 1000, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numResultCode, 1, 6);

                visionLayout.Controls.Add(new Label { Text = "超时(ms):", TextAlign = ContentAlignment.MiddleRight }, 0, 7);
                numTimeoutMs = new NumericUpDown { Minimum = 1000, Maximum = 60000, Value = 10000, Increment = 1000, Dock = DockStyle.Fill };
                visionLayout.Controls.Add(numTimeoutMs, 1, 7);

                visionPanel.Controls.Add(visionLayout);
                mainLayout.Controls.Add(visionPanel, 0, 7);
                mainLayout.SetColumnSpan(visionPanel, 2);

                // 按钮
                FlowLayoutPanel btnPanel = new FlowLayoutPanel();
                btnPanel.Dock = DockStyle.Fill;
                btnPanel.FlowDirection = FlowDirection.RightToLeft;
                btnPanel.Padding = new Padding(0, 10, 0, 0);

                btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Size = new Size(75, 30) };
                btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(75, 30) };
                btnPanel.Controls.Add(btnOK);
                btnPanel.Controls.Add(btnCancel);

                mainLayout.Controls.Add(btnPanel, 0, 8);
                mainLayout.SetColumnSpan(btnPanel, 2);

                for (int i = 0; i < 7; i++)
                    mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

                this.Controls.Add(mainLayout);
                this.AcceptButton = btnOK;
                this.CancelButton = btnCancel;
            }

            private void CmbType_SelectedIndexChanged(object sender, EventArgs e)
            {
                string selectedType = cmbType.SelectedItem?.ToString() ?? "";
                bool showVision = selectedType == "TriggerVision";
                visionPanel.Visible = showVision;
                this.Height = showVision ? 490 : 420;
            }

            private void LoadConfig()
            {
                cmbType.SelectedItem = Config.Type;
                numAxis.Value = Config.Axis;
                numTarget.Value = Config.TargetPos;
                numHome.Value = Config.HomePos;
                numDelay.Value = Config.DelayMs;
                numVel.Value = (decimal)Config.Vel;
                numAcc.Value = (decimal)Config.Acc;

                txtVisionIp.Text = Config.VisionServerIp ?? "127.0.0.1";
                numVisionPort.Value = Config.VisionServerPort > 0 ? Config.VisionServerPort : 503;
                numTriggerCoil.Value = Config.TriggerCoilAddress > 0 ? Config.TriggerCoilAddress : 100;
                numBusyCoil.Value = Config.BusyCoilAddress > 0 ? Config.BusyCoilAddress : 101;
                numResultCoil.Value = Config.ResultCoilAddress > 0 ? Config.ResultCoilAddress : 102;
                numResultCode.Value = Config.ResultCodeRegister > 0 ? Config.ResultCodeRegister : 1000;
                numTimeoutMs.Value = Config.VisionTimeoutMs > 0 ? Config.VisionTimeoutMs : 10000;

                bool showVision = Config.Type == "TriggerVision";
                visionPanel.Visible = showVision;
                this.Height = showVision ? 490 : 420;
            }

            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                if (this.DialogResult == DialogResult.OK)
                {
                    Config.Type = cmbType.SelectedItem?.ToString() ?? "Home";
                    Config.Axis = (int)numAxis.Value;
                    Config.TargetPos = (int)numTarget.Value;
                    Config.HomePos = (int)numHome.Value;
                    Config.DelayMs = (int)numDelay.Value;
                    Config.Vel = (double)numVel.Value;
                    Config.Acc = (double)numAcc.Value;

                    Config.VisionServerIp = txtVisionIp.Text.Trim();
                    Config.VisionServerPort = (int)numVisionPort.Value;
                    Config.TriggerCoilAddress = (int)numTriggerCoil.Value;
                    Config.BusyCoilAddress = (int)numBusyCoil.Value;
                    Config.ResultCoilAddress = (int)numResultCoil.Value;
                    Config.ResultCodeRegister = (int)numResultCode.Value;
                    Config.VisionTimeoutMs = (int)numTimeoutMs.Value;
                }
                base.OnFormClosing(e);
            }
        }
    }
}