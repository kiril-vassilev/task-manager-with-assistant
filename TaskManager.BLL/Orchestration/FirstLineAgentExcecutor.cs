using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class FirstLineAgentExecutor : Executor<ChatMessage, FirstLineResponse>
{

    public ChatClientAgent? agent { get; private set; }
    public AgentSession? session { get; set; }

    public FirstLineAgentExecutor(ChatClientAgent agent): base("FirstLineAgentExecutor")
    {
        this.agent = agent;
    }

    public override async ValueTask<FirstLineResponse> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (this.agent == null)
            throw new InvalidOperationException("FirstLineAgentExecutor is not initialized with an ChatClientAgent.");

        if (this.session == null)
            throw new InvalidOperationException("FirstLineAgentExecutor is not initialized with an AgentSession.");

        var response = await this.agent.RunAsync<FirstLineResponse>(message.Text, this.session, cancellationToken: cancellationToken);

        return response.Result;
    }
}