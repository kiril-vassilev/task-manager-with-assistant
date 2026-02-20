using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class QnAAgentExecutor : Executor<FirstLineResponse, AskResponse>
{

    public ChatClientAgent? agent { get; private set; }
    public AgentSession? session { get; set; }

    public QnAAgentExecutor(ChatClientAgent agent): base("QnAAgentExecutor")
    {
        this.agent = agent;
    }

    public override async ValueTask<AskResponse> HandleAsync(FirstLineResponse firstLineResponse, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (this.agent == null)
            throw new InvalidOperationException("QnAAgentExecutor is not initialized with an ChatClientAgent.");

        if (this.session == null)
            throw new InvalidOperationException("QnAAgentExecutor is not initialized with an AgentSession.");

        if( firstLineResponse.Redirect != RedirectType.QnAAgent)
            throw new InvalidOperationException("QnAAgentExecutor invoked but redirect type is not QnAAgent.");

        var originalQuestion = await context.ReadStateAsync<ChatMessage>("OriginalQuestion", scopeName: TaskManagerConfiguration.defaultWorkflowMessageScope, cancellationToken)
            ?? throw new InvalidOperationException("Original question not found in workflow state.");   

        var response = await this.agent.RunAsync(originalQuestion.Text, this.session, cancellationToken: cancellationToken);


        return new AskResponse
        {
            Answer = "QnA: " + response.Text,
            Tasks = []
        };
    }
}