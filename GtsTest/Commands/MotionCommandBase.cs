using GtsTest.Core;
using System.Diagnostics;

namespace GtsTest.Commands
{
    public abstract class MotionCommandBase : IMotionCommand
    {
        protected readonly GtsModel _model;
        public string Name { get; protected set; } = "未知指令";
        public bool IsCompleted { get; protected set; }
        public bool IsFaulted { get; protected set; }
        public string FaultReason { get; protected set; } = "";
        public event Action<string> OnLog = delegate { };

        protected MotionCommandBase(GtsModel model) => _model = model;

        // 提供给子类的日志方法
        protected void Log(string msg) => OnLog?.Invoke($"[{Name}] {msg}");

        // 核心执行骨架（模板方法）
        public void Execute(CancellationToken ct)
        {
            IsCompleted = false;
            IsFaulted = false;
            FaultReason = "";
            try
            {
                Log("开始执行...");
                ExecuteCore(ct); // 调用子类实现的具体逻辑
                Log("执行成功 ✅");
                IsCompleted = true;
            }
            catch (OperationCanceledException)
            {
                Log("已被用户取消 ⚠️");
                IsFaulted = true;
                FaultReason = "用户取消";
            }
            catch (Exception ex)
            {
                Log($"执行失败 ❌: {ex.Message}");
                IsFaulted = true;
                FaultReason = ex.Message;
            }
        }

        // 抽象方法，由具体命令实现（如回零、定位等）
        protected abstract void ExecuteCore(CancellationToken ct);

        // 急停接口（子类可重写）
        public virtual void Stop() { }

        // 辅助方法：轮询等待条件（带超时），避免占用100% CPU
        protected void WaitForCondition(Func<bool> condition, int timeoutMs, CancellationToken ct, int pollIntervalMs = 20)
        {
            var sw = Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(pollIntervalMs);
            }
            if (!condition()) throw new TimeoutException($"等待超时 ({timeoutMs}ms)");
        }
    }
}