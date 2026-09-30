using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GtsTest.Diagnostics
{
    /// <summary>
    /// 通信报文监视器（Modbus Poll 风格）
    ///
    /// 注意：
    ///   - 本窗体的所有 UI 均由构造函数调用的 InitializeUI() 动态构建
    ///   - 不使用 Designer 生成的 InitializeComponent()
    ///   - 如果 VS 自动生成了 FrameMonitorForm.Designer.cs / .resx，请删除它们
    /// </summary>
    public partial class FrameMonitorForm : Form
    {
        // ---- 数据源 ----
        private List<FrameLogEntry> _allEntries = new List<FrameLogEntry>();
        private List<FrameLogEntry> _filteredEntries = new List<FrameLogEntry>();
        private readonly object _dataLock = new object();

        // ---- UI 控件 ----
        private ComboBox cmbProtocol;
        private TextBox txtKeyword;
        private CheckBox chkAutoScroll;
        private Button btnPause, btnClear, btnExport;
        private ListView listViewFrames;
        private Label lblStats;

        // ---- 状态 ----
        private bool _paused = false;

        public FrameMonitorForm()
        {
            InitializeUI();
            SubscribeHub();

            this.FormClosing += (s, e) => UnsubscribeHub();
        }

        private void InitializeUI()
        {
            this.Text = "🔍 通信报文监视器";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 400);

            // ---- 顶部工具条 ----
            var toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(245, 245, 250),
                Padding = new Padding(6)
            };

            var lblProtocol = new Label
            {
                Text = "协议:",
                Location = new Point(6, 12),
                AutoSize = true
            };
            cmbProtocol = new ComboBox
            {
                Location = new Point(48, 8),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbProtocol.Items.AddRange(new object[]
            {
                "全部", "Modbus TCP", "Modbus RTU", "OPC UA", "MQTT"
            });
            cmbProtocol.SelectedIndex = 0;
            cmbProtocol.SelectedIndexChanged += (s, e) => ApplyFilterAndRefresh();

            var lblKeyword = new Label
            {
                Text = "关键字:",
                Location = new Point(180, 12),
                AutoSize = true
            };
            txtKeyword = new TextBox
            {
                Location = new Point(235, 8),
                Width = 200
            };
            txtKeyword.TextChanged += (s, e) => ApplyFilterAndRefresh();

            chkAutoScroll = new CheckBox
            {
                Text = "自动滚动",
                Location = new Point(448, 10),
                AutoSize = true,
                Checked = true
            };

            btnPause = new Button
            {
                Text = "⏸ 暂停",
                Location = new Point(560, 6),
                Size = new Size(75, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightYellow
            };
            btnPause.Click += BtnPause_Click;

            btnClear = new Button
            {
                Text = "🗑 清空",
                Location = new Point(640, 6),
                Size = new Size(75, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGray
            };
            btnClear.Click += BtnClear_Click;

            btnExport = new Button
            {
                Text = "💾 导出 CSV",
                Location = new Point(720, 6),
                Size = new Size(95, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGreen
            };
            btnExport.Click += BtnExport_Click;

            toolPanel.Controls.AddRange(new Control[]
            {
                lblProtocol, cmbProtocol, lblKeyword, txtKeyword,
                chkAutoScroll, btnPause, btnClear, btnExport
            });

            // ---- 中间列表（虚拟模式）----
            listViewFrames = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                VirtualMode = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9F),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            listViewFrames.Columns.Add("时间", 95);
            listViewFrames.Columns.Add("协议", 80);
            listViewFrames.Columns.Add("方向", 55);
            listViewFrames.Columns.Add("摘要", 480);
            listViewFrames.Columns.Add("数据 (HEX)", 350);
            listViewFrames.RetrieveVirtualItem += ListViewFrames_RetrieveVirtualItem;
            listViewFrames.DrawColumnHeader += ListViewFrames_DrawColumnHeader;
            listViewFrames.DrawItem += ListViewFrames_DrawItem;
            listViewFrames.DrawSubItem += ListViewFrames_DrawSubItem;

            // ---- 底部统计 ----
            lblStats = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(245, 245, 250),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Text = "就绪"
            };

            this.Controls.Add(listViewFrames);
            this.Controls.Add(toolPanel);
            this.Controls.Add(lblStats);
        }

        // ================================================================
        // Hub 订阅
        // ================================================================
        private Action? _hubHandler;

        private void SubscribeHub()
        {
            _hubHandler = () => RefreshFromHub();
            FrameMonitorHub.Instance.FramesUpdated += _hubHandler;
        }

        private void UnsubscribeHub()
        {
            if (_hubHandler != null)
            {
                try { FrameMonitorHub.Instance.FramesUpdated -= _hubHandler; } catch { }
                _hubHandler = null;
            }
        }

        /// <summary>从 Hub 拉取最新数据并刷新（在 UI 线程调用）</summary>
        private void RefreshFromHub()
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action(RefreshFromHub)); } catch { }
                return;
            }

            if (_paused) return;

            // 拉取全量快照
            var snapshot = FrameMonitorHub.Instance.Snapshot();

            lock (_dataLock)
            {
                _allEntries = snapshot.ToList();
                ApplyFilterInternal();
            }

            // 更新列表大小
            listViewFrames.VirtualListSize = _filteredEntries.Count;
            listViewFrames.Invalidate();

            // 自动滚动到底部
            if (chkAutoScroll.Checked && _filteredEntries.Count > 0)
            {
                try { listViewFrames.EnsureVisible(_filteredEntries.Count - 1); } catch { }
            }

            // 更新统计
            UpdateStatsText();
        }

        private void ApplyFilterAndRefresh()
        {
            lock (_dataLock)
            {
                ApplyFilterInternal();
            }
            listViewFrames.VirtualListSize = _filteredEntries.Count;
            listViewFrames.Invalidate();

            if (chkAutoScroll.Checked && _filteredEntries.Count > 0)
            {
                try { listViewFrames.EnsureVisible(_filteredEntries.Count - 1); } catch { }
            }
            UpdateStatsText();
        }

        private void ApplyFilterInternal()
        {
            string protocolFilter = cmbProtocol?.SelectedItem?.ToString() ?? "全部";
            string keyword = txtKeyword?.Text?.Trim() ?? "";

            IEnumerable<FrameLogEntry> q = _allEntries;

            // 协议过滤
            if (protocolFilter != "全部")
            {
                var proto = protocolFilter switch
                {
                    "Modbus TCP" => (FrameProtocol?)FrameProtocol.ModbusTcp,
                    "Modbus RTU" => FrameProtocol.ModbusRtu,
                    "OPC UA" => FrameProtocol.OpcUa,
                    "MQTT" => FrameProtocol.Mqtt,
                    _ => null
                };
                if (proto.HasValue)
                    q = q.Where(e => e.Protocol == proto.Value);
            }

            // 关键字过滤
            if (!string.IsNullOrEmpty(keyword))
            {
                q = q.Where(e =>
                    (e.Summary?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.Payload?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.DeviceId?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.DeviceName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            _filteredEntries = q.ToList();
        }

        private void UpdateStatsText()
        {
            var stats = FrameMonitorHub.Instance.GetStats();
            int total = _allEntries.Count;
            int shown = _filteredEntries.Count;

            lblStats.Text = $"总记录: {total} | 显示: {shown} | " +
                $"TX: {stats.TotalTx} | RX: {stats.TotalRx} | 错误: {stats.TotalError} | " +
                $"Modbus TCP: {stats.ModbusTcpCount} | RTU: {stats.ModbusRtuCount} | " +
                $"OPC UA: {stats.OpcUaCount} | MQTT: {stats.MqttCount}";
        }

        // ================================================================
        // 虚拟列表事件
        // ================================================================
        private void ListViewFrames_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _filteredEntries.Count)
            {
                e.Item = new ListViewItem("");
                return;
            }

            var entry = _filteredEntries[e.ItemIndex];
            var item = new ListViewItem(entry.TimeText);
            item.SubItems.Add(entry.ProtocolText);
            item.SubItems.Add(entry.DirectionText);
            item.SubItems.Add(entry.Summary);
            item.SubItems.Add(entry.Payload);
            item.Tag = entry;
            e.Item = item;
        }

        private void ListViewFrames_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.DarkSlateGray, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, this.Font,
                e.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void ListViewFrames_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // 用 DrawSubItem 绘制，这里什么都不做
            e.DrawDefault = false;
        }

        private void ListViewFrames_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _filteredEntries.Count) return;

            var entry = _filteredEntries[e.ItemIndex];

            // 背景
            Color bg = e.Item.Selected ? Color.DarkBlue : Color.Black;
            Color fg = Color.LightGreen;

            if (entry.IsError || entry.Direction == FrameDirection.Error)
                fg = Color.OrangeRed;
            else if (entry.Direction == FrameDirection.TX)
                fg = Color.DeepSkyBlue;
            else if (entry.Direction == FrameDirection.RX)
                fg = Color.LightGreen;
            else if (entry.Direction == FrameDirection.Info)
                fg = Color.Gray;

            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listViewFrames.Font,
                e.Bounds, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        // ================================================================
        // 按钮事件
        // ================================================================
        private void BtnPause_Click(object sender, EventArgs e)
        {
            _paused = !_paused;
            btnPause.Text = _paused ? "▶ 继续" : "⏸ 暂停";
            btnPause.BackColor = _paused ? Color.OrangeRed : Color.LightYellow;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空所有报文吗？", "确认", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            FrameMonitorHub.Instance.Clear();

            lock (_dataLock)
            {
                _allEntries.Clear();
                _filteredEntries.Clear();
            }

            listViewFrames.VirtualListSize = 0;
            listViewFrames.Invalidate();
            UpdateStatsText();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_filteredEntries.Count == 0)
            {
                MessageBox.Show("没有可导出的报文", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "导出报文",
                Filter = "CSV 文件|*.csv",
                DefaultExt = "csv",
                FileName = $"FrameMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("序号,时间,协议,方向,设备ID,设备名称,摘要,数据HEX,是否错误,错误信息");

                foreach (var e2 in _filteredEntries)
                {
                    sb.AppendLine(string.Join(",",
                        e2.Seq,
                        Csv(e2.TimeText),
                        Csv(e2.ProtocolText),
                        Csv(e2.DirectionText),
                        Csv(e2.DeviceId),
                        Csv(e2.DeviceName),
                        Csv(e2.Summary),
                        Csv(e2.Payload),
                        e2.IsError ? "1" : "0",
                        Csv(e2.ErrorMessage)));
                }

                // UTF-8 BOM 保证 Excel 打开中文不乱码
                File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));

                MessageBox.Show($"已导出 {_filteredEntries.Count} 条报文到：\n{dialog.FileName}",
                    "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}