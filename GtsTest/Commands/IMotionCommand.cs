using System;

namespace GtsTest.Commands
{
    /// <summary>
    /// 运动指令接口
    /// </summary>
    public interface IMotionCommand
    {
        string Name { get; }
        bool IsCompleted { get; }//是否干完了（成功标志）
        bool IsFaulted { get; }//是否干砸了（失败标志）
        string FaultReason { get; }//如果干砸了，具体是超时还是撞限位了（报错原因）
        event Action<string> OnLog; // 实时输出日志到界面

        void Execute(CancellationToken ct); // 执行核心逻辑
        void Stop(); // 急停或中断
    }
}