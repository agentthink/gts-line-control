using GtsTest.Models;
using System;

namespace GtsTest.Core
{
    /// <summary>
    /// 工作流执行引擎接口：
    /// 把"加载工作流 / 组装命令链 / 执行 / 结果收集"从 DeviceManager 中剥离
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// 确保工作流已加载到 DeviceRuntime（惰性加载，已加载则跳过）
        /// </summary>
        void EnsureWorkflowLoaded(DeviceRuntime runtime);

        /// <summary>
        /// 执行一次完整工作流周期（同步阻塞，由调用方 Task 承载）
        /// </summary>
        /// <param name="runtime">设备运行时上下文</param>
        /// <param name="ct">取消令牌</param>
        /// <param name="onStepChanged">步骤变化回调（用于 UI 更新）</param>
        WorkflowCycleResult ExecuteCycle(
            DeviceRuntime runtime,
            System.Threading.CancellationToken ct,
            Action<string>? onStepChanged = null);
    }
}