using System.ComponentModel;
using TaskManager.BLL.Orchestration;

namespace TaskManager.BLL;

public sealed class ToolsPlugin
{
    private readonly AgentService _agentService;

    public ToolsPlugin(AgentService agentService) => _agentService = agentService;

    [Description("It returns today's date.")]
    public String Today() => DateTime.UtcNow.Date.ToShortDateString();

    [Description("It clears the chat history and the context.")]
    public string Clear()
    {
        _agentService.CreateClearHistory();
        return "The history and context have been cleared.";
    }
}
    