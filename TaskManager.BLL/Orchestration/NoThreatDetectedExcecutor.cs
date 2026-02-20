using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class NoThreatDetectedExecutor() : Executor<GuardianResponse>("NoThreatDetectedExecutor")
{
    public override async ValueTask HandleAsync(GuardianResponse guardianResponse, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (guardianResponse.IsThreatDetected)
            throw new InvalidOperationException("NoThreatDetectedExecutor invoked but threat was detected.");

        // it is safe so send the message to the worker
        var originalQuestion = await context.ReadStateAsync<ChatMessage>("OriginalQuestion", scopeName: TaskManagerConfiguration.defaultWorkflowMessageScope, cancellationToken)
            ?? throw new InvalidOperationException("Original question not found in workflow state.");   

        await context.SendMessageAsync(originalQuestion, cancellationToken: cancellationToken);

        // Send a turn token to trigger the First Line's processing
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken);            

    }

}