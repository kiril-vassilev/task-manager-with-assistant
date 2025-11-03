using Microsoft.Agents.AI.Workflows;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class ThreatDetectedExecutor() : Executor<GuardianResponse, AskResponse>("ThreatDetectedExecutor")
{
    public override ValueTask<AskResponse> HandleAsync(GuardianResponse message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {

        // Pass the guard agent's response to the user, protecting the worker agent.
        return ValueTask.FromResult(
            new AskResponse
            {
                Answer = "WARNING: " + message.Answer,
                Tasks = []
            });
    }

}