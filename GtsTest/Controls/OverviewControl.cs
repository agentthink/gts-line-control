using GtsTest.Core;
using GtsTest.Models;
using GtsTest.Services.Alarm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GtsTest.Controls
{
    public partial class OverviewControl : UserControl
    {
        private readonly DeviceManager _deviceManager;
        private readonly IAlarmManager _alarmManager;
        private System.Windows.Forms.Timer _refreshTimer;

        // UI 控件（不再依赖 .Designer.cs）
        private TableLayoutPanel tableLayoutMain;
        private FlowLayoutPanel flowDeviceCards;
        private Panel panelTrend;
        private PictureBox picTrend;

        // 趋势图数据
        private readonly List<int> _trendData = new List<int>();
        private readonly int _maxTrendPoints = 100;
        private readonly object _trendLock = new object();

        // ============================================================
        // 构造函数
        // ============================================================
        public OverviewControl()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                // 设计时显示占位
                this.flowDeviceCards.Controls.Clear();
                var placeholder = new Label
                {
                    Text = "设计视图 - 运行时显示设备卡片",
                    AutoSize = false,
                    Size = new Size(200, 50),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                this.flowDeviceCards.Controls.Add(placeholder);
            }
        }

        public OverviewControl(DeviceManager deviceManager, IAlarmManager alarmManager) : this()
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _alarmManager = alarmManager;

            // 订阅 PictureBox 的 Paint 事件
            this.picTrend.Paint += PicTrend_Paint;
            this.picTrend.Resize += (s, e) => picTrend.Invalidate();

            // 启动定时器
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 1000;
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            RefreshOverview();
        }

        // ============================================================
        // 设计器代码（完全内嵌，无需 .Designer.cs）
        // ============================================================
        private void InitializeComponent()
        {
            this.tableLayoutMain = new TableLayoutPanel();
            this.flowDeviceCards = new FlowLayoutPanel();
            this.panelTrend = new Panel();
            this.picTrend = new PictureBox();
            this.SuspendLayout();

            // tableLayoutMain
            this.tableLayoutMain.Dock = DockStyle.Fill;
            this.tableLayoutMain.ColumnCount = 1;
            this.tableLayoutMain.RowCount = 2;
            this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            this.tableLayoutMain.Controls.Add(this.flowDeviceCards, 0, 0);
            this.tableLayoutMain.Controls.Add(this.panelTrend, 0, 1);

            // flowDeviceCards
            this.flowDeviceCards.Dock = DockStyle.Fill;
            this.flowDeviceCards.FlowDirection = FlowDirection.LeftToRight;
            this.flowDeviceCards.Padding = new Padding(10);
            this.flowDeviceCards.AutoScroll = true;
            this.flowDeviceCards.WrapContents = true;

            // panelTrend
            this.panelTrend.Dock = DockStyle.Fill;
            this.panelTrend.Padding = new Padding(10);
            this.panelTrend.Controls.Add(this.picTrend);

            // picTrend
            this.picTrend.Dock = DockStyle.Fill;
            this.picTrend.BackColor = Color.White;
            this.picTrend.BorderStyle = BorderStyle.FixedSingle;

            this.Controls.Add(this.tableLayoutMain);
            this.ResumeLayout(false);
        }

        // ============================================================
        // 业务逻辑
        // ============================================================
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visible && !this.DesignMode)
            {
                RefreshOverview();
            }
        }

        public void RefreshOverview()
        {
            if (_deviceManager == null) return;

            // 释放旧卡片
            for (int i = this.flowDeviceCards.Controls.Count - 1; i >= 0; i--)
            {
                this.flowDeviceCards.Controls[i].Dispose();
            }
            this.flowDeviceCards.Controls.Clear();

            // 创建新卡片
            var devices = _deviceManager.GetAllDevices();
            foreach (var runtime in devices)
            {
                var card = CreateDeviceCard(runtime);
                this.flowDeviceCards.Controls.Add(card);
            }

            // 更新趋势数据
            UpdateTrendData();
        }

        private Panel CreateDeviceCard(DeviceRuntime runtime)
        {
            var config = runtime.Config;
            var panel = new Panel
            {
                Size = new Size(180, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Padding = new Padding(8)
            };

            var lblName = new Label
            {
                Text = config.Name,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 25
            };

            var lblStatus = new Label
            {
                Text = runtime.IsOnline ? "● 在线" : "○ 离线",
                ForeColor = runtime.IsOnline ? Color.Green : Color.Red,
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9F)
            };

            var progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Maximum = Math.Max(config.TargetCount, 1),
                Value = Math.Min(config.CurrentCount, config.TargetCount),
                ForeColor = config.CurrentCount >= config.TargetCount ? Color.Green : Color.DodgerBlue
            };

            var lblProd = new Label
            {
                Text = $"{config.CurrentCount} / {config.TargetCount}",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F)
            };

            var lblStep = new Label
            {
                Text = $"步骤: {runtime.CurrentStep}",
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };

            panel.Controls.Add(lblStep);
            panel.Controls.Add(lblProd);
            panel.Controls.Add(progressBar);
            panel.Controls.Add(lblStatus);
            panel.Controls.Add(lblName);

            return panel;
        }

        private void UpdateTrendData()
        {
            if (_deviceManager == null) return;
            var devices = _deviceManager.GetAllDevices();
            int total = devices.Sum(d => d.Config.CurrentCount);

            lock (_trendLock)
            {
                _trendData.Add(total);
                if (_trendData.Count > _maxTrendPoints)
                    _trendData.RemoveAt(0);
            }

            // 触发重绘
            this.picTrend.Invalidate();
        }

        private void PicTrend_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int width = this.picTrend.ClientSize.Width - 20;
            int height = this.picTrend.ClientSize.Height - 30;
            int leftMargin = 10;
            int bottomMargin = 20;

            // 绘制背景
            g.Clear(Color.White);

            // 绘制边框
            using (var pen = new Pen(Color.LightGray, 1))
            {
                g.DrawRectangle(pen, leftMargin, 0, width, height);
            }

            // 获取数据
            List<int> data;
            lock (_trendLock)
            {
                data = new List<int>(_trendData);
            }

            if (data.Count < 2)
            {
                // 显示提示文字
                using (var font = new Font("Segoe UI", 12F))
                using (var brush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("等待数据...", font, brush,
                        new RectangleF(leftMargin + 10, height / 2 - 10, width - 20, 30),
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
                return;
            }

            // 计算范围
            int maxVal = data.Max();
            int minVal = data.Min();
            int range = maxVal - minVal;
            if (range == 0) range = 1;

            // 计算绘图区域
            float plotWidth = width - 10;
            float plotHeight = height - 10;

            // 绘制网格线
            using (var gridPen = new Pen(Color.FromArgb(200, 230, 230, 230), 1))
            {
                for (int i = 0; i <= 5; i++)
                {
                    float y = i * plotHeight / 5;
                    g.DrawLine(gridPen, leftMargin, y, leftMargin + plotWidth, y);
                }
            }

            // 绘制折线
            using (var linePen = new Pen(Color.FromArgb(0, 120, 215), 2))
            {
                PointF[] points = new PointF[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    float x = leftMargin + (i / (float)(data.Count - 1)) * plotWidth;
                    float y = plotHeight - ((data[i] - minVal) / (float)range) * plotHeight;
                    points[i] = new PointF(x, y);
                }
                g.DrawLines(linePen, points);
            }

            // 绘制Y轴标签（最小值和最大值）
            using (var font = new Font("Segoe UI", 8F))
            using (var brush = new SolidBrush(Color.DarkGray))
            {
                g.DrawString(minVal.ToString(), font, brush, leftMargin, plotHeight + 2);
                g.DrawString(maxVal.ToString(), font, brush, leftMargin, 2);
                g.DrawString("总产量", font, brush, leftMargin + 5, 2);
            }

            // 绘制X轴标签
            using (var font = new Font("Segoe UI", 8F))
            using (var brush = new SolidBrush(Color.DarkGray))
            {
                if (data.Count > 1)
                {
                    g.DrawString(DateTime.Now.ToString("HH:mm:ss"), font, brush,
                        leftMargin + plotWidth - 60, plotHeight + 2);
                }
            }
        }

        // ============================================================
        // 资源释放
        // ============================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                // 控件会由父容器自动释放，无需额外处理
            }
            base.Dispose(disposing);
        }
    }
}