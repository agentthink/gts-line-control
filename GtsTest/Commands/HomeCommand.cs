using GtsTest.Core;
using System;

namespace GtsTest.Commands
{
    public class HomeCommand : MotionCommandBase
    {
        private readonly short _axis;
        private readonly int _homePos;

        public HomeCommand(GtsModel model, short axis, int homePos = 0) : base(model)
        {
            Name = $"轴{axis} 回零 (目标: {homePos})";
            _axis = axis;
            _homePos = homePos;
        }

        protected override void ExecuteCore(CancellationToken ct)
        {
            // 1. 调用 Model 的回零方法
            short rt = _model.HomeAxis(_axis, _homePos);
            if (rt != 0) throw new Exception($"回零启动失败, 错误码: {rt}");

            // 2. 轮询等待回零完成 (超时 10 秒)
            bool done = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!done && sw.ElapsedMilliseconds < 10000)
            {
                ct.ThrowIfCancellationRequested();
                done = _model.CheckHomeDone(_axis);
                Thread.Sleep(20);
            }
            if (!done) throw new TimeoutException("回零操作超时 (10s)");
        }

        public override void Stop()
        {
            // 回零过程急停：调用 GT_Stop 停止该轴
            // 注意：真实场景需传入 mask，简化略
            Log("收到急停信号，停止回零");
        }
    }
}