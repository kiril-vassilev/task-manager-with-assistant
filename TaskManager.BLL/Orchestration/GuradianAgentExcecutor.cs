using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class GuardianAgentExecutor : Executor<ChatMessage, GuardianResponse>
{

    private readonly ChatClientAgent? _agent;
    private AgentSession? _session;

    public GuardianAgentExecutor(ChatClientAgent agent) : base("GuardianAgentExecutor")
    {
        _agent = agent;
        CreateClearHistory();   
    }

    public void CreateClearHistory()
    {
        _session = null;
    }

    public override async ValueTask<GuardianResponse> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (_agent == null)
            throw new InvalidOperationException("GuardianAgentExecutor is not initialized with an ChatClientAgent.");

        _session ??= await _agent.CreateSessionAsync(cancellationToken);

        // Invoke the agent
        var response = await _agent.RunAsync<GuardianResponse>(message.Text, _session, cancellationToken: cancellationToken);

        // Store the original question in the workflow state for later use by the worker agent (If it is not a threat)
        if (!response.Result.IsThreatDetected)
            await context.QueueStateUpdateAsync("OriginalQuestion", message, scopeName: TaskManagerConfiguration.defaultWorkflowMessageScope, cancellationToken);
            
        return response.Result;
    }

}
