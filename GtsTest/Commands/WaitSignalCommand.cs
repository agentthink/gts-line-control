using GtsTest.Core;

//等待目标设备的线圈状态
namespace GtsTest.Commands
{
    public class WaitSignalCommand : MotionCommandBase
    {
        private readonly DeviceManager _deviceManager;
        private readonly string _targetDeviceId;
        private readonly int _address;
        private readonly bool _expectValue;

        public WaitSignalCommand(GtsModel model, DeviceManager deviceManager,
                                 string targetDeviceId, int address, bool expectValue)
            : base(model)
        {
            Name = $"等待 {targetDeviceId} 线圈 [{address}] = {expectValue}";
            _deviceManager = deviceManager;
            _targetDeviceId = targetDeviceId;
            _address = address;
            _expectValue = expectValue;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            var device = _deviceManager.GetDevice(_targetDeviceId);
            if (device == null)
                throw new Exception($"目标设备 [{_targetDeviceId}] 不存在");

            bool matched = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!matched && sw.ElapsedMilliseconds < 10000) // 10秒超时
            {
                ct.ThrowIfCancellationRequested();

                if (!device.ModbusClient.IsConnected)
                    throw new Exception($"目标设备 [{_targetDeviceId}] 连接断开");

                // 读取线圈值
                var result = device.ModbusClient.ReadDataByType((ushort)_address, 1);
                if (result?.RawRegisters != null && result.RawRegisters.Length > 0)
                {
                    bool current = result.RawRegisters[0] == 1;
                    matched = (current == _expectValue);
                }
                Thread.Sleep(50);
            }
            if (!matched)
                throw new TimeoutException($"等待 {_targetDeviceId} 线圈 [{_address}] 超时 (10s)");
        }
    }
}