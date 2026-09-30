using GtsTest.Core;
using GtsTest.Models;
using GtsTest.Services.Alarm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GtsTest.Controls
{
    public partial class ProductionOverviewControl : UserControl
    {
        private DeviceManager _deviceManager;
        private IAlarmManager _alarmManager;
        private System.Windows.Forms.Timer _refreshTimer; // 显式使用 Forms.Timer
        private string _selectedDeviceId = "";

        // 统计卡片控件（显式声明）
        private Label lblTotalProductionValue;
        private Label lblYieldRateValue;
        private Label lblOnlineCountValue;
        private Label lblAlarmCountValue;

        // 其他控件
        private Panel panelStats;
        private Panel panelCharts;
        private Panel panelCards;
        private Panel panelDetail;
        private SplitContainer chartSplitter;
        private Chart chartTrend;
        private Chart chartPie;
        private ListView listViewDevices;
        private TableLayoutPanel detailLayout;
        private Label lblDetailDeviceName;
        private Label lblDetailAxis;
        private Label lblDetailStatus;
        private Label lblDetailStep;
        private Label lblDetailPos;
        private Label lblDetailVel;
        private Label lblDetailServo;
        private Label lblDetailModbus;

        public event EventHandler<string> DeviceSelected;

        public ProductionOverviewControl()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                listViewDevices.Items.Clear();
                listViewDevices.Items.Add(new ListViewItem("设计视图 - 运行时显示数据") { ForeColor = Color.Gray });
            }
        }

        public void SetDeviceManager(DeviceManager deviceManager, IAlarmManager alarmManager)
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _alarmManager = alarmManager ?? throw new ArgumentNullException(nameof(alarmManager));

            if (_refreshTimer == null)
            {
                _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _refreshTimer.Tick += RefreshTimer_Tick;
                _refreshTimer.Start();
            }
            RefreshOverview();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = Color.FromArgb(240, 240, 245);

            // ---- 统计面板（高度增加到85px以确保内容可见） ----
            panelStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(4)
            };

            // 使用 FlowLayoutPanel 确保每个卡片等宽且不重叠
            var flowStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0)
            };

            // 创建四个统计卡片，每个宽度25%
            var card1 = CreateStatCard("📦 总产量", Color.FromArgb(0, 120, 215), out lblTotalProductionValue);
            var card2 = CreateStatCard("✅ 良品率", Color.FromArgb(0, 176, 80), out lblYieldRateValue);
            var card3 = CreateStatCard("🟢 在线设备", Color.FromArgb(255, 140, 0), out lblOnlineCountValue);
            var card4 = CreateStatCard("🔴 未处理报警", Color.Red, out lblAlarmCountValue);

            // 设置初始值
            lblTotalProductionValue.Text = "0";
            lblYieldRateValue.Text = "0%";
            lblOnlineCountValue.Text = "0/0";
            lblAlarmCountValue.Text = "0";

            // 添加到流式布局
            flowStats.Controls.Add(card1);
            flowStats.Controls.Add(card2);
            flowStats.Controls.Add(card3);
            flowStats.Controls.Add(card4);
            panelStats.Controls.Add(flowStats);

            // ---- 图表面板 ----
            panelCharts = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                BackColor = Color.White,
                Padding = new Padding(4)
            };
            chartSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 3,
                BackColor = Color.White,
                IsSplitterFixed = true
            };
            chartSplitter.SplitterDistance = (int)(Width * 0.70);

            chartTrend = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            chartTrend.ChartAreas.Add(new ChartArea("Main"));
            chartTrend.ChartAreas[0].AxisX.Title = "时间";
            chartTrend.ChartAreas[0].AxisY.Title = "产量";
            chartTrend.Series.Add(new Series("产量") { ChartType = SeriesChartType.Line, Color = Color.FromArgb(0, 120, 215), BorderWidth = 2 });
            chartTrend.Legends.Add(new Legend());
            chartSplitter.Panel1.Controls.Add(chartTrend);

            chartPie = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            chartPie.ChartAreas.Add(new ChartArea("Pie"));
            chartPie.Series.Add(new Series("设备状态") { ChartType = SeriesChartType.Pie });
            chartPie.Series[0].Points.AddXY("无设备", 1);
            chartPie.Series[0].Points[0].Color = Color.LightGray;
            chartPie.Legends.Add(new Legend());
            chartSplitter.Panel2.Controls.Add(chartPie);
            panelCharts.Controls.Add(chartSplitter);

            // ---- 设备列表 ----
            panelCards = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 248, 250),
                Padding = new Padding(4)
            };
            listViewDevices = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            listViewDevices.Columns.Add("设备名称", 100);
            listViewDevices.Columns.Add("状态", 60);
            listViewDevices.Columns.Add("产量", 80);
            listViewDevices.Columns.Add("当前步骤", 120);
            listViewDevices.SelectedIndexChanged += (s, e) =>
            {
                if (listViewDevices.SelectedItems.Count > 0)
                {
                    var id = listViewDevices.SelectedItems[0].Tag as string;
                    if (!string.IsNullOrEmpty(id))
                    {
                        _selectedDeviceId = id;
                        var dev = _deviceManager?.GetDevice(id);
                        if (dev != null)
                        {
                            UpdateDeviceDetail(dev);
                            DeviceSelected?.Invoke(this, id);
                        }
                    }
                }
            };
            panelCards.Controls.Add(listViewDevices);

            // ---- 详情面板 ----
            panelDetail = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(6)
            };
            detailLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1
            };
            var font = new Font("Segoe UI", 8.5F);
            string[] detailNames = { "设备: --", "轴号: --", "状态: --", "步骤: --", "位置: --", "速度: --", "使能: --", "Modbus: --" };
            var labels = new Label[8];
            for (int i = 0; i < 8; i++)
            {
                labels[i] = new Label
                {
                    Text = detailNames[i],
                    Font = font,
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill
                };
                detailLayout.Controls.Add(labels[i], i, 0);
                if (i < 7) detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 8F));
            }
            lblDetailDeviceName = labels[0];
            lblDetailAxis = labels[1];
            lblDetailStatus = labels[2];
            lblDetailStep = labels[3];
            lblDetailPos = labels[4];
            lblDetailVel = labels[5];
            lblDetailServo = labels[6];
            lblDetailModbus = labels[7];
            panelDetail.Controls.Add(detailLayout);

            // 添加顺序
            Controls.Add(panelCards);
            Controls.Add(panelDetail);
            Controls.Add(panelCharts);
            Controls.Add(panelStats);

            // 自适应分割距离
            Resize += (s, e) =>
            {
                if (chartSplitter != null && Width > 0)
                    chartSplitter.SplitterDistance = (int)(Width * 0.70);
            };

            ResumeLayout(false);
        }

        private Panel CreateStatCard(string title, Color color, out Label valueLabel)
        {
            // 使用固定大小的 Panel，避免被压缩
            var panel = new Panel
            {
                Width = 200,  // 固定宽度，FlowLayoutPanel 会自动排列
                Height = 65,   // 固定高度，确保标题和数值都能显示
                BackColor = Color.FromArgb(248, 248, 250),
                Padding = new Padding(4),
                Margin = new Padding(3)
            };

            // 内部用 TableLayoutPanel 分成两行
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RowStyles = { new RowStyle(SizeType.Percent, 40F), new RowStyle(SizeType.Percent, 60F) }
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(lblTitle, 0, 0);

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(valueLabel, 0, 1);

            panel.Controls.Add(layout);
            return panel;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (Visible && !DesignMode && _deviceManager != null)
                RefreshOverview();
        }

        public void RefreshOverview()
        {
            if (_deviceManager == null) return;

            var devices = _deviceManager.GetAllDevices();

            // 计算总产量
            int total = devices.Sum(d => d.Config.CurrentCount);

            // 更新总产量
            if (lblTotalProductionValue != null)
                lblTotalProductionValue.Text = total.ToString();

            // ★★★ 修改点：良品率基于真实产量计算 ★★★
            if (lblYieldRateValue != null)
            {
                if (total == 0)
                    lblYieldRateValue.Text = "0%";
                else
                {
                    // TODO: 后续可根据实际良品数计算，目前假设全部合格
                    // 例如：int qualified = devices.Sum(d => d.Config.QualifiedCount);
                    // lblYieldRateValue.Text = ((double)qualified / total * 100).ToString("F1") + "%";
                    lblYieldRateValue.Text = "100%";
                }
            }

            // 更新在线设备
            if (lblOnlineCountValue != null)
            {
                int online = devices.Count(d => d.IsOnline);
                lblOnlineCountValue.Text = devices.Count > 0 ? $"{online}/{devices.Count}" : "0/0";
            }

            // 更新报警数
            if (lblAlarmCountValue != null)
            {
                int alarmCount = _alarmManager?.GetActiveAlarms().Count() ?? 0;
                lblAlarmCountValue.Text = alarmCount.ToString();
                lblAlarmCountValue.ForeColor = alarmCount > 0 ? Color.Red : Color.Green;
            }

            // 更新设备列表
            listViewDevices.Items.Clear();
            if (devices.Count == 0)
                listViewDevices.Items.Add(new ListViewItem("暂无设备") { ForeColor = Color.Gray });
            else
            {
                foreach (var dev in devices)
                {
                    var item = new ListViewItem(dev.Config.Name);
                    item.SubItems.Add(dev.IsOnline ? "● 在线" : "○ 离线");
                    item.SubItems.Add($"{dev.Config.CurrentCount}/{dev.Config.TargetCount}");
                    item.SubItems.Add(dev.CurrentStep);
                    item.Tag = dev.Config.DeviceId;
                    item.ForeColor = dev.IsOnline ? Color.Green : Color.Red;
                    listViewDevices.Items.Add(item);
                }
            }

            // 更新图表
            UpdateCharts(devices);

            // 选中第一个设备
            if (listViewDevices.Items.Count > 0 && string.IsNullOrEmpty(_selectedDeviceId))
            {
                listViewDevices.Items[0].Selected = true;
                var first = devices.FirstOrDefault();
                if (first != null)
                {
                    _selectedDeviceId = first.Config.DeviceId;
                    UpdateDeviceDetail(first);
                }
            }
            else if (!string.IsNullOrEmpty(_selectedDeviceId))
            {
                var dev = _deviceManager.GetDevice(_selectedDeviceId);
                if (dev != null) UpdateDeviceDetail(dev);
            }
        }

        private void UpdateDeviceDetail(DeviceRuntime device)
        {
            lblDetailDeviceName.Text = $"设备: {device.Config.Name}";
            lblDetailAxis.Text = $"轴号: {device.Config.Axis}";

            if (device.IsRunning)
            {
                lblDetailStatus.Text = "状态: 🟡 运行中";
                lblDetailStatus.ForeColor = Color.Orange;
            }
            else if (device.IsOnline)
            {
                lblDetailStatus.Text = "状态: 🟢 空闲";
                lblDetailStatus.ForeColor = Color.Green;
            }
            else
            {
                lblDetailStatus.Text = "状态: 🔴 离线";
                lblDetailStatus.ForeColor = Color.Red;
            }

            lblDetailStep.Text = $"步骤: {device.CurrentStep}";

            if (device.IsOnline && _deviceManager?.Model != null)
            {
                uint clk;
                int status;
                double pos = 0, vel = 0;
                var model = _deviceManager.Model;
                model.GetAxisStatus(device.Config.Axis, out status, out clk);
                model.GetPrfPos(device.Config.Axis, out pos, out clk);
                model.GetPrfVel(device.Config.Axis, out vel, out clk);

                lblDetailPos.Text = $"位置: {pos:F1}";
                lblDetailPos.ForeColor = Color.Black;
                lblDetailVel.Text = $"速度: {vel:F1}";
                lblDetailVel.ForeColor = Color.Black;

                bool servoOn = (status & 0x200) != 0;
                lblDetailServo.Text = servoOn ? "使能: ✅ 已使能" : "使能: ❌ 未使能";
                lblDetailServo.ForeColor = servoOn ? Color.Green : Color.Red;
                lblDetailModbus.Text = "Modbus: 🟢 已连接";
                lblDetailModbus.ForeColor = Color.Green;
            }
            else
            {
                lblDetailPos.Text = "位置: --";
                lblDetailPos.ForeColor = Color.Black;
                lblDetailVel.Text = "速度: --";
                lblDetailVel.ForeColor = Color.Black;
                lblDetailServo.Text = "使能: ❌ 未使能";
                lblDetailServo.ForeColor = Color.Red;
                lblDetailModbus.Text = "Modbus: 🔴 未连接";
                lblDetailModbus.ForeColor = Color.Red;
            }
        }

        private void UpdateCharts(List<DeviceRuntime> devices)
        {
            try
            {
                if (devices.Count > 0)
                {
                    var total = devices.Sum(d => d.Config.CurrentCount);
                    chartTrend.Series[0].Points.AddY(total);
                    if (chartTrend.Series[0].Points.Count > 60)
                        chartTrend.Series[0].Points.RemoveAt(0);
                }
                else
                    chartTrend.Series[0].Points.Clear();

                chartPie.Series[0].Points.Clear();
                if (devices.Count > 0)
                {
                    int online = devices.Count(d => d.IsOnline);
                    int offline = devices.Count - online;
                    chartPie.Series[0].Points.AddXY("在线", online);
                    chartPie.Series[0].Points.AddXY("离线", offline);
                    chartPie.Series[0].Points[0].Color = Color.Green;
                    chartPie.Series[0].Points[1].Color = Color.Red;
                }
                else
                {
                    chartPie.Series[0].Points.AddXY("无设备", 1);
                    chartPie.Series[0].Points[0].Color = Color.LightGray;
                }
            }
            catch { }
        }

        public void SetSelectedDevice(string deviceId) => _selectedDeviceId = deviceId;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                chartTrend?.Dispose();
                chartPie?.Dispose();
                chartSplitter?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}