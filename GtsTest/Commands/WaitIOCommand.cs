using GtsTest.Core;
using GtsTest.Models;
using System;

namespace GtsTest.Commands
{
    public class WaitIOCommand : MotionCommandBase
    {
        private readonly int _ioIndex;
        private readonly bool _expectValue;
        private readonly DeviceRuntime? _runtime; // 🆕 接收 DeviceRuntime

        public WaitIOCommand(GtsModel model, DeviceRuntime? runtime, int ioIndex, bool expectValue) : base(model)
        {
            Name = $"等待 IO[{ioIndex}] == {expectValue}";
            _ioIndex = ioIndex;
            _expectValue = expectValue;
            _runtime = runtime;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            bool matched = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (!matched && sw.ElapsedMilliseconds < 5000)
            {
                ct.ThrowIfCancellationRequested();

                // 🆕 使用带强制模拟的读取（如果 _runtime 有强制值，优先取强制值）
                bool current = _model.ReadDIWithForce(_ioIndex, _runtime);
                matched = (current == _expectValue);
                Thread.Sleep(50);
            }

            if (!matched) throw new TimeoutException($"等待 IO[{_ioIndex}] 超时 (5s)");
        }
    }
}