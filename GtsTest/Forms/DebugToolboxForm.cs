using GtsTest.Commands;
using GtsTest.Core;
using GtsTest.Modbus;
using GtsTest.Models;
using GtsTest.Services.Alarm;
using GtsTest.Services.Authentication;
using GtsTest.Services.Data;
using GtsTest.Services.Plc;
using GtsTest.Services.Serial;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GtsTest.Forms
{
    public partial class DebugToolboxForm : Form
    {
        private readonly DeviceManager _deviceManager;
        private readonly GtsModel _model;
        private readonly IAlarmManager _alarmManager;
        private readonly IDataRepository _repo;
        private readonly IAuthenticationService _authService;
        private readonly PlcManager _plcManager;

        private DeviceRuntime? _currentDevice;
        private readonly string _currentUser;
        private readonly long _currentUserId;

        // 串口调试
        private SerialPortManager? _serialManager;
        private ISerialPortDriver? _serialDriver;
        private long _serialRecvBytes = 0;

        // PLC 调试
        private IPlcClient? _plcClient;

        public DebugToolboxForm(
            DeviceManager deviceManager,
            GtsModel model,
            IAlarmManager alarmManager,
            IDataRepository repo,
            IAuthenticationService authService,
            PlcManager plcManager)
        {
            InitializeComponent();

            _deviceManager = deviceManager;
            _model = model;
            _alarmManager = alarmManager;
            _repo = repo;
            _authService = authService;
            _plcManager = plcManager;

            var user = SessionManager.CurrentUser;
            _currentUser = user?.Username ?? "未登录";
            _currentUserId = user?.Id ?? 0;

            LoadDevices();
            cmbDevice.SelectedIndexChanged += CmbDevice_SelectedIndexChanged;
            UpdateDeviceInfo();

            timerRefresh.Tick += TimerRefresh_Tick;
            timerRefresh.Start();

            // 轴控制
            btnHome.Click += BtnHome_Click;
            btnMoveAbs.Click += BtnMoveAbs_Click;
            btnJogP.Click += BtnJogP_Click;
            btnJogN.Click += BtnJogN_Click;
            btnStopAxis.Click += BtnStopAxis_Click;
            btnServoOn.Click += BtnServoOn_Click;
            btnServoOff.Click += BtnServoOff_Click;
            btnAxisAlarmReset.Click += BtnAxisAlarmReset_Click;

            // Modbus
            btnWriteRegister.Click += BtnWriteRegister_Click;
            btnWriteCoil.Click += BtnWriteCoil_Click;
            btnReadRegister.Click += BtnReadRegister_Click;

            // 设备控制
            btnStartDevice.Click += BtnStartDevice_Click;
            btnStopDevice.Click += BtnStopDevice_Click;
            btnDeviceConfig.Click += BtnDeviceConfig_Click;
            btnConnectModbus.Click += BtnConnectModbus_Click;
            btnDisconnectModbus.Click += BtnDisconnectModbus_Click;

            // 视觉触发
            btnTriggerVision.Click += BtnTriggerVision_Click;
            UpdateVisionStatus("就绪，等待触发...", Color.DimGray);

            // 串口调试初始化
            _serialManager = new SerialPortManager();
            BindSerialEvents();
            RefreshSerialPortList();

            // PLC 调试初始化
            BindPlcEvents();

            // IO 强制模拟
            btnForceIO.Click += BtnForceIO_Click;
            btnClearForceIO.Click += BtnClearForceIO_Click;

            AppLogger.Info($"调试工具箱已打开，用户: {_currentUser}", "DebugToolbox");
        }

        // ================================================================
        //  设备加载与 UI 更新
        // ================================================================
        private void LoadDevices()
        {
            cmbDevice.Items.Clear();
            var devices = _deviceManager.GetAllDevices();
            foreach (var d in devices)
                cmbDevice.Items.Add(d.Config.Name);
            if (cmbDevice.Items.Count > 0)
                cmbDevice.SelectedIndex = 0;
        }

        private void CmbDevice_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var name = cmbDevice.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(name)) return;
            var device = _deviceManager.GetAllDevices().FirstOrDefault(d => d.Config.Name == name);
            _currentDevice = device;
            UpdateDeviceInfo();
        }

        private void UpdateDeviceInfo()
        {
            if (_currentDevice == null)
            {
                lblDeviceName.Text = "未选择";
                lblDeviceStatus.Text = "离线";
                lblDeviceStatus.ForeColor = Color.Gray;
                lblProdInfo.Text = "0/0";
                lblAxisValue.Text = "--";
                lblAxisStatus.Text = "--";
                lblAxisPos.Text = "--";
                lblAxisVel.Text = "--";
                lblCurrentDevice.Text = "当前设备: 未选择";
                UpdateModbusButtonState();
                return;
            }

            lblDeviceName.Text = _currentDevice.Config.Name;
            bool modbusConnected = _currentDevice.ModbusClient?.IsConnected ?? false;
            lblDeviceStatus.Text = modbusConnected ? "在线" : "离线";
            lblDeviceStatus.ForeColor = modbusConnected ? Color.Green : Color.Red;
            lblProdInfo.Text = $"{_currentDevice.Config.CurrentCount}/{_currentDevice.Config.TargetCount}";

            if (modbusConnected)
            {
                try
                {
                    uint clk;
                    int status;
                    _model.GetAxisStatus(_currentDevice.Config.Axis, out status, out clk);
                    double pos, vel;
                    _model.GetPrfPos(_currentDevice.Config.Axis, out pos, out clk);
                    _model.GetPrfVel(_currentDevice.Config.Axis, out vel, out clk);

                    lblAxisValue.Text = _currentDevice.Config.Axis.ToString();
                    lblAxisStatus.Text = (status & 0x200) != 0 ? "已使能" : "未使能";
                    lblAxisPos.Text = pos.ToString("F2");
                    lblAxisVel.Text = vel.ToString("F2");
                }
                catch
                {
                    lblAxisValue.Text = _currentDevice.Config.Axis.ToString();
                    lblAxisStatus.Text = "--";
                    lblAxisPos.Text = "--";
                    lblAxisVel.Text = "--";
                }
            }
            else
            {
                lblAxisValue.Text = _currentDevice.Config.Axis.ToString();
                lblAxisStatus.Text = "离线";
                lblAxisPos.Text = "--";
                lblAxisVel.Text = "--";
            }

            string statusText = modbusConnected ? "Modbus已连接" : "Modbus未连接";
            lblCurrentDevice.Text = $"当前设备: {_currentDevice.Config.Name} ({statusText})";

            UpdateModbusButtonState();
        }

        private void UpdateModbusButtonState()
        {
            if (_currentDevice != null && _currentDevice.ModbusClient != null)
            {
                bool connected = _currentDevice.ModbusClient.IsConnected;
                btnConnectModbus.Enabled = !connected;
                btnDisconnectModbus.Enabled = connected;
            }
            else
            {
                btnConnectModbus.Enabled = false;
                btnDisconnectModbus.Enabled = false;
            }
        }

        private void TimerRefresh_Tick(object? sender, EventArgs e)
        {
            if (this.IsDisposed) return;
            UpdateDeviceInfo();
        }

        private void UpdateVisionStatus(string message, Color color)
        {
            if (lblVisionStatus == null || lblVisionStatus.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateVisionStatus(message, color)));
                return;
            }
            lblVisionStatus.Text = message;
            lblVisionStatus.ForeColor = color;
        }

        // ================================================================
        //  🆕 IO 强制模拟
        // ================================================================
        private void BtnForceIO_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("请先选择设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int ioIndex = (int)numForceIOIndex.Value;
            bool forceValue = chkForceIOValue.Checked;

            _currentDevice.ForcedIOs[ioIndex] = forceValue;
            AppLogger.Info($"🔧 [调试] 强制 IO[{ioIndex}] = {forceValue}", "DebugToolbox");
            MessageBox.Show($"已强制 IO[{ioIndex}] = {forceValue}", "强制成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClearForceIO_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null) return;
            _currentDevice.ForcedIOs.Clear();
            AppLogger.Info("🔧 [调试] 已清除所有 IO 强制状态", "DebugToolbox");
            MessageBox.Show("已清除所有 IO 强制状态", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ================================================================
        //  轴控制
        // ================================================================
        private bool ValidateDevice()
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("设备未选择", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!_currentDevice.ModbusClient.IsConnected)
            {
                MessageBox.Show("Modbus 未连接，请先连接设备", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void BtnHome_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            int homePos = _currentDevice.Config.HomePosition;
            try
            {
                short result = _model.HomeAxis(axis, homePos);
                if (result != 0)
                {
                    AppLogger.Error($"回零失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"回零启动失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"回零已启动: 设备={_currentDevice.Config.Name}, 轴={axis}, 目标位置={homePos}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugHome", $"设备={_currentDevice.Config.Name}, 轴={axis}, 目标位置={homePos}", _repo);
                MessageBox.Show("回零已启动", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"回零异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"回零异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnMoveAbs_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            if (!int.TryParse(txtTargetPos.Text, out int target))
            {
                MessageBox.Show("请输入有效目标位置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            short axis = _currentDevice!.Config.Axis;
            double vel = 10, acc = 5;
            double.TryParse(txtJogSpeed.Text, out vel);
            double.TryParse(txtAcc.Text, out acc);
            try
            {
                short result = _model.MoveAbs(axis, target, vel, acc);
                if (result != 0)
                {
                    AppLogger.Error($"定位失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 目标={target}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"定位启动失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"定位已启动: 设备={_currentDevice.Config.Name}, 轴={axis}, 目标={target}, 速度={vel}, 加速度={acc}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugMoveAbs", $"设备={_currentDevice.Config.Name}, 轴={axis}, 目标={target}, 速度={vel}", _repo);
                MessageBox.Show("定位已启动", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"定位异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"定位异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnJogP_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            double speed = 10;
            double.TryParse(txtJogSpeed.Text, out speed);
            try
            {
                short result = _model.StartJog(axis, speed, true);
                if (result != 0)
                {
                    AppLogger.Error($"点动+启动失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"点动启动失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"点动+已启动: 设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugJogP", $"设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}", _repo);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"点动+异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"点动异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnJogN_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            double speed = 10;
            double.TryParse(txtJogSpeed.Text, out speed);
            try
            {
                short result = _model.StartJog(axis, speed, false);
                if (result != 0)
                {
                    AppLogger.Error($"点动-启动失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"点动启动失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"点动-已启动: 设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugJogN", $"设备={_currentDevice.Config.Name}, 轴={axis}, 速度={speed}", _repo);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"点动-异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"点动异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopAxis_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("设备未选择", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_currentDevice.ModbusClient.IsConnected)
            {
                MessageBox.Show("Modbus 未连接", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            short axis = _currentDevice.Config.Axis;
            try
            {
                _model.GT_Stop(1 << (axis - 1), 0);
                AppLogger.Info($"轴停止: 设备={_currentDevice.Config.Name}, 轴={axis}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugStopAxis", $"设备={_currentDevice.Config.Name}, 轴={axis}", _repo);
                MessageBox.Show("轴运动已停止", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"停止轴异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"停止轴异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnServoOn_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            try
            {
                short result = _model.GT_AxisOn(axis);
                if (result != 0)
                {
                    AppLogger.Error($"伺服使能失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"使能失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"伺服使能成功: 设备={_currentDevice.Config.Name}, 轴={axis}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugServoOn", $"设备={_currentDevice.Config.Name}, 轴={axis}", _repo);
                MessageBox.Show("伺服已使能", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateDeviceInfo();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"伺服使能异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"使能异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnServoOff_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            try
            {
                short result = _model.GT_AxisOff(axis);
                if (result != 0)
                {
                    AppLogger.Error($"伺服去使能失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"去使能失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"伺服去使能成功: 设备={_currentDevice.Config.Name}, 轴={axis}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugServoOff", $"设备={_currentDevice.Config.Name}, 轴={axis}", _repo);
                MessageBox.Show("伺服已去使能", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateDeviceInfo();
            }
            catch (Exception ex)
            {
                AppLogger.Error($"伺服去使能异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"去使能异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAxisAlarmReset_Click(object? sender, EventArgs e)
        {
            if (!ValidateDevice()) return;
            short axis = _currentDevice!.Config.Axis;
            try
            {
                short result = _model.GT_ClrSts(axis, 1);
                if (result != 0)
                {
                    AppLogger.Error($"轴报警复位失败: 设备={_currentDevice.Config.Name}, 轴={axis}, 错误码={result}", "DebugToolbox");
                    MessageBox.Show($"复位报警失败，错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AppLogger.Info($"轴报警复位成功: 设备={_currentDevice.Config.Name}, 轴={axis}", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugAxisReset", $"设备={_currentDevice.Config.Name}, 轴={axis}", _repo);
                MessageBox.Show("轴报警已复位", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"轴报警复位异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"复位报警异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  Modbus 读写
        // ================================================================
        private bool ValidateModbusDevice()
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("设备未选择", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!_currentDevice.ModbusClient.IsConnected)
            {
                MessageBox.Show("Modbus 未连接，请先连接设备", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void BtnWriteRegister_Click(object? sender, EventArgs e)
        {
            if (!ValidateModbusDevice()) return;
            ushort address = (ushort)numRegAddress.Value;
            DataType dataType = (DataType)cmbDataType.SelectedIndex;
            ByteOrder byteOrder = (ByteOrder)cmbByteOrder.SelectedIndex;
            var parts = txtRegValues.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<object>();
            foreach (var part in parts)
            {
                try
                {
                    object converted = dataType switch
                    {
                        DataType.Int16 => Convert.ToInt16(part),
                        DataType.UInt16 => Convert.ToUInt16(part),
                        DataType.Int32 => Convert.ToInt32(part),
                        DataType.UInt32 => Convert.ToUInt32(part),
                        DataType.Float => Convert.ToSingle(part),
                        DataType.Double => Convert.ToDouble(part),
                        _ => throw new NotSupportedException()
                    };
                    values.Add(converted);
                }
                catch
                {
                    MessageBox.Show($"值 '{part}' 无法转换为 {dataType}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (values.Count == 0)
            {
                MessageBox.Show("请至少输入一个有效值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ushort[] raw = ModbusClient.EncodeValue(values.ToArray(), dataType, byteOrder);
                if (raw.Length == 0)
                {
                    MessageBox.Show("编码数据为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                bool success = raw.Length == 1
                    ? _currentDevice!.ModbusClient.WriteSingleRegister(address, raw[0])
                    : _currentDevice!.ModbusClient.WriteMultipleRegisters(address, raw);
                if (success)
                {
                    AppLogger.Info($"Modbus写寄存器成功: 设备={_currentDevice.Config.Name}, 地址={address}, 类型={dataType}, 值={string.Join(",", values)}", "DebugToolbox");
                    AuditService.Log(_currentUserId, _currentUser, "DebugModbusWriteReg", $"设备={_currentDevice.Config.Name}, 地址={address}, 类型={dataType}, 值={string.Join(",", values)}", _repo);
                    MessageBox.Show("写入成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("写入失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Modbus写寄存器异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"写入异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnWriteCoil_Click(object? sender, EventArgs e)
        {
            if (!ValidateModbusDevice()) return;
            ushort address = (ushort)numCoilAddress.Value;
            bool value = cmbCoilValue.SelectedIndex == 0;
            try
            {
                bool success = _currentDevice!.ModbusClient.WriteSingleCoil(address, value);
                if (success)
                {
                    AppLogger.Info($"Modbus写线圈成功: 设备={_currentDevice.Config.Name}, 地址={address}, 值={value}", "DebugToolbox");
                    AuditService.Log(_currentUserId, _currentUser, "DebugModbusWriteCoil", $"设备={_currentDevice.Config.Name}, 地址={address}, 值={value}", _repo);
                    MessageBox.Show("线圈写入成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("线圈写入失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Modbus写线圈异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"写入异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReadRegister_Click(object? sender, EventArgs e)
        {
            if (!ValidateModbusDevice()) return;
            ushort address = (ushort)numReadAddress.Value;
            ushort count = (ushort)numReadCount.Value;
            try
            {
                var result = _currentDevice!.ModbusClient.ReadHoldingRegistersWithRaw(address, count);
                if (result == null)
                {
                    MessageBox.Show("读取失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string rawStr = string.Join(", ", result.RawRegisters);
                string valStr = result.ConvertedValue?.ToString() ?? "null";
                txtReadResult.Text = $"原始: [{rawStr}]\n转换: {valStr}";
                AppLogger.Info($"Modbus读寄存器成功: 设备={_currentDevice.Config.Name}, 地址={address}, 数量={count}, 原始值=[{rawStr}]", "DebugToolbox");
                AuditService.Log(_currentUserId, _currentUser, "DebugModbusReadReg", $"设备={_currentDevice.Config.Name}, 地址={address}, 数量={count}, 原始值=[{rawStr}]", _repo);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Modbus读寄存器异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"读取异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  设备控制
        // ================================================================
        private void BtnStartDevice_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("未选择设备", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (_deviceManager.StartDevice(_currentDevice.Config.DeviceId))
                {
                    AppLogger.Info($"设备启动成功: {_currentDevice.Config.Name}", "DebugToolbox");
                    AuditService.Log(_currentUserId, _currentUser, "DebugStartDevice", $"设备={_currentDevice.Config.Name}", _repo);
                    MessageBox.Show("设备启动成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateDeviceInfo();
                }
                else
                {
                    MessageBox.Show("设备启动失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"设备启动异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"启动异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopDevice_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("未选择设备", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (_deviceManager.StopDevice(_currentDevice.Config.DeviceId))
                {
                    AppLogger.Info($"设备停止成功: {_currentDevice.Config.Name}", "DebugToolbox");
                    AuditService.Log(_currentUserId, _currentUser, "DebugStopDevice", $"设备={_currentDevice.Config.Name}", _repo);
                    MessageBox.Show("设备已停止", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateDeviceInfo();
                }
                else
                {
                    MessageBox.Show("设备停止失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"设备停止异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"停止异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeviceConfig_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("未选择设备", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var form = new ModbusConfigForm(_currentDevice.Config.Modbus))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _currentDevice.Config.Modbus = form.Config;
                        AppLogger.Info($"Modbus配置已更新: 设备={_currentDevice.Config.Name}", "DebugToolbox");
                        AuditService.Log(_currentUserId, _currentUser, "DebugModbusConfig", $"设备={_currentDevice.Config.Name}", _repo);
                        MessageBox.Show("配置已保存，请重新连接 Modbus", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateModbusButtonState();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"配置设备异常: {ex.Message}", "DebugToolbox");
                MessageBox.Show($"配置异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  Modbus 连接/断开
        // ================================================================
        private void BtnConnectModbus_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null)
            {
                MessageBox.Show("未选择设备", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_currentDevice.ModbusClient == null)
            {
                MessageBox.Show("Modbus 客户端未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (_currentDevice.ModbusClient.IsConnected)
            {
                MessageBox.Show("Modbus 已连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                if (_currentDevice.ModbusClient.Connect())
                {
                    MessageBox.Show("Modbus 连接成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppLogger.Info($"Modbus 连接成功: {_currentDevice.Config.Name}", "DebugToolbox");
                    UpdateDeviceInfo();
                }
                else
                {
                    MessageBox.Show("Modbus 连接失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"Modbus 连接异常: {ex.Message}", "DebugToolbox");
            }
        }

        private void BtnDisconnectModbus_Click(object? sender, EventArgs e)
        {
            if (_currentDevice == null) return;
            if (_currentDevice.ModbusClient == null) return;
            if (!_currentDevice.ModbusClient.IsConnected) return;
            try
            {
                _currentDevice.ModbusClient.Disconnect();
                MessageBox.Show("Modbus 已断开", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info($"Modbus 断开: {_currentDevice.Config.Name}", "DebugToolbox");
                UpdateDeviceInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  视觉触发调试
        // ================================================================
        private async void BtnTriggerVision_Click(object? sender, EventArgs e)
        {
            btnTriggerVision.Enabled = false;
            UpdateVisionStatus("⏳ 正在连接视觉服务器...", Color.Orange);
            try
            {
                var config = new CommandConfig
                {
                    Type = "TriggerVision",
                    VisionServerIp = txtVisionIp.Text.Trim(),
                    VisionServerPort = (int)numVisionPort.Value,
                    VisionTimeoutMs = (int)numVisionTimeout.Value,
                    TriggerCoilAddress = 100,
                    BusyCoilAddress = 101,
                    ResultCoilAddress = 102,
                    ResultCodeRegister = 1000,
                    FileNameRegisterStart = 1001
                };
                var command = new TriggerVisionCommand(_model, _deviceManager, config);
                command.OnLog += msg => AppLogger.Info(msg, "VisionTrigger");
                AppLogger.Info("🔍 手动触发视觉拍照...", "DebugToolbox");
                UpdateVisionStatus("📸 正在触发拍照...", Color.Orange);
                await Task.Run(() => command.Execute(CancellationToken.None));
                UpdateVisionStatus("✅ 视觉拍照成功！", Color.Green);
                MessageBox.Show("✅ 视觉拍照成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppLogger.Info("✅ 视觉拍照成功", "DebugToolbox");
            }
            catch (Exception ex)
            {
                UpdateVisionStatus($"❌ 失败: {ex.Message}", Color.Red);
                MessageBox.Show($"❌ 视觉拍照失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error($"视觉触发失败: {ex.Message}", "DebugToolbox");
            }
            finally
            {
                btnTriggerVision.Enabled = true;
            }
        }

        // ================================================================
        //  串口调试
        // ================================================================
        private void BindSerialEvents()
        {
            if (btnSerialRefresh != null) btnSerialRefresh.Click += (s, e) => RefreshSerialPortList();
            if (btnSerialOpen != null) btnSerialOpen.Click += BtnSerialOpen_Click;
            if (btnSerialClose != null) btnSerialClose.Click += BtnSerialClose_Click;
            if (btnSerialSend != null) btnSerialSend.Click += BtnSerialSend_Click;
            if (btnSerialClearRecv != null) btnSerialClearRecv.Click += (s, e) =>
            {
                txtSerialRecv.Clear();
                _serialRecvBytes = 0;
                UpdateSerialRecvCount();
            };
            if (txtSerialSend != null) txtSerialSend.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Control)
                {
                    BtnSerialSend_Click(s, e);
                    e.SuppressKeyPress = true;
                }
            };
        }

        private void RefreshSerialPortList()
        {
            try
            {
                var ports = SerialPortManager.GetAvailablePorts();
                cmbSerialPort.Items.Clear();
                cmbSerialPort.Items.AddRange(ports);
                if (ports.Length > 0)
                    cmbSerialPort.SelectedIndex = 0;
                else
                    cmbSerialPort.Items.Add("(未检测到串口)");
            }
            catch (Exception ex)
            {
                AppLogger.Warn($"⚠️ 枚举串口失败: {ex.Message}", "DebugToolbox");
            }
        }

        private void BtnSerialOpen_Click(object? sender, EventArgs e)
        {
            if (_serialManager == null) return;

            var portName = cmbSerialPort.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(portName) || portName.StartsWith("("))
            {
                MessageBox.Show("请选择有效串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var config = new SerialPortConfig
            {
                PortName = portName,
                BaudRate = int.TryParse(cmbBaudRate.Text, out var br) ? br : 9600,
                DataBits = int.TryParse(cmbDataBits.Text, out var db) ? db : 8,
                StopBits = cmbStopBits.SelectedIndex switch
                {
                    1 => System.IO.Ports.StopBits.OnePointFive,
                    2 => System.IO.Ports.StopBits.Two,
                    _ => System.IO.Ports.StopBits.One
                },
                Parity = cmbParity.SelectedIndex switch
                {
                    1 => System.IO.Ports.Parity.Odd,
                    2 => System.IO.Ports.Parity.Even,
                    3 => System.IO.Ports.Parity.Mark,
                    4 => System.IO.Ports.Parity.Space,
                    _ => System.IO.Ports.Parity.None
                },
                FrameType = cmbFrameType.SelectedIndex switch
                {
                    1 => SerialFrameType.FixedLength,
                    2 => SerialFrameType.LengthField,
                    3 => SerialFrameType.Delimiter,
                    _ => SerialFrameType.Raw
                },
                Delimiter = ParseHex(txtDelimiterHex.Text) ?? new byte[] { 0x0D, 0x0A }
            };

            _serialDriver = _serialManager.OpenPort(config);
            if (_serialDriver == null)
            {
                MessageBox.Show($"串口打开失败: {portName}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _serialDriver.ConnectionStateChanged += OnSerialConnectionChanged;
            _serialDriver.FrameReceived += OnSerialFrameReceived;
            _serialDriver.RawDataReceived += OnSerialRawReceived;
            _serialDriver.ErrorOccurred += OnSerialError;

            OnSerialConnectionChanged(_serialDriver, _serialDriver.IsOpen);

            btnSerialOpen.Enabled = false;
            btnSerialClose.Enabled = true;
            btnSerialSend.Enabled = true;
            cmbSerialPort.Enabled = false;
            cmbBaudRate.Enabled = false;
            cmbDataBits.Enabled = false;
            cmbStopBits.Enabled = false;
            cmbParity.Enabled = false;
            cmbFrameType.Enabled = false;
            txtDelimiterHex.Enabled = false;
            btnSerialRefresh.Enabled = false;

            AppLogger.Info($"🔌 串口已打开: {portName} @ {config.BaudRate}", "DebugToolbox");
        }

        private void BtnSerialClose_Click(object? sender, EventArgs e)
        {
            if (_serialDriver != null)
            {
                _serialDriver.ConnectionStateChanged -= OnSerialConnectionChanged;
                _serialDriver.FrameReceived -= OnSerialFrameReceived;
                _serialDriver.RawDataReceived -= OnSerialRawReceived;
                _serialDriver.ErrorOccurred -= OnSerialError;
            }
            _serialManager?.ClosePort(_serialDriver?.Config.PortName ?? "");
            _serialDriver = null;

            btnSerialOpen.Enabled = true;
            btnSerialClose.Enabled = false;
            btnSerialSend.Enabled = false;
            cmbSerialPort.Enabled = true;
            cmbBaudRate.Enabled = true;
            cmbDataBits.Enabled = true;
            cmbStopBits.Enabled = true;
            cmbParity.Enabled = true;
            cmbFrameType.Enabled = true;
            txtDelimiterHex.Enabled = true;
            btnSerialRefresh.Enabled = true;

            lblSerialStatus.Text = "● 未连接";
            lblSerialStatus.ForeColor = Color.Gray;
            AppLogger.Info("🔌 串口已关闭", "DebugToolbox");
        }

        private void BtnSerialSend_Click(object? sender, EventArgs e)
        {
            if (_serialDriver == null || !_serialDriver.IsOpen) return;

            string text = txtSerialSend.Text;
            if (string.IsNullOrEmpty(text)) return;

            byte[] data;
            if (chkSerialSendHex.Checked)
            {
                data = ParseHex(text) ?? Array.Empty<byte>();
                if (data.Length == 0)
                {
                    MessageBox.Show("HEX 输入格式错误", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                data = System.Text.Encoding.UTF8.GetBytes(text);
                if (chkSerialAppendCRLF.Checked)
                {
                    var crlf = new byte[] { 0x0D, 0x0A };
                    var combined = new byte[data.Length + crlf.Length];
                    Array.Copy(data, combined, data.Length);
                    Array.Copy(crlf, 0, combined, data.Length, crlf.Length);
                    data = combined;
                }
            }

            if (_serialDriver.Send(data))
            {
                AppendSerialLog("TX", data);
            }
        }

        private void OnSerialConnectionChanged(object? sender, bool open)
        {
            SafeInvoke(() =>
            {
                lblSerialStatus.Text = open ? "● 已连接" : "● 未连接";
                lblSerialStatus.ForeColor = open ? Color.Green : Color.Gray;
            });
        }

        private void OnSerialFrameReceived(object? sender, byte[] frame)
        {
            SafeInvoke(() => AppendSerialLog("RX", frame, isFrame: true));
        }

        private void OnSerialRawReceived(object? sender, byte[] raw)
        {
            _serialRecvBytes += raw.Length;
            SafeInvoke(UpdateSerialRecvCount);
        }

        private void OnSerialError(object? sender, string err)
        {
            SafeInvoke(() => AppLogger.Warn($"⚠️ 串口错误: {err}", "DebugToolbox"));
        }

        private void AppendSerialLog(string tag, byte[] data, bool isFrame = false)
        {
            if (txtSerialRecv == null || txtSerialRecv.IsDisposed) return;

            string content;
            if (chkSerialRecvHex.Checked)
                content = BitConverter.ToString(data).Replace("-", " ");
            else
                content = new string(data.Select(b => b >= 0x20 && b <= 0x7E ? (char)b : '.').ToArray());

            string prefix = isFrame ? "🟢 [帧]" : "⚪ [流]";
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {prefix} {tag}: {content}";

            txtSerialRecv.AppendText(line + Environment.NewLine);
            if (chkSerialAutoScroll.Checked)
                txtSerialRecv.ScrollToCaret();
        }

        private void UpdateSerialRecvCount()
        {
            if (lblSerialRecvCount != null)
                lblSerialRecvCount.Text = $"已收: {_serialRecvBytes} 字节";
        }

        private static byte[]? ParseHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var clean = s.Replace(" ", "").Replace("-", "").Replace(",", "").Replace("0x", "").Replace("0X", "");
            if (clean.Length % 2 != 0) return null;
            try
            {
                var bytes = new byte[clean.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                return bytes;
            }
            catch { return null; }
        }

        // ================================================================
        //  PLC 调试
        // ================================================================
        private void BindPlcEvents()
        {
            if (btnPlcConnect != null) btnPlcConnect.Click += BtnPlcConnect_Click;
            if (btnPlcDisconnect != null) btnPlcDisconnect.Click += BtnPlcDisconnect_Click;
            if (btnPlcRead != null) btnPlcRead.Click += BtnPlcRead_Click;
            if (btnPlcWrite != null) btnPlcWrite.Click += BtnPlcWrite_Click;
            if (btnPlcClearLog != null) btnPlcClearLog.Click += (s, e) => txtPlcLog.Clear();
        }

        private void BtnPlcConnect_Click(object? sender, EventArgs e)
        {
            if (_plcManager == null) return;

            var plcType = cmbPlcType.SelectedIndex switch
            {
                1 => PlcType.SiemensS1200,
                2 => PlcType.SiemensS1500,
                3 => PlcType.SiemensS300,
                4 => PlcType.SiemensS400,
                5 => PlcType.SiemensS200Smart,
                6 => PlcType.MitsubishiMc, // 🆕 三菱
                _ => PlcType.Simulated
            };

            var config = new PlcConfig
            {
                Type = plcType,
                IpAddress = txtPlcIp.Text.Trim(),
                Port = (int)numPlcPort.Value,
                Rack = (short)numPlcRack.Value,
                Slot = (short)numPlcSlot.Value,
                Name = txtPlcName.Text.Trim()
            };

            _plcClient = _plcManager.CreateClient(config);
            if (_plcClient == null)
            {
                MessageBox.Show("创建 PLC 客户端失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _plcClient.ConnectionStateChanged += OnPlcConnectionChanged;
            _plcClient.ErrorOccurred += OnPlcError;

            OnPlcConnectionChanged(_plcClient, _plcClient.IsConnected);

            if (_plcClient.IsConnected)
            {
                btnPlcConnect.Enabled = false;
                btnPlcDisconnect.Enabled = true;
                cmbPlcType.Enabled = false;
                txtPlcIp.Enabled = false;
                numPlcPort.Enabled = false;
                numPlcRack.Enabled = false;
                numPlcSlot.Enabled = false;
                txtPlcName.Enabled = false;

                AppendPlcLog($"✅ PLC [{config.Name}] 已连接 ({config.Type} @ {config.IpAddress})");
            }
            else
            {
                MessageBox.Show($"PLC 连接失败: {config.IpAddress}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendPlcLog($"❌ PLC [{config.Name}] 连接失败");
            }
        }

        private void BtnPlcDisconnect_Click(object? sender, EventArgs e)
        {
            if (_plcClient != null)
            {
                _plcClient.ConnectionStateChanged -= OnPlcConnectionChanged;
                _plcClient.ErrorOccurred -= OnPlcError;
            }
            _plcManager?.RemoveClient(_plcClient?.Config.Name ?? "");
            _plcClient = null;

            btnPlcConnect.Enabled = true;
            btnPlcDisconnect.Enabled = false;
            cmbPlcType.Enabled = true;
            txtPlcIp.Enabled = true;
            numPlcPort.Enabled = true;
            numPlcRack.Enabled = true;
            numPlcSlot.Enabled = true;
            txtPlcName.Enabled = true;

            lblPlcStatus.Text = "● 未连接";
            lblPlcStatus.ForeColor = Color.Gray;
            AppendPlcLog("🔌 PLC 已断开");
        }

        private void BtnPlcRead_Click(object? sender, EventArgs e)
        {
            if (_plcClient == null || !_plcClient.IsConnected)
            {
                MessageBox.Show("PLC 未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addr = txtPlcReadAddr.Text.Trim();
            if (string.IsNullOrEmpty(addr))
            {
                MessageBox.Show("请输入地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string result = "";
            bool ok = false;

            try
            {
                switch (cmbPlcReadType.SelectedIndex)
                {
                    case 0: if (_plcClient.ReadBool(addr, out bool bv)) { result = bv.ToString(); ok = true; } break;
                    case 1: if (_plcClient.ReadShort(addr, out short sv)) { result = sv.ToString(); ok = true; } break;
                    case 2: if (_plcClient.ReadUShort(addr, out ushort usv)) { result = usv.ToString(); ok = true; } break;
                    case 3: if (_plcClient.ReadInt(addr, out int iv)) { result = iv.ToString(); ok = true; } break;
                    case 4: if (_plcClient.ReadUInt(addr, out uint uiv)) { result = uiv.ToString(); ok = true; } break;
                    case 5: if (_plcClient.ReadFloat(addr, out float fv)) { result = fv.ToString("F4"); ok = true; } break;
                    case 6: if (_plcClient.ReadDouble(addr, out double dv)) { result = dv.ToString("F4"); ok = true; } break;
                    case 7: if (_plcClient.ReadString(addr, 32, out string strv)) { result = strv; ok = true; } break;
                }
            }
            catch (Exception ex)
            {
                AppendPlcLog($"❌ 读异常: {ex.Message}");
                return;
            }

            if (ok)
                AppendPlcLog($"📥 [{addr}] ({cmbPlcReadType.Text}) = {result}");
            else
                AppendPlcLog($"⚠️ 读取失败 [{addr}]");
        }

        private void BtnPlcWrite_Click(object? sender, EventArgs e)
        {
            if (_plcClient == null || !_plcClient.IsConnected)
            {
                MessageBox.Show("PLC 未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string addr = txtPlcWriteAddr.Text.Trim();
            string valStr = txtPlcWriteValue.Text.Trim();
            if (string.IsNullOrEmpty(addr)) return;

            bool ok = false;
            try
            {
                switch (cmbPlcWriteType.SelectedIndex)
                {
                    case 0: ok = _plcClient.WriteBool(addr, valStr == "1" || valStr.ToLower() == "true"); break;
                    case 1: ok = _plcClient.WriteShort(addr, short.Parse(valStr)); break;
                    case 2: ok = _plcClient.WriteUShort(addr, ushort.Parse(valStr)); break;
                    case 3: ok = _plcClient.WriteInt(addr, int.Parse(valStr)); break;
                    case 4: ok = _plcClient.WriteUInt(addr, uint.Parse(valStr)); break;
                    case 5: ok = _plcClient.WriteFloat(addr, float.Parse(valStr)); break;
                    case 6: ok = _plcClient.WriteDouble(addr, double.Parse(valStr)); break;
                    case 7: ok = _plcClient.WriteString(addr, valStr); break;
                }
            }
            catch (Exception ex)
            {
                AppendPlcLog($"❌ 写异常: {ex.Message}");
                return;
            }

            if (ok)
                AppendPlcLog($"📤 [{addr}] ({cmbPlcWriteType.Text}) ← {valStr}");
            else
                AppendPlcLog($"⚠️ 写入失败 [{addr}]");
        }

        private void OnPlcConnectionChanged(object? sender, bool connected)
        {
            SafeInvoke(() =>
            {
                lblPlcStatus.Text = connected ? "● 已连接" : "● 未连接";
                lblPlcStatus.ForeColor = connected ? Color.Green : Color.Gray;
            });
        }

        private void OnPlcError(object? sender, string err)
        {
            SafeInvoke(() => AppendPlcLog($"⚠️ PLC 错误: {err}"));
        }

        private void AppendPlcLog(string msg)
        {
            if (txtPlcLog == null || txtPlcLog.IsDisposed) return;
            SafeInvoke(() =>
            {
                txtPlcLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
                txtPlcLog.SelectionStart = txtPlcLog.TextLength;
                txtPlcLog.ScrollToCaret();
            });
        }

        // ================================================================
        //  通用辅助
        // ================================================================
        private void SafeInvoke(Action action)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
                this.BeginInvoke(action);
            else
                action();
        }

        // ================================================================
        //  窗体关闭
        // ================================================================
        private void DebugToolboxForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            timerRefresh.Stop();

            // 释放串口
            try
            {
                if (_serialDriver != null)
                {
                    _serialDriver.ConnectionStateChanged -= OnSerialConnectionChanged;
                    _serialDriver.FrameReceived -= OnSerialFrameReceived;
                    _serialDriver.RawDataReceived -= OnSerialRawReceived;
                    _serialDriver.ErrorOccurred -= OnSerialError;
                }
                _serialManager?.CloseAll();
                _serialManager?.Dispose();
                _serialManager = null;
                _serialDriver = null;
            }
            catch { }

            // 释放 PLC
            try
            {
                if (_plcClient != null)
                {
                    _plcClient.ConnectionStateChanged -= OnPlcConnectionChanged;
                    _plcClient.ErrorOccurred -= OnPlcError;
                }
                // 注意：不调用 _plcManager.CloseAll()，因为是单例，会影响其他界面
                _plcClient = null;
            }
            catch { }

            AppLogger.Info($"调试工具箱已关闭", "DebugToolbox");
        }
    }
}