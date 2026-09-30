using System.Collections.Generic;

namespace GtsTest.Commands
{
    public class WorkflowConfig
    {
        public string Name { get; set; } = "未命名流程";
        public string Description { get; set; } = "";
        public List<CommandConfig> Commands { get; set; } = new List<CommandConfig>();
    }
}