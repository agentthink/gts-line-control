namespace GtsTest.Commands
{
    /// <summary>
    /// 序列指令：按顺序执行一组子指令，任一失败则整体停止
    /// </summary>
    public class SequenceCommand : IMotionCommand
    {
        private readonly List<IMotionCommand> _commands = new();
        public string Name => "工作流序列";
        public bool IsCompleted { get; private set; }
        public bool IsFaulted { get; private set; }
        public string FaultReason { get; private set; } = "";
        public event Action<string> OnLog = delegate { };

        public SequenceCommand(params IMotionCommand[] commands)
        {
            _commands.AddRange(commands);
            foreach (var cmd in _commands)
            {
                cmd.OnLog += msg => OnLog?.Invoke(msg);
            }
        }

        public void Execute(CancellationToken ct)
        {
            IsCompleted = false;
            IsFaulted = false;
            try
            {
                foreach (var cmd in _commands)
                {
                    if (ct.IsCancellationRequested) break;
                    cmd.Execute(ct);
                    if (cmd.IsFaulted)
                    {
                        IsFaulted = true;
                        FaultReason = $"子指令 [{cmd.Name}] 失败: {cmd.FaultReason}";
                        break;
                    }
                }
                if (!IsFaulted) IsCompleted = true;
            }
            catch (Exception ex)
            {
                IsFaulted = true;
                FaultReason = ex.Message;
            }
        }

        /// <summary>
        /// 🆕 增强 Stop：向下传播给所有子命令
        /// </summary>
        public void Stop()
        {
            foreach (var cmd in _commands)
            {
                try { cmd.Stop(); }
                catch { /* 忽略单个命令停止异常 */ }
            }
        }
    }
}