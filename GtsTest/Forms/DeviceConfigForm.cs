using GtsTest.Models;
using System;
using System.Net;
using System.Windows.Forms;

namespace GtsTest
{
    public partial class DeviceConfigForm : Form
    {
        public DeviceConfig Config { get; private set; } = new DeviceConfig();

        public DeviceConfigForm()
        {
            InitializeComponent();

            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            Config.Name = txtName.Text.Trim();
            Config.Modbus.IpAddress = txtIp.Text.Trim();
            Config.Modbus.Port = (int)numPort.Value;
            Config.Modbus.StartAddress = (ushort)numStart.Value;
            Config.Modbus.RegisterCount = (ushort)numCount.Value;
            Config.Axis = (short)numAxis.Value;
            Config.TargetCount = (int)numTarget.Value;

            if (string.IsNullOrEmpty(Config.Name))
            {
                MessageBox.Show("设备名称不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (!IsValidIpAddress(Config.Modbus.IpAddress))
            {
                MessageBox.Show("设备IP格式错误", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool IsValidIpAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // 1. 先解析为 IPAddress 对象
            if (!IPAddress.TryParse(input, out IPAddress? address))
                return false;

            // 2. 必须是 IPv4 格式 (Modbus通常用IPv4)
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            // 3. 【关键】严查输入是否包含4个部分，防止 "1" 被转成 "0.0.0.1"
            string[] parts = input.Split('.');
            if (parts.Length != 4) return false;

            // 4. 检查每部分是否都是纯数字且在 0-255 之间（不能有前导零如 "01"）
            foreach (string part in parts)
            {
                if (!byte.TryParse(part, out _)) return false; // byte范围就是0-255，且拒绝"256"
            }

            return true;
        }
    }
}