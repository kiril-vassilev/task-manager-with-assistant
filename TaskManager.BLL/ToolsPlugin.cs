using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace TaskManager.BLL;

public sealed class ToolsPlugin
{
    public ToolsPlugin()
    {
    }

    [KernelFunction, Description("It returns today's date.")]
    public String Today() => DateTime.UtcNow.Date.ToShortDateString();
}
    