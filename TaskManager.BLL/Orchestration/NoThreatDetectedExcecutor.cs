using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class NoThreatDetectedExecutor() : Executor<GuardianResponse, ChatMessage>("NoThreatDetectedExecutor")
{
    public override async ValueTask<ChatMessage> HandleAsync(GuardianResponse guardianResponse, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (guardianResponse.IsThreatDetected)
            throw new InvalidOperationException("NoThreatDetectedExecutor invoked but threat was detected.");

        // it is safe so pass the message to the worker
        var originalQuestion = await context.ReadStateAsync<ChatMessage>("OriginalQuestion", scopeName: TaskManagerConfiguration.defaultWorkflowMessageScope, cancellationToken)
            ?? throw new InvalidOperationException("Original question not found in workflow state.");           

        return originalQuestion;
    }

}