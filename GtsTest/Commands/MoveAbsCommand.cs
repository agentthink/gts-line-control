using GtsTest.Core;
using System;

namespace GtsTest.Commands
{
    public class MoveAbsCommand : MotionCommandBase
    {
        private readonly short _axis;
        private readonly int _targetPos;
        private readonly double _vel;
        private readonly double _acc;

        public MoveAbsCommand(GtsModel model, short axis, int targetPos, double vel = 10, double acc = 5) : base(model)
        {
            Name = $"轴{axis} 定位到 {targetPos}";
            _axis = axis;
            _targetPos = targetPos;
            _vel = vel;
            _acc = acc;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            // 1. 启动绝对定位
            short rt = _model.MoveAbs(_axis, _targetPos, _vel, _acc);
            if (rt != 0) throw new Exception($"定位启动失败, 错误码: {rt}");

            // 2. 等待到达目标位置 (允许 ±5 误差)
            double currentPos = 0;
            uint clk = 0;
            bool arrived = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!arrived && sw.ElapsedMilliseconds < 5000)
            {
                ct.ThrowIfCancellationRequested();//响应退出
                _model.GetPrfPos(_axis, out currentPos, out clk);
                arrived = Math.Abs(currentPos - _targetPos) < 5;
                Thread.Sleep(20);
            }
            if (!arrived) throw new TimeoutException($"定位到 {_targetPos} 超时 (5s), 当前停在 {currentPos:F1}");
        }
    }
}