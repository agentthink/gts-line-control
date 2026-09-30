namespace GtsTest.Core
{
    /// <summary>
    /// 一次工作流周期的执行结果
    /// </summary>
    public class WorkflowCycleResult
    {
        /// <summary>是否成功完成</summary>
        public bool Success { get; set; }

        /// <summary>是否失败（子指令失败 / 抛异常）</summary>
        public bool IsFaulted { get; set; }

        /// <summary>失败原因（IsFaulted=true 时有效）</summary>
        public string FaultReason { get; set; } = "";

        /// <summary>失败时应该从哪个步骤恢复（断点）</summary>
        public int StepIndex { get; set; }
    }
}