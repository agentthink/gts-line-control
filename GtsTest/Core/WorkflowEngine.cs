using GtsTest.Commands;
using GtsTest.Models;
using GtsTest.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GtsTest.Core
{
    /// <summary>
    /// 工作流执行引擎
    /// 职责：加载工作流 → 组装命令链 → 执行 → 收集结果
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly GtsModel _model;
        private readonly DeviceManager _deviceManager;
        private readonly ILogger _logger;

        public WorkflowEngine(GtsModel model, DeviceManager deviceManager, ILogger logger)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _logger = logger ?? new AppLoggerWrapper();
        }

        public void EnsureWorkflowLoaded(DeviceRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.CurrentWorkflow != null) return;
            if (string.IsNullOrEmpty(runtime.CurrentWorkflowName)) return;

            runtime.CurrentWorkflow = LoadWorkflowFromFile(runtime.CurrentWorkflowName);
        }

        private WorkflowConfig LoadWorkflowFromFile(string workflowName)
        {
            if (string.IsNullOrEmpty(workflowName)) workflowName = "Default";

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workflows", workflowName + ".json");
            if (!File.Exists(path))
            {
                _logger.Warn($"工作流文件不存在: {path}，返回空流程", "WorkflowEngine");
                return new WorkflowConfig { Name = "Empty", Commands = new List<CommandConfig>() };
            }
            try
            {
                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<WorkflowConfig>(json);
                if (config == null)
                {
                    _logger.Warn($"工作流文件 {path} 解析失败", "WorkflowEngine");
                    return new WorkflowConfig { Name = "Invalid", Commands = new List<CommandConfig>() };
                }
                return config;
            }
            catch (Exception ex)
            {
                _logger.Error($"加载工作流失败: {ex.Message}", "WorkflowEngine");
                return new WorkflowConfig { Name = "Error", Commands = new List<CommandConfig>() };
            }
        }

        public WorkflowCycleResult ExecuteCycle(
            DeviceRuntime runtime,
            CancellationToken ct,
            Action<string>? onStepChanged = null)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));

            var workflow = runtime.CurrentWorkflow;
            if (workflow == null || workflow.Commands.Count == 0)
            {
                return new WorkflowCycleResult { Success = true };
            }

            int startIndex = runtime.CurrentStepIndex;
            if (startIndex >= workflow.Commands.Count)
            {
                startIndex = 0;
                runtime.CurrentStepIndex = 0;
            }

            // 🆕 传入 runtime，支持 IO 强制
            var commands = workflow.Commands
                .Skip(startIndex)
                .Select(cfg => CommandFactory.Create(_model, runtime, _deviceManager, cfg))
                .ToList();

            if (commands.Count == 0)
            {
                runtime.CurrentStepIndex = 0;
                return new WorkflowCycleResult { Success = true };
            }

            var sequence = new SequenceCommand(commands.ToArray());
            runtime.CurrentCommand = sequence;

            int currentStepIdx = 0;
            sequence.OnLog += msg =>
            {
                if (currentStepIdx < commands.Count)
                {
                    var cmd = commands[currentStepIdx];
                    runtime.CurrentStep = cmd.Name;
                    onStepChanged?.Invoke(cmd.Name);
                }
                _logger.Info(msg, "Workflow");
            };

            _logger.Info($"设备 [{runtime.Config.Name}] 开始执行工作流周期 (产量 {runtime.Config.CurrentCount}/{runtime.Config.TargetCount})", "WorkflowEngine");
            sequence.Execute(ct);

            if (sequence.IsFaulted)
            {
                return new WorkflowCycleResult
                {
                    Success = false,
                    IsFaulted = true,
                    FaultReason = sequence.FaultReason,
                    StepIndex = startIndex
                };
            }

            return new WorkflowCycleResult
            {
                Success = true,
                IsFaulted = false,
                StepIndex = 0
            };
        }
    }
}