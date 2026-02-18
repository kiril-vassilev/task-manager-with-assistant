using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class WorkerAgentExecutor : Executor<FirstLineResponse, AskResponse>
{

    public ChatClientAgent? agent { get; private set; }
    public AgentSession? session { get; set; }

    public WorkerAgentExecutor(ChatClientAgent agent): base("WorkerAgentExecutor")
    {
        this.agent = agent;
    }

    public override async ValueTask<AskResponse> HandleAsync(FirstLineResponse firstLineResponse, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (this.agent == null)
            throw new InvalidOperationException("WorkerAgentExecutor is not initialized with an ChatClientAgent.");

        if( firstLineResponse.Redirect != RedirectType.WorkerAgent)
            throw new InvalidOperationException("WorkerAgentExecutor invoked but redirect type is not WorkerAgent.");            
        
        if (this.session == null)
            throw new InvalidOperationException("WorkerAgentExecutor is not initialized with an AgentSession.");


        var originalQuestion = await context.ReadStateAsync<ChatMessage>("OriginalQuestion", scopeName: TaskManagerConfiguration.defaultWorkflowMessageScope, cancellationToken)
            ?? throw new InvalidOperationException("Original question not found in workflow state.");   

        var finalAnswer = String.Empty;
        var response = await this.agent.RunAsync<AskResponse>(originalQuestion.Text, this.session, cancellationToken: cancellationToken);

        if (TaskManagerConfiguration.showAgentThinking)
            foreach (var msg in response.Messages)
                finalAnswer += FormatMessage(msg);

        finalAnswer += response.Result.Answer;

        return new AskResponse
        {
            Answer = finalAnswer,
            Tasks = response.Result.Tasks
        };
    }

    private string FormatMessage(ChatMessage message)
    {
        var res = String.Empty;

        foreach (var content in message.Contents)
        {
            if (content is TextContent textContent)
            {
                res += $"[TextContent] ({TruncateString(textContent.Text)})\n";
            }
            else if (content is FunctionCallContent functionCall)
            {
                res += $"[FunctionCallContent] [{functionCall.Name}]\n";
            }
            else if (content is FunctionResultContent functionResult)
            {
                res += $"[FunctionResultContent] ({TruncateString(functionResult.Result?.ToString() ?? "null")})\n";
            }
        }

        return res;
    }

    private string TruncateString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "none";

        return input.Length > 30 ? input.Substring(0, 30) + "..." : input;
    }    
}