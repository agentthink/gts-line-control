using GtsTest.Core;

//向目标设备写线圈
namespace GtsTest.Commands
{
    public class WriteSignalCommand : MotionCommandBase
    {
        private readonly DeviceManager _deviceManager;
        private readonly string _targetDeviceId;
        private readonly int _address;
        private readonly bool _value;

        public WriteSignalCommand(GtsModel model, DeviceManager deviceManager,
                                  string targetDeviceId, int address, bool value)
            : base(model)
        {
            Name = $"向 {targetDeviceId} 写线圈 [{address}] = {value}";
            _deviceManager = deviceManager;
            _targetDeviceId = targetDeviceId;
            _address = address;
            _value = value;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            var device = _deviceManager.GetDevice(_targetDeviceId);
            if (device == null)
                throw new Exception($"目标设备 [{_targetDeviceId}] 不存在");

            if (!device.ModbusClient.IsConnected)
                throw new Exception($"目标设备 [{_targetDeviceId}] Modbus 未连接");

            bool success = device.ModbusClient.WriteSingleCoil((ushort)_address, _value);
            if (!success)
                throw new Exception($"向 {_targetDeviceId} 写线圈 {_address} 失败");
        }
    }
}