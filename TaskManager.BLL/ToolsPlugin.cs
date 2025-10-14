using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace TaskManager.BLL;

public sealed class ToolsPlugin
{
    private readonly AgentTaskService _agentTaskService;

    public ToolsPlugin(AgentTaskService agentTaskService)
    {
        _agentTaskService = agentTaskService;
    }

    [KernelFunction, Description("It returns today's date.")]
    public String Today() => DateTime.UtcNow.Date.ToShortDateString();

    [KernelFunction, Description("It clears the chat history and the context.")]
    public string Clear()
    {
        _agentTaskService.CreateClearHistory();
        return "Hello! How can I help you?";
    }
}
    