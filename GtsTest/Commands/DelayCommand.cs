using GtsTest.Core;
using System;

namespace GtsTest.Commands
{
    public class DelayCommand : MotionCommandBase
    {
        private readonly int _delayMs;

        public DelayCommand(GtsModel model, int delayMs) : base(model)
        {
            Name = $"延时 {delayMs} ms";
            _delayMs = delayMs;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            if (_delayMs > 0)
                Task.Delay(_delayMs, ct).Wait(ct);
        }
    }
}