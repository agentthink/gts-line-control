using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace GtsTest.Modbus
{
    public partial class ModbusConfigForm : Form
    {
        private readonly ModbusConfig _originalConfig;  // 传入的原始配置副本
        public ModbusConfig Config { get; private set; }
        public bool IsChanged { get; private set; } = false;

        public ModbusConfigForm(ModbusConfig currentConfig )
        {
            InitializeComponent();
            _originalConfig = new ModbusConfig
            {
                Protocol = currentConfig.Protocol,
                IpAddress = currentConfig.IpAddress,
                Port = currentConfig.Port,
                PortName = currentConfig.PortName,
                BaudRate = currentConfig.BaudRate,
                DataBits = currentConfig.DataBits,
                StopBits = currentConfig.StopBits,
                Parity = currentConfig.Parity,
                DataType = currentConfig.DataType,
                DisplayFormat = currentConfig.DisplayFormat,
                TimeoutMs = currentConfig.TimeoutMs,
                ByteOrder = currentConfig.ByteOrder,
                StartAddress = currentConfig.StartAddress,
                RegisterCount = currentConfig.RegisterCount,
                SlaveAddress = currentConfig.SlaveAddress,
                AddressType = currentConfig.AddressType
            };
            Config = currentConfig;
            LoadConfig();
            RefreshComPorts();
        }

        private void LoadConfig()
        {
            cmbProtocol.SelectedIndex = Config.Protocol == ModbusProtocol.Tcp ? 0 : 1;
            txtIp.Text = Config.IpAddress;
            numPort.Value = Config.Port;
            cmbComPort.Text = Config.PortName;
            cmbBaudRate.SelectedItem = Config.BaudRate;
            cmbDataBits.SelectedItem = Config.DataBits;
            cmbStopBits.SelectedItem = Config.StopBits switch
            {
                StopBits.One => "1",
                StopBits.OnePointFive => "1.5",
                StopBits.Two => "2",
                _ => "1"
            };
            cmbParity.SelectedItem = Config.Parity.ToString();
            cmbDataType.SelectedIndex = (int)Config.DataType;
            cmbDisplayFormat.SelectedIndex = (int)Config.DisplayFormat;
            numStartAddress.Value = Config.StartAddress;
            numRegisterCount.Value = Config.RegisterCount;
            numSlaveAddress.Value = Config.SlaveAddress;
            cmbAddressType.SelectedIndex = (int)Config.AddressType;
            cmbByteOrder.SelectedIndex = Config.ByteOrder == ByteOrder.BigEndian ? 0 : 1;

            // 更新 TCP/RTU 可见性
            CmbProtocol_SelectedIndexChanged(null, null);
        }

        // 协议切换事件
        private void CmbProtocol_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isTcp = cmbProtocol.SelectedIndex == 0;
            grpTcp.Enabled = isTcp;
            grpRtu.Enabled = !isTcp;
        }
        // 刷新串口列表
        private void BtnRefreshCom_Click(object sender, EventArgs e)
        {
            RefreshComPorts();
        }

        private void RefreshComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            cmbComPort.Items.Clear();
            cmbComPort.Items.AddRange(ports);
            if (cmbComPort.Items.Count > 0 && string.IsNullOrEmpty(cmbComPort.Text))
                cmbComPort.SelectedIndex = 0;
        }

        // 确定按钮点击事件
        private void btnOK_Click(object sender, EventArgs e)
        {
            // 读取配置
            Config.Protocol = cmbProtocol.SelectedIndex == 0 ? ModbusProtocol.Tcp : ModbusProtocol.Rtu;
            Config.IpAddress = txtIp.Text.Trim();
            Config.SlaveAddress = (byte)numSlaveAddress.Value;
            Config.Port = (int)numPort.Value;
            Config.PortName = cmbComPort.Text.Trim();
            Config.BaudRate = (int)cmbBaudRate.SelectedItem;
            Config.DataBits = (int)cmbDataBits.SelectedItem;
            Config.StopBits = cmbStopBits.SelectedItem.ToString() switch
            {
                "1" => StopBits.One,
                "1.5" => StopBits.OnePointFive,
                "2" => StopBits.Two,
                _ => StopBits.One
            };
            Config.Parity = cmbParity.SelectedItem.ToString() switch
            {
                "None" => Parity.None,
                "Odd" => Parity.Odd,
                "Even" => Parity.Even,
                "Mark" => Parity.Mark,
                "Space" => Parity.Space,
                _ => Parity.None
            };
            Config.DataType = (DataType)cmbDataType.SelectedIndex;
            Config.DisplayFormat = (DisplayFormat)cmbDisplayFormat.SelectedIndex;
            Config.StartAddress = (ushort)numStartAddress.Value;
            Config.RegisterCount = (ushort)numRegisterCount.Value;
            Config.AddressType = (AddressType)cmbAddressType.SelectedIndex;

            // 读取字节序
            Config.ByteOrder = cmbByteOrder.SelectedIndex == 0 ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            // 比较是否变化
            IsChanged = !ConfigsEqual(_originalConfig, Config);
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ConfigsEqual(ModbusConfig a, ModbusConfig b)
        {
            return a.Protocol == b.Protocol &&
                   a.IpAddress == b.IpAddress &&
                   a.Port == b.Port &&
                   a.PortName == b.PortName &&
                   a.BaudRate == b.BaudRate &&
                   a.DataBits == b.DataBits &&
                   a.StopBits == b.StopBits &&
                   a.Parity == b.Parity &&
                   a.DataType == b.DataType &&
                   a.DisplayFormat == b.DisplayFormat &&
                   a.TimeoutMs == b.TimeoutMs &&
                   a.StartAddress == b.StartAddress &&
                   a.RegisterCount == b.RegisterCount &&
                   a.SlaveAddress == b.SlaveAddress &&
                   a.ByteOrder == b.ByteOrder &&
                   a.AddressType == b.AddressType;
        }

        // 取消按钮点击事件（可选，但设计器中已绑定）
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}