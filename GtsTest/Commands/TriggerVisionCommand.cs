using GtsTest.Core;
using GtsTest.Modbus;
using System;
using System.Diagnostics;
using System.Threading;

namespace GtsTest.Commands
{
    /// <summary>
    /// 触发视觉拍照命令
    /// 通过 Modbus 触发视觉服务器拍照，并等待结果
    /// 
    /// ⭐ 结果码语义：
    ///   0  = OK（检测通过）
    ///   3  = NG（业务结果：未检测到目标 / 尺寸超差）→ 不抛异常，工作流继续
    ///   1  = 系统内部错误 → 抛异常
    ///   2  = 相机未连接 → 抛异常
    ///   99 = 系统未知错误 → 抛异常
    /// </summary>
    public class TriggerVisionCommand : MotionCommandBase
    {
        private readonly CommandConfig _config;
        private readonly DeviceManager _deviceManager;
        private ModbusClient? _modbusClient;

        public bool? VisionPass { get; private set; }
        public double DiameterMm { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public int DefectCount { get; private set; }

        public TriggerVisionCommand(GtsModel model, DeviceManager deviceManager, CommandConfig config)
            : base(model)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            Name = $"触发视觉拍照 ({config.VisionServerIp}:{config.VisionServerPort})";
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            // ============================================================
            // 1. 创建 Modbus 客户端
            // ============================================================
            var modbusConfig = new ModbusConfig
            {
                Protocol = ModbusProtocol.Tcp,
                IpAddress = _config.VisionServerIp,
                Port = _config.VisionServerPort,
                SlaveAddress = 1,
                TimeoutMs = 3000,
                AddressType = AddressType.Coil,   // 视觉服务器忙状态/结果都是线圈
                DataType = DataType.UInt16        // 用 UInt16 明确类型
            };

            _modbusClient = new ModbusClient(modbusConfig);
            _modbusClient.LogMessage += msg => Log($"Modbus: {msg}");

            try
            {
                // ============================================================
                // 2. 连接视觉服务器
                // ============================================================
                Log($"正在连接视觉服务器 {_config.VisionServerIp}:{_config.VisionServerPort}...");
                if (!_modbusClient.Connect())
                    throw new Exception($"无法连接到视觉服务器 {_config.VisionServerIp}:{_config.VisionServerPort}");
                Log($"✅ 视觉服务器连接成功");

                // ============================================================
                // 3. 检查忙状态（读线圈）
                // ============================================================
                ct.ThrowIfCancellationRequested();

                bool isBusy = true;
                int retryCount = 0;
                while (isBusy && retryCount < 3)
                {
                    var busyResult = _modbusClient.ReadDataByType((ushort)_config.BusyCoilAddress, 1);
                    if (busyResult?.RawRegisters?.Length > 0)
                    {
                        isBusy = busyResult.RawRegisters[0] == 1;
                        if (isBusy)
                        {
                            Log($"⚠️ 视觉服务器正忙，等待 1 秒后重试... (尝试 {retryCount + 1}/3)");
                            Thread.Sleep(1000);
                            ct.ThrowIfCancellationRequested();
                            retryCount++;
                        }
                    }
                    else
                    {
                        Log($"⚠️ 读取忙状态失败，等待 500ms 后重试...");
                        Thread.Sleep(500);
                        retryCount++;
                    }
                }

                if (isBusy)
                    throw new Exception("视觉服务器正忙，请稍后重试");

                // ============================================================
                // 4. 写入触发信号
                // ============================================================
                ct.ThrowIfCancellationRequested();
                Log($"📸 发送触发信号 (线圈 {_config.TriggerCoilAddress} = 1)");

                bool triggerSuccess = _modbusClient.WriteSingleCoil((ushort)_config.TriggerCoilAddress, true);
                if (!triggerSuccess)
                    throw new Exception("触发信号写入失败");

                // ============================================================
                // 5. 等待视觉完成
                // ============================================================
                Log($"⏳ 等待视觉服务器处理... (超时 {_config.VisionTimeoutMs}ms)");

                var sw = Stopwatch.StartNew();
                bool isBusyState = true;
                int pollCount = 0;

                while (isBusyState && sw.ElapsedMilliseconds < _config.VisionTimeoutMs)
                {
                    ct.ThrowIfCancellationRequested();

                    var status = _modbusClient.ReadDataByType((ushort)_config.BusyCoilAddress, 1);
                    if (status?.RawRegisters?.Length > 0)
                    {
                        isBusyState = status.RawRegisters[0] == 1;
                        pollCount++;
                        if (pollCount % 20 == 0)
                            Log($"⏳ 等待视觉完成... ({sw.ElapsedMilliseconds}ms)");
                    }
                    else
                    {
                        Log($"⚠️ 读取忙状态失败，尝试重连...");
                        _modbusClient.Disconnect();
                        Thread.Sleep(200);
                        if (!_modbusClient.Connect())
                            throw new Exception("与视觉服务器通信中断");
                    }

                    Thread.Sleep(50);
                }

                if (isBusyState)
                    throw new TimeoutException($"视觉拍照超时 ({_config.VisionTimeoutMs}ms)");

                // ============================================================
                // 6. 读取结果（★ 用 Raw，绕过 ConvertRawToType 的字节交换）
                // ============================================================

                // ---- 6.1 结果码寄存器（地址 1000 = 0x03E8）----
                var codeRead = _modbusClient.ReadHoldingRegistersWithRaw((ushort)_config.ResultCodeRegister, 1);
                int code = 999;
                if (codeRead?.RawRegisters?.Length > 0)
                {
                    code = codeRead.RawRegisters[0];
                    Log($"📋 结果码 = {code} (0x{code:X4})");
                }

                // ---- 6.2 直径（地址 1003），单位 0.01mm ----
                var diaRead = _modbusClient.ReadHoldingRegistersWithRaw((ushort)1003, 1);
                if (diaRead?.RawRegisters?.Length > 0)
                    DiameterMm = diaRead.RawRegisters[0] / 100.0;

                // ---- 6.3 缺陷数（地址 1004）----
                var defRead = _modbusClient.ReadHoldingRegistersWithRaw((ushort)1004, 1);
                if (defRead?.RawRegisters?.Length > 0)
                    DefectCount = defRead.RawRegisters[0];

                // ---- 6.4 X 坐标（地址 1005），单位 0.01mm，有符号 ----
                var xRead = _modbusClient.ReadHoldingRegistersWithRaw((ushort)1005, 1);
                if (xRead?.RawRegisters?.Length > 0)
                    X = (short)xRead.RawRegisters[0] / 100.0;

                // ---- 6.5 Y 坐标（地址 1006），单位 0.01mm，有符号 ----
                var yRead = _modbusClient.ReadHoldingRegistersWithRaw((ushort)1006, 1);
                if (yRead?.RawRegisters?.Length > 0)
                    Y = (short)yRead.RawRegisters[0] / 100.0;

                // ============================================================
                // 7. 结果判定
                // ============================================================
                if (code == 0)
                {
                    VisionPass = true;
                    Log($"✅ 视觉执行成功 (结果码: 0)");
                    Log($"📏 直径: {DiameterMm:F2} mm");
                    Log($"🔍 缺陷数: {DefectCount}");
                    Log($"📍 X 坐标: {X:F2}");
                    Log($"📍 Y 坐标: {Y:F2}");
                    SaveResultToContext(true);
                }
                else if (code == 3)
                {
                    VisionPass = false;
                    Log($"⚠️ 视觉结果: NG（未检测到目标或尺寸超差）(结果码: 3)");

                    var coilResult = _modbusClient.ReadDataByType((ushort)_config.ResultCoilAddress, 1);
                    bool coil = coilResult?.RawRegisters?.Length > 0 && coilResult.RawRegisters[0] == 1;
                    Log($"   ResultCoil = {coil}");

                    SaveResultToContext(false);
                    // 不抛异常，工作流继续
                }
                else
                {
                    string errorMsg = code switch
                    {
                        1 => "视觉系统内部错误",
                        2 => "相机未连接",
                        99 => "视觉系统未知错误",
                        _ => $"错误码 {code}"
                    };
                    throw new Exception($"视觉系统故障: {errorMsg} (code={code})");
                }

                Log($"✅ 视觉触发完成");
            }
            catch (OperationCanceledException)
            {
                Log($"⚠️ 视觉触发被取消");
                throw;
            }
            catch (Exception ex)
            {
                Log($"❌ 视觉触发失败: {ex.Message}");
                throw;
            }
            finally
            {
                if (_modbusClient != null)
                {
                    try
                    {
                        _modbusClient.Disconnect();
                        _modbusClient.Dispose();
                    }
                    catch { }
                    _modbusClient = null;
                }
                Log($"🔌 视觉服务器连接已断开");
            }
        }

        private void SaveResultToContext(bool pass)
        {
            try
            {
                foreach (var dev in _deviceManager.GetAllDevices())
                {
                    if (dev.IsRunning)
                    {
                        dev.WorkflowContext["LastVisionPass"] = pass;
                        dev.WorkflowContext["LastDiameter"] = DiameterMm;
                        dev.WorkflowContext["LastX"] = X;
                        dev.WorkflowContext["LastY"] = Y;
                        dev.WorkflowContext["LastDefect"] = DefectCount;
                        break;
                    }
                }
            }
            catch { }
        }

        public override void Stop()
        {
            Log($"🛑 收到停止信号，立即断开视觉服务器连接");
            try
            {
                _modbusClient?.Disconnect();
                _modbusClient?.Dispose();
                _modbusClient = null;
            }
            catch { }
            base.Stop();
        }
    }
}