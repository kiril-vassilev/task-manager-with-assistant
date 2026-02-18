using System.ComponentModel;
using TaskManager.BLL.Orchestration;

namespace TaskManager.BLL;

public sealed class ToolsPlugin
{
    private readonly AgentService _agentService;

    public ToolsPlugin(AgentService agentService) => _agentService = agentService;

    public String Today() => DateTime.UtcNow.Date.ToShortDateString();

    public async Task<string> ClearAsync()
    {
        await _agentService.CreateClearHistoryAsync();
        return "The history and context have been cleared.";
    }
}
    